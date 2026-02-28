using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class UnitResetManager : MonoBehaviour
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

    [Header("Units Parent (all units under this)")]
    public Transform unitsRoot; // hepsi bunun altında

    [Header("Reset Input")]
    public InputActionReference resetAction;   // Quest tuşu
    public KeyCode debugKey = KeyCode.R;       // Editor test

    // Her ünite root'u için cache
    private readonly Dictionary<Transform, List<ItemData>> _cache = new();

    private void Awake()
    {
        if (unitsRoot == null) unitsRoot = this.transform;

        // unitsRoot altındaki her "ünite root" için başlangıç snapshot al
        for (int i = 0; i < unitsRoot.childCount; i++)
        {
            var unit = unitsRoot.GetChild(i);

            // ünite root’u dahil tüm child transformları kaydedelim
            var list = new List<ItemData>();
            var transforms = unit.GetComponentsInChildren<Transform>(includeInactive: true);

            foreach (var tr in transforms)
            {
                var rb = tr.GetComponent<Rigidbody>();
                list.Add(new ItemData
                {
                    t = tr,
                    localPos = tr.localPosition,
                    localRot = tr.localRotation,
                    localScale = tr.localScale,
                    rb = rb
                });
            }

            _cache[unit] = list;
        }
    }

    private void OnEnable()
    {
        if (resetAction?.action != null)
        {
            resetAction.action.performed += OnResetPerformed;
            resetAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (resetAction?.action != null)
        {
            resetAction.action.performed -= OnResetPerformed;
            resetAction.action.Disable();
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (debugKey != KeyCode.None && Input.GetKeyDown(debugKey))
            ResetActiveUnit();
#endif
    }

    private void OnResetPerformed(InputAction.CallbackContext ctx)
    {
        ResetActiveUnit();
    }

    // unitsRoot altında aktif olan ilk üniteyi bulup resetler
    public void ResetActiveUnit()
    {
        if (unitsRoot == null) return;

        Transform activeUnit = null;
        for (int i = 0; i < unitsRoot.childCount; i++)
        {
            var unit = unitsRoot.GetChild(i);
            if (unit.gameObject.activeInHierarchy)
            {
                activeUnit = unit;
                break;
            }
        }

        if (activeUnit == null)
        {
            Debug.LogWarning("Reset: Aktif ünite bulunamadı.");
            return;
        }

        ResetUnit(activeUnit);
    }

    private void ResetUnit(Transform unitRoot)
    {
        // 1) Eğer XRGrabInteractable tutuluysa, önce disable/enable ile bırakmayı zorlayabiliriz (pratik hack)
        // (En sağlamı interactionManager üzerinden select exit, ama hızlı çözüm bu.)
        var grabs = unitRoot.GetComponentsInChildren<XRGrabInteractable>(includeInactive: true);
        foreach (var g in grabs)
        {
            if (!g.enabled) continue;
            g.enabled = false;
            g.enabled = true;
        }

        // 2) Velocity sıfırla + local transformları başlangıca al
        if (_cache.TryGetValue(unitRoot, out var list))
        {
            foreach (var item in list)
            {
                if (item.rb != null)
                {
                    item.rb.linearVelocity = Vector3.zero;
                    item.rb.angularVelocity = Vector3.zero;
                }

                item.t.localPosition = item.localPos;
                item.t.localRotation = item.localRot;
                item.t.localScale = item.localScale;
            }
        }
        else
        {
            Debug.LogWarning($"Reset: Cache yok -> {unitRoot.name}");
        }
    }
}