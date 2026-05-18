using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using TBTK;

public class PerkSceneController : MonoBehaviour
{
    public static PerkSceneController Instance { get; private set; }

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("References")]
    [SerializeField] private PerkManager perkManager;
    [SerializeField] private UIPerkScreen perkScreen;
    [SerializeField] private ZoidsPerkProgressManager progressManager;
    [SerializeField] private ZoidsGameJoltCloudSaveManager cloudSaveManager;

    [Header("Game Jolt Cloud Sync")]
    [SerializeField] private bool downloadLatestCloudSaveOnStart = true;
    [SerializeField] private bool uploadCloudSaveAfterUnlock = true;
    [SerializeField] private bool uploadCloudSaveOnSaveOnly = true;
    [SerializeField] private bool uploadCloudSaveBeforeBackToMenu = true;

    [Tooltip("Panel that blocks the perk UI while downloading/uploading.")]
    [SerializeField] private GameObject canvasBlockerPanel;
    [SerializeField] private TMP_Text blockerStatusLabel;

    [Header("Options")]
    [SerializeField] private bool showPerkScreenOnStart = true;
    [SerializeField] private bool forcePerkManagerAsMenu = true;
    [SerializeField] private bool activateInactivePerkScreen = true;
    [SerializeField] private int showDelayFrames = 2;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool waitingForCloudDownload = false;
    private bool waitingForCloudUpload = false;
    private bool pendingBackToMenuAfterUpload = false;

    public bool IsCloudSyncing
    {
        get { return waitingForCloudDownload || waitingForCloudUpload || (cloudSaveManager != null && cloudSaveManager.IsBusy); }
    }

    private void Awake()
    {
        Instance = this;
        RefreshReferences();
        SetCanvasBlocker(false, "");
    }

    private void OnEnable()
    {
        RefreshReferences();
        SubscribeCloudEvents();
    }

    private void OnDisable()
    {
        UnsubscribeCloudEvents();
        SetCanvasBlocker(false, "");
    }

    private IEnumerator Start()
    {
        RefreshReferences();
        SubscribeCloudEvents();

        EnsurePerkManager();
        EnsureProgressManager();

        if (forcePerkManagerAsMenu && perkManager != null)
            perkManager.inGameScene = false;

        if (downloadLatestCloudSaveOnStart && cloudSaveManager != null)
        {
            SetCanvasBlocker(true, "Downloading latest perk data...");
            waitingForCloudDownload = true;
            cloudSaveManager.DownloadAndApplyCloudSave();

            while (waitingForCloudDownload && cloudSaveManager != null && cloudSaveManager.IsBusy)
                yield return null;

            yield return null;
        }

        RefreshLocalPerkFromSave();

        if (perkScreen != null && activateInactivePerkScreen && !perkScreen.gameObject.activeInHierarchy)
            perkScreen.gameObject.SetActive(true);

        StartCoroutine(DelayedShowRoutine());
        SetCanvasBlocker(false, "");
    }

    private IEnumerator DelayedShowRoutine()
    {
        for (int i = 0; i < Mathf.Max(1, showDelayFrames); i++)
            yield return null;

        RefreshReferences();

        if (perkScreen != null)
            perkScreen.SetInstance();

        RefreshLocalPerkFromSave();

        yield return null;

        if (showPerkScreenOnStart && perkScreen != null)
        {
            perkScreen.SetInstance();
            UIPerkScreen.Show();

            if (debugLog)
                Debug.Log("[PerkSceneController] Showing UIPerkScreen.");
        }
        else if (perkScreen == null)
        {
            Debug.LogWarning("[PerkSceneController] UIPerkScreen missing.");
        }
    }

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

    private void OnCloudDownloadFinished(bool success, ZoidsGameJoltSavePayload payload)
    {
        waitingForCloudDownload = false;
        RefreshLocalPerkFromSave();

        if (debugLog)
            Debug.Log("[PerkSceneController] Cloud download finished. Success=" + success);
    }

    private void OnCloudPayloadApplied(ZoidsGameJoltSavePayload payload)
    {
        RefreshLocalPerkFromSave();

        if (debugLog)
            Debug.Log("[PerkSceneController] Cloud payload applied to perk scene.");
    }

    private void OnCloudUploadFinished(bool success)
    {
        waitingForCloudUpload = false;

        if (debugLog)
            Debug.Log("[PerkSceneController] Cloud upload finished. Success=" + success);

        if (pendingBackToMenuAfterUpload)
        {
            pendingBackToMenuAfterUpload = false;
            LoadMainMenuScene();
            return;
        }

        SetCanvasBlocker(false, "");
    }

    private void RefreshLocalPerkFromSave()
    {
        RefreshReferences();

        EnsurePerkManager();
        EnsureProgressManager();

        if (progressManager != null)
        {
            progressManager.LoadProgress();
            progressManager.ApplyProgressToPerkManager();
        }

        if (PerkManager.Instance != null)
            PerkManager.Instance.SyncFromZoidsPerkProgress();

        if (perkScreen != null && UIPerkScreen.IsShowing())
            perkScreen.UpdateList();
    }

    private void EnsurePerkManager()
    {
        if (perkManager != null)
            return;

        perkManager = FindManager<PerkManager>();

        if (perkManager != null)
            return;

        GameObject obj = new GameObject("PerkManager_AUTO");
        perkManager = obj.AddComponent<PerkManager>();
        perkManager.inGameScene = false;
        perkManager.loadProgressFromCache = true;
        perkManager.saveProgressToCache = true;

        if (debugLog)
            Debug.Log("[PerkSceneController] Created PerkManager_AUTO.");
    }

    private void EnsureProgressManager()
    {
        if (progressManager != null)
            return;

        if (ZoidsPerkProgressManager.Instance != null)
            progressManager = ZoidsPerkProgressManager.Instance;

        if (progressManager != null)
            return;

        progressManager = FindManager<ZoidsPerkProgressManager>();

        if (progressManager != null)
            return;

        GameObject obj = new GameObject("ZoidsPerkProgressManager_AUTO");
        progressManager = obj.AddComponent<ZoidsPerkProgressManager>();

        if (debugLog)
            Debug.Log("[PerkSceneController] Created ZoidsPerkProgressManager_AUTO.");
    }

    private void RefreshReferences()
    {
        if (perkManager == null)
            perkManager = FindManager<PerkManager>();

        if (perkScreen == null)
            perkScreen = FindManager<UIPerkScreen>();

        if (progressManager == null && ZoidsPerkProgressManager.Instance != null)
            progressManager = ZoidsPerkProgressManager.Instance;

        if (progressManager == null)
            progressManager = FindManager<ZoidsPerkProgressManager>();

        if (cloudSaveManager == null && ZoidsGameJoltCloudSaveManager.Instance != null)
            cloudSaveManager = ZoidsGameJoltCloudSaveManager.Instance;

        if (cloudSaveManager == null)
            cloudSaveManager = FindManager<ZoidsGameJoltCloudSaveManager>();
    }

    public void ForceRefreshAndShow()
    {
        StartCoroutine(ForceRefreshAndShowRoutine());
    }

    private IEnumerator ForceRefreshAndShowRoutine()
    {
        RefreshLocalPerkFromSave();

        if (perkScreen != null && activateInactivePerkScreen && !perkScreen.gameObject.activeInHierarchy)
            perkScreen.gameObject.SetActive(true);

        for (int i = 0; i < Mathf.Max(1, showDelayFrames); i++)
            yield return null;

        RefreshReferences();

        if (perkScreen == null)
        {
            Debug.LogWarning("[PerkSceneController] Cannot show perk screen. UIPerkScreen missing.");
            yield break;
        }

        perkScreen.SetInstance();
        UIPerkScreen.Show();
    }

    public void SaveAndBackToMainMenu()
    {
        RefreshReferences();

        if (progressManager != null)
            progressManager.SaveProgressFromPerkManager();

        if (uploadCloudSaveBeforeBackToMenu && cloudSaveManager != null && !cloudSaveManager.IsBusy)
        {
            pendingBackToMenuAfterUpload = true;
            UploadPerkProgressToGameJolt("Saving perks before returning...");
            return;
        }

        LoadMainMenuScene();
    }

    public void SaveOnly()
    {
        RefreshReferences();

        if (progressManager != null)
            progressManager.SaveProgressFromPerkManager();

        if (uploadCloudSaveOnSaveOnly)
            UploadPerkProgressToGameJolt("Uploading perk data...");
    }

    public void Add100CurrencyDebug()
    {
        RefreshReferences();

        if (progressManager != null)
            progressManager.AddCurrencyDebug(100);

        RefreshLocalPerkFromSave();

        if (uploadCloudSaveAfterUnlock)
            UploadPerkProgressToGameJolt("Uploading debug perk currency...");
    }

    public void OnPerkUnlockedFromUI()
    {
        RefreshReferences();

        if (progressManager != null)
            progressManager.SaveProgressFromPerkManager();

        if (uploadCloudSaveAfterUnlock)
            UploadPerkProgressToGameJolt("Uploading unlocked perk...");
    }

    public void UploadPerkProgressToGameJolt(string blockerMessage)
    {
        RefreshReferences();
        SubscribeCloudEvents();

        if (cloudSaveManager == null)
        {
            Debug.LogWarning("[PerkSceneController] Cannot upload perks. Cloud save manager missing.");
            return;
        }

        if (cloudSaveManager.IsBusy)
        {
            Debug.LogWarning("[PerkSceneController] Cannot upload perks. Cloud save manager is busy.");
            SetCanvasBlocker(true, "Cloud sync in progress...");
            return;
        }

        if (progressManager != null)
            progressManager.SaveProgressFromPerkManager();

        waitingForCloudUpload = true;
        SetCanvasBlocker(true, blockerMessage);

        cloudSaveManager.UploadLocalSaveToCloud();
    }

    private void SetCanvasBlocker(bool active, string message)
    {
        if (canvasBlockerPanel != null)
            canvasBlockerPanel.SetActive(active);

        if (blockerStatusLabel != null)
            blockerStatusLabel.text = message;
    }

    private void LoadMainMenuScene()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("[PerkSceneController] Main menu scene name is empty.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private T FindManager<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>(true);
#endif
    }
}
