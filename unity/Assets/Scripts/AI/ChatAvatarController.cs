using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

public class ChatAvatarController : MonoBehaviour
{
    [Header("Avatar Source (GLB)")]
    [SerializeField] private string glbFileName = "model1.glb";
    [SerializeField] private string maleGlbFileName = "model 2.glb";

    [Header("Male Avatar Alignment")]
    [SerializeField] private bool alignMaleFeetToAvatarRoot = true;
    [SerializeField] private bool disableMaleImportedIdleAnimation = false;
    [SerializeField] private Vector3 maleModelPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 maleModelEulerOffset = new Vector3(7f, 0f, 0f);
    [SerializeField] private Vector3 maleModelScaleMultiplier = Vector3.one;
    [SerializeField] private bool keepMaleAvatarGroundedDuringAnimation = true;
    [SerializeField] private float maleGroundingYOffset = -0.15f;
    [SerializeField] private bool stabilizeMaleRightFoot = true;
    [SerializeField, Range(0f, 1f)] private float maleRightFootStabilizeWeight = 1f;
    [SerializeField] private Vector3 maleRightFootEulerOffset = new Vector3(0f, 0f, -6f);

    [Header("Look At Camera")]
    [SerializeField] private bool lookAtCamera = true;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float lookAtPitchOffset = -5f;

    [Header("Göz Kırpma")]
    [SerializeField] private float blinkIntervalMin = 2.5f;
    [SerializeField] private float blinkIntervalMax = 6f;
    [SerializeField] private float blinkSpeed = 0.15f;

    [Header("Nefes (Idle)")]
    [SerializeField] private float breathCycle = 4f;
    [SerializeField] private float breathIntensity = 0.3f;

    [Header("Gülümseme")]
    [SerializeField] private float smileIntervalMin = 6f;
    [SerializeField] private float smileIntervalMax = 14f;
    [SerializeField] private float smileDuration = 2f;
    [SerializeField] private float smileIntensity = 0.60f;

    [Header("Lip Sync")]
    [SerializeField] private float lipSyncSensitivity = 0.015f;
    [SerializeField] private float lipSyncSmooth = 18f;
    [SerializeField] private float lipSyncCloseSpeed = 30f;
    [SerializeField] private float jawMaxWeight = 2.2f;
    [SerializeField] private float mouthMaxWeight = 1.5f;
    [SerializeField] private float visemeMaxWeight = 4f;

    private GltfAsset _gltfAsset;
    private Transform _cameraTransform;
    private bool _loaded;
    private bool _isMaleAvatar;
    private Transform _maleModelRoot;
    private Transform _maleRightFoot;
    private Transform _maleRightToe;

    // ── Blendshape face targets ──
    private SkinnedMeshRenderer _headSkin;
    private SkinnedMeshRenderer _teethSkin;
    private SkinnedMeshRenderer _primarySkin;
    private int _headMouthOpen = -1, _headSmile = -1;
    private int _teethMouthOpen = -1;
    private int _primaryJawOpen = -1, _primaryMouthOpen = -1;
    private int _primarySmileL = -1, _primarySmileR = -1;
    private int _primaryBlinkL = -1, _primaryBlinkR = -1;
    private SkinnedMeshRenderer _eyeLSkin, _eyeRSkin;
    private int _eyeLBlink = -1, _eyeRBlink = -1;

    private bool _faceReady;
    private bool _useSingleMesh;
    private AudioSource _ttsAudio;

    // ── Viseme blendshape indices ──
    private int _visemeSil = -1, _visemeAA = -1, _visemeE = -1;
    private int _visemeI = -1, _visemeO = -1, _visemeU = -1;
    private int _visemePP = -1, _visemeFF = -1, _visemeTH = -1;
    private int _visemeDD = -1, _visemeCH = -1, _visemeSS = -1;
    private int _visemeNN = -1, _visemeRR = -1, _visemeKK = -1;
    private bool _hasVisemes;
    private SkinnedMeshRenderer _visemeSkin;

    // ── Blendshape anim state ──
    private float _nextBlink, _blinkT;
    private bool _blinking;
    private float _nextSmile, _smileT;
    private bool _smiling;
    private float _jawWeight, _mouthWeight;
    private float _currentVisemeTime;
    private int _currentVisemeIdx;
    private float _prevVisemeWeight, _nextVisemeWeight;
    private float[] _samples = new float[256];

    private async void Start()
    {
        _cameraTransform = Camera.main != null ? Camera.main.transform : null;
        glbFileName = ResolveAvatarFileName();
        _isMaleAvatar = IsMaleAvatarFile(glbFileName);
        await LoadAvatar();
    }

    private string ResolveAvatarFileName()
    {
        // SettingsManager yoksa da son seçimi PlayerPrefs'ten okuyarak avatarı koru.
        if (SettingsManager.Instance != null &&
            SettingsManager.Instance.SelectedAvatarType == SettingsManager.AvatarType.Male)
        {
            return maleGlbFileName;
        }

        int rawValue = PlayerPrefs.GetInt("AvatarType", (int)SettingsManager.AvatarType.Female);
        return rawValue == (int)SettingsManager.AvatarType.Male ? maleGlbFileName : glbFileName;
    }

    private async Task LoadAvatar()
    {
        _gltfAsset = gameObject.AddComponent<GltfAsset>();
        _gltfAsset.LoadOnStartup = false;

        bool success = await TryLoadAvatarFromKnownPaths(glbFileName);

        if (success)
        {
            Debug.Log("[ChatAvatar] GLB yüklendi, setup başlıyor...");
            StartCoroutine(SetupAfterLoad());
        }
        else
        {
            Debug.LogError($"[ChatAvatar] Avatar yüklenemedi: {glbFileName}");
        }
    }

    private async Task<bool> TryLoadAvatarFromKnownPaths(string fileName)
    {
        var candidates = new List<(bool useStreamingAsset, string url)>
        {
            (true, fileName),
            (true, $"Avatars/{fileName}")
        };

        string streamingPath = Path.Combine(Application.dataPath, "StreamingAssets", fileName);
        string avatarsPath = Path.Combine(Application.dataPath, "Avatars", fileName);

        if (File.Exists(streamingPath))
            candidates.Add((false, $"file:///{streamingPath.Replace("\\", "/")}"));

        if (File.Exists(avatarsPath))
            candidates.Add((false, $"file:///{avatarsPath.Replace("\\", "/")}"));

        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            _gltfAsset.StreamingAsset = candidate.useStreamingAsset;
            _gltfAsset.Url = candidate.url;

            string fullUrl = _gltfAsset.FullUrl;
            bool loaded = await _gltfAsset.Load(fullUrl);
            if (loaded)
            {
                Debug.Log($"[ChatAvatar] Avatar yüklendi: {fullUrl}");
                return true;
            }
        }

        return false;
    }

    private IEnumerator SetupAfterLoad()
    {
        yield return null;
        yield return null;

        ApplyAvatarModelAlignment();
        SetupAnimation();
        SetupAvatarLighting();
        InitFace();

        _loaded = true;
        Debug.Log("[ChatAvatar] Avatar tamamen hazır.");
    }

    // ═══════════════════════════════════════════════════════════
    //  ANİMASYON — GLB içindeki animasyonları bul ve oynat
    // ═══════════════════════════════════════════════════════════

    private void SetupAnimation()
    {
        if (_isMaleAvatar && disableMaleImportedIdleAnimation)
        {
            Debug.Log("[ChatAvatar] Erkek avatar sabit idle pozunda tutuluyor.");
            return;
        }

        Animation legacyAnim = GetComponentInChildren<Animation>();
        if (legacyAnim != null)
        {
            if (legacyAnim.clip != null)
            {
                legacyAnim.clip.wrapMode = WrapMode.Loop;
                legacyAnim.wrapMode = WrapMode.Loop;
                legacyAnim.Play();
                Debug.Log($"[ChatAvatar] Animation oynatılıyor: '{legacyAnim.clip.name}' ({legacyAnim.clip.length:F2}s)");
                return;
            }

            foreach (AnimationState state in legacyAnim)
            {
                state.wrapMode = WrapMode.Loop;
                legacyAnim.clip = state.clip;
                legacyAnim.wrapMode = WrapMode.Loop;
                legacyAnim.Play();
                Debug.Log($"[ChatAvatar] Animation state oynatılıyor: '{state.clip.name}' ({state.clip.length:F2}s)");
                return;
            }
        }

        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            Debug.Log("[ChatAvatar] Animator + Controller bulundu.");
            return;
        }

        Debug.LogWarning("[ChatAvatar] GLB'de animasyon bulunamadı.");
    }

    private bool IsMaleAvatarFile(string fileName)
    {
        return string.Equals(Path.GetFileName(fileName), maleGlbFileName, System.StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyAvatarModelAlignment()
    {
        if (!_isMaleAvatar) return;

        _maleModelRoot = FindLoadedModelRoot();
        if (_maleModelRoot == null)
        {
            Debug.LogWarning("[ChatAvatar] Erkek avatar root transform bulunamadı, hizalama atlandı.");
            return;
        }

        _maleModelRoot.localPosition += maleModelPositionOffset;
        _maleModelRoot.localRotation = Quaternion.Euler(maleModelEulerOffset) * _maleModelRoot.localRotation;
        Vector3 scaleMultiplier = maleModelScaleMultiplier == Vector3.zero ? Vector3.one : maleModelScaleMultiplier;
        _maleModelRoot.localScale = Vector3.Scale(_maleModelRoot.localScale, scaleMultiplier);

        if (alignMaleFeetToAvatarRoot)
            AlignModelBottomToRoot(_maleModelRoot);

        ConfigureMaleRigForStableStanding();
        CacheMaleRightFootBones(_maleModelRoot);
        Debug.Log("[ChatAvatar] Erkek avatar zemine göre hizalandı.");
    }

    private Transform FindLoadedModelRoot()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponentInChildren<Renderer>(true) != null ||
                child.GetComponentInChildren<Animator>(true) != null ||
                child.GetComponentInChildren<Animation>(true) != null)
            {
                return child;
            }
        }

        return null;
    }

    private void AlignModelBottomToRoot(Transform modelRoot)
    {
        if (!TryGetModelBounds(modelRoot, out Bounds bounds)) return;

        float verticalOffset = transform.position.y + maleGroundingYOffset - bounds.min.y;
        modelRoot.position += Vector3.up * verticalOffset;
    }

    private bool TryGetModelBounds(Transform modelRoot, out Bounds bounds)
    {
        var renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        bounds = default;

        bool hasBounds = false;
        foreach (var renderer in renderers)
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return hasBounds;
    }

    private void ConfigureMaleRigForStableStanding()
    {
        var animator = GetComponentInChildren<Animator>(true);
        if (animator != null)
            animator.applyRootMotion = false;

        if (!disableMaleImportedIdleAnimation) return;

        var legacyAnim = GetComponentInChildren<Animation>(true);
        if (legacyAnim == null) return;

        legacyAnim.Stop();
        legacyAnim.enabled = false;
        Debug.Log("[ChatAvatar] Erkek avatar import idle animasyonu devre dışı bırakıldı.");
    }

    private void CacheMaleRightFootBones(Transform modelRoot)
    {
        _maleRightFoot = FindChildByName(modelRoot, "RightFoot");
        _maleRightToe = FindChildByName(modelRoot, "RightToeBase");

        if (_maleRightFoot == null)
            Debug.LogWarning("[ChatAvatar] Erkek avatar RightFoot kemiği bulunamadı.");
    }

    private Transform FindChildByName(Transform root, string targetName)
    {
        if (root.name == targetName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindChildByName(root.GetChild(i), targetName);
            if (match != null) return match;
        }

        return null;
    }

    // ═══════════════════════════════════════════════════════════
    //  AYDINLATMA — 3-nokta stüdyo ışığı
    // ═══════════════════════════════════════════════════════════

    private void SetupAvatarLighting()
    {
        GameObject keyLightObj = new GameObject("AvatarKeyLight");
        keyLightObj.transform.SetParent(transform);
        keyLightObj.transform.localPosition = new Vector3(0.5f, 1.5f, -1.5f);
        keyLightObj.transform.LookAt(transform.position + Vector3.up * 0.8f);
        Light keyLight = keyLightObj.AddComponent<Light>();
        keyLight.type = LightType.Spot;
        keyLight.color = new Color(1f, 0.97f, 0.92f);
        keyLight.intensity = 3f;
        keyLight.range = 8f;
        keyLight.spotAngle = 60f;
        keyLight.innerSpotAngle = 40f;
        keyLight.shadows = LightShadows.Soft;

        GameObject fillLightObj = new GameObject("AvatarFillLight");
        fillLightObj.transform.SetParent(transform);
        fillLightObj.transform.localPosition = new Vector3(-1f, 1f, -1f);
        fillLightObj.transform.LookAt(transform.position + Vector3.up * 0.8f);
        Light fillLight = fillLightObj.AddComponent<Light>();
        fillLight.type = LightType.Spot;
        fillLight.color = new Color(0.85f, 0.9f, 1f);
        fillLight.intensity = 1.5f;
        fillLight.range = 8f;
        fillLight.spotAngle = 70f;
        fillLight.shadows = LightShadows.None;

        GameObject rimLightObj = new GameObject("AvatarRimLight");
        rimLightObj.transform.SetParent(transform);
        rimLightObj.transform.localPosition = new Vector3(0f, 1.2f, 1f);
        rimLightObj.transform.LookAt(transform.position + Vector3.up * 0.8f);
        Light rimLight = rimLightObj.AddComponent<Light>();
        rimLight.type = LightType.Spot;
        rimLight.color = new Color(0.7f, 0.85f, 1f);
        rimLight.intensity = 2f;
        rimLight.range = 6f;
        rimLight.spotAngle = 50f;
        rimLight.shadows = LightShadows.None;

        Debug.Log("[ChatAvatar] 3-nokta aydınlatma eklendi.");
    }

    // ═══════════════════════════════════════════════════════════
    //  YÜZ ANİMASYONU
    // ═══════════════════════════════════════════════════════════

    private void InitFace()
    {
        SkinnedMeshRenderer[] all = GetComponentsInChildren<SkinnedMeshRenderer>();
        bool foundWolf3D = false;

        SkinnedMeshRenderer bestSingleMesh = null;
        int maxBlendShapes = 0;

        foreach (var smr in all)
        {
            if (smr.sharedMesh == null) continue;
            Mesh m = smr.sharedMesh;
            string meshName = smr.gameObject.name;

            Debug.Log($"[Face] Mesh: '{meshName}' — {m.blendShapeCount} blendshape");

            if (m.blendShapeCount > maxBlendShapes)
            {
                maxBlendShapes = m.blendShapeCount;
                bestSingleMesh = smr;
            }

            if (meshName.Contains("Head"))
            {
                _headSkin = smr;
                _headMouthOpen = FindShapeMulti(m, "mouthOpen", "jawOpen");
                if (_headMouthOpen < 0) _headMouthOpen = FindPartial(m, "mouth");
                _headSmile = FindShapeMulti(m, "mouthSmile", "mouthSmileLeft");
                if (_headSmile < 0) _headSmile = FindPartial(m, "smile");
                foundWolf3D = true;
            }
            else if (meshName.Contains("Teeth") || meshName.Contains("teeth"))
            {
                _teethSkin = smr;
                _teethMouthOpen = FindShapeMulti(m, "mouthOpen", "jawOpen");
                if (_teethMouthOpen < 0) _teethMouthOpen = FindPartial(m, "mouth");
                if (_teethMouthOpen < 0) _teethMouthOpen = FindPartial(m, "jaw");
                foundWolf3D = true;
            }
            else if (meshName.Contains("EyeLeft") || meshName.Contains("Eye_L"))
            {
                _eyeLSkin = smr;
                _eyeLBlink = FindBlinkShape(m);
                foundWolf3D = true;
            }
            else if (meshName.Contains("EyeRight") || meshName.Contains("Eye_R"))
            {
                _eyeRSkin = smr;
                _eyeRBlink = FindBlinkShape(m);
                foundWolf3D = true;
            }
        }

        if (!foundWolf3D && bestSingleMesh != null && maxBlendShapes > 0)
        {
            _useSingleMesh = true;
            _primarySkin = bestSingleMesh;
            Mesh pm = bestSingleMesh.sharedMesh;

            _primaryBlinkL = FindShapeMulti(pm, "eyeBlinkLeft", "eyeBlink_L", "EyeBlinkLeft", "blink_L", "blinkLeft");
            _primaryBlinkR = FindShapeMulti(pm, "eyeBlinkRight", "eyeBlink_R", "EyeBlinkRight", "blink_R", "blinkRight");
            _primaryJawOpen = FindShapeMulti(pm, "jawOpen", "Jaw_Open", "JawOpen", "jaw_open");
            _primaryMouthOpen = FindShapeMulti(pm, "mouthOpen", "Mouth_Open", "MouthOpen", "mouth_open", "viseme_O", "viseme_aa");
            _primarySmileL = FindShapeMulti(pm, "mouthSmileLeft", "mouthSmile_L", "MouthSmileLeft", "smile_L", "smileLeft", "mouthSmile");
            _primarySmileR = FindShapeMulti(pm, "mouthSmileRight", "mouthSmile_R", "MouthSmileRight", "smile_R", "smileRight");

            if (_primaryJawOpen < 0 && _primaryMouthOpen < 0)
            {
                _primaryJawOpen = FindPartial2(pm, "jaw", "open");
                _primaryMouthOpen = FindPartial2(pm, "mouth", "open");
            }
            if (_primaryBlinkL < 0) _primaryBlinkL = FindPartial2(pm, "blink", "l");
            if (_primaryBlinkR < 0) _primaryBlinkR = FindPartial2(pm, "blink", "r");
            if (_primarySmileL < 0) _primarySmileL = FindPartial2(pm, "smile", "l");
            if (_primarySmileR < 0) _primarySmileR = FindPartial2(pm, "smile", "r");

            Debug.Log($"[Face] Single mesh → Jaw:{_primaryJawOpen} Mouth:{_primaryMouthOpen} BlinkL:{_primaryBlinkL} BlinkR:{_primaryBlinkR}");
        }

        DetectVisemes();

        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        foreach (var a in audioSources)
        {
            if (a.gameObject != gameObject) { _ttsAudio = a; break; }
        }
        if (_ttsAudio == null && audioSources.Length > 0)
            _ttsAudio = audioSources[0];

        _nextBlink = Time.time + Random.Range(blinkIntervalMin, blinkIntervalMax);
        _nextSmile = Time.time + Random.Range(smileIntervalMin, smileIntervalMax);

        _faceReady = _useSingleMesh
            ? (_primarySkin != null && (_primaryJawOpen >= 0 || _primaryMouthOpen >= 0 || _primarySmileL >= 0))
            : (_headSkin != null && (_headMouthOpen >= 0 || _headSmile >= 0));

        Debug.Log(_faceReady ? "[Face] Yüz animasyonu hazır!" : "[Face] Blendshape bulunamadı — yüz devre dışı.");
    }

    // ═══════════════════════════════════════════════════════════
    //  BLENDSHAPE YARDIMCI
    // ═══════════════════════════════════════════════════════════

    private int FindBlinkShape(Mesh m)
    {
        string[] names = { "eyeBlinkLeft", "eyeBlinkRight", "eyeBlink_L", "eyeBlink_R", "blink" };
        foreach (var n in names)
        {
            int idx = m.GetBlendShapeIndex(n);
            if (idx >= 0) return idx;
        }
        return FindPartial(m, "blink");
    }

    private int FindShapeMulti(Mesh m, params string[] names)
    {
        foreach (string n in names)
        {
            int idx = m.GetBlendShapeIndex(n);
            if (idx >= 0) return idx;
        }
        return -1;
    }

    private int FindPartial(Mesh m, string keyword)
    {
        string kw = keyword.ToLowerInvariant();
        for (int i = 0; i < m.blendShapeCount; i++)
            if (m.GetBlendShapeName(i).ToLowerInvariant().Contains(kw))
                return i;
        return -1;
    }

    private int FindPartial2(Mesh m, string keyword1, string keyword2)
    {
        string k1 = keyword1.ToLowerInvariant();
        string k2 = keyword2.ToLowerInvariant();
        for (int i = 0; i < m.blendShapeCount; i++)
        {
            string name = m.GetBlendShapeName(i).ToLowerInvariant();
            if (name.Contains(k1) && name.Contains(k2))
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

        StabilizeMaleRightFootAfterAnimation();
        GroundMaleAvatarAfterAnimation();

        if (!_faceReady) return;

        bool speaking = _ttsAudio != null && _ttsAudio.isPlaying;

        DoBlink();
        if (!speaking) DoBlendshapeBreath();
        DoSmile();
        DoLipSync(speaking);
    }

    private void StabilizeMaleRightFootAfterAnimation()
    {
        if (!_isMaleAvatar || !stabilizeMaleRightFoot || _maleRightFoot == null) return;

        Quaternion targetRotation = _maleRightFoot.rotation;

        if (_maleRightToe != null)
        {
            Vector3 toeDirection = _maleRightToe.position - _maleRightFoot.position;
            Vector3 flatToeDirection = Vector3.ProjectOnPlane(toeDirection, Vector3.up);
            if (toeDirection.sqrMagnitude > 0.0001f && flatToeDirection.sqrMagnitude > 0.0001f)
            {
                targetRotation = Quaternion.FromToRotation(toeDirection.normalized, flatToeDirection.normalized) * targetRotation;
            }
        }

        targetRotation *= Quaternion.Euler(maleRightFootEulerOffset);
        _maleRightFoot.rotation = Quaternion.Slerp(_maleRightFoot.rotation, targetRotation, maleRightFootStabilizeWeight);
    }

    private void GroundMaleAvatarAfterAnimation()
    {
        if (!_isMaleAvatar || !keepMaleAvatarGroundedDuringAnimation || _maleModelRoot == null) return;
        AlignModelBottomToRoot(_maleModelRoot);
    }

    // ═══════════════════════════════════════════════════════════
    //  BLENDSHAPE ANİMASYONLAR
    // ═══════════════════════════════════════════════════════════

    private void DoBlink()
    {
        bool hasTarget = _useSingleMesh
            ? (_primaryBlinkL >= 0 || _primaryBlinkR >= 0)
            : (_eyeLBlink >= 0 || _eyeRBlink >= 0);
        if (!hasTarget) return;

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

        if (_useSingleMesh)
        {
            if (_primaryBlinkL >= 0) _primarySkin.SetBlendShapeWeight(_primaryBlinkL, w);
            if (_primaryBlinkR >= 0) _primarySkin.SetBlendShapeWeight(_primaryBlinkR, w);
        }
        else
        {
            if (_eyeLSkin != null && _eyeLBlink >= 0) _eyeLSkin.SetBlendShapeWeight(_eyeLBlink, w);
            if (_eyeRSkin != null && _eyeRBlink >= 0) _eyeRSkin.SetBlendShapeWeight(_eyeRBlink, w);
        }
    }

    private void DoBlendshapeBreath()
    {
        float t = (Mathf.Sin(Time.time * (2f * Mathf.PI / breathCycle)) + 1f) * 0.5f;
        float w = t * breathIntensity;

        if (_useSingleMesh)
        {
            if (_primaryJawOpen >= 0)
                _primarySkin.SetBlendShapeWeight(_primaryJawOpen, w);
        }
        else
        {
            if (_headMouthOpen >= 0 && _headSkin != null)
                _headSkin.SetBlendShapeWeight(_headMouthOpen, w);
            if (_teethSkin != null && _teethMouthOpen >= 0)
                _teethSkin.SetBlendShapeWeight(_teethMouthOpen, _mouthWeight * 1.95f);
        }
    }

    private void DoSmile()
    {
        bool hasSmile = _useSingleMesh
            ? (_primarySmileL >= 0 || _primarySmileR >= 0)
            : (_headSmile >= 0 && _headSkin != null);
        if (!hasSmile) return;

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

        if (_useSingleMesh)
        {
            if (_primarySmileL >= 0) _primarySkin.SetBlendShapeWeight(_primarySmileL, w);
            if (_primarySmileR >= 0) _primarySkin.SetBlendShapeWeight(_primarySmileR, w);
        }
        else
        {
            _headSkin.SetBlendShapeWeight(_headSmile, w);
        }
    }

    private void DetectVisemes()
    {
        SkinnedMeshRenderer[] allSmr = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var smr in allSmr)
        {
            if (smr.sharedMesh == null) continue;
            Mesh m = smr.sharedMesh;
            int aa = FindViseme(m, "viseme_aa");
            if (aa < 0) continue;

            _visemeSkin = smr;
            _visemeAA = aa;
            _visemeSil = FindViseme(m, "viseme_sil");
            _visemeE = FindViseme(m, "viseme_E");
            _visemeI = FindViseme(m, "viseme_I");
            _visemeO = FindViseme(m, "viseme_O");
            _visemeU = FindViseme(m, "viseme_U");
            _visemePP = FindViseme(m, "viseme_PP");
            _visemeFF = FindViseme(m, "viseme_FF");
            _visemeTH = FindViseme(m, "viseme_TH");
            _visemeDD = FindViseme(m, "viseme_DD");
            _visemeCH = FindViseme(m, "viseme_CH");
            _visemeSS = FindViseme(m, "viseme_SS");
            _visemeNN = FindViseme(m, "viseme_nn");
            _visemeRR = FindViseme(m, "viseme_RR");
            _visemeKK = FindViseme(m, "viseme_kk");
            _hasVisemes = true;

            int count = 0;
            int[] all = { _visemeSil, _visemeAA, _visemeE, _visemeI, _visemeO, _visemeU,
                         _visemePP, _visemeFF, _visemeTH, _visemeDD, _visemeCH, _visemeSS,
                         _visemeNN, _visemeRR, _visemeKK };
            foreach (int v in all) if (v >= 0) count++;
            Debug.Log($"[Face] Viseme desteği bulundu! {count} viseme ({smr.gameObject.name})");
            return;
        }
        Debug.Log("[Face] Viseme blendshape bulunamadı — basit lip sync kullanılacak.");
    }

    private int FindViseme(Mesh m, string name)
    {
        int idx = m.GetBlendShapeIndex(name);
        if (idx >= 0) return idx;
        string lower = name.ToLowerInvariant();
        for (int i = 0; i < m.blendShapeCount; i++)
            if (m.GetBlendShapeName(i).ToLowerInvariant() == lower)
                return i;
        return -1;
    }

    private static readonly int[] VisemeCycle = { 0, 1, 2, 3, 4, 5, 6, 3, 1, 7, 2, 4 };

    private void DoLipSync(bool speaking)
    {
        float rms = 0f;
        if (speaking && _ttsAudio != null)
        {
            _ttsAudio.GetOutputData(_samples, 0);
            float sum = 0f;
            for (int i = 0; i < _samples.Length; i++)
                sum += _samples[i] * _samples[i];
            rms = Mathf.Sqrt(sum / _samples.Length);
        }

        float amplitude = Mathf.Clamp01(rms * lipSyncSensitivity * 100f);

        if (_hasVisemes && _visemeSkin != null)
        {
            DoVisemeLipSync(speaking, amplitude);
            return;
        }

        float jawTarget = speaking ? amplitude * jawMaxWeight : 0f;
        float mouthTarget = speaking ? amplitude * mouthMaxWeight : 0f;

        float openSpeed = Time.deltaTime * lipSyncSmooth;
        float closeSpeed = Time.deltaTime * lipSyncCloseSpeed;

        _jawWeight = Mathf.Lerp(_jawWeight, jawTarget,
            jawTarget > _jawWeight ? openSpeed : closeSpeed);
        _mouthWeight = Mathf.Lerp(_mouthWeight, mouthTarget,
            mouthTarget > _mouthWeight ? openSpeed : closeSpeed);

        if (_useSingleMesh)
        {
            if (_primaryJawOpen >= 0)
                _primarySkin.SetBlendShapeWeight(_primaryJawOpen, _jawWeight);
            if (_primaryMouthOpen >= 0)
                _primarySkin.SetBlendShapeWeight(_primaryMouthOpen, _mouthWeight);
        }
        else
        {
            if (_headMouthOpen >= 0 && _headSkin != null)
                _headSkin.SetBlendShapeWeight(_headMouthOpen, _jawWeight);
            if (_teethSkin != null && _teethMouthOpen >= 0)
                _teethSkin.SetBlendShapeWeight(_teethMouthOpen, _jawWeight * 0.5f);
        }
    }

    private void DoVisemeLipSync(bool speaking, float amplitude)
    {
        int[] visemeIndices = {
            _visemeSil, _visemeAA, _visemeE, _visemeO,
            _visemeU, _visemeFF, _visemePP, _visemeDD
        };

        if (!speaking || amplitude < 0.01f)
        {
            foreach (int idx in visemeIndices)
                if (idx >= 0) _visemeSkin.SetBlendShapeWeight(idx,
                    Mathf.Lerp(_visemeSkin.GetBlendShapeWeight(idx), 0f, Time.deltaTime * lipSyncCloseSpeed));
            if (_visemeNN >= 0) _visemeSkin.SetBlendShapeWeight(_visemeNN,
                Mathf.Lerp(_visemeSkin.GetBlendShapeWeight(_visemeNN), 0f, Time.deltaTime * lipSyncCloseSpeed));
            if (_visemeSS >= 0) _visemeSkin.SetBlendShapeWeight(_visemeSS,
                Mathf.Lerp(_visemeSkin.GetBlendShapeWeight(_visemeSS), 0f, Time.deltaTime * lipSyncCloseSpeed));
            if (_visemeTH >= 0) _visemeSkin.SetBlendShapeWeight(_visemeTH,
                Mathf.Lerp(_visemeSkin.GetBlendShapeWeight(_visemeTH), 0f, Time.deltaTime * lipSyncCloseSpeed));
            if (_visemeCH >= 0) _visemeSkin.SetBlendShapeWeight(_visemeCH,
                Mathf.Lerp(_visemeSkin.GetBlendShapeWeight(_visemeCH), 0f, Time.deltaTime * lipSyncCloseSpeed));
            if (_visemeRR >= 0) _visemeSkin.SetBlendShapeWeight(_visemeRR,
                Mathf.Lerp(_visemeSkin.GetBlendShapeWeight(_visemeRR), 0f, Time.deltaTime * lipSyncCloseSpeed));
            if (_visemeKK >= 0) _visemeSkin.SetBlendShapeWeight(_visemeKK,
                Mathf.Lerp(_visemeSkin.GetBlendShapeWeight(_visemeKK), 0f, Time.deltaTime * lipSyncCloseSpeed));
            if (_visemeI >= 0) _visemeSkin.SetBlendShapeWeight(_visemeI,
                Mathf.Lerp(_visemeSkin.GetBlendShapeWeight(_visemeI), 0f, Time.deltaTime * lipSyncCloseSpeed));
            _currentVisemeTime = 0f;
            return;
        }

        _currentVisemeTime += Time.deltaTime;
        float cycleSpeed = 0.08f + (1f - amplitude) * 0.06f;
        if (_currentVisemeTime > cycleSpeed)
        {
            _currentVisemeTime = 0f;
            _currentVisemeIdx = (_currentVisemeIdx + 1) % VisemeCycle.Length;
        }

        float targetWeight = amplitude * visemeMaxWeight;
        float blend = Mathf.Clamp01(_currentVisemeTime / cycleSpeed);

        for (int v = 0; v < visemeIndices.Length; v++)
        {
            if (visemeIndices[v] < 0) continue;
            int cycleVal = VisemeCycle[_currentVisemeIdx];
            float w = (v == cycleVal) ? Mathf.Lerp(0f, targetWeight, blend) : 0f;
            float current = _visemeSkin.GetBlendShapeWeight(visemeIndices[v]);
            _visemeSkin.SetBlendShapeWeight(visemeIndices[v],
                Mathf.Lerp(current, w, Time.deltaTime * lipSyncSmooth));
        }
    }
}
