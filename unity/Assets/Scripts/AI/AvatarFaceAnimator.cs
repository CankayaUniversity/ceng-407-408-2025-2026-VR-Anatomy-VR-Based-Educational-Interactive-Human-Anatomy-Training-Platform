using UnityEngine;

public class AvatarFaceAnimator : MonoBehaviour
{
    [Header("Audio (Lip Sync)")]
    [Tooltip("TTS sesini çalan AudioSource. Boş bırakırsanız sahnede aranır.")]
    [SerializeField] private AudioSource audioSource;

    [Header("Lip Sync")]
    [SerializeField] private float lipSyncSensitivity = 3f;
    [SerializeField] private float lipSyncSmooth      = 12f;
    [SerializeField] private float lipSyncMaxWeight   = 80f;

    [Header("Göz Kırpma")]
    [SerializeField] private float blinkIntervalMin = 2.5f;
    [SerializeField] private float blinkIntervalMax = 6f;
    [SerializeField] private float blinkSpeed       = 0.15f;

    [Header("Nefes (Idle)")]
    [SerializeField] private float breathCycle     = 4f;
    [SerializeField] private float breathIntensity = 2.5f;

    [Header("Gülümseme")]
    [SerializeField] private float smileIntervalMin = 5f;
    [SerializeField] private float smileIntervalMax = 12f;
    [SerializeField] private float smileDuration    = 1.5f;
    [SerializeField] private float smileIntensity   = 15f;

    // Blendshape index'leri
    private int _iBlinkL    = -1;
    private int _iBlinkR    = -1;
    private int _iJawOpen   = -1;
    private int _iMouthOpen = -1;
    private int _iSmileL    = -1;
    private int _iSmileR    = -1;

    // İç durum
    private SkinnedMeshRenderer _skin;
    private Mesh _mesh;
    private bool _ready;
    private float _initWaitTimer;

    // Blink
    private float _nextBlink;
    private float _blinkT;
    private bool  _blinking;

    // Smile
    private float _nextSmile;
    private float _smileT;
    private bool  _smiling;

    // Lip sync
    private float   _mouthWeight;
    private float[] _samples = new float[256];

    private void Update()
    {
        if (!_ready)
        {
            // GLTFast async yükleme süresi için biraz bekle
            _initWaitTimer += Time.deltaTime;
            if (_initWaitTimer > 0.5f) // her 0.5s'de bir dene
            {
                TryFindSkin();
                _initWaitTimer = 0f;
            }
            return;
        }

        bool speaking = audioSource != null && audioSource.isPlaying;

        UpdateBlink();

        if (!speaking)
            UpdateBreath();

        UpdateSmile();
        UpdateLipSync(speaking);
    }

    // ─────────────────── Skin Keşfi ─────────────────────────────
    private void TryFindSkin()
    {
        // Tüm child'lardaki SkinnedMeshRenderer'ları bul
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        if (renderers.Length == 0)
        {
            Debug.Log("[AvatarFaceAnimator] Henüz SkinnedMeshRenderer bulunamadı, bekleniyor...");
            return;
        }

        // Blendshape'li olanı tercih et
        SkinnedMeshRenderer best = null;
        int maxShapes = 0;

        foreach (var smr in renderers)
        {
            if (smr.sharedMesh == null) continue;
            int count = smr.sharedMesh.blendShapeCount;
            Debug.Log($"[AvatarFaceAnimator] Mesh bulundu: '{smr.gameObject.name}' — {count} blendshape");

            if (count > maxShapes)
            {
                maxShapes = count;
                best = smr;
            }
        }

        if (best == null)
        {
            // Blendshape'siz bile olsa ilk renderer'ı al
            best = renderers[0];
            Debug.LogWarning("[AvatarFaceAnimator] Hiçbir mesh'te blendshape yok!");
        }

        InitSkin(best);
    }

    private void InitSkin(SkinnedMeshRenderer smr)
    {
        _skin = smr;
        _mesh = smr.sharedMesh;

        // Tüm blendshape isimlerini logla
        Debug.Log($"[AvatarFaceAnimator] ═══ Blendshape Listesi ({_mesh.blendShapeCount} adet) ═══");
        for (int i = 0; i < _mesh.blendShapeCount; i++)
        {
            Debug.Log($"  [{i}] {_mesh.GetBlendShapeName(i)}");
        }

        // Birden fazla isim formatını dene
        _iBlinkL    = FindShapeMulti("eyeBlinkLeft", "eyeBlink_L", "EyeBlinkLeft", "blink_L", "blinkLeft");
        _iBlinkR    = FindShapeMulti("eyeBlinkRight", "eyeBlink_R", "EyeBlinkRight", "blink_R", "blinkRight");
        _iJawOpen   = FindShapeMulti("jawOpen", "Jaw_Open", "JawOpen", "jaw_open", "viseme_aa");
        _iMouthOpen = FindShapeMulti("mouthOpen", "Mouth_Open", "MouthOpen", "mouth_open", "viseme_O");
        _iSmileL    = FindShapeMulti("mouthSmileLeft", "mouthSmile_L", "MouthSmileLeft", "smile_L", "smileLeft");
        _iSmileR    = FindShapeMulti("mouthSmileRight", "mouthSmile_R", "MouthSmileRight", "smile_R", "smileRight");

        // Sonuç raporu
        Debug.Log($"[AvatarFaceAnimator] Blink L/R: {_iBlinkL}/{_iBlinkR}");
        Debug.Log($"[AvatarFaceAnimator] JawOpen: {_iJawOpen}, MouthOpen: {_iMouthOpen}");
        Debug.Log($"[AvatarFaceAnimator] Smile L/R: {_iSmileL}/{_iSmileR}");

        // Eğer spesifik isimler bulunamadıysa ve blendshape'ler varsa,
        // partial match ile bulmaya çalış
        if (_iJawOpen < 0 && _iMouthOpen < 0 && _mesh.blendShapeCount > 0)
        {
            Debug.Log("[AvatarFaceAnimator] Standart isimler bulunamadı, partial match deneniyor...");
            _iJawOpen   = FindShapePartial("jaw", "open");
            _iMouthOpen = FindShapePartial("mouth", "open");
            _iBlinkL    = _iBlinkL  >= 0 ? _iBlinkL  : FindShapePartial("blink", "l");
            _iBlinkR    = _iBlinkR  >= 0 ? _iBlinkR  : FindShapePartial("blink", "r");
            _iSmileL    = _iSmileL  >= 0 ? _iSmileL  : FindShapePartial("smile", "l");
            _iSmileR    = _iSmileR  >= 0 ? _iSmileR  : FindShapePartial("smile", "r");

            Debug.Log($"[AvatarFaceAnimator] Partial match sonuçları → Jaw:{_iJawOpen} Mouth:{_iMouthOpen} BlinkL:{_iBlinkL} BlinkR:{_iBlinkR} SmileL:{_iSmileL} SmileR:{_iSmileR}");
        }

        // AudioSource atanmadıysa sahnede bul
        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>();
            if (audioSource == null)
                audioSource = FindObjectOfType<AudioSource>();
            if (audioSource != null)
                Debug.Log($"[AvatarFaceAnimator] AudioSource otomatik bulundu: '{audioSource.gameObject.name}'");
            else
                Debug.LogWarning("[AvatarFaceAnimator] AudioSource bulunamadı! Inspector'dan atayın.");
        }

        _nextBlink = Time.time + Random.Range(blinkIntervalMin, blinkIntervalMax);
        _nextSmile = Time.time + Random.Range(smileIntervalMin, smileIntervalMax);
        _ready = true;

        Debug.Log("[AvatarFaceAnimator] ✓ Hazır!");
    }

    /// <summary>Verilen isimlerden ilk bulunanı döndürür.</summary>
    private int FindShapeMulti(params string[] names)
    {
        foreach (string n in names)
        {
            int idx = _mesh.GetBlendShapeIndex(n);
            if (idx >= 0) return idx;
        }
        return -1;
    }

    /// <summary>İsimde her iki keyword de geçen ilk blendshape'i döndürür (case-insensitive).</summary>
    private int FindShapePartial(string keyword1, string keyword2)
    {
        string k1 = keyword1.ToLowerInvariant();
        string k2 = keyword2.ToLowerInvariant();

        for (int i = 0; i < _mesh.blendShapeCount; i++)
        {
            string name = _mesh.GetBlendShapeName(i).ToLowerInvariant();
            if (name.Contains(k1) && name.Contains(k2))
                return i;
        }
        return -1;
    }

    // ─────────────────── Göz Kırpma ─────────────────────────────
    private void UpdateBlink()
    {
        if (_iBlinkL < 0 && _iBlinkR < 0) return;

        if (!_blinking)
        {
            if (Time.time < _nextBlink) return;
            _blinking = true;
            _blinkT   = 0f;
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
            _blinking  = false;
            _nextBlink = Time.time + Random.Range(blinkIntervalMin, blinkIntervalMax);
        }

        SetWeight(_iBlinkL, w);
        SetWeight(_iBlinkR, w);
    }

    // ─────────────────── Nefes ───────────────────────────────────
    private void UpdateBreath()
    {
        if (_iJawOpen < 0) return;
        float t = (Mathf.Sin(Time.time * (2f * Mathf.PI / breathCycle)) + 1f) * 0.5f;
        SetWeight(_iJawOpen, t * breathIntensity);
    }

    // ─────────────────── Gülümseme ──────────────────────────────
    private void UpdateSmile()
    {
        if (_iSmileL < 0 && _iSmileR < 0) return;

        if (!_smiling)
        {
            if (Time.time < _nextSmile) return;
            _smiling = true;
            _smileT  = 0f;
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
            _smiling   = false;
            _nextSmile = Time.time + Random.Range(smileIntervalMin, smileIntervalMax);
        }

        SetWeight(_iSmileL, w);
        SetWeight(_iSmileR, w);
    }

    // ─────────────────── Lip Sync ───────────────────────────────
    private void UpdateLipSync(bool speaking)
    {
        float target = 0f;

        if (speaking && audioSource != null)
        {
            audioSource.GetOutputData(_samples, 0);

            float sum = 0f;
            for (int i = 0; i < _samples.Length; i++)
                sum += _samples[i] * _samples[i];

            float rms = Mathf.Sqrt(sum / _samples.Length);
            target = Mathf.Clamp(rms * lipSyncSensitivity * 100f, 0f, lipSyncMaxWeight);
        }

        _mouthWeight = Mathf.Lerp(_mouthWeight, target, Time.deltaTime * lipSyncSmooth);

        if (_iJawOpen >= 0)
            SetWeight(_iJawOpen, _mouthWeight);
        if (_iMouthOpen >= 0)
            SetWeight(_iMouthOpen, _mouthWeight * 0.6f);
    }

    // ─────────────────── Yardımcı ───────────────────────────────
    private void SetWeight(int idx, float w)
    {
        if (idx >= 0)
            _skin.SetBlendShapeWeight(idx, w);
    }
}
