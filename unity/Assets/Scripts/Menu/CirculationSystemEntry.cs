using UnityEngine;

public class CirculationSystemEntry : MonoBehaviour
{
    [Header("Heart")]
    [SerializeField] private GameObject heartInnerPanel;
    [SerializeField] private GameObject heartOuterPanel;

    [Header("Arteries")]
    [SerializeField] private GameObject upperExtremityArteriesPanel;
    [SerializeField] private GameObject abdominalAortaBranchesPanel;
    [SerializeField] private GameObject lowerExtremityArteriesPanel;
    [SerializeField] private GameObject palpableArteriesPanel;

    [Header("Veins")]
    [SerializeField] private GameObject upperExtremityVeinsPanel;
    [SerializeField] private GameObject lowerExtremityVeinsPanel;

    [Header("Circulation Types")]
    [SerializeField] private GameObject systemicCirculationPanel;
    [SerializeField] private GameObject pulmonaryCirculationPanel;

    [Header("Optional Default")]
    [SerializeField] private GameObject defaultPanel;

    private void Start()
    {
        HideAll();

        switch (NavigationState.SelectedCirculationSubUnit)
        {
            case CirculationSubUnit.HeartInnerStructure:
                ShowOnly(heartInnerPanel);
                break;

            case CirculationSubUnit.HeartOuterStructure:
                ShowOnly(heartOuterPanel);
                break;

            case CirculationSubUnit.UpperExtremityArteries:
                ShowOnly(upperExtremityArteriesPanel);
                break;

            case CirculationSubUnit.AbdominalAortaBranches:
                ShowOnly(abdominalAortaBranchesPanel);
                break;

            case CirculationSubUnit.LowerExtremityArteries:
                ShowOnly(lowerExtremityArteriesPanel);
                break;

            case CirculationSubUnit.PalpableArteries:
                ShowOnly(palpableArteriesPanel);
                break;

            case CirculationSubUnit.UpperExtremityVeins:
                ShowOnly(upperExtremityVeinsPanel);
                break;

            case CirculationSubUnit.LowerExtremityVeins:
                ShowOnly(lowerExtremityVeinsPanel);
                break;

            case CirculationSubUnit.SystemicCirculation:
                ShowOnly(systemicCirculationPanel);
                break;

            case CirculationSubUnit.PulmonaryCirculation:
                ShowOnly(pulmonaryCirculationPanel);
                break;

            default:
                ShowOnly(defaultPanel);
                Debug.LogWarning("[CirculationSystemEntry] No circulation subunit selected. Default panel opened.");
                break;
        }
    }

    private void HideAll()
    {
        SetActiveIfNotNull(heartInnerPanel, false);
        SetActiveIfNotNull(heartOuterPanel, false);

        SetActiveIfNotNull(upperExtremityArteriesPanel, false);
        SetActiveIfNotNull(abdominalAortaBranchesPanel, false);
        SetActiveIfNotNull(lowerExtremityArteriesPanel, false);
        SetActiveIfNotNull(palpableArteriesPanel, false);

        SetActiveIfNotNull(upperExtremityVeinsPanel, false);
        SetActiveIfNotNull(lowerExtremityVeinsPanel, false);

        SetActiveIfNotNull(systemicCirculationPanel, false);
        SetActiveIfNotNull(pulmonaryCirculationPanel, false);

        SetActiveIfNotNull(defaultPanel, false);
    }

    private void ShowOnly(GameObject target)
    {
        if (target != null)
        {
            target.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[CirculationSystemEntry] Target panel is null.");
        }
    }

    private void SetActiveIfNotNull(GameObject obj, bool state)
    {
        if (obj != null)
        {
            obj.SetActive(state);
        }
    }
}