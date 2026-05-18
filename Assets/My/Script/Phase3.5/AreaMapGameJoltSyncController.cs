using UnityEngine;
using TMPro;

public class AreaMapGameJoltSyncController : MonoBehaviour
{
    public static AreaMapGameJoltSyncController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private ZoidsGameJoltCloudSaveManager privateCloudSaveManager;
    [SerializeField] private ZoidsGameJoltGlobalAreaManager globalAreaManager;
    [SerializeField] private AreaBattleStateManager areaStateManager;
    [SerializeField] private PlayerProfileManager profileManager;
    [SerializeField] private PlayerZoidTeamManager teamManager;
    [SerializeField] private UnitProgressManager unitProgressManager;

    [Header("Canvas Blocker")]
    [SerializeField] private GameObject canvasBlockerPanel;
    [SerializeField] private TMP_Text blockerStatusLabel;

    [Header("Sync Options")]
    [SerializeField] private bool downloadPrivateSaveOnEnable = true;
    [SerializeField] private bool downloadGlobalAreaOnEnable = true;
    [SerializeField] private bool refreshAreaButtonsAfterSync = true;

    [Tooltip("If private cloud save does not exist, continue using local data.")]
    [SerializeField] private bool allowNoPrivateSave = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool privateDownloadPending = false;
    private bool globalDownloadPending = false;
    private bool syncStarted = false;

    public bool IsSyncing
    {
        get
        {
            bool privateBusy = privateCloudSaveManager != null && privateCloudSaveManager.IsBusy;
            bool globalBusy = globalAreaManager != null && globalAreaManager.IsBusy;
            return privateDownloadPending || globalDownloadPending || privateBusy || globalBusy;
        }
    }

    private void Awake()
    {
        Instance = this;
        RefreshReferences();
        SetBlocker(false, "");
    }

    private void OnEnable()
    {
        RefreshReferences();
        SubscribeEvents();
        StartMapSync();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        SetBlocker(false, "");
    }

    public void RefreshReferences()
    {
        if (privateCloudSaveManager == null && ZoidsGameJoltCloudSaveManager.Instance != null)
            privateCloudSaveManager = ZoidsGameJoltCloudSaveManager.Instance;

        if (globalAreaManager == null && ZoidsGameJoltGlobalAreaManager.Instance != null)
            globalAreaManager = ZoidsGameJoltGlobalAreaManager.Instance;

        if (areaStateManager == null && AreaBattleStateManager.Instance != null)
            areaStateManager = AreaBattleStateManager.Instance;

        if (profileManager == null && PlayerProfileManager.Instance != null)
            profileManager = PlayerProfileManager.Instance;

        if (teamManager == null && PlayerZoidTeamManager.Instance != null)
            teamManager = PlayerZoidTeamManager.Instance;

        if (unitProgressManager == null && UnitProgressManager.Instance != null)
            unitProgressManager = UnitProgressManager.Instance;

        if (privateCloudSaveManager == null)
            privateCloudSaveManager = FindManager<ZoidsGameJoltCloudSaveManager>();

        if (globalAreaManager == null)
            globalAreaManager = FindManager<ZoidsGameJoltGlobalAreaManager>();

        if (areaStateManager == null)
            areaStateManager = FindManager<AreaBattleStateManager>();

        if (profileManager == null)
            profileManager = FindManager<PlayerProfileManager>();

        if (teamManager == null)
            teamManager = FindManager<PlayerZoidTeamManager>();

        if (unitProgressManager == null)
            unitProgressManager = FindManager<UnitProgressManager>();
    }

    private T FindManager<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>(true);
#endif
    }

    private void SubscribeEvents()
    {
        if (privateCloudSaveManager != null)
        {
            privateCloudSaveManager.OnDownloadFinished -= OnPrivateDownloadFinished;
            privateCloudSaveManager.OnPayloadApplied -= OnPrivatePayloadApplied;

            privateCloudSaveManager.OnDownloadFinished += OnPrivateDownloadFinished;
            privateCloudSaveManager.OnPayloadApplied += OnPrivatePayloadApplied;
        }

        if (globalAreaManager != null)
        {
            globalAreaManager.OnGlobalDownloadFinished -= OnGlobalDownloadFinished;
            globalAreaManager.OnGlobalDownloadFinished += OnGlobalDownloadFinished;
        }
    }

    private void UnsubscribeEvents()
    {
        if (privateCloudSaveManager != null)
        {
            privateCloudSaveManager.OnDownloadFinished -= OnPrivateDownloadFinished;
            privateCloudSaveManager.OnPayloadApplied -= OnPrivatePayloadApplied;
        }

        if (globalAreaManager != null)
            globalAreaManager.OnGlobalDownloadFinished -= OnGlobalDownloadFinished;
    }

    public void StartMapSync()
    {
        RefreshReferences();
        SubscribeEvents();

        syncStarted = true;
        privateDownloadPending = false;
        globalDownloadPending = false;

        bool willSync = false;

        if (downloadPrivateSaveOnEnable && privateCloudSaveManager != null)
        {
            privateDownloadPending = true;
            willSync = true;
        }

        if (downloadGlobalAreaOnEnable && globalAreaManager != null)
        {
            globalDownloadPending = true;
            willSync = true;
        }

        if (!willSync)
        {
            SetBlocker(false, "");
            RefreshAllAreaUI();
            return;
        }

        SetBlocker(true, "Loading map data from Game Jolt...");

        if (privateDownloadPending)
        {
            if (debugLog)
                Debug.Log("[AreaMapGameJoltSyncController] Downloading private player save...");

            privateCloudSaveManager.DownloadAndApplyCloudSave();
        }

        if (globalDownloadPending)
        {
            if (debugLog)
                Debug.Log("[AreaMapGameJoltSyncController] Downloading global area ownership...");

            globalAreaManager.DownloadGlobalAreas(true);
        }
    }

    public void RefreshMapFromGameJoltButton()
    {
        StartMapSync();
    }

    private void OnPrivateDownloadFinished(bool success, ZoidsGameJoltSavePayload payload)
    {
        privateDownloadPending = false;

        if (!success && !allowNoPrivateSave)
            Debug.LogWarning("[AreaMapGameJoltSyncController] Private save download failed.");

        ReloadLocalManagers();

        if (debugLog)
            Debug.Log("[AreaMapGameJoltSyncController] Private save download finished. Success=" + success);

        TryFinishSync();
    }

    private void OnPrivatePayloadApplied(ZoidsGameJoltSavePayload payload)
    {
        ReloadLocalManagers();

        if (debugLog)
            Debug.Log("[AreaMapGameJoltSyncController] Private payload applied.");
    }

    private void OnGlobalDownloadFinished(bool success, ZoidsGlobalAreaOwnershipSave save)
    {
        globalDownloadPending = false;

        if (debugLog)
            Debug.Log("[AreaMapGameJoltSyncController] Global area download finished. Success=" + success);

        if (areaStateManager != null)
            areaStateManager.Load();

        TryFinishSync();
    }

    private void TryFinishSync()
    {
        if (!syncStarted)
            return;

        if (privateDownloadPending || globalDownloadPending)
            return;

        ReloadLocalManagers();
        RefreshAllAreaUI();

        SetBlocker(false, "");
        syncStarted = false;

        if (debugLog)
            Debug.Log("[AreaMapGameJoltSyncController] Map sync complete.");
    }

    private void ReloadLocalManagers()
    {
        RefreshReferences();

        if (profileManager != null)
            profileManager.LoadProfile();

        if (unitProgressManager != null)
            unitProgressManager.LoadProgress();

        if (teamManager != null)
            teamManager.LoadTeams();

        if (areaStateManager != null)
            areaStateManager.Load();
    }

    public void RefreshAllAreaUI()
    {
        if (!refreshAreaButtonsAfterSync)
            return;

#if UNITY_2023_1_OR_NEWER
        DummyAreaStatusUI[] areaUIs = Object.FindObjectsByType<DummyAreaStatusUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        DummyAreaStatusUI[] areaUIs = Object.FindObjectsOfType<DummyAreaStatusUI>(true);
#endif

        for (int i = 0; i < areaUIs.Length; i++)
        {
            if (areaUIs[i] != null)
                areaUIs[i].RefreshDisplay();
        }

        if (debugLog)
            Debug.Log("[AreaMapGameJoltSyncController] Refreshed area buttons. Count=" + areaUIs.Length);
    }

    private void SetBlocker(bool active, string message)
    {
        if (canvasBlockerPanel != null)
            canvasBlockerPanel.SetActive(active);

        if (blockerStatusLabel != null)
            blockerStatusLabel.text = message;
    }
}
