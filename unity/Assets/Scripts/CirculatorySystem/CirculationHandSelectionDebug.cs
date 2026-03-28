using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CirculationHandSelectionDebug : MonoBehaviour
{
    public XRBaseInteractor rightInteractor;
    public XRBaseInteractor leftInteractor;

    private float _t;

    private void Update()
    {
        _t += Time.deltaTime;
        if (_t < 0.5f) return;
        _t = 0f;

        if (rightInteractor != null)
            Debug.Log("[CIRC DBG] RIGHT " + rightInteractor.name +
                      " hasSelection=" + rightInteractor.hasSelection +
                      " first=" + (rightInteractor.hasSelection ? rightInteractor.firstInteractableSelected?.transform.name : ""));

        if (leftInteractor != null)
            Debug.Log("[CIRC DBG] LEFT " + leftInteractor.name +
                      " hasSelection=" + leftInteractor.hasSelection +
                      " first=" + (leftInteractor.hasSelection ? leftInteractor.firstInteractableSelected?.transform.name : ""));
    }
}