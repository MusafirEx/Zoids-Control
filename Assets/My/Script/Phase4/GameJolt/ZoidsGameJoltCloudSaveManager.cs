using System;
using UnityEngine;
using GameJolt.API;

public class ZoidsGameJoltCloudSaveManager : MonoBehaviour
{
    public static ZoidsGameJoltCloudSaveManager Instance { get; private set; }

    [Header("Cloud Save")]
    [SerializeField] private string cloudSaveKey = "zoids_field_of_rebellion_save_v1";
    [SerializeField] private bool globalDataStore = false;

    [Header("PlayerPrefs Keys")]
    [SerializeField] private string playerProfileKey = "zoids_player_profile_main";
    [SerializeField] private string unitProgressKey = "zoids_unit_progress_main";
    [SerializeField] private string playerTeamsKey = "ZOIDS_PLAYER_TEAMS_V1";
    [SerializeField] private string areaBattleStateKey = "ZOIDS_AREA_BATTLE_STATE_V1";
    [SerializeField] private string perkProgressKey = "ZOIDS_PERK_PROGRESS_V1";
    [SerializeField] private string scoreboardProgressKey = "ZOIDS_GAMEJOLT_SCOREBOARD_PROGRESS_V1";

    [Header("References")]
    [SerializeField] private ZoidsGameJoltAccountManager accountManager;

    [Header("Auto Sync")]
    [SerializeField] private bool downloadOnStartIfLoggedIn = false;
    [SerializeField] private bool uploadOnApplicationQuit = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public bool IsBusy { get; private set; }
    public ZoidsGameJoltSavePayload LastPayload { get; private set; }

    public event Action<bool> OnUploadFinished;
    public event Action<bool, ZoidsGameJoltSavePayload> OnDownloadFinished;
    public event Action<ZoidsGameJoltSavePayload> OnPayloadApplied;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshRuntimeReferences();
    }

    private void Start()
    {
        if (downloadOnStartIfLoggedIn && IsLoggedIn())
            DownloadAndApplyCloudSave();
    }

    public void RefreshRuntimeReferences()
    {
        if (accountManager == null && ZoidsGameJoltAccountManager.Instance != null)
            accountManager = ZoidsGameJoltAccountManager.Instance;

        if (accountManager == null)
            accountManager = FindManager<ZoidsGameJoltAccountManager>();
    }

    private T FindManager<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return UnityEngine.Object.FindObjectOfType<T>(true);
#endif
    }

    private bool IsLoggedIn()
    {
        RefreshRuntimeReferences();
        return accountManager != null && accountManager.IsLoggedIn;
    }

    public ZoidsGameJoltSavePayload BuildPayloadFromLocal()
    {
        RefreshRuntimeReferences();

        ZoidsGameJoltSavePayload payload = new ZoidsGameJoltSavePayload();

        string userId = accountManager != null ? accountManager.UserId : "LOCAL_PLAYER";
        string username = accountManager != null ? accountManager.Username : "Local Player";

        payload.Touch(userId, username);

        payload.playerProfileJson = PlayerPrefs.GetString(playerProfileKey, "");
        payload.unitProgressJson = PlayerPrefs.GetString(unitProgressKey, "");
        payload.playerTeamsJson = PlayerPrefs.GetString(playerTeamsKey, "");
        payload.areaBattleStateJson = PlayerPrefs.GetString(areaBattleStateKey, "");
        payload.perkProgressJson = PlayerPrefs.GetString(perkProgressKey, "");
        payload.scoreboardProgressJson = PlayerPrefs.GetString(scoreboardProgressKey, "");

        LastPayload = payload;

        if (debugLog)
        {
            Debug.Log("[ZoidsGameJoltCloudSaveManager] Built local payload. " +
                      "profile=" + payload.playerProfileJson.Length +
                      " unit=" + payload.unitProgressJson.Length +
                      " teams=" + payload.playerTeamsJson.Length +
                      " area=" + payload.areaBattleStateJson.Length +
                      " perk=" + payload.perkProgressJson.Length +
                      " scoreboard=" + payload.scoreboardProgressJson.Length);
        }

        return payload;
    }

    public void UploadLocalSaveToCloud()
    {
        if (IsBusy)
        {
            Debug.LogWarning("[ZoidsGameJoltCloudSaveManager] Upload ignored. Busy.");
            return;
        }

        if (!IsLoggedIn())
        {
            Debug.LogWarning("[ZoidsGameJoltCloudSaveManager] Cannot upload. Game Jolt user not logged in.");
            OnUploadFinished?.Invoke(false);
            return;
        }

        ZoidsGameJoltSavePayload payload = BuildPayloadFromLocal();
        string json = JsonUtility.ToJson(payload, false);

        IsBusy = true;

        DataStore.Set(cloudSaveKey, json, globalDataStore, success =>
        {
            IsBusy = false;

            if (debugLog)
                Debug.Log("[ZoidsGameJoltCloudSaveManager] Upload finished. Success=" + success +
                          " Key=" + cloudSaveKey +
                          " Bytes=" + json.Length);

            OnUploadFinished?.Invoke(success);
        });
    }

    public void DownloadCloudSave()
    {
        if (IsBusy)
        {
            Debug.LogWarning("[ZoidsGameJoltCloudSaveManager] Download ignored. Busy.");
            return;
        }

        if (!IsLoggedIn())
        {
            Debug.LogWarning("[ZoidsGameJoltCloudSaveManager] Cannot download. Game Jolt user not logged in.");
            OnDownloadFinished?.Invoke(false, null);
            return;
        }

        IsBusy = true;

        DataStore.Get(cloudSaveKey, globalDataStore, json =>
        {
            IsBusy = false;

            if (string.IsNullOrEmpty(json))
            {
                if (debugLog)
                    Debug.Log("[ZoidsGameJoltCloudSaveManager] No cloud save found.");

                OnDownloadFinished?.Invoke(false, null);
                return;
            }

            ZoidsGameJoltSavePayload payload = null;

            try
            {
                payload = JsonUtility.FromJson<ZoidsGameJoltSavePayload>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("[ZoidsGameJoltCloudSaveManager] Cloud save parse failed: " + ex.Message);
            }

            bool success = payload != null;

            if (success)
                LastPayload = payload;

            if (debugLog)
            {
                Debug.Log("[ZoidsGameJoltCloudSaveManager] Download finished. Success=" + success +
                          " Bytes=" + json.Length +
                          (payload != null ? " SavedAt=" + payload.savedAtUtc : ""));
            }

            OnDownloadFinished?.Invoke(success, payload);
        });
    }

    public void DownloadAndApplyCloudSave()
    {
        if (IsBusy)
        {
            Debug.LogWarning("[ZoidsGameJoltCloudSaveManager] Download+Apply ignored. Busy.");
            return;
        }

        if (!IsLoggedIn())
        {
            Debug.LogWarning("[ZoidsGameJoltCloudSaveManager] Cannot download+apply. Game Jolt user not logged in.");
            OnDownloadFinished?.Invoke(false, null);
            return;
        }

        IsBusy = true;

        DataStore.Get(cloudSaveKey, globalDataStore, json =>
        {
            IsBusy = false;

            if (string.IsNullOrEmpty(json))
            {
                if (debugLog)
                    Debug.Log("[ZoidsGameJoltCloudSaveManager] No cloud save found to apply.");

                OnDownloadFinished?.Invoke(false, null);
                return;
            }

            ZoidsGameJoltSavePayload payload = null;

            try
            {
                payload = JsonUtility.FromJson<ZoidsGameJoltSavePayload>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("[ZoidsGameJoltCloudSaveManager] Cloud save parse failed: " + ex.Message);
            }

            bool success = payload != null;

            if (success)
                ApplyPayloadToLocal(payload, true);

            OnDownloadFinished?.Invoke(success, payload);
        });
    }

    public void ApplyPayloadToLocal(ZoidsGameJoltSavePayload payload, bool reloadManagers)
    {
        if (payload == null)
        {
            Debug.LogWarning("[ZoidsGameJoltCloudSaveManager] Cannot apply null payload.");
            return;
        }

        if (!string.IsNullOrEmpty(payload.playerProfileJson))
            PlayerPrefs.SetString(playerProfileKey, payload.playerProfileJson);

        if (!string.IsNullOrEmpty(payload.unitProgressJson))
            PlayerPrefs.SetString(unitProgressKey, payload.unitProgressJson);

        if (!string.IsNullOrEmpty(payload.playerTeamsJson))
            PlayerPrefs.SetString(playerTeamsKey, payload.playerTeamsJson);

        if (!string.IsNullOrEmpty(payload.areaBattleStateJson))
            PlayerPrefs.SetString(areaBattleStateKey, payload.areaBattleStateJson);

        if (!string.IsNullOrEmpty(payload.perkProgressJson))
            PlayerPrefs.SetString(perkProgressKey, payload.perkProgressJson);

        if (!string.IsNullOrEmpty(payload.scoreboardProgressJson))
            PlayerPrefs.SetString(scoreboardProgressKey, payload.scoreboardProgressJson);

        PlayerPrefs.Save();

        LastPayload = payload;

        if (reloadManagers)
            ReloadLocalManagers();

        if (debugLog)
            Debug.Log("[ZoidsGameJoltCloudSaveManager] Applied cloud save to PlayerPrefs. SavedAt=" + payload.savedAtUtc);

        OnPayloadApplied?.Invoke(payload);
    }

    public void ReloadLocalManagers()
    {
        if (PlayerProfileManager.Instance != null)
            PlayerProfileManager.Instance.LoadProfile();

        if (UnitProgressManager.Instance != null)
            UnitProgressManager.Instance.LoadProgress();

        if (PlayerZoidTeamManager.Instance != null)
            PlayerZoidTeamManager.Instance.LoadTeams();

        if (AreaBattleStateManager.Instance != null)
            AreaBattleStateManager.Instance.Load();

        if (ZoidsPerkProgressManager.Instance != null)
        {
            ZoidsPerkProgressManager.Instance.LoadProgress();
            ZoidsPerkProgressManager.Instance.ApplyProgressToPerkManager();
        }

        if (ZoidsGameJoltScoreboardManager.Instance != null)
            ZoidsGameJoltScoreboardManager.Instance.LoadProgress();

        if (debugLog)
            Debug.Log("[ZoidsGameJoltCloudSaveManager] Reloaded local managers from PlayerPrefs.");
    }

    public void DeleteCloudSave()
    {
        if (!IsLoggedIn())
        {
            Debug.LogWarning("[ZoidsGameJoltCloudSaveManager] Cannot delete. Game Jolt user not logged in.");
            return;
        }

        DataStore.Delete(cloudSaveKey, globalDataStore, success =>
        {
            if (debugLog)
                Debug.Log("[ZoidsGameJoltCloudSaveManager] Delete cloud save success=" + success);
        });
    }

    private void OnApplicationQuit()
    {
        if (uploadOnApplicationQuit && IsLoggedIn())
            UploadLocalSaveToCloud();
    }
}
