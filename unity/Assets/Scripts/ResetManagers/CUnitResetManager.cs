using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CUnitResetManager : MonoBehaviour
{
    [System.Serializable]
    private class ItemData
    {
        public Transform t;
        public Vector3 localPos;
        public Quaternion localRot;
        public Vector3 localScale;
        public Rigidbody rb;
    }

    [Header("Units Parent")]
    [SerializeField] private Transform unitsRoot;

    [Header("Current Selected Unit")]
    [SerializeField] private Transform currentUnitRoot;

    [Header("Reset Input")]
    [SerializeField] private InputActionReference resetAction;

    [Header("Editor Debug Input")]
    [SerializeField] private bool enableEditorDebugKey = true;
    [SerializeField] private Key debugKey = Key.R;

    private readonly Dictionary<Transform, List<ItemData>> cache = new();

    private void Awake()
    {
        if (unitsRoot == null)
            unitsRoot = transform;

        CacheUnits();
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
#if UNITY_EDITOR
        if (!enableEditorDebugKey)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current[debugKey].wasPressedThisFrame)
        {
            ResetCurrentUnit();
        }
#endif
    }

    private void CacheUnits()
    {
        cache.Clear();

        if (unitsRoot == null)
        {
            Debug.LogWarning("[CUnitResetManager] unitsRoot atanmadı.");
            return;
        }

        for (int i = 0; i < unitsRoot.childCount; i++)
        {
            Transform unit = unitsRoot.GetChild(i);

            List<ItemData> list = new List<ItemData>();
            Transform[] transforms = unit.GetComponentsInChildren<Transform>(true);

            foreach (Transform tr in transforms)
            {
                Rigidbody rb = tr.GetComponent<Rigidbody>();

                list.Add(new ItemData
                {
                    t = tr,
                    localPos = tr.localPosition,
                    localRot = tr.localRotation,
                    localScale = tr.localScale,
                    rb = rb
                });
            }

            cache[unit] = list;
        }
    }

    public void SetCurrentUnit(Transform unitRoot)
    {
        currentUnitRoot = unitRoot;
    }

    private void OnResetPerformed(InputAction.CallbackContext ctx)
    {
        ResetCurrentUnit();
    }

    public void ResetCurrentUnit()
    {
        if (currentUnitRoot != null)
        {
            ResetUnit(currentUnitRoot);
            return;
        }

        ResetFirstActiveUnitFallback();
    }

    private void ResetFirstActiveUnitFallback()
    {
        if (unitsRoot == null)
            return;

        for (int i = 0; i < unitsRoot.childCount; i++)
        {
            Transform unit = unitsRoot.GetChild(i);

            if (unit.gameObject.activeInHierarchy)
            {
                ResetUnit(unit);
                return;
            }
        }

        Debug.LogWarning("[CUnitResetManager] Resetlenecek aktif ünite bulunamadı.");
    }

    private void ResetUnit(Transform unitRoot)
    {
        if (unitRoot == null)
            return;

        ForceReleaseGrabbedObjects(unitRoot);

        if (!cache.TryGetValue(unitRoot, out List<ItemData> list))
        {
            Debug.LogWarning("[CUnitResetManager] Cache yok: " + unitRoot.name);
            return;
        }

        foreach (ItemData item in list)
        {
            if (item == null || item.t == null)
                continue;

            if (item.rb != null)
            {
                item.rb.linearVelocity = Vector3.zero;
                item.rb.angularVelocity = Vector3.zero;
            }

            item.t.localPosition = item.localPos;
            item.t.localRotation = item.localRot;
            item.t.localScale = item.localScale;
        }

        Physics.SyncTransforms();

        Debug.Log("[CUnitResetManager] Resetlendi: " + unitRoot.name);
    }

    private void ForceReleaseGrabbedObjects(Transform unitRoot)
    {
        XRGrabInteractable[] grabs = unitRoot.GetComponentsInChildren<XRGrabInteractable>(true);

        foreach (XRGrabInteractable grab in grabs)
        {
            if (grab == null || !grab.enabled)
                continue;

            grab.enabled = false;
            grab.enabled = true;
        }
    }
}