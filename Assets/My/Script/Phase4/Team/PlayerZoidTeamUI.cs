using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TBTK;

public class PlayerZoidTeamUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerZoidTeamManager teamManager;
    [SerializeField] private UnitProgressManager progressManager;
    private MapSFX soundManager;
    public AudioClip saveSfx;
    public AudioClip moveSfxSucces;
    public AudioClip moveSfxFailed;

    [Header("Left Column - Available Owned Zoids")]
    [SerializeField] private Transform availableParent;
    [SerializeField] private PlayerZoidTeamUnitItemUI availableItemPrefab;

    [Header("Right Column - Current Team")]
    [SerializeField] private Transform teamParent;
    [SerializeField] private PlayerZoidTeamUnitItemUI teamItemPrefab;

    [Header("Team Buttons")]
    [SerializeField] private Button team1Button;
    [SerializeField] private Button team2Button;
    [SerializeField] private Button team3Button;
    [SerializeField] private TMP_Text currentTeamLabel;

    [Header("Action Buttons")]
    [SerializeField] private Button addButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private Button clearTeamButton;
    [SerializeField] private Button saveButton;

    [Header("Status")]
    [SerializeField] private TMP_Text statusLabel;

    [Header("Game Jolt Cloud Sync")]
    [SerializeField] private ZoidsGameJoltCloudSaveManager cloudSaveManager;

    [Tooltip("Panel that blocks the canvas while downloading/uploading. Put this panel above all Team Manager UI.")]
    [SerializeField] private GameObject canvasBlockerPanel;

    [Tooltip("Optional TMP text on the blocker panel.")]
    [SerializeField] private TMP_Text blockerStatusLabel;

    [SerializeField] private bool downloadLatestCloudSaveOnEnable = true;
    [SerializeField] private bool uploadCloudSaveAfterSaveButton = true;
    [SerializeField] private bool uploadCloudSaveAfterClearTeam = true;
    [SerializeField] private bool uploadCloudSaveAfterClearAllTeams = true;

    [Header("Auto Resize")]
    [SerializeField] private bool autoResizeParents = true;
    [SerializeField] private float buttonSpacing = 10f;
    [SerializeField] private float fallbackButtonHeight = 60f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private int activeTeamIndex = 0;

    private readonly List<int> availableUnitIds = new List<int>();
    private readonly List<PlayerZoidTeamUnitItemUI> availableItems = new List<PlayerZoidTeamUnitItemUI>();
    private readonly List<PlayerZoidTeamUnitItemUI> teamItems = new List<PlayerZoidTeamUnitItemUI>();

    private PlayerZoidTeamUnitItemUI selectedAvailableItem;
    private PlayerZoidTeamUnitItemUI selectedTeamItem;

    private bool isCloudSyncing = false;

    private void Reset()
    {
        RefreshRuntimeReferences();
    }

    private void Awake()
    {
        RefreshRuntimeReferences();
        HookButtons();
    }

    public void Start()
    {
        soundManager = MapSFX.Instance;
    }

    private void OnEnable()
    {
        RefreshRuntimeReferences();
        SubscribeCloudEvents();

        if (downloadLatestCloudSaveOnEnable)
            DownloadLatestDataFromGameJolt();
        else
            RefreshUI();
    }

    private void OnDisable()
    {
        UnsubscribeCloudEvents();
        SetCanvasBlocker(false, "");
    }

    private void HookButtons()
    {
        if (team1Button != null) team1Button.onClick.AddListener(delegate { SelectTeam(0); });
        if (team2Button != null) team2Button.onClick.AddListener(delegate { SelectTeam(1); });
        if (team3Button != null) team3Button.onClick.AddListener(delegate { SelectTeam(2); });

        if (addButton != null) addButton.onClick.AddListener(AddSelectedAvailableToTeam);
        if (removeButton != null) removeButton.onClick.AddListener(RemoveSelectedFromTeam);
        if (clearTeamButton != null) clearTeamButton.onClick.AddListener(ClearActiveTeam);
        if (saveButton != null) saveButton.onClick.AddListener(SaveTeams);
    }

    public void RefreshRuntimeReferences()
    {
        if (teamManager == null && PlayerZoidTeamManager.Instance != null)
            teamManager = PlayerZoidTeamManager.Instance;

        if (progressManager == null && UnitProgressManager.Instance != null)
            progressManager = UnitProgressManager.Instance;

        if (cloudSaveManager == null && ZoidsGameJoltCloudSaveManager.Instance != null)
            cloudSaveManager = ZoidsGameJoltCloudSaveManager.Instance;

        if (teamManager == null)
            teamManager = FindFirstObjectByTypeCompat<PlayerZoidTeamManager>();

        if (progressManager == null)
            progressManager = FindFirstObjectByTypeCompat<UnitProgressManager>();

        if (cloudSaveManager == null)
            cloudSaveManager = FindFirstObjectByTypeCompat<ZoidsGameJoltCloudSaveManager>();

        if (debugLog)
        {
            if (teamManager == null)
                Debug.LogWarning("[PlayerZoidTeamUI] PlayerZoidTeamManager not found.");

            if (progressManager == null)
                Debug.LogWarning("[PlayerZoidTeamUI] UnitProgressManager not found.");

            if (cloudSaveManager == null)
                Debug.LogWarning("[PlayerZoidTeamUI] ZoidsGameJoltCloudSaveManager not found. Cloud team sync disabled.");
        }
    }

    public void SelectTeam(int teamIndex)
    {
        activeTeamIndex = Mathf.Max(0, teamIndex);
        ClearSelection();
        RefreshUI();
    }

    public void SelectAvailableListItem(PlayerZoidTeamUnitItemUI item)
    {
        if (selectedAvailableItem != null)
            selectedAvailableItem.SetSelected(false);

        selectedAvailableItem = item;
        selectedTeamItem = null;

        if (selectedAvailableItem != null)
            selectedAvailableItem.SetSelected(true);

        for (int i = 0; i < teamItems.Count; i++)
            if (teamItems[i] != null)
                teamItems[i].SetSelected(false);

        RefreshActionButtons();
    }

    public void SelectTeamListItem(PlayerZoidTeamUnitItemUI item)
    {
        if (selectedTeamItem != null)
            selectedTeamItem.SetSelected(false);

        selectedTeamItem = item;
        selectedAvailableItem = null;

        if (selectedTeamItem != null)
            selectedTeamItem.SetSelected(true);

        for (int i = 0; i < availableItems.Count; i++)
            if (availableItems[i] != null)
                availableItems[i].SetSelected(false);

        RefreshActionButtons();
    }

    public void AddSelectedAvailableToTeam()
    {
        if (isCloudSyncing)
        {
            SetStatus("Please wait for cloud sync to finish.");
            return;
        }

        if (teamManager == null || selectedAvailableItem == null)
            return;

        int unitId = selectedAvailableItem.UnitId;
        bool success = teamManager.AddUnitToTeam(activeTeamIndex, unitId);

        if (success)
        {
            SetStatus("Added unit " + unitId + " to Team " + (activeTeamIndex + 1));
            ClearSelection();
            RefreshUI();
            soundManager.sfx.PlayOneShot(moveSfxSucces);
        }
        else
        {
            SetStatus("Failed to add unit " + unitId);
            soundManager.sfx.PlayOneShot(moveSfxFailed);
        }
    }

    public void RemoveSelectedFromTeam()
    {
        if (isCloudSyncing)
        {
            SetStatus("Please wait for cloud sync to finish.");
            return;
        }

        if (teamManager == null)
        {
            SetStatus("Cannot remove. Team manager missing.");
            return;
        }

        if (selectedTeamItem == null)
        {
            SetStatus("Select a Zoid from the team list first.");
            return;
        }

        int slotIndex = selectedTeamItem.ListIndex;
        int unitId = selectedTeamItem.UnitId;
        string unitName = GetUnitDisplayName(unitId);

        bool success = teamManager.RemoveUnitFromTeamAt(activeTeamIndex, slotIndex);

        if (success)
        {
            SetStatus("Removed " + unitName + " from Team " + (activeTeamIndex + 1));
            ClearSelection();
            RefreshUI();
            soundManager.sfx.PlayOneShot(moveSfxSucces);
        }
        else
        {
            SetStatus("Failed to remove " + unitName);
            soundManager.sfx.PlayOneShot(moveSfxFailed);
        }
    }

    public void ClearActiveTeam()
    {
        if (isCloudSyncing)
        {
            SetStatus("Please wait for cloud sync to finish.");
            return;
        }

        if (teamManager == null)
            return;

        teamManager.ClearTeam(activeTeamIndex);
        SetStatus("Cleared Team " + (activeTeamIndex + 1));
        ClearSelection();
        RefreshUI();

        if (uploadCloudSaveAfterClearTeam)
            UploadTeamDataToGameJolt("Uploading cleared team...");
    }

    public void SaveTeams()
    {
        if (isCloudSyncing)
        {
            SetStatus("Please wait for cloud sync to finish.");
            return;
        }

        if (teamManager == null)
            return;

        teamManager.SaveTeams();
        SetStatus("Teams saved.");

        soundManager.sfx.PlayOneShot(saveSfx);

        if (uploadCloudSaveAfterSaveButton)
            UploadTeamDataToGameJolt("Uploading teams...");
    }

    public void RefreshUI()
    {
        RefreshRuntimeReferences();

        BuildAvailableList();
        BuildTeamList();

        RefreshHeader();
        RefreshActionButtons();
    }

    private void BuildAvailableList()
    {
        ClearAvailableItems();
        availableUnitIds.Clear();

        if (progressManager == null || teamManager == null)
            return;

        List<int> currentTeamIds = teamManager.GetTeamUnitIds(activeTeamIndex);
        Dictionary<int, int> usedInTeam = BuildCountMap(currentTeamIds);

        List<IntValueEntry> ownedEntries = progressManager.GetOwnedUnitEntries();

        for (int i = 0; i < ownedEntries.Count; i++)
        {
            IntValueEntry entry = ownedEntries[i];
            if (entry == null || entry.value <= 0)
                continue;

            int used = usedInTeam.ContainsKey(entry.id) ? usedInTeam[entry.id] : 0;
            int availableCount = Mathf.Max(0, entry.value - used);

            for (int c = 0; c < availableCount; c++)
                availableUnitIds.Add(entry.id);
        }

        for (int i = 0; i < availableUnitIds.Count; i++)
        {
            if (availableItemPrefab == null || availableParent == null)
                break;

            PlayerZoidTeamUnitItemUI item = Instantiate(availableItemPrefab, availableParent);
            item.gameObject.SetActive(true);
            item.Setup(this, availableUnitIds[i], i, false);
            availableItems.Add(item);
        }

        ResizeParent(availableParent, availableItemPrefab != null ? availableItemPrefab.GetComponent<RectTransform>() : null, availableItems.Count);
    }

    private void BuildTeamList()
    {
        ClearTeamItems();

        if (teamManager == null)
            return;

        List<int> teamIds = teamManager.GetTeamUnitIds(activeTeamIndex);

        for (int i = 0; i < teamIds.Count; i++)
        {
            if (teamItemPrefab == null || teamParent == null)
                break;

            PlayerZoidTeamUnitItemUI item = Instantiate(teamItemPrefab, teamParent);
            item.gameObject.SetActive(true);
            item.Setup(this, teamIds[i], i, true);
            teamItems.Add(item);
        }

        ResizeParent(teamParent, teamItemPrefab != null ? teamItemPrefab.GetComponent<RectTransform>() : null, teamItems.Count);
    }

    private void ClearAvailableItems()
    {
        for (int i = 0; i < availableItems.Count; i++)
            if (availableItems[i] != null)
                Destroy(availableItems[i].gameObject);

        availableItems.Clear();
    }

    private void ClearTeamItems()
    {
        for (int i = 0; i < teamItems.Count; i++)
            if (teamItems[i] != null)
                Destroy(teamItems[i].gameObject);

        teamItems.Clear();
    }

    private void ClearSelection()
    {
        selectedAvailableItem = null;
        selectedTeamItem = null;
    }

    private void RefreshHeader()
    {
        if (teamManager == null)
            return;

        PlayerZoidTeamData team = teamManager.GetTeam(activeTeamIndex);
        string teamName = team != null ? team.teamName : "Team " + (activeTeamIndex + 1);

        if (currentTeamLabel != null)
            currentTeamLabel.text = teamName + " (" + (team != null ? team.Count : 0) + "/" + teamManager.MaxUnitsPerTeam + ")";
    }

    private void RefreshActionButtons()
    {
        if (addButton != null)
            addButton.interactable = !isCloudSyncing && selectedAvailableItem != null;

        if (removeButton != null)
            removeButton.interactable = !isCloudSyncing && selectedTeamItem != null;

        if (clearTeamButton != null)
            clearTeamButton.interactable = !isCloudSyncing && teamManager != null && teamManager.GetTeamUnitIds(activeTeamIndex).Count > 0;

        if (saveButton != null)
            saveButton.interactable = !isCloudSyncing && teamManager != null;

        if (team1Button != null)
            team1Button.interactable = !isCloudSyncing;

        if (team2Button != null)
            team2Button.interactable = !isCloudSyncing;

        if (team3Button != null)
            team3Button.interactable = !isCloudSyncing;
    }

    private Dictionary<int, int> BuildCountMap(List<int> unitIds)
    {
        Dictionary<int, int> map = new Dictionary<int, int>();

        if (unitIds == null)
            return map;

        for (int i = 0; i < unitIds.Count; i++)
        {
            int id = unitIds[i];
            if (!map.ContainsKey(id))
                map.Add(id, 0);

            map[id]++;
        }

        return map;
    }

    private void ResizeParent(Transform parent, RectTransform prefabRect, int itemCount)
    {
        if (!autoResizeParents || parent == null)
            return;

        RectTransform parentRect = parent as RectTransform;
        if (parentRect == null)
            parentRect = parent.GetComponent<RectTransform>();

        if (parentRect == null)
            return;

        if (itemCount <= 0)
        {
            parentRect.sizeDelta = new Vector2(parentRect.sizeDelta.x, 0f);
            return;
        }

        float buttonHeight = fallbackButtonHeight;
        if (prefabRect != null && prefabRect.rect.height > 0)
            buttonHeight = prefabRect.rect.height;

        float height = (buttonHeight + buttonSpacing) * itemCount;
        parentRect.sizeDelta = new Vector2(parentRect.sizeDelta.x, height);
    }

    private string GetUnitDisplayName(int unitId)
    {
        Unit unit = UnitDB.GetPrefab(unitId);
        if (unit == null)
            return "Unit " + unitId;

        if (!string.IsNullOrEmpty(unit.itemName))
            return unit.itemName;

        return unit.gameObject.name;
    }


    // ---------------------------------------------------------
    // Game Jolt cloud sync
    // ---------------------------------------------------------

    private void SubscribeCloudEvents()
    {
        if (cloudSaveManager == null)
            return;

        cloudSaveManager.OnDownloadFinished -= OnCloudDownloadFinished;
        cloudSaveManager.OnUploadFinished -= OnCloudUploadFinished;
        cloudSaveManager.OnPayloadApplied -= OnCloudPayloadApplied;

        cloudSaveManager.OnDownloadFinished += OnCloudDownloadFinished;
        cloudSaveManager.OnUploadFinished += OnCloudUploadFinished;
        cloudSaveManager.OnPayloadApplied += OnCloudPayloadApplied;
    }

    private void UnsubscribeCloudEvents()
    {
        if (cloudSaveManager == null)
            return;

        cloudSaveManager.OnDownloadFinished -= OnCloudDownloadFinished;
        cloudSaveManager.OnUploadFinished -= OnCloudUploadFinished;
        cloudSaveManager.OnPayloadApplied -= OnCloudPayloadApplied;
    }

    public void DownloadLatestDataFromGameJolt()
    {
        RefreshRuntimeReferences();
        SubscribeCloudEvents();

        if (cloudSaveManager == null)
        {
            SetStatus("Cloud save manager missing. Using local team data.");
            RefreshUI();
            return;
        }

        if (cloudSaveManager.IsBusy)
        {
            SetStatus("Cloud save is busy. Please wait.");
            SetCanvasBlocker(true, "Syncing...");
            return;
        }

        SetCanvasBlocker(true, "Downloading latest team data...");
        SetStatus("Downloading latest team data from Game Jolt...");

        cloudSaveManager.DownloadAndApplyCloudSave();
    }

    private void UploadTeamDataToGameJolt(string blockerMessage)
    {
        RefreshRuntimeReferences();
        SubscribeCloudEvents();

        if (cloudSaveManager == null)
        {
            SetStatus("Cloud save manager missing. Team saved locally only.");
            return;
        }

        if (cloudSaveManager.IsBusy)
        {
            SetStatus("Cloud save is busy. Try again after sync finishes.");
            SetCanvasBlocker(true, "Syncing...");
            return;
        }

        SetCanvasBlocker(true, blockerMessage);
        SetStatus(blockerMessage);

        cloudSaveManager.UploadLocalSaveToCloud();
    }

    private void OnCloudDownloadFinished(bool success, ZoidsGameJoltSavePayload payload)
    {
        // DownloadAndApplyCloudSave already applies payload and reloads managers on success.
        RefreshRuntimeReferences();

        if (teamManager != null)
            teamManager.LoadTeams();

        if (progressManager != null)
            progressManager.LoadProgress();

        ClearSelection();
        RefreshUI();

        if (success && payload != null)
            SetStatus("Latest team data downloaded.");
        else
            SetStatus("No cloud team data found. Using local team data.");

        SetCanvasBlocker(false, "");
    }

    private void OnCloudPayloadApplied(ZoidsGameJoltSavePayload payload)
    {
        RefreshRuntimeReferences();

        if (teamManager != null)
            teamManager.LoadTeams();

        if (progressManager != null)
            progressManager.LoadProgress();

        ClearSelection();
        RefreshUI();
    }

    private void OnCloudUploadFinished(bool success)
    {
        SetStatus(success ? "Teams uploaded to Game Jolt." : "Team upload failed.");
        SetCanvasBlocker(false, "");
    }

    private void SetCanvasBlocker(bool active, string message)
    {
        isCloudSyncing = active;

        if (canvasBlockerPanel != null)
            canvasBlockerPanel.SetActive(active);

        if (blockerStatusLabel != null)
            blockerStatusLabel.text = message;

        RefreshActionButtons();
    }

    private void SetStatus(string message)
    {
        if (statusLabel != null)
            statusLabel.text = message;

        if (debugLog)
            Debug.Log("[PlayerZoidTeamUI] " + message);
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
