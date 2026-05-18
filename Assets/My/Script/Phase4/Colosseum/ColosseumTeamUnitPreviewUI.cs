using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TBTK;

public class ColosseumTeamUnitPreviewUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text labelName;
    [SerializeField] private Image iconImage;

    public void Setup(int unitId)
    {
        Unit unit = UnitDB.GetPrefab(unitId);

        if (unit == null)
        {
            if (labelName != null)
                labelName.text = "Missing Unit " + unitId;

            if (iconImage != null)
                iconImage.enabled = false;

            return;
        }

        string displayName = !string.IsNullOrEmpty(unit.itemName) ? unit.itemName : unit.gameObject.name;

        if (labelName != null)
            labelName.text = displayName;

        if (iconImage != null)
        {
            iconImage.sprite = unit.icon;
            iconImage.enabled = unit.icon != null;
        }
    }
}
