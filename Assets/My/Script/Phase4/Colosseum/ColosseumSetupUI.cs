using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColosseumSetupUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerZoidTeamManager teamManager;
    [SerializeField] private ColosseumManager colosseumManager;

    [Header("Team Panel")]
    [SerializeField] private ColosseumTeamButtonUI team1Button;
    [SerializeField] private ColosseumTeamButtonUI team2Button;
    [SerializeField] private ColosseumTeamButtonUI team3Button;

    [Header("Selected Team Panel")]
    [SerializeField] private TMP_Text selectedTeamCountText;
    [SerializeField] private Transform selectedTeamUnitParent;
    [SerializeField] private ColosseumTeamUnitPreviewUI teamUnitPreviewPrefab;

    [Header("Battle Size Panel")]
    [SerializeField] private Transform battleSizeButtonParent;
    [SerializeField] private ColosseumBattleSizeButtonUI battleSizeButtonPrefab;
    [SerializeField] private int maxBattleSize = 10;

    [Header("Bottom Panel")]
    [SerializeField] private TMP_Text selectedSummaryText;
    [SerializeField] private TMP_Text roundInfoText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    [Header("Colosseum Rules")]
    [SerializeField] private int totalRounds = 5;
    [SerializeField] private int defaultTeamIndex = 0;
    [SerializeField] private int defaultBattleSize = 1;

    [Header("Auto Resize")]
    [SerializeField] private bool autoResizeParents = true;
    [SerializeField] private float itemSpacing = 10f;
    [SerializeField] private float fallbackItemHeight = 60f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    [Header("Reference Retry")]
    [SerializeField] private bool retryFindManagers = true;
    [SerializeField] private float retryInterval = 0.25f;

    private float nextRetryTime = 0f;
    private bool initializedAfterManagersFound = false;

    private int selectedTeamIndex = 0;
    private int selectedBattleSize = 1;

    private readonly List<ColosseumTeamUnitPreviewUI> spawnedTeamPreviews = new List<ColosseumTeamUnitPreviewUI>();
    private readonly List<ColosseumBattleSizeButtonUI> spawnedSizeButtons = new List<ColosseumBattleSizeButtonUI>();

    private void Reset()
    {
        RefreshRuntimeReferences();
    }

    private void Awake()
    {
        RefreshRuntimeReferences();

        if (startButton != null)
            startButton.onClick.AddListener(StartColosseum);

        selectedTeamIndex = Mathf.Max(0, defaultTeamIndex);
        selectedBattleSize = Mathf.Max(1, defaultBattleSize);
    }

    private void OnEnable()
    {
        initializedAfterManagersFound = false;
        RefreshRuntimeReferences();
        TryInitializeWhenManagersReady();
    }

    private void Update()
    {
        if (!retryFindManagers)
            return;

        if (initializedAfterManagersFound)
            return;

        if (Time.time < nextRetryTime)
            return;

        nextRetryTime = Time.time + retryInterval;

        RefreshRuntimeReferences();
        TryInitializeWhenManagersReady();
    }

    private void RefreshRuntimeReferences()
    {
        if (teamManager == null)
            teamManager = FindManager<PlayerZoidTeamManager>();

        if (colosseumManager == null)
            colosseumManager = FindManager<ColosseumManager>();

        if (teamManager != null)
            teamManager.LoadTeams();

        if (debugLog)
        {
            if (teamManager == null)
                Debug.LogWarning("[ColosseumSetupUI] PlayerZoidTeamManager not found yet.");

            if (colosseumManager == null)
                Debug.LogWarning("[ColosseumSetupUI] ColosseumManager not found yet.");
        }
    }


    private void AutoSelectFirstNonEmptyTeamIfNeeded()
    {
        if (teamManager == null)
            return;

        List<int> selectedUnits = teamManager.GetTeamUnitIds(selectedTeamIndex);
        if (selectedUnits != null && selectedUnits.Count > 0)
            return;

        for (int i = 0; i < 3; i++)
        {
            List<int> unitIds = teamManager.GetTeamUnitIds(i);
            if (unitIds != null && unitIds.Count > 0)
            {
                selectedTeamIndex = i;
                selectedBattleSize = 1;

                if (debugLog)
                    Debug.Log("[ColosseumSetupUI] Auto-selected Team " + (i + 1) + " because it has units.");

                return;
            }
        }
    }


    private void TryInitializeWhenManagersReady()
    {
        if (teamManager == null || colosseumManager == null)
            return;

        initializedAfterManagersFound = true;

        AutoSelectFirstNonEmptyTeamIfNeeded();
        RefreshAll();

        if (debugLog)
            Debug.Log("[ColosseumSetupUI] Managers found. UI initialized.");
    }


    public void ForceRefreshReferencesFromScene()
    {
        teamManager = null;
        colosseumManager = null;
        initializedAfterManagersFound = false;

        RefreshRuntimeReferences();
        TryInitializeWhenManagersReady();
    }

    public void SelectTeam(int teamIndex)
    {
        selectedTeamIndex = Mathf.Max(0, teamIndex);

        int teamCount = GetSelectedTeamUnitCount();
        if (teamCount <= 0)
            selectedBattleSize = 1;
        else if (selectedBattleSize > teamCount)
            selectedBattleSize = Mathf.Clamp(teamCount, 1, maxBattleSize);

        RefreshAll();
    }

    public void SelectBattleSize(int battleSize)
    {
        selectedBattleSize = Mathf.Clamp(battleSize, 1, maxBattleSize);
        RefreshAll();
    }

    public void RefreshAll()
    {
        RefreshRuntimeReferences();

        RefreshTeamButtons();
        RefreshSelectedTeamPanel();
        RefreshBattleSizeButtons();
        RefreshBottomPanel();
    }

    private void RefreshTeamButtons()
    {
        SetupTeamButton(team1Button, 0);
        SetupTeamButton(team2Button, 1);
        SetupTeamButton(team3Button, 2);
    }

    private void SetupTeamButton(ColosseumTeamButtonUI button, int teamIndex)
    {
        if (button == null || teamManager == null)
            return;

        PlayerZoidTeamData team = teamManager.GetTeam(teamIndex);
        string teamName = team != null ? team.teamName : "Team " + (teamIndex + 1);
        int unitCount = team != null ? team.Count : 0;

        button.Setup(this, teamIndex, teamName, unitCount, selectedTeamIndex == teamIndex);
    }

    private void RefreshSelectedTeamPanel()
    {
        ClearTeamPreviews();

        List<int> unitIds = GetSelectedTeamUnitIds();

        if (selectedTeamCountText != null)
            selectedTeamCountText.text = unitIds.Count + " / 10 Zoids";

        if (selectedTeamUnitParent == null || teamUnitPreviewPrefab == null)
            return;

        for (int i = 0; i < unitIds.Count; i++)
        {
            ColosseumTeamUnitPreviewUI item = Instantiate(teamUnitPreviewPrefab, selectedTeamUnitParent);
            item.gameObject.SetActive(true);
            item.Setup(unitIds[i]);
            spawnedTeamPreviews.Add(item);
        }

        ResizeParent(selectedTeamUnitParent, teamUnitPreviewPrefab.GetComponent<RectTransform>(), spawnedTeamPreviews.Count);
    }

    private void RefreshBattleSizeButtons()
    {
        ClearBattleSizeButtons();

        int teamCount = GetSelectedTeamUnitCount();

        if (battleSizeButtonParent == null || battleSizeButtonPrefab == null)
            return;

        for (int size = 1; size <= maxBattleSize; size++)
        {
            bool unlocked = teamCount >= size;

            ColosseumBattleSizeButtonUI item = Instantiate(battleSizeButtonPrefab, battleSizeButtonParent);
            item.gameObject.SetActive(true);
            item.Setup(this, size, unlocked, selectedBattleSize == size && unlocked);
            spawnedSizeButtons.Add(item);
        }

        ResizeParent(battleSizeButtonParent, battleSizeButtonPrefab.GetComponent<RectTransform>(), spawnedSizeButtons.Count);
    }

    private void RefreshBottomPanel()
    {
        List<int> unitIds = GetSelectedTeamUnitIds();

        bool validTeam = unitIds.Count > 0;
        bool validSize = selectedBattleSize >= 1 && selectedBattleSize <= unitIds.Count && selectedBattleSize <= maxBattleSize;
        bool ready = validTeam && validSize;

        string teamName = "Team " + (selectedTeamIndex + 1);
        if (teamManager != null)
        {
            PlayerZoidTeamData team = teamManager.GetTeam(selectedTeamIndex);
            if (team != null && !string.IsNullOrEmpty(team.teamName))
                teamName = team.teamName;
        }

        if (selectedSummaryText != null)
            selectedSummaryText.text = ready
                ? "Selected: " + teamName + " | " + selectedBattleSize + " VS " + selectedBattleSize + " | " + totalRounds + " Rounds"
                : "Selected: " + teamName + " | No valid battle size";

        if (roundInfoText != null)
            roundInfoText.text = "Rounds: " + totalRounds;

        if (statusText != null)
        {
            if (!validTeam)
                statusText.text = "Selected team is empty";
            else if (!validSize)
                statusText.text = "Battle size is higher than selected team count";
            else
                statusText.text = "Ready";
        }

        if (startButton != null)
            startButton.interactable = ready;
    }

    private List<int> GetSelectedTeamUnitIds()
    {
        if (teamManager == null)
            return new List<int>();

        return teamManager.GetTeamUnitIds(selectedTeamIndex);
    }

    private int GetSelectedTeamUnitCount()
    {
        List<int> unitIds = GetSelectedTeamUnitIds();
        return unitIds != null ? unitIds.Count : 0;
    }

    private void StartColosseum()
    {
        List<int> unitIds = GetSelectedTeamUnitIds();

        if (unitIds == null || unitIds.Count <= 0)
        {
            SetStatus("Cannot start. Selected team is empty.");
            return;
        }

        if (selectedBattleSize > unitIds.Count)
        {
            SetStatus("Cannot start. Battle size is higher than selected team count.");
            return;
        }

        RefreshRuntimeReferences();

        if (colosseumManager == null)
        {
            SetStatus("Cannot start. ColosseumManager missing.");
            return;
        }

        bool started = colosseumManager.StartColosseum(selectedTeamIndex, selectedBattleSize, totalRounds);

        if (started)
        {
            SetStatus("Starting Colosseum: Team " + (selectedTeamIndex + 1) +
                      " | " + selectedBattleSize + " VS " + selectedBattleSize +
                      " | " + totalRounds + " Rounds");
        }
        else
        {
            SetStatus("Failed to start Colosseum.");
        }

        Debug.Log("[ColosseumSetupUI] Start requested. TeamIndex=" + selectedTeamIndex +
                  " BattleSize=" + selectedBattleSize +
                  " TotalRounds=" + totalRounds +
                  " TeamUnitCount=" + unitIds.Count);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        if (debugLog)
            Debug.Log("[ColosseumSetupUI] " + message);
    }

    private void ClearTeamPreviews()
    {
        for (int i = 0; i < spawnedTeamPreviews.Count; i++)
        {
            if (spawnedTeamPreviews[i] != null)
                Destroy(spawnedTeamPreviews[i].gameObject);
        }

        spawnedTeamPreviews.Clear();
    }

    private void ClearBattleSizeButtons()
    {
        for (int i = 0; i < spawnedSizeButtons.Count; i++)
        {
            if (spawnedSizeButtons[i] != null)
                Destroy(spawnedSizeButtons[i].gameObject);
        }

        spawnedSizeButtons.Clear();
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

        float itemHeight = fallbackItemHeight;
        if (prefabRect != null && prefabRect.rect.height > 0)
            itemHeight = prefabRect.rect.height;

        float height = (itemHeight + itemSpacing) * itemCount;
        parentRect.sizeDelta = new Vector2(parentRect.sizeDelta.x, height);
    }


    private T FindManager<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        T found = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (found != null)
            return found;
#else
        T found = Object.FindObjectOfType<T>(true);
        if (found != null)
            return found;
#endif

        return null;
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
