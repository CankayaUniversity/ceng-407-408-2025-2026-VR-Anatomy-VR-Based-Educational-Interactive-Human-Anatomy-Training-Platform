using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AvatarAnimationSetup
{
    private const string IdleFbxPath = "Assets/Avatars/Idle.fbx";
    private const string OutputClipPath = "Assets/Avatars/IdleRemapped.anim";
    private const string ModelGlbRoot = "AvatarRoot";

    [MenuItem("VRAnatomy/Setup Avatar Idle Animation")]
    public static void SetupIdleAnimation()
    {
        var fbxClips = GetClipsFromFbx(IdleFbxPath);
        if (fbxClips.Count == 0)
        {
            Debug.LogError($"[AvatarSetup] {IdleFbxPath} içinde animasyon bulunamadı.");
            return;
        }

        AnimationClip sourceClip = fbxClips[0];
        Debug.Log($"[AvatarSetup] Kaynak klip: '{sourceClip.name}' ({sourceClip.length:F2}s, {sourceClip.frameRate} fps)");

        var bindings = AnimationUtility.GetCurveBindings(sourceClip);
        Debug.Log($"[AvatarSetup] {bindings.Length} curve binding bulundu.");

        string fbxRoot = DetectRootFromBindings(bindings);
        Debug.Log($"[AvatarSetup] FBX root: '{fbxRoot}' → Model root: '{ModelGlbRoot}'");

        AnimationClip remapped = new AnimationClip();
        remapped.name = "IdleRemapped";
        remapped.legacy = true;
        remapped.wrapMode = WrapMode.Loop;
        remapped.frameRate = sourceClip.frameRate;

        int remappedCount = 0;
        foreach (var binding in bindings)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            if (curve == null) continue;

            EditorCurveBinding newBinding = binding;
            newBinding.path = RemapPath(binding.path, fbxRoot, ModelGlbRoot);

            AnimationUtility.SetEditorCurve(remapped, newBinding, curve);
            remappedCount++;
        }

        var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(sourceClip);
        foreach (var binding in objectBindings)
        {
            var keyframes = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
            if (keyframes == null) continue;

            EditorCurveBinding newBinding = binding;
            newBinding.path = RemapPath(binding.path, fbxRoot, ModelGlbRoot);

            AnimationUtility.SetObjectReferenceCurve(remapped, newBinding, keyframes);
            remappedCount++;
        }

        var settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(remapped, settings);

        AssetDatabase.CreateAsset(remapped, OutputClipPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AvatarSetup] Remapped klip kaydedildi: {OutputClipPath} ({remappedCount} curve)");
        LogSamplePaths(remapped, 10);

        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputClipPath));
    }

    [MenuItem("VRAnatomy/Log Idle.fbx Animation Paths")]
    public static void LogIdlePaths()
    {
        var clips = GetClipsFromFbx(IdleFbxPath);
        if (clips.Count == 0)
        {
            Debug.LogError($"[AvatarSetup] {IdleFbxPath} içinde klip yok.");
            return;
        }

        foreach (var clip in clips)
        {
            Debug.Log($"[AvatarSetup] ═══ Klip: '{clip.name}' ({clip.length:F2}s) ═══");
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var paths = bindings.Select(b => b.path).Distinct().OrderBy(p => p).ToList();

            Debug.Log($"[AvatarSetup] {paths.Count} unique path:");
            foreach (string path in paths)
                Debug.Log($"  {path}");
        }
    }

    private static List<AnimationClip> GetClipsFromFbx(string path)
    {
        var result = new List<AnimationClip>();
        var objects = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var obj in objects)
        {
            if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                result.Add(clip);
        }
        return result;
    }

    private static string DetectRootFromBindings(EditorCurveBinding[] bindings)
    {
        var roots = new HashSet<string>();
        foreach (var b in bindings)
        {
            if (string.IsNullOrEmpty(b.path)) continue;
            string first = b.path.Split('/')[0];
            roots.Add(first);
        }

        if (roots.Count == 1)
            return roots.First();

        foreach (string r in roots)
        {
            string lower = r.ToLowerInvariant();
            if (lower.Contains("armature") || lower.Contains("root") || lower.Contains("avatar"))
                return r;
        }

        string mostCommon = bindings
            .Where(b => !string.IsNullOrEmpty(b.path))
            .Select(b => b.path.Split('/')[0])
            .GroupBy(r => r)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        return mostCommon ?? "";
    }

    private static string RemapPath(string originalPath, string fbxRoot, string modelRoot)
    {
        if (string.IsNullOrEmpty(originalPath))
            return originalPath;

        if (string.IsNullOrEmpty(fbxRoot))
            return modelRoot + "/" + originalPath;

        if (originalPath == fbxRoot)
            return modelRoot;

        if (originalPath.StartsWith(fbxRoot + "/"))
            return modelRoot + originalPath.Substring(fbxRoot.Length);

        return originalPath;
    }

    private static void LogSamplePaths(AnimationClip clip, int maxPaths)
    {
        var bindings = AnimationUtility.GetCurveBindings(clip);
        var paths = bindings.Select(b => b.path).Distinct().OrderBy(p => p).ToList();

        int count = Mathf.Min(paths.Count, maxPaths);
        Debug.Log($"[AvatarSetup] Remapped path örnekleri ({count}/{paths.Count}):");
        for (int i = 0; i < count; i++)
            Debug.Log($"  {paths[i]}");
    }
}
