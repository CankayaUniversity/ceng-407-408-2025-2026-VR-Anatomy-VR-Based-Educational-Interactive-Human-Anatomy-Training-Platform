using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

public class ChatAvatarController : MonoBehaviour
{
    [Header("Avatar Source")]
    [Tooltip("GLB file name inside StreamingAssets")]
    [SerializeField] private string glbFileName = "son.glb";

    [Header("Look At Camera")]
    [SerializeField] private bool lookAtCamera = true;
    [SerializeField] private float rotationSpeed = 2f;
    [Tooltip("Yüzü kameraya doğru eğmek için pitch açısı (negatif = aşağı)")]
    [SerializeField] private float lookAtPitchOffset = -5f;

    [Header("Göz Kırpma (Eğer blendshape varsa)")]
    [SerializeField] private float blinkIntervalMin = 2.5f;
    [SerializeField] private float blinkIntervalMax = 6f;
    [SerializeField] private float blinkSpeed = 0.15f;

    [Header("Nefes (Idle — mouthOpen ile)")]
    [SerializeField] private float breathCycle = 4f;
    [SerializeField] private float breathIntensity = 0.3f;

    [Header("Gülümseme (mouthSmile ile)")]
    [SerializeField] private float smileIntervalMin = 6f;
    [SerializeField] private float smileIntervalMax = 14f;
    [SerializeField] private float smileDuration = 2f;
    [SerializeField] private float smileIntensity = 0.60f;

    [Header("Lip Sync (Ses şiddetine bağlı)")]
    [SerializeField] private float lipSyncSensitivity = 0.22f;
    [SerializeField] private float lipSyncSmooth = 7f;
    [SerializeField] private float lipSyncMaxWeight = 2.5f;

    private GltfAsset _gltfAsset;
    private Transform _cameraTransform;
    private bool _loaded;

    // ── Her animasyon hedefi: mesh + index ──
    private SkinnedMeshRenderer _headSkin;   // Wolf3D_Head — ağız & gülümseme
    private SkinnedMeshRenderer _teethSkin;  // Wolf3D_Teeth — dişler de hareket etsin
    private int _headMouthOpen = -1;
    private int _headSmile = -1;
    private int _teethMouthOpen = -1;

    // Göz kırpma (varsa)
    private SkinnedMeshRenderer _eyeLSkin, _eyeRSkin;
    private int _eyeLBlink = -1, _eyeRBlink = -1;

    private bool _faceReady;
    private AudioSource _ttsAudio;

    // State
    private float _nextBlink, _blinkT;
    private bool _blinking;
    private float _nextSmile, _smileT;
    private bool _smiling;
    private float _mouthWeight;
    private float[] _samples = new float[256];

    private async void Start()
    {
        _cameraTransform = Camera.main != null ? Camera.main.transform : null;
        await LoadAvatar();
    }

    private async Task LoadAvatar()
    {
        _gltfAsset = gameObject.AddComponent<GltfAsset>();
        _gltfAsset.LoadOnStartup = false;
        _gltfAsset.StreamingAsset = true;
        _gltfAsset.Url = glbFileName;

        bool success = await _gltfAsset.Load(_gltfAsset.FullUrl);

        if (success)
        {
            _loaded = true;
            Debug.Log("[ChatAvatarController] Avatar yüklendi.");
            InitFace();
        }
        else
        {
            Debug.LogError($"[ChatAvatarController] Avatar yüklenemedi: {glbFileName}");
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  YÜZ ANİMASYONU — İsme göre doğru mesh'e bağla
    // ═══════════════════════════════════════════════════════════

    private void InitFace()
    {
        SkinnedMeshRenderer[] all = GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var smr in all)
        {
            if (smr.sharedMesh == null) continue;
            Mesh m = smr.sharedMesh;
            string meshName = smr.gameObject.name;

            Debug.Log($"[Face] Mesh: '{meshName}' — {m.blendShapeCount} blendshape");
            for (int i = 0; i < m.blendShapeCount; i++)
                Debug.Log($"  [{meshName}] [{i}] {m.GetBlendShapeName(i)}");

            // ── Wolf3D_Head → ağız ve gülümseme ana mesh'i ──
            if (meshName.Contains("Head"))
            {
                _headSkin = smr;
                _headMouthOpen = m.GetBlendShapeIndex("mouthOpen");
                _headSmile = m.GetBlendShapeIndex("mouthSmile");

                // mouthSmile bulunamadıysa alternatif isimler dene
                if (_headSmile < 0) _headSmile = m.GetBlendShapeIndex("mouthSmileLeft");
                if (_headSmile < 0) _headSmile = FindPartial(m, "smile");

                // mouthOpen bulunamadıysa alternatif
                if (_headMouthOpen < 0) _headMouthOpen = m.GetBlendShapeIndex("jawOpen");
                if (_headMouthOpen < 0) _headMouthOpen = FindPartial(m, "mouth");

                Debug.Log($"[Face] ★ HEAD → mouthOpen:{_headMouthOpen} smile:{_headSmile}");
            }
            // ── Wolf3D_Teeth → dişler de hareket etsin ──
            else if (
                meshName.Contains("Teeth") ||
                meshName.Contains("teeth") ||
                meshName.Contains("Tooth") ||
                meshName.Contains("tooth") ||
                meshName.Contains("Dent")
            )
            {
                _teethSkin = smr;
                _teethMouthOpen = m.GetBlendShapeIndex("mouthOpen");
                if (_teethMouthOpen < 0) _teethMouthOpen = m.GetBlendShapeIndex("jawOpen");
                if (_teethMouthOpen < 0) _teethMouthOpen = FindPartial(m, "mouth");
                if (_teethMouthOpen < 0) _teethMouthOpen = FindPartial(m, "jaw");

                Debug.Log($"[Face] ★ TEETH → mouthOpen:{_teethMouthOpen}");
            }
            // ── EyeLeft / EyeRight → göz kırpma ──
            else if (meshName.Contains("EyeLeft") || meshName.Contains("Eye_L"))
            {
                _eyeLSkin = smr;
                _eyeLBlink = FindBlinkShape(m);
                Debug.Log($"[Face] ★ EYE_L → blink:{_eyeLBlink}");
            }
            else if (meshName.Contains("EyeRight") || meshName.Contains("Eye_R"))
            {
                _eyeRSkin = smr;
                _eyeRBlink = FindBlinkShape(m);
                Debug.Log($"[Face] ★ EYE_R → blink:{_eyeRBlink}");
            }
        }

        // AudioSource bul
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        foreach (var a in audioSources)
        {
            if (a.gameObject != gameObject) { _ttsAudio = a; break; }
        }
        if (_ttsAudio == null && audioSources.Length > 0)
            _ttsAudio = audioSources[0];

        Debug.Log(_ttsAudio != null
            ? $"[Face] AudioSource: '{_ttsAudio.gameObject.name}'"
            : "[Face] AudioSource bulunamadı");

        _nextBlink = Time.time + Random.Range(blinkIntervalMin, blinkIntervalMax);
        _nextSmile = Time.time + Random.Range(smileIntervalMin, smileIntervalMax);

        _faceReady = _headSkin != null && (_headMouthOpen >= 0 || _headSmile >= 0);

        if (_faceReady)
            Debug.Log("[Face] ✓ Yüz animasyonu hazır!");
        else
            Debug.LogWarning("[Face] ✗ Wolf3D_Head bulunamadı veya blendshape yok!");
    }

    private int FindBlinkShape(Mesh m)
    {
        // Blink-özel isimler
        string[] names = { "eyeBlinkLeft", "eyeBlinkRight", "eyeBlink_L", "eyeBlink_R", "blink" };
        foreach (var n in names)
        {
            int idx = m.GetBlendShapeIndex(n);
            if (idx >= 0) return idx;
        }
        return FindPartial(m, "blink");
    }

    private int FindPartial(Mesh m, string keyword)
    {
        string kw = keyword.ToLowerInvariant();
        for (int i = 0; i < m.blendShapeCount; i++)
        {
            if (m.GetBlendShapeName(i).ToLowerInvariant().Contains(kw))
                return i;
        }
        return -1;
    }

    // ═══════════════════════════════════════════════════════════
    //  UPDATE
    // ═══════════════════════════════════════════════════════════

    private void LateUpdate()
    {
        if (!_loaded) return;

        // Look at camera
        if (lookAtCamera && _cameraTransform != null)
        {
            Vector3 dir = _cameraTransform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion yaw = Quaternion.LookRotation(dir);
                Quaternion target = yaw * Quaternion.Euler(lookAtPitchOffset, 0f, 0f);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, target, Time.deltaTime * rotationSpeed);
            }
        }

        if (!_faceReady) return;

        bool speaking = _ttsAudio != null && _ttsAudio.isPlaying;

        DoBlink();
        if (!speaking) DoBreath();
        DoSmile();
        DoLipSync(speaking);
    }

    // ═══════════ Göz Kırpma ═══════════

    private void DoBlink()
    {
        if (_eyeLBlink < 0 && _eyeRBlink < 0) return;

        if (!_blinking)
        {
            if (Time.time < _nextBlink) return;
            _blinking = true;
            _blinkT = 0f;
        }

        _blinkT += Time.deltaTime;
        float half = blinkSpeed * 0.5f;
        float w;

        if (_blinkT < half)
            w = Mathf.Lerp(0f, 100f, _blinkT / half);
        else if (_blinkT < blinkSpeed)
            w = Mathf.Lerp(100f, 0f, (_blinkT - half) / half);
        else
        {
            w = 0f;
            _blinking = false;
            _nextBlink = Time.time + Random.Range(blinkIntervalMin, blinkIntervalMax);
        }

        if (_eyeLSkin != null && _eyeLBlink >= 0)
            _eyeLSkin.SetBlendShapeWeight(_eyeLBlink, w);
        if (_eyeRSkin != null && _eyeRBlink >= 0)
            _eyeRSkin.SetBlendShapeWeight(_eyeRBlink, w);
    }

    // ═══════════ Nefes (Idle — mouthOpen) ═══════════

    private void DoBreath()
    {
        if (_headMouthOpen < 0 || _headSkin == null) return;

        float t = (Mathf.Sin(Time.time * (2f * Mathf.PI / breathCycle)) + 1f) * 0.5f;
        float w = t * breathIntensity;

        _headSkin.SetBlendShapeWeight(_headMouthOpen, w);
        if (_teethSkin != null && _teethMouthOpen >= 0)
            _teethSkin.SetBlendShapeWeight(_teethMouthOpen, _mouthWeight * 1.95f);
    }

    // ═══════════ Gülümseme (mouthSmile) ═══════════

    private void DoSmile()
    {
        if (_headSmile < 0 || _headSkin == null) return;

        if (!_smiling)
        {
            if (Time.time < _nextSmile) return;
            _smiling = true;
            _smileT = 0f;
        }

        _smileT += Time.deltaTime;
        float half = smileDuration * 0.5f;
        float w;

        if (_smileT < half)
            w = Mathf.SmoothStep(0f, smileIntensity, _smileT / half);
        else if (_smileT < smileDuration)
            w = Mathf.SmoothStep(smileIntensity, 0f, (_smileT - half) / half);
        else
        {
            w = 0f;
            _smiling = false;
            _nextSmile = Time.time + Random.Range(smileIntervalMin, smileIntervalMax);
        }

        _headSkin.SetBlendShapeWeight(_headSmile, w);
    }

    // ═══════════ Lip Sync ═══════════

    private void DoLipSync(bool speaking)
    {
        if (_headMouthOpen < 0 || _headSkin == null) return;

        float target = 0f;

        if (speaking && _ttsAudio != null)
        {
            _ttsAudio.GetOutputData(_samples, 0);
            float sum = 0f;
            for (int i = 0; i < _samples.Length; i++)
                sum += _samples[i] * _samples[i];

            float rms = Mathf.Sqrt(sum / _samples.Length);
            target = Mathf.Clamp(rms * lipSyncSensitivity * 100f, 0f, lipSyncMaxWeight);
        }

        _mouthWeight = Mathf.Lerp(_mouthWeight, target, Time.deltaTime * lipSyncSmooth);

        _headSkin.SetBlendShapeWeight(_headMouthOpen, _mouthWeight);

        if (_teethSkin != null && _teethMouthOpen >= 0)
            _teethSkin.SetBlendShapeWeight(_teethMouthOpen, _mouthWeight * 1.35f);
    }
}
