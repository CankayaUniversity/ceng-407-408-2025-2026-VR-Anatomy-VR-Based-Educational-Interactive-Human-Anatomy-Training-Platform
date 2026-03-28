using TMPro;
using UnityEngine;

public class BoneNamePresenter : MonoBehaviour
{
    [SerializeField] private MovementSystemDatabase database;
    [SerializeField] private TMP_Text nameText;

    public void ShowName(GameObject target)
    {
        if (target == null)
        {
            nameText.text = "";
            return;
        }

        BoneIdentity identity = target.GetComponent<BoneIdentity>();
        if (identity == null)
            identity = target.GetComponentInParent<BoneIdentity>();

        if (identity != null)
        {
            if (database != null && database.TryGetTitle(identity.id, out string dbTitle))
            {
                nameText.text = dbTitle;
                return;
            }

            if (!string.IsNullOrWhiteSpace(identity.fallbackDisplayName))
            {
                nameText.text = identity.fallbackDisplayName;
                return;
            }
        }

        nameText.text = target.name;
    }
}