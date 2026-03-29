using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CFreeExploreHeldNameUI : MonoBehaviour
{
    [Header("XR Interactors (Select/Grab yapan interactor'ları ver)")]
    [SerializeField] private XRBaseInteractor rightInteractor;
    [SerializeField] private XRBaseInteractor leftInteractor;

    [Header("Input")]
    [SerializeField] private InputActionReference toggleAction;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text nameText;

    [Header("Data")]
    [SerializeField] private CirculationSystemDatabase circulationDb;

    private bool isVisible = true;

    private void OnEnable()
    {
        if (toggleAction != null && toggleAction.action != null)
        {
            toggleAction.action.Enable();
            toggleAction.action.performed += OnTogglePressed;
        }

        RefreshUI();
    }

    private void OnDisable()
    {
        if (toggleAction != null && toggleAction.action != null)
        {
            toggleAction.action.performed -= OnTogglePressed;
            toggleAction.action.Disable();
        }
    }

    private void Update()
    {
        if (isVisible)
            RefreshUI();
    }

    private void OnTogglePressed(InputAction.CallbackContext ctx)
    {
        isVisible = !isVisible;

        if (panelRoot != null)
            panelRoot.SetActive(isVisible);

        if (isVisible)
            RefreshUI();
    }

    private void RefreshUI()
    {
        GameObject target = GetCurrentSelectedObject();

        if (target == null)
        {
            if (nameText != null)
                nameText.text = "";
            return;
        }

        if (nameText != null)
            nameText.text = ResolveDisplayName(target);
    }

    private GameObject GetCurrentSelectedObject()
    {
        GameObject leftObj = GetSelectedObjectFromInteractor(leftInteractor);
        if (leftObj != null)
            return leftObj;

        GameObject rightObj = GetSelectedObjectFromInteractor(rightInteractor);
        if (rightObj != null)
            return rightObj;

        return null;
    }

    private GameObject GetSelectedObjectFromInteractor(XRBaseInteractor interactor)
    {
        if (interactor == null)
            return null;

        var interactables = interactor.interactablesSelected;
        if (interactables == null || interactables.Count == 0)
            return null;

        var first = interactables[0];
        if (first == null)
            return null;

        Component comp = first as Component;
        return comp != null ? comp.gameObject : null;
    }

    private string ResolveDisplayName(GameObject target)
    {
        if (target == null)
            return "";

        VeinIdentity identity = target.GetComponent<VeinIdentity>();
        if (identity == null)
            identity = target.GetComponentInParent<VeinIdentity>();

        if (identity != null)
        {
            if (circulationDb != null &&
                !string.IsNullOrWhiteSpace(identity.id) &&
                circulationDb.TryGetTitle(identity.id, out string dbTitle))
            {
                return dbTitle;
            }

            if (!string.IsNullOrWhiteSpace(identity.fallbackDisplayName))
            {
                return identity.fallbackDisplayName;
            }
        }

        return target.name;
    }
}