using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TBTK;

public class FactoryUnitDetailUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FactoryManager factoryManager;
    [SerializeField] private FactoryListUI listUI;

    [Header("UI")]
    [SerializeField] private TMP_Text labelName;
    [SerializeField] private TMP_Text labelRarity;
    [SerializeField] private TMP_Text labelData;
    [SerializeField] private TMP_Text labelOwned;
    [SerializeField] private TMP_Text labelDescription;
    [SerializeField] private TMP_Text labelStatus;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button manufactureButton;

    [Header("Manufacture")]
    [SerializeField] private int quantity = 1;

    private int currentUnitId = -1;

    private void Reset()
    {
        factoryManager = FindFirstObjectByTypeCompat<FactoryManager>();
    }

    private void Awake()
    {
        RefreshReferences();

        if (manufactureButton != null)
            manufactureButton.onClick.AddListener(ManufactureSelected);
    }

    private void OnEnable()
    {
        RefreshReferences();
        Refresh();
    }

    public void ShowUnit(int unitId)
    {
        currentUnitId = unitId;
        Refresh();
    }

    public void Refresh()
    {
        RefreshReferences();

        if (currentUnitId < 0)
        {
            ClearDisplay();
            return;
        }

        if (factoryManager == null)
        {
            SetStatus("Missing FactoryManager.");
            return;
        }

        Unit unit = factoryManager.GetUnitDefinition(currentUnitId);
        if (unit == null)
        {
            ClearDisplay();
            SetStatus("Unit not found: " + currentUnitId);
            return;
        }

        int data = factoryManager.GetCurrentData(currentUnitId);
        int costPerUnit = factoryManager.GetManufactureCost(currentUnitId);
        int totalCost = costPerUnit * Mathf.Max(1, quantity);
        int owned = factoryManager.GetOwnedCount(currentUnitId);
        bool canManufacture = data >= totalCost;

        string displayName = !string.IsNullOrEmpty(unit.itemName) ? unit.itemName : unit.gameObject.name;

        if (labelName != null)
            labelName.text = displayName;

        if (labelRarity != null)
            labelRarity.text = "Rarity: " + unit.rarity;

        if (labelData != null)
            labelData.text = "Data: " + data + " / " + totalCost;

        if (labelOwned != null)
            labelOwned.text = "Owned: " + owned;

        if (labelDescription != null)
            labelDescription.text = unit.desp;

        if (iconImage != null)
        {
            iconImage.sprite = unit.icon;
            iconImage.enabled = unit.icon != null;
        }

        if (manufactureButton != null)
            manufactureButton.interactable = canManufacture;

        if (labelStatus != null)
            labelStatus.text = canManufacture ? "Ready to manufacture" : "Need " + Mathf.Max(0, totalCost - data) + " more data";
    }

    public void ManufactureSelected()
    {
        if (currentUnitId < 0 || factoryManager == null)
            return;

        bool success = factoryManager.TryManufacture(currentUnitId, quantity);

        if (success)
            SetStatus("Manufactured unit " + currentUnitId);
        else
            SetStatus("Manufacture failed");

        Refresh();

        if (listUI != null)
            listUI.RefreshAll();
    }

    public void SetQuantity(int newQuantity)
    {
        quantity = Mathf.Clamp(newQuantity, 1, 99);
        Refresh();
    }

    private void ClearDisplay()
    {
        if (labelName != null) labelName.text = "";
        if (labelRarity != null) labelRarity.text = "";
        if (labelData != null) labelData.text = "";
        if (labelOwned != null) labelOwned.text = "";
        if (labelDescription != null) labelDescription.text = "";
        if (labelStatus != null) labelStatus.text = "";
        if (iconImage != null) iconImage.enabled = false;
        if (manufactureButton != null) manufactureButton.interactable = false;
    }

    private void SetStatus(string message)
    {
        if (labelStatus != null)
            labelStatus.text = message;
    }

    private void RefreshReferences()
    {
        if (factoryManager == null)
            factoryManager = FindFirstObjectByTypeCompat<FactoryManager>();
    }

    private T FindFirstObjectByTypeCompat<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
