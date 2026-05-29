using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TBTK;

public class FactoryUnitButtonUI : MonoBehaviour,IPointerEnterHandler,IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private TMP_Text labelName;
    [SerializeField] private TMP_Text labelData;
    [SerializeField] private TMP_Text labelOwned;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectedHighlight;
    [SerializeField] private Button button;

    public AudioClip fxClick;
    public AudioClip fXHover;
    private MapSFX soundManager;

    private Unit unitDefinition;
    private FactoryManager factoryManager;
    private FactoryListUI listUI;

    public int UnitId
    {
        get { return unitDefinition != null ? unitDefinition.prefabID : -1; }
    }

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    public void Start()
    {
        soundManager = MapSFX.Instance;
    }

    public void Setup(Unit unit, FactoryManager manager, FactoryListUI ownerList)
    {
        unitDefinition = unit;
        factoryManager = manager;
        listUI = ownerList;
        Refresh();
        SetSelected(false);
    }

    public void Refresh()
    {
        if (unitDefinition == null)
            return;

        string displayName = !string.IsNullOrEmpty(unitDefinition.itemName) ? unitDefinition.itemName : unitDefinition.gameObject.name;

        if (labelName != null)
            labelName.text = displayName + " [" + unitDefinition.rarity + "]";

        if (iconImage != null)
        {
            iconImage.sprite = unitDefinition.icon;
            iconImage.enabled = unitDefinition.icon != null;
        }

        int data = factoryManager != null ? factoryManager.GetCurrentData(UnitId) : 0;
        int cost = factoryManager != null ? factoryManager.GetManufactureCost(UnitId) : unitDefinition.value;
        int owned = factoryManager != null ? factoryManager.GetOwnedCount(UnitId) : 0;
        bool limitedOwned = factoryManager != null && factoryManager.IsOwnedLimited(UnitId);
        string ownedLimitLabel = factoryManager != null ? factoryManager.GetOwnedLimitLabel(UnitId) : "Unlimited";

        if (labelData != null)
            labelData.text = "Data: \n" + data + " / " + cost;

        if (labelOwned != null)
            labelOwned.text = limitedOwned ? "Owned: \n" + owned + " / " + ownedLimitLabel : "Owned: \n" + owned;
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
            selectedHighlight.gameObject.SetActive(selected);
    }

    private void OnClick()
    {
        if (unitDefinition == null || listUI == null)
            return;

        listUI.SelectUnit(UnitId);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        soundManager.sfx.PlayOneShot(fXHover);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        soundManager.sfx.PlayOneShot(fxClick);
    }
}
