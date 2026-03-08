using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MatchingRowUI : MonoBehaviour
{
    public TMP_Text leftText;
    public TMP_Dropdown rightDropdown;

    public void Setup(string leftValue, List<string> rightOptions)
    {
        if (leftText != null)
            leftText.text = leftValue;

        if (rightDropdown != null)
        {
            rightDropdown.ClearOptions();
            rightDropdown.AddOptions(rightOptions);

            if (rightDropdown.options.Count > 0)
                rightDropdown.value = 0;

            rightDropdown.RefreshShownValue();
        }
    }

    public string GetSelectedOptionText()
    {
        if (rightDropdown == null || rightDropdown.options.Count == 0)
            return string.Empty;

        return rightDropdown.options[rightDropdown.value].text;
    }
}