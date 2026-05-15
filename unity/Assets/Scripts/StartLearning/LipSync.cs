using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class LipSync : MonoBehaviour
{
    [Header("Audio Source")]
    [Tooltip("Drag the AudioSource that plays the MP3 or TTS voice here.")]
    [SerializeField] private AudioSource speechAudioSource;

    [Tooltip("If true, the script tries to find an AudioSource on this model or its children.")]
    [SerializeField] private bool autoFindAudioSourceInChildren = true;

    [Tooltip("If true, the script also tries to find an AudioSource in the parent objects.")]
    [SerializeField] private bool autoFindAudioSourceInParents = true;

    [Tooltip("Only enable this if your speech AudioSource is somewhere else in the scene.")]
    [SerializeField] private bool autoFindFirstSceneAudioSource = false;

    [Header("Look At Camera")]
    [SerializeField] private bool lookAtCamera = true;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float lookAtPitchOffset = -5f;

    [Header("Blink")]
    [SerializeField] private float blinkIntervalMin = 2.5f;
    [SerializeField] private float blinkIntervalMax = 6f;
    [SerializeField] private float blinkSpeed = 0.15f;

    [Header("Idle Mouth")]
    [SerializeField] private float breathCycle = 4f;
    [SerializeField] private float breathIntensity = 0.3f;

    [Header("Smile")]
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

    private Transform cameraTransform;

    private SkinnedMeshRenderer headSkin;
    private SkinnedMeshRenderer teethSkin;
    private SkinnedMeshRenderer primarySkin;

    private int headMouthOpen = -1;

    private int headSmile = -1;
    private int headSmileL = -1;
    private int headSmileR = -1;

    private int teethMouthOpen = -1;

    private int primaryJawOpen = -1;
    private int primaryMouthOpen = -1;
    private int primarySmileL = -1;
    private int primarySmileR = -1;
    private int primaryBlinkL = -1;
    private int primaryBlinkR = -1;

    private SkinnedMeshRenderer eyeLSkin;
    private SkinnedMeshRenderer eyeRSkin;
    private int eyeLBlink = -1;
    private int eyeRBlink = -1;

    private bool initialized;
    private bool faceReady;
    private bool useSingleMesh;

    private SkinnedMeshRenderer visemeSkin;
    private bool hasVisemes;

    private int visemeSil = -1;
    private int visemeAA = -1;
    private int visemeE = -1;
    private int visemeI = -1;
    private int visemeO = -1;
    private int visemeU = -1;
    private int visemePP = -1;
    private int visemeFF = -1;
    private int visemeTH = -1;
    private int visemeDD = -1;
    private int visemeCH = -1;
    private int visemeSS = -1;
    private int visemeNN = -1;
    private int visemeRR = -1;
    private int visemeKK = -1;

    private float nextBlink;
    private float blinkT;
    private bool blinking;

    private float nextSmile;
    private float smileT;
    private bool smiling;

    private float jawWeight;
    private float mouthWeight;

    private float currentVisemeTime;
    private int currentVisemeIndex;

    private readonly float[] samples = new float[256];

    private static readonly int[] VisemeCycle =
    {
        0, 1, 2, 3, 4, 5, 6, 3, 1, 7, 2, 4
    };

    private IEnumerator Start()
    {
        yield return null;
        Initialize();
    }

    public void Initialize()
    {
        cameraTransform = Camera.main != null ? Camera.main.transform : null;

        ResolveAudioSource();
        InitFace();

        nextBlink = Time.time + Random.Range(blinkIntervalMin, blinkIntervalMax);
        nextSmile = Time.time + Random.Range(smileIntervalMin, smileIntervalMax);

        initialized = true;

        Debug.Log("[ChatAvatar] Lip sync controller initialized.");
    }

    public void SetSpeechAudioSource(AudioSource newAudioSource)
    {
        speechAudioSource = newAudioSource;
    }

    private void ResolveAudioSource()
    {
        if (speechAudioSource != null)
            return;

        if (TryGetComponent(out AudioSource ownAudio))
        {
            speechAudioSource = ownAudio;
            Debug.Log("[ChatAvatar] AudioSource found on same GameObject.");
            return;
        }

        if (autoFindAudioSourceInChildren)
        {
            speechAudioSource = GetComponentInChildren<AudioSource>(true);

            if (speechAudioSource != null)
            {
                Debug.Log("[ChatAvatar] AudioSource found in children.");
                return;
            }
        }

        if (autoFindAudioSourceInParents)
        {
            speechAudioSource = GetComponentInParent<AudioSource>();

            if (speechAudioSource != null)
            {
                Debug.Log("[ChatAvatar] AudioSource found in parents.");
                return;
            }
        }

        if (autoFindFirstSceneAudioSource)
        {
            AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

            foreach (AudioSource source in allAudioSources)
            {
                if (source != null && source.isPlaying)
                {
                    speechAudioSource = source;
                    Debug.Log("[ChatAvatar] Playing scene AudioSource found.");
                    return;
                }
            }

            if (allAudioSources.Length > 0)
            {
                speechAudioSource = allAudioSources[0];
                Debug.Log("[ChatAvatar] First scene AudioSource assigned.");
                return;
            }
        }

        Debug.LogWarning("[ChatAvatar] No AudioSource assigned. Drag the speech AudioSource into the Inspector.");
    }

    private void InitFace()
    {
        SkinnedMeshRenderer[] all = GetComponentsInChildren<SkinnedMeshRenderer>(true);

        bool foundSeparatedFaceMeshes = false;
        SkinnedMeshRenderer bestSingleMesh = null;
        int maxBlendShapes = 0;

        foreach (SkinnedMeshRenderer smr in all)
        {
            if (smr == null || smr.sharedMesh == null)
                continue;

            Mesh mesh = smr.sharedMesh;
            string objectName = smr.gameObject.name;

            Debug.Log("[Face] Mesh: " + objectName + " - " + mesh.blendShapeCount + " blendshape(s)");

            if (mesh.blendShapeCount > maxBlendShapes)
            {
                maxBlendShapes = mesh.blendShapeCount;
                bestSingleMesh = smr;
            }

            string lowerName = objectName.ToLowerInvariant();

            if (lowerName.Contains("head"))
            {
                headSkin = smr;

                headMouthOpen = FindShapeMulti(mesh, "mouthOpen", "jawOpen");
                if (headMouthOpen < 0) headMouthOpen = FindPartial(mesh, "mouth");
                if (headMouthOpen < 0) headMouthOpen = FindPartial(mesh, "jaw");

                headSmile = FindShapeMulti(mesh, "mouthSmile", "MouthSmile", "smile");

                headSmileL = FindShapeMulti(
                    mesh,
                    "mouthSmileLeft",
                    "mouthSmile_L",
                    "MouthSmileLeft",
                    "smileLeft",
                    "smile_L"
                );

                headSmileR = FindShapeMulti(
                    mesh,
                    "mouthSmileRight",
                    "mouthSmile_R",
                    "MouthSmileRight",
                    "smileRight",
                    "smile_R"
                );

                if (headSmile < 0)
                    headSmile = FindPartial(mesh, "smile");

                if (headSmileL < 0)
                    headSmileL = FindPartial2(mesh, "smile", "left");

                if (headSmileR < 0)
                    headSmileR = FindPartial2(mesh, "smile", "right");

                if (headSmileL < 0)
                    headSmileL = FindPartial2(mesh, "smile", "l");

                if (headSmileR < 0)
                    headSmileR = FindPartial2(mesh, "smile", "r");

                Debug.Log("[Face] Head smile indices - Center: " + headSmile + ", Left: " + headSmileL + ", Right: " + headSmileR);

                foundSeparatedFaceMeshes = true;
            }
            else if (lowerName.Contains("teeth"))
            {
                teethSkin = smr;

                teethMouthOpen = FindShapeMulti(mesh, "mouthOpen", "jawOpen");
                if (teethMouthOpen < 0) teethMouthOpen = FindPartial(mesh, "mouth");
                if (teethMouthOpen < 0) teethMouthOpen = FindPartial(mesh, "jaw");

                foundSeparatedFaceMeshes = true;
            }
            else if (lowerName.Contains("eyeleft") || lowerName.Contains("eye_l"))
            {
                eyeLSkin = smr;
                eyeLBlink = FindBlinkShape(mesh);
                foundSeparatedFaceMeshes = true;
            }
            else if (lowerName.Contains("eyeright") || lowerName.Contains("eye_r"))
            {
                eyeRSkin = smr;
                eyeRBlink = FindBlinkShape(mesh);
                foundSeparatedFaceMeshes = true;
            }
        }

        if (!foundSeparatedFaceMeshes && bestSingleMesh != null && maxBlendShapes > 0)
        {
            useSingleMesh = true;
            primarySkin = bestSingleMesh;

            Mesh mesh = bestSingleMesh.sharedMesh;

            primaryBlinkL = FindShapeMulti(mesh, "eyeBlinkLeft", "eyeBlink_L", "EyeBlinkLeft", "blink_L", "blinkLeft");
            primaryBlinkR = FindShapeMulti(mesh, "eyeBlinkRight", "eyeBlink_R", "EyeBlinkRight", "blink_R", "blinkRight");

            primaryJawOpen = FindShapeMulti(mesh, "jawOpen", "Jaw_Open", "JawOpen", "jaw_open");
            primaryMouthOpen = FindShapeMulti(mesh, "mouthOpen", "Mouth_Open", "MouthOpen", "mouth_open", "viseme_O", "viseme_aa");

            primarySmileL = FindShapeMulti(mesh, "mouthSmileLeft", "mouthSmile_L", "MouthSmileLeft", "smile_L", "smileLeft", "mouthSmile");
            primarySmileR = FindShapeMulti(mesh, "mouthSmileRight", "mouthSmile_R", "MouthSmileRight", "smile_R", "smileRight");

            if (primaryJawOpen < 0)
                primaryJawOpen = FindPartial2(mesh, "jaw", "open");

            if (primaryMouthOpen < 0)
                primaryMouthOpen = FindPartial2(mesh, "mouth", "open");

            if (primaryBlinkL < 0)
                primaryBlinkL = FindPartial2(mesh, "blink", "l");

            if (primaryBlinkR < 0)
                primaryBlinkR = FindPartial2(mesh, "blink", "r");

            if (primarySmileL < 0)
                primarySmileL = FindPartial2(mesh, "smile", "l");

            if (primarySmileR < 0)
                primarySmileR = FindPartial2(mesh, "smile", "r");

            Debug.Log("[Face] Single mesh mode enabled.");
            Debug.Log("[Face] Primary smile indices - Left: " + primarySmileL + ", Right: " + primarySmileR);
        }

        DetectVisemes();

        faceReady = useSingleMesh
            ? primarySkin != null && (primaryJawOpen >= 0 || primaryMouthOpen >= 0 || primarySmileL >= 0 || primarySmileR >= 0)
            : headSkin != null && (headMouthOpen >= 0 || headSmile >= 0 || headSmileL >= 0 || headSmileR >= 0);

        if (faceReady)
            Debug.Log("[Face] Face animation ready.");
        else
            Debug.LogWarning("[Face] No usable face blendshape found. Lip sync disabled.");
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        if (lookAtCamera && cameraTransform != null)
            LookAtCamera();

        if (!faceReady)
            return;

        bool speaking = IsSpeechPlaying();

        DoBlink();

        if (!speaking)
            DoIdleMouth();

        DoSmile();
        DoLipSync(speaking);
    }

    private void LookAtCamera()
    {
        Vector3 direction = cameraTransform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion yaw = Quaternion.LookRotation(direction);
        Quaternion target = yaw * Quaternion.Euler(lookAtPitchOffset, 0f, 0f);

        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * rotationSpeed);
    }

    private bool IsSpeechPlaying()
    {
        return speechAudioSource != null
            && speechAudioSource.enabled
            && speechAudioSource.gameObject.activeInHierarchy
            && speechAudioSource.clip != null
            && speechAudioSource.isPlaying;
    }

    private void DoBlink()
    {
        bool hasTarget = useSingleMesh
            ? primaryBlinkL >= 0 || primaryBlinkR >= 0
            : eyeLBlink >= 0 || eyeRBlink >= 0;

        if (!hasTarget)
            return;

        if (!blinking)
        {
            if (Time.time < nextBlink)
                return;

            blinking = true;
            blinkT = 0f;
        }

        blinkT += Time.deltaTime;

        float half = blinkSpeed * 0.5f;
        float weight;

        if (blinkT < half)
            weight = Mathf.Lerp(0f, 100f, blinkT / half);
        else if (blinkT < blinkSpeed)
            weight = Mathf.Lerp(100f, 0f, (blinkT - half) / half);
        else
        {
            weight = 0f;
            blinking = false;
            nextBlink = Time.time + Random.Range(blinkIntervalMin, blinkIntervalMax);
        }

        if (useSingleMesh)
        {
            if (primaryBlinkL >= 0)
                primarySkin.SetBlendShapeWeight(primaryBlinkL, weight);

            if (primaryBlinkR >= 0)
                primarySkin.SetBlendShapeWeight(primaryBlinkR, weight);
        }
        else
        {
            if (eyeLSkin != null && eyeLBlink >= 0)
                eyeLSkin.SetBlendShapeWeight(eyeLBlink, weight);

            if (eyeRSkin != null && eyeRBlink >= 0)
                eyeRSkin.SetBlendShapeWeight(eyeRBlink, weight);
        }
    }

    private void DoIdleMouth()
    {
        float t = Mathf.Sin(Time.time * (2f * Mathf.PI / breathCycle));
        float weight = ((t + 1f) * 0.5f) * breathIntensity;

        if (useSingleMesh)
        {
            if (primaryJawOpen >= 0)
                primarySkin.SetBlendShapeWeight(primaryJawOpen, weight);

            if (primaryMouthOpen >= 0)
                primarySkin.SetBlendShapeWeight(primaryMouthOpen, weight);
        }
        else
        {
            if (headSkin != null && headMouthOpen >= 0)
                headSkin.SetBlendShapeWeight(headMouthOpen, weight);

            if (teethSkin != null && teethMouthOpen >= 0)
                teethSkin.SetBlendShapeWeight(teethMouthOpen, weight);
        }
    }

    private void DoSmile()
    {
        bool hasSmile = useSingleMesh
            ? primarySmileL >= 0 || primarySmileR >= 0
            : headSmile >= 0 || headSmileL >= 0 || headSmileR >= 0;

        if (!hasSmile)
            return;

        if (!smiling)
        {
            if (Time.time < nextSmile)
                return;

            smiling = true;
            smileT = 0f;
        }

        smileT += Time.deltaTime;

        float half = smileDuration * 0.5f;
        float weight;

        if (smileT < half)
            weight = Mathf.SmoothStep(0f, smileIntensity, smileT / half);
        else if (smileT < smileDuration)
            weight = Mathf.SmoothStep(smileIntensity, 0f, (smileT - half) / half);
        else
        {
            weight = 0f;
            smiling = false;
            nextSmile = Time.time + Random.Range(smileIntervalMin, smileIntervalMax);
        }

        if (useSingleMesh)
        {
            if (primarySmileL >= 0)
                primarySkin.SetBlendShapeWeight(primarySmileL, weight);

            if (primarySmileR >= 0)
                primarySkin.SetBlendShapeWeight(primarySmileR, weight);
        }
        else
        {
            if (headSkin == null)
                return;

            if (headSmileL >= 0)
                headSkin.SetBlendShapeWeight(headSmileL, weight);

            if (headSmileR >= 0)
                headSkin.SetBlendShapeWeight(headSmileR, weight);

            if (headSmileL < 0 && headSmileR < 0 && headSmile >= 0)
                headSkin.SetBlendShapeWeight(headSmile, weight);
        }
    }

    private void DoLipSync(bool speaking)
    {
        float rms = 0f;

        if (speaking && speechAudioSource != null)
        {
            speechAudioSource.GetOutputData(samples, 0);

            float sum = 0f;

            for (int i = 0; i < samples.Length; i++)
                sum += samples[i] * samples[i];

            rms = Mathf.Sqrt(sum / samples.Length);
        }

        float amplitude = Mathf.Clamp01(rms * lipSyncSensitivity * 100f);

        if (hasVisemes && visemeSkin != null)
        {
            DoVisemeLipSync(speaking, amplitude);
            return;
        }

        float jawTarget = speaking ? amplitude * jawMaxWeight : 0f;
        float mouthTarget = speaking ? amplitude * mouthMaxWeight : 0f;

        float openSpeed = Time.deltaTime * lipSyncSmooth;
        float closeSpeed = Time.deltaTime * lipSyncCloseSpeed;

        jawWeight = Mathf.Lerp(jawWeight, jawTarget, jawTarget > jawWeight ? openSpeed : closeSpeed);
        mouthWeight = Mathf.Lerp(mouthWeight, mouthTarget, mouthTarget > mouthWeight ? openSpeed : closeSpeed);

        if (useSingleMesh)
        {
            if (primaryJawOpen >= 0)
                primarySkin.SetBlendShapeWeight(primaryJawOpen, jawWeight);

            if (primaryMouthOpen >= 0)
                primarySkin.SetBlendShapeWeight(primaryMouthOpen, mouthWeight);
        }
        else
        {
            if (headSkin != null && headMouthOpen >= 0)
                headSkin.SetBlendShapeWeight(headMouthOpen, jawWeight);

            if (teethSkin != null && teethMouthOpen >= 0)
                teethSkin.SetBlendShapeWeight(teethMouthOpen, jawWeight * 0.5f);
        }
    }

    private void DoVisemeLipSync(bool speaking, float amplitude)
    {
        int[] mainVisemes =
        {
            visemeSil,
            visemeAA,
            visemeE,
            visemeO,
            visemeU,
            visemeFF,
            visemePP,
            visemeDD
        };

        if (!speaking || amplitude < 0.01f)
        {
            FadeOutViseme(visemeSil);
            FadeOutViseme(visemeAA);
            FadeOutViseme(visemeE);
            FadeOutViseme(visemeI);
            FadeOutViseme(visemeO);
            FadeOutViseme(visemeU);
            FadeOutViseme(visemePP);
            FadeOutViseme(visemeFF);
            FadeOutViseme(visemeTH);
            FadeOutViseme(visemeDD);
            FadeOutViseme(visemeCH);
            FadeOutViseme(visemeSS);
            FadeOutViseme(visemeNN);
            FadeOutViseme(visemeRR);
            FadeOutViseme(visemeKK);

            currentVisemeTime = 0f;
            return;
        }

        currentVisemeTime += Time.deltaTime;

        float cycleSpeed = 0.08f + (1f - amplitude) * 0.06f;

        if (currentVisemeTime > cycleSpeed)
        {
            currentVisemeTime = 0f;
            currentVisemeIndex = (currentVisemeIndex + 1) % VisemeCycle.Length;
        }

        float targetWeight = amplitude * visemeMaxWeight;
        float blend = Mathf.Clamp01(currentVisemeTime / cycleSpeed);
        int activeCycleValue = VisemeCycle[currentVisemeIndex];

        for (int i = 0; i < mainVisemes.Length; i++)
        {
            int blendShapeIndex = mainVisemes[i];

            if (blendShapeIndex < 0)
                continue;

            float target = i == activeCycleValue ? Mathf.Lerp(0f, targetWeight, blend) : 0f;
            float current = visemeSkin.GetBlendShapeWeight(blendShapeIndex);

            visemeSkin.SetBlendShapeWeight(
                blendShapeIndex,
                Mathf.Lerp(current, target, Time.deltaTime * lipSyncSmooth)
            );
        }
    }

    private void FadeOutViseme(int index)
    {
        if (visemeSkin == null || index < 0)
            return;

        float current = visemeSkin.GetBlendShapeWeight(index);

        visemeSkin.SetBlendShapeWeight(
            index,
            Mathf.Lerp(current, 0f, Time.deltaTime * lipSyncCloseSpeed)
        );
    }

    private void DetectVisemes()
    {
        SkinnedMeshRenderer[] allSmr = GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (SkinnedMeshRenderer smr in allSmr)
        {
            if (smr == null || smr.sharedMesh == null)
                continue;

            Mesh mesh = smr.sharedMesh;

            int aa = FindViseme(mesh, "viseme_aa");

            if (aa < 0)
                continue;

            visemeSkin = smr;

            visemeAA = aa;
            visemeSil = FindViseme(mesh, "viseme_sil");
            visemeE = FindViseme(mesh, "viseme_E");
            visemeI = FindViseme(mesh, "viseme_I");
            visemeO = FindViseme(mesh, "viseme_O");
            visemeU = FindViseme(mesh, "viseme_U");
            visemePP = FindViseme(mesh, "viseme_PP");
            visemeFF = FindViseme(mesh, "viseme_FF");
            visemeTH = FindViseme(mesh, "viseme_TH");
            visemeDD = FindViseme(mesh, "viseme_DD");
            visemeCH = FindViseme(mesh, "viseme_CH");
            visemeSS = FindViseme(mesh, "viseme_SS");
            visemeNN = FindViseme(mesh, "viseme_nn");
            visemeRR = FindViseme(mesh, "viseme_RR");
            visemeKK = FindViseme(mesh, "viseme_kk");

            hasVisemes = true;

            Debug.Log("[Face] Viseme support found on " + smr.gameObject.name);
            return;
        }

        Debug.Log("[Face] No viseme blendshapes found. Basic lip sync will be used.");
    }

    private int FindBlinkShape(Mesh mesh)
    {
        string[] names =
        {
            "eyeBlinkLeft",
            "eyeBlinkRight",
            "eyeBlink_L",
            "eyeBlink_R",
            "blink"
        };

        foreach (string name in names)
        {
            int index = mesh.GetBlendShapeIndex(name);

            if (index >= 0)
                return index;
        }

        return FindPartial(mesh, "blink");
    }

    private int FindShapeMulti(Mesh mesh, params string[] names)
    {
        foreach (string name in names)
        {
            int index = mesh.GetBlendShapeIndex(name);

            if (index >= 0)
                return index;
        }

        return -1;
    }

    private int FindPartial(Mesh mesh, string keyword)
    {
        string lowerKeyword = keyword.ToLowerInvariant();

        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            string shapeName = mesh.GetBlendShapeName(i).ToLowerInvariant();

            if (shapeName.Contains(lowerKeyword))
                return i;
        }

        return -1;
    }

    private int FindPartial2(Mesh mesh, string keyword1, string keyword2)
    {
        string lowerKeyword1 = keyword1.ToLowerInvariant();
        string lowerKeyword2 = keyword2.ToLowerInvariant();

        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            string shapeName = mesh.GetBlendShapeName(i).ToLowerInvariant();

            if (shapeName.Contains(lowerKeyword1) && shapeName.Contains(lowerKeyword2))
                return i;
        }

        return -1;
    }

    private int FindViseme(Mesh mesh, string name)
    {
        int index = mesh.GetBlendShapeIndex(name);

        if (index >= 0)
            return index;

        string lowerName = name.ToLowerInvariant();

        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            string shapeName = mesh.GetBlendShapeName(i).ToLowerInvariant();

            if (shapeName == lowerName)
                return i;
        }

        return -1;
    }

    private void OnDisable()
    {
        ResetMouthBlendShapes();
    }

    private void ResetMouthBlendShapes()
    {
        if (useSingleMesh && primarySkin != null)
        {
            SafeSetBlendShape(primarySkin, primaryJawOpen, 0f);
            SafeSetBlendShape(primarySkin, primaryMouthOpen, 0f);
            SafeSetBlendShape(primarySkin, primarySmileL, 0f);
            SafeSetBlendShape(primarySkin, primarySmileR, 0f);
            SafeSetBlendShape(primarySkin, primaryBlinkL, 0f);
            SafeSetBlendShape(primarySkin, primaryBlinkR, 0f);
        }

        if (headSkin != null)
        {
            SafeSetBlendShape(headSkin, headMouthOpen, 0f);
            SafeSetBlendShape(headSkin, headSmile, 0f);
            SafeSetBlendShape(headSkin, headSmileL, 0f);
            SafeSetBlendShape(headSkin, headSmileR, 0f);
        }

        if (teethSkin != null)
            SafeSetBlendShape(teethSkin, teethMouthOpen, 0f);

        if (eyeLSkin != null)
            SafeSetBlendShape(eyeLSkin, eyeLBlink, 0f);

        if (eyeRSkin != null)
            SafeSetBlendShape(eyeRSkin, eyeRBlink, 0f);

        if (visemeSkin != null)
        {
            SafeSetBlendShape(visemeSkin, visemeSil, 0f);
            SafeSetBlendShape(visemeSkin, visemeAA, 0f);
            SafeSetBlendShape(visemeSkin, visemeE, 0f);
            SafeSetBlendShape(visemeSkin, visemeI, 0f);
            SafeSetBlendShape(visemeSkin, visemeO, 0f);
            SafeSetBlendShape(visemeSkin, visemeU, 0f);
            SafeSetBlendShape(visemeSkin, visemePP, 0f);
            SafeSetBlendShape(visemeSkin, visemeFF, 0f);
            SafeSetBlendShape(visemeSkin, visemeTH, 0f);
            SafeSetBlendShape(visemeSkin, visemeDD, 0f);
            SafeSetBlendShape(visemeSkin, visemeCH, 0f);
            SafeSetBlendShape(visemeSkin, visemeSS, 0f);
            SafeSetBlendShape(visemeSkin, visemeNN, 0f);
            SafeSetBlendShape(visemeSkin, visemeRR, 0f);
            SafeSetBlendShape(visemeSkin, visemeKK, 0f);
        }
    }

    private void SafeSetBlendShape(SkinnedMeshRenderer skin, int index, float weight)
    {
        if (skin == null || skin.sharedMesh == null)
            return;

        if (index < 0 || index >= skin.sharedMesh.blendShapeCount)
            return;

        skin.SetBlendShapeWeight(index, weight);
    }
}