using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class FreeExploreHeldNameUI : MonoBehaviour
{
    [Header("XR Interactors (Select/Grab yapan interactor'ları ver)")]
    public XRBaseInteractor rightInteractor;   // Right Controller > Near-Far Interactor (öneri)
    public XRBaseInteractor leftInteractor;    // Left Controller  > Near-Far Interactor (öneri)

    [Header("Input")]
    public InputActionReference toggleAction;  // FreeExplore/ToggleHeldName

    [Header("UI")]
    public GameObject panelRoot;               // NamePanel
    public TextMeshProUGUI nameText;           // NameText

    [Header("Data")]
    public MovementSystemDatabase movementDb;

    private bool _isOpen;

    // ✅ Panel açıkken hangi objeyi göstereceğiz? En son seçilen.
    private XRBaseInteractable _active;

    private void Start()
    {
        Debug.Log($"[HeldNameUI] Start. toggleAction={toggleAction?.action?.name} panelRoot={panelRoot?.name} nameText={nameText?.name}");
        if (panelRoot != null) panelRoot.SetActive(false);
        _isOpen = false;
    }

    private void OnEnable()
    {
        var act = toggleAction?.action;
        if (act == null)
        {
            Debug.LogWarning("[HeldNameUI] toggleAction boş! Inspector'dan FreeExplore/ToggleHeldName ver.");
        }
        else
        {
            act.performed += OnToggle;
            act.Enable();
            Debug.Log("[HeldNameUI] OnEnable -> Subscribed & Enabled action: " + act.name);
        }

        SubscribeInteractor(rightInteractor);
        SubscribeInteractor(leftInteractor);
    }

    private void OnDisable()
    {
        var act = toggleAction?.action;
        if (act != null)
        {
            act.performed -= OnToggle;
            act.Disable();
            Debug.Log("[HeldNameUI] OnDisable -> Unsubscribed & Disabled action: " + act.name);
        }

        UnsubscribeInteractor(rightInteractor);
        UnsubscribeInteractor(leftInteractor);
    }

    private void SubscribeInteractor(XRBaseInteractor interactor)
    {
        if (interactor == null) return;
        interactor.selectEntered.AddListener(OnSelectEntered);
        interactor.selectExited.AddListener(OnSelectExited);
    }

    private void UnsubscribeInteractor(XRBaseInteractor interactor)
    {
        if (interactor == null) return;
        interactor.selectEntered.RemoveListener(OnSelectEntered);
        interactor.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // ✅ XRIT sürüm uyumu için XRBaseInteractable olarak tutuyoruz
        _active = args.interactableObject as XRBaseInteractable;

        // Panel açıksa anında güncelle
        if (_isOpen) RefreshUI();
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        var exited = args.interactableObject as XRBaseInteractable;
        if (_active == exited) _active = null;

        if (_isOpen) RefreshUI();
    }

    private void OnToggle(InputAction.CallbackContext ctx)
    {
        Debug.Log("TOGGLE BASILDI!");
        Debug.Log($"[HeldNameUI] Toggle fired! phase={ctx.phase} control={ctx.control?.path} device={ctx.control?.device}");

        _isOpen = !_isOpen;

        if (panelRoot == null || nameText == null)
        {
            Debug.LogWarning("[HeldNameUI] panelRoot veya nameText atanmamış!");
            return;
        }

        panelRoot.SetActive(_isOpen);

        if (_isOpen)
        {
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        if (panelRoot == null || nameText == null) return;

        GameObject selectedGO = null;

        // 1) Önce “aktif” seçim varsa onu kullan
        if (_active != null)
        {
            selectedGO = _active.gameObject;
        }

        // 2) Aktif yoksa, ellerden selection bul (fallback)
        if (selectedGO == null)
            selectedGO = GetSelectedObject();

        // 3) UI bas
        if (selectedGO == null)
        {
            nameText.text = "Elinde bir model yok";
            return;
        }

        nameText.text = ResolveDisplayName(selectedGO);
    }

    private GameObject GetSelectedObject()
    {
        var go = GetSelectedFromInteractor(rightInteractor);
        if (go != null) return go;

        go = GetSelectedFromInteractor(leftInteractor);
        if (go != null) return go;

        return null;
    }

    private GameObject GetSelectedFromInteractor(XRBaseInteractor interactor)
    {
        if (interactor == null) return null;
        if (!interactor.hasSelection) return null;

        var selected = interactor.firstInteractableSelected;
        if (selected == null) return null;

        return selected.transform.gameObject;
    }

    private string ResolveDisplayName(GameObject selectedGO)
    {
        var identity = FindBoneIdentity(selectedGO.transform);
        if (identity != null && !string.IsNullOrWhiteSpace(identity.id) && movementDb != null)
        {
            if (movementDb.TryGetTitle(identity.id, out var title) && !string.IsNullOrWhiteSpace(title))
                return title;

            return identity.id;
        }

        return selectedGO.name;
    }

    private BoneIdentity FindBoneIdentity(Transform t)
    {
        if (t == null) return null;

        var id = t.GetComponent<BoneIdentity>();
        if (id != null) return id;

        var p = t.parent;
        while (p != null)
        {
            id = p.GetComponent<BoneIdentity>();
            if (id != null) return id;
            p = p.parent;
        }

        return t.GetComponentInChildren<BoneIdentity>(true);
    }
}