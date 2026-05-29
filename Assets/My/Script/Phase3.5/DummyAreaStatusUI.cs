using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DummyAreaStatusUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Area")]
    [SerializeField] private int areaId = 0;
    [SerializeField] private string defaultAreaName = "Area";

    [Header("Button UI")]
    [Tooltip("Optional old label. It will no longer be shown on hover if Use Tooltip Panel is enabled.")]
    [SerializeField] private TMP_Text label;

    [SerializeField] private Button selectButton;
    [SerializeField] private Image FactionLogo;
    [SerializeField] private Sprite NaturalAreaLogo;
    [SerializeField] private Color NaturalAreaColor = Color.white;

    [Header("Tooltip Panel")]
    [SerializeField] private bool useTooltipPanel = true;

    [Tooltip("Use ONE shared tooltip panel object for all area buttons.")]
    [SerializeField] private RectTransform tooltipPanel;

    [Tooltip("Text inside the shared tooltip panel.")]
    [SerializeField] private TMP_Text tooltipLabel;

    [Tooltip("Canvas that contains the tooltip panel. If empty, it will auto-find from tooltipPanel.")]
    [SerializeField] private Canvas tooltipCanvas;

    [Tooltip("Tooltip offset from this area button.")]
    [SerializeField] private Vector2 tooltipOffset = new Vector2(20f, 20f);

    [Tooltip("If true, tooltip follows the mouse. If false, tooltip appears near the button.")]
    [SerializeField] private bool followMouse = false;

    [Tooltip("Keep tooltip inside canvas rect.")]
    [SerializeField] private bool clampTooltipToCanvas = true;

    [Header("Sound")]
    //[SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip sfxSound;

    [Header("References")]
    [SerializeField] private DummyMapSelectorManager selectorManager;
    [SerializeField] private AreaMapGameJoltSyncController mapSyncController;
    [SerializeField] private DummyAreaDatabase areaDatabase;
    [SerializeField] private FactionStarterDatabase factionDatabase;
    [SerializeField] private MapSFX SoundManager;


    [Header("Refresh")]
    [SerializeField] private bool refreshEverySecond = true;

    private float nextRefreshTime = 0f;
    private bool isHovering = false;

    private void Reset()
    {
        selectorManager = FindFirstObjectByTypeCompat<DummyMapSelectorManager>();

        if (selectButton == null)
            selectButton = GetComponent<Button>();

        if (FactionLogo == null)
            FactionLogo = GetComponent<Image>();
    }

    private void Awake()
    {
        RefreshReferences();

        if (label != null && useTooltipPanel)
            label.gameObject.SetActive(false);

        HideTooltip();
    }

    private void OnEnable()
    {
        RefreshReferences();
        RefreshDisplay();

        if (label != null && useTooltipPanel)
            label.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (isHovering)
            HideTooltip();

        isHovering = false;
    }

    private void Update()
    {
        if (refreshEverySecond && Time.time >= nextRefreshTime)
        {
            nextRefreshTime = Time.time + 1f;
            RefreshDisplay();
        }

        if (isHovering && useTooltipPanel && followMouse)
            PositionTooltip(null);
    }

    public void SetAreaId(int newAreaId)
    {
        areaId = newAreaId;
        RefreshDisplay();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;

        RefreshDisplay();

        if (useTooltipPanel)
            ShowTooltip(eventData);
        else if (label != null)
            label.gameObject.SetActive(true);

        if (sfxSound != null)
            SoundManager.sfx.PlayOneShot(sfxSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;

        if (useTooltipPanel)
            HideTooltip();
        else if (label != null)
            label.gameObject.SetActive(false);
    }

    public void OnSelectAreaButton()
    {
        RefreshReferences();

        if (mapSyncController != null && mapSyncController.IsSyncing)
        {
            Debug.LogWarning("[DummyAreaStatusUI] Cannot select area while map data is syncing.");
            return;
        }

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

        string tooltipText = BuildTooltipText();
        bool canSelect = CanSelect();

        if (mapSyncController != null && mapSyncController.IsSyncing)
            canSelect = false;

        if (label != null && !useTooltipPanel)
            label.text = tooltipText;

        if (isHovering && useTooltipPanel && tooltipLabel != null)
            tooltipLabel.text = tooltipText;

        if (selectButton != null)
            selectButton.interactable = canSelect;

        UpdateFactionLogo();
    }

    private void RefreshReferences()
    {
        if (selectorManager == null)
            selectorManager = FindFirstObjectByTypeCompat<DummyMapSelectorManager>();

        if (mapSyncController == null && AreaMapGameJoltSyncController.Instance != null)
            mapSyncController = AreaMapGameJoltSyncController.Instance;

        if (mapSyncController == null)
            mapSyncController = FindFirstObjectByTypeCompat<AreaMapGameJoltSyncController>();

        if (selectButton == null)
            selectButton = GetComponent<Button>();

        if (tooltipCanvas == null && tooltipPanel != null)
            tooltipCanvas = tooltipPanel.GetComponentInParent<Canvas>();
        if(SoundManager==null)
            SoundManager = MapSFX.Instance;
    }

    private string BuildTooltipText()
    {
        string areaName = GetAreaName();
        string status = GetStatusText();

        return "<color=red><b>" + areaName + "</b></color>\n" + status;
    }

    private void ShowTooltip(PointerEventData eventData)
    {
        if (tooltipPanel == null || tooltipLabel == null)
            return;

        tooltipLabel.text = BuildTooltipText();
        tooltipPanel.gameObject.SetActive(true);
        tooltipPanel.SetAsLastSibling();

        PositionTooltip(eventData);
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.gameObject.SetActive(false);
    }

    private void PositionTooltip(PointerEventData eventData)
    {
        if (tooltipPanel == null)
            return;

        RefreshReferences();

        if (tooltipCanvas == null)
            return;

        RectTransform canvasRect = tooltipCanvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Camera uiCamera = tooltipCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : tooltipCanvas.worldCamera;

        Vector2 screenPosition;

        if (followMouse)
        {
            screenPosition = Input.mousePosition;
        }
        else
        {
            RectTransform sourceRect = selectButton != null
                ? selectButton.transform as RectTransform
                : transform as RectTransform;

            if (sourceRect != null)
                screenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, sourceRect.position);
            else
                screenPosition = Input.mousePosition;
        }

        screenPosition += tooltipOffset;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out localPoint);

        if (clampTooltipToCanvas)
            localPoint = ClampToCanvas(canvasRect, tooltipPanel, localPoint);

        tooltipPanel.anchoredPosition = localPoint;
    }

    private Vector2 ClampToCanvas(RectTransform canvasRect, RectTransform panelRect, Vector2 localPoint)
    {
        Vector2 canvasSize = canvasRect.rect.size;
        Vector2 panelSize = panelRect.rect.size;

        float minX = -canvasSize.x * 0.5f + panelSize.x * panelRect.pivot.x;
        float maxX = canvasSize.x * 0.5f - panelSize.x * (1f - panelRect.pivot.x);

        float minY = -canvasSize.y * 0.5f + panelSize.y * panelRect.pivot.y;
        float maxY = canvasSize.y * 0.5f - panelSize.y * (1f - panelRect.pivot.y);

        localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
        localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

        return localPoint;
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
        Color colorToUse = NaturalAreaColor;

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
        FactionLogo.enabled = logoToUse != null;

        if (selectButton != null && selectButton.targetGraphic != null)
            selectButton.targetGraphic.color = colorToUse;
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
            status += "\nDefenders: " + state.defenderUnitIds.Count;

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
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>(true);
#endif
    }
}
