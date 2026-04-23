using TMPro;
using UnityEngine;

public class VeinNamePresenter : MonoBehaviour
{
    [SerializeField] private CirculationSystemDatabase database;
    [SerializeField] private TMP_Text nameText;

    public void ShowName(GameObject target)
{
    Debug.Log("ShowName target: " + (target ? target.name : "NULL"));

    if (target == null)
    {
        nameText.text = "";
        return;
    }

    VeinIdentity identity = target.GetComponent<VeinIdentity>();
    if (identity == null)
        identity = target.GetComponentInParent<VeinIdentity>();

    Debug.Log(identity != null
        ? $"Identity found -> id: {identity.id}, obj: {identity.gameObject.name}"
        : "Identity NOT found");

    if (identity != null)
    {
        if (database != null && database.TryGetTitle(identity.id, out string dbTitle))
        {
            Debug.Log("DB title found: " + dbTitle);
            nameText.text = dbTitle;
            return;
        }

        Debug.Log("DB title not found. Fallback: " + identity.fallbackDisplayName);

        if (!string.IsNullOrWhiteSpace(identity.fallbackDisplayName))
        {
            nameText.text = identity.fallbackDisplayName;
            return;
        }
    }

    Debug.Log("Falling back to target.name: " + target.name);
    nameText.text = target.name;
}

    
}