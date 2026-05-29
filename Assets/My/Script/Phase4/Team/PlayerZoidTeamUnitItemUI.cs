using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TBTK;
using UnityEngine.EventSystems;

public class PlayerZoidTeamUnitItemUI : MonoBehaviour,IPointerClickHandler,IPointerExitHandler
{
    [Header("UI")]
     public TMP_Text labelName;
    [SerializeField] private TMP_Text labelInfo;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectedHighlight;
    [SerializeField] private Button button;

    public AudioClip fxClip;
    private MapSFX soundManager;
    

    private PlayerZoidTeamUI ownerUI;
    private int unitId = -1;
    private int listIndex = -1;
    private bool isTeamListItem = false;

    public int UnitId { get { return unitId; } }
    public int ListIndex { get { return listIndex; } }
    public bool IsTeamListItem { get { return isTeamListItem; } }

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

    public void Setup(PlayerZoidTeamUI owner, int unitId, int listIndex, bool isTeamListItem)
    {
        ownerUI = owner;
        this.unitId = unitId;
        this.listIndex = listIndex;
        this.isTeamListItem = isTeamListItem;

        Refresh();
        SetSelected(false);
    }

    public void Refresh()
    {
        Unit unit = UnitDB.GetPrefab(unitId);

        string unitName = "Unit " + unitId;
        string rarityText = "";

        if (unit != null)
        {
            unitName = !string.IsNullOrEmpty(unit.itemName) ? unit.itemName : unit.gameObject.name;
            rarityText = unit.rarity.ToString();

            if (iconImage != null)
            {
                iconImage.sprite = unit.icon;
                iconImage.enabled = unit.icon != null;
            }
        }
        else
        {
            if (iconImage != null)
                iconImage.enabled = false;
        }

        if (labelName != null)
            labelName.text = unitName;

        if (labelInfo != null)
        {
            string side = isTeamListItem ? "Team Slot " + (listIndex + 1) : "Available";
            labelInfo.text = side + (string.IsNullOrEmpty(rarityText) ? "" : " | " + rarityText);
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
            selectedHighlight.gameObject.SetActive(selected);
    }

    private void OnClick()
    {
        if (ownerUI == null)
            return;

        if (isTeamListItem)
            ownerUI.SelectTeamListItem(this);
        else
            ownerUI.SelectAvailableListItem(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        soundManager.sfx.PlayOneShot(fxClip);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        
    }
}
