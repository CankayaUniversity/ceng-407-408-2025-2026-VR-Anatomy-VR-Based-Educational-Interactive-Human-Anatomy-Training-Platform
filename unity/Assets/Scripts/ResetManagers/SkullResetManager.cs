using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class SkullResetManager : MonoBehaviour
{
    [System.Serializable]
    private class BoneData
    {
        public Transform transform;
        public Vector3 startLocalPosition;
        public Quaternion startLocalRotation;
        public Vector3 startLocalScale;
        public Rigidbody rb;
    }

    [Header("Reset Input")]
    public InputActionReference resetAction;   // Quest'te tuş için
    public KeyCode debugKey = KeyCode.R;       // PC'de test için

    private List<BoneData> _bones = new List<BoneData>();

    private void Awake()
    {
        // Altındaki tüm XR Grab Interactable'ları bul ve başlangıç transformlarını kaydet
        var grabables = GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(includeInactive: true);
        foreach (var grab in grabables)
        {
            Transform t = grab.transform;
            var rb = t.GetComponent<Rigidbody>();

            _bones.Add(new BoneData
            {
                transform = t,
                startLocalPosition = t.localPosition,
                startLocalRotation = t.localRotation,
                startLocalScale = t.localScale,
                rb = rb
            });
        }
    }

    private void OnEnable()
    {
        if (resetAction != null && resetAction.action != null)
        {
            resetAction.action.performed += OnResetPerformed;
            resetAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (resetAction != null && resetAction.action != null)
        {
            resetAction.action.performed -= OnResetPerformed;
            resetAction.action.Disable();
        }
    }

    private void Update()
    {
        // Editörde hızlı test için klavyeden R
        if (debugKey != KeyCode.None && Input.GetKeyDown(debugKey))
        {
            ResetBones();
        }
    }

    private void OnResetPerformed(InputAction.CallbackContext ctx)
    {
        ResetBones();
    }

    public void ResetBones()
    {
        foreach (var b in _bones)
        {
            if (b.rb != null)
            {
                b.rb.linearVelocity = Vector3.zero;
                b.rb.angularVelocity = Vector3.zero;
            }

            b.transform.localPosition = b.startLocalPosition;
            b.transform.localRotation = b.startLocalRotation;
            b.transform.localScale = b.startLocalScale;
        }
    }
}
