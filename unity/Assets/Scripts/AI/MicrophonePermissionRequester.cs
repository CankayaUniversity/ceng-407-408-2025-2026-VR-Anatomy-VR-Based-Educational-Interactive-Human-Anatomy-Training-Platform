using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class MicrophonePermissionRequester : MonoBehaviour
{
    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            Debug.Log("[Permission] Microphone permission requested.");
        }
        else
        {
            Debug.Log("[Permission] Microphone permission already granted.");
        }
#endif
    }
}