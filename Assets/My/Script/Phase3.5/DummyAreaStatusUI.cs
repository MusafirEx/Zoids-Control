using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DummyAreaStatusUI : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    [Header("Area")]
    [SerializeField] private int areaId = 0;
    [SerializeField] private string defaultAreaName = "Area";

    [Header("UI")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private Button selectButton;
    [SerializeField] private Image FactionLogo;
    [SerializeField] private Sprite NaturalAreaLogo;
    [SerializeField] private UnityEngine.Color NaturalAreaColor;

    [Header ("Sound")]
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip sfxSound;

    [Header("References")]
    [SerializeField] private DummyMapSelectorManager selectorManager;
    [SerializeField] private DummyAreaDatabase areaDatabase;
    [SerializeField] private FactionStarterDatabase factionDatabase;

    [Header("Refresh")]
    [SerializeField] private bool refreshEverySecond = true;

    private float nextRefreshTime = 0f;

    private void Reset()
    {
        selectorManager = FindFirstObjectByTypeCompat<DummyMapSelectorManager>();
    }

    private void Awake()
    {
        RefreshReferences();
    }

    private void OnEnable()
    {
        RefreshReferences();
        RefreshDisplay();
    }

    private void Update()
    {
        if (!refreshEverySecond)
            return;

        if (Time.time < nextRefreshTime)
            return;

        nextRefreshTime = Time.time + 1f;
        RefreshDisplay();
    }

    public void SetAreaId(int newAreaId)
    {
        areaId = newAreaId;
        RefreshDisplay();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        label.gameObject.SetActive(true);
        sfx.PlayOneShot(sfxSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        label.gameObject.SetActive(false);
    }

    public void OnSelectAreaButton()
    {
        RefreshReferences();

        if (selectorManager == null)
        {
            Debug.LogWarning("[DummyAreaStatusUI] Missing DummyMapSelectorManager.");
            return;
        }

        selectorManager.TrySelectArea(areaId);
    }

    public void RefreshDisplay()
    {
        RefreshReferences();

        string areaName = GetAreaName();
        string status = GetStatusText();
        bool canSelect = CanSelect();

        if (label != null)
        {
            label.text = "<color=red><b>" + areaName + "</b></color>" + "\n" +  status;
        }

        if (selectButton != null)
        {
            selectButton.interactable = canSelect;
        }

        UpdateFactionLogo();
    }

    private void RefreshReferences()
    {
        if (selectorManager == null)
            selectorManager = FindFirstObjectByTypeCompat<DummyMapSelectorManager>();
    }

    private string GetAreaName()
    {
        if (areaDatabase != null)
        {
            DummyAreaDefinition area = areaDatabase.GetArea(areaId);
            if (area != null && !string.IsNullOrEmpty(area.areaName))
                return area.areaName;
        }

        return defaultAreaName + " " + areaId;
    }


    private void UpdateFactionLogo()
    {
        if (FactionLogo == null)
            return;

        Sprite logoToUse = NaturalAreaLogo;
        UnityEngine.Color colorToUse = NaturalAreaColor;

        if (AreaBattleStateManager.Instance != null)
        {
            AreaBattleStateData state = AreaBattleStateManager.Instance.GetAreaState(areaId, false);

            if (state != null && state.ownerFactionId >= 0 && factionDatabase != null)
            {
                FactionStarterData faction = factionDatabase.GetFaction(state.ownerFactionId);
                if (faction != null && faction.factionLogo != null)
                {
                    logoToUse = faction.factionLogo;
                    colorToUse = faction.factionColor;
                }
            }
        }

        FactionLogo.sprite = logoToUse;
        selectButton.targetGraphic.color = colorToUse;
        FactionLogo.enabled = logoToUse != null;
    }

    private string GetStatusText()
    {
        if (AreaBattleStateManager.Instance == null)
            return "Available";

        AreaBattleStateData state = AreaBattleStateManager.Instance.GetAreaState(areaId, false);
        if (state == null)
            return "Natural / Available";

        string status = "";

        if (!string.IsNullOrEmpty(state.ownerFactionName))
            status += "Owner: " + state.ownerFactionName;
        else
            status += "Natural";

        if (state.defenderUnitIds != null && state.defenderUnitIds.Count > 0)
            status += " | Defenders: " + state.defenderUnitIds.Count;

        if (state.IsAreaLocked())
            status += "\nArea Locked: " + AreaBattleStateManager.Instance.FormatTimeSpan(state.GetAreaLockRemaining());

        if (state.IsPlayerAttemptLocked())
            status += "\nAttempt Cooldown: " + AreaBattleStateManager.Instance.FormatTimeSpan(state.GetPlayerAttemptRemaining());

        if (!state.IsAreaLocked() && !state.IsPlayerAttemptLocked())
            status += "\nAvailable";

        return status;
    }

    private bool CanSelect()
    {
        if (AreaBattleStateManager.Instance == null)
            return true;

        string reason;
        return AreaBattleStateManager.Instance.CanAttemptArea(areaId, out reason);
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
