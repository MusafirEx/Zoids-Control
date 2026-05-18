using System;
using System.Collections.Generic;
using UnityEngine;
using GameJolt.API;

public class ZoidsGameJoltGlobalAreaManager : MonoBehaviour
{
    public static ZoidsGameJoltGlobalAreaManager Instance { get; private set; }

    [Header("Game Jolt Global Datastore")]
    [SerializeField] private string globalAreaKey = "zoids_global_area_ownership_v1";
    [SerializeField] private bool globalDataStore = true;

    [Header("Rules")]
    [SerializeField] private int areaLockHours = 24;
    [SerializeField] private int winContributionAmount = 1;

    [Header("References")]
    [SerializeField] private AreaBattleStateManager localAreaStateManager;
    [SerializeField] private ZoidsGameJoltAccountManager accountManager;

    [Header("Options")]
    [SerializeField] private bool downloadOnStart = false;
    [SerializeField] private bool applyDownloadedGlobalToLocal = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public bool IsBusy { get; private set; }
    public ZoidsGlobalAreaOwnershipSave LastGlobalSave { get; private set; }

    public event Action<bool> OnGlobalUploadFinished;
    public event Action<bool, ZoidsGlobalAreaOwnershipSave> OnGlobalDownloadFinished;

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
        if (downloadOnStart)
            DownloadGlobalAreas(true);
    }

    public void RefreshRuntimeReferences()
    {
        if (localAreaStateManager == null && AreaBattleStateManager.Instance != null)
            localAreaStateManager = AreaBattleStateManager.Instance;

        if (accountManager == null && ZoidsGameJoltAccountManager.Instance != null)
            accountManager = ZoidsGameJoltAccountManager.Instance;

        if (localAreaStateManager == null)
            localAreaStateManager = FindManager<AreaBattleStateManager>();

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

    public void DownloadGlobalAreas(bool applyToLocal)
    {
        if (IsBusy)
        {
            Debug.LogWarning("[ZoidsGameJoltGlobalAreaManager] Download ignored. Busy.");
            return;
        }

        IsBusy = true;

        DataStore.Get(globalAreaKey, globalDataStore, json =>
        {
            IsBusy = false;

            if (string.IsNullOrEmpty(json))
            {
                LastGlobalSave = new ZoidsGlobalAreaOwnershipSave();
                LastGlobalSave.savedAtUtc = DateTime.UtcNow.ToString("o");

                if (debugLog)
                    Debug.Log("[ZoidsGameJoltGlobalAreaManager] No global area save found. Created empty runtime save.");

                OnGlobalDownloadFinished?.Invoke(false, LastGlobalSave);
                return;
            }

            ZoidsGlobalAreaOwnershipSave save = null;

            try
            {
                save = JsonUtility.FromJson<ZoidsGlobalAreaOwnershipSave>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("[ZoidsGameJoltGlobalAreaManager] Failed to parse global area save: " + ex.Message);
            }

            if (save == null)
                save = new ZoidsGlobalAreaOwnershipSave();

            save.EnsureValid();
            LastGlobalSave = save;

            if (applyToLocal && applyDownloadedGlobalToLocal)
                ApplyGlobalSaveToLocalAreaState(save);

            if (debugLog)
                Debug.Log("[ZoidsGameJoltGlobalAreaManager] Downloaded global area save. Areas=" + save.areas.Count);

            OnGlobalDownloadFinished?.Invoke(true, save);
        });
    }

    public void UploadGlobalAreas(ZoidsGlobalAreaOwnershipSave save)
    {
        if (save == null)
        {
            Debug.LogWarning("[ZoidsGameJoltGlobalAreaManager] Cannot upload null global save.");
            OnGlobalUploadFinished?.Invoke(false);
            return;
        }

        if (IsBusy)
        {
            Debug.LogWarning("[ZoidsGameJoltGlobalAreaManager] Upload ignored. Busy.");
            OnGlobalUploadFinished?.Invoke(false);
            return;
        }

        save.EnsureValid();
        save.savedAtUtc = DateTime.UtcNow.ToString("o");

        string json = JsonUtility.ToJson(save, false);

        IsBusy = true;

        DataStore.Set(globalAreaKey, json, globalDataStore, success =>
        {
            IsBusy = false;

            if (success)
                LastGlobalSave = save;

            if (debugLog)
                Debug.Log("[ZoidsGameJoltGlobalAreaManager] Upload global area save. Success=" + success +
                          " Areas=" + save.areas.Count +
                          " Bytes=" + json.Length);

            OnGlobalUploadFinished?.Invoke(success);
        });
    }

    public void ApplyAreaBattleWinToGlobal(BattleContextData context)
    {
        if (context == null)
        {
            Debug.LogWarning("[ZoidsGameJoltGlobalAreaManager] Cannot apply global area win. Context is null.");
            return;
        }

        if (context.areaId < 0)
        {
            if (debugLog)
                Debug.Log("[ZoidsGameJoltGlobalAreaManager] Ignored global area update for invalid areaId=" + context.areaId);
            return;
        }

        if (IsBusy)
        {
            Debug.LogWarning("[ZoidsGameJoltGlobalAreaManager] Global update ignored. Manager is busy.");
            return;
        }

        IsBusy = true;

        DataStore.Get(globalAreaKey, globalDataStore, json =>
        {
            ZoidsGlobalAreaOwnershipSave save = null;

            if (!string.IsNullOrEmpty(json))
            {
                try { save = JsonUtility.FromJson<ZoidsGlobalAreaOwnershipSave>(json); }
                catch (Exception ex) { Debug.LogError("[ZoidsGameJoltGlobalAreaManager] Parse before update failed: " + ex.Message); }
            }

            if (save == null)
                save = new ZoidsGlobalAreaOwnershipSave();

            save.EnsureValid();

            ApplyContextWinToSave(save, context);

            string updatedJson = JsonUtility.ToJson(save, false);

            DataStore.Set(globalAreaKey, updatedJson, globalDataStore, success =>
            {
                IsBusy = false;

                if (success)
                {
                    LastGlobalSave = save;

                    if (applyDownloadedGlobalToLocal)
                        ApplyGlobalSaveToLocalAreaState(save);
                }

                if (debugLog)
                    Debug.Log("[ZoidsGameJoltGlobalAreaManager] Applied area win to global. Area=" + context.areaId +
                              " Faction=" + context.playerFactionName +
                              " Success=" + success);

                OnGlobalUploadFinished?.Invoke(success);
            });
        });
    }

    private void ApplyContextWinToSave(ZoidsGlobalAreaOwnershipSave save, BattleContextData context)
    {
        save.EnsureValid();

        ZoidsGlobalAreaOwnershipData area = save.GetArea(context.areaId, true);

        DateTime now = DateTime.UtcNow;

        area.areaId = context.areaId;
        area.areaName = context.areaName ?? "";
        area.ownerFactionId = context.playerFactionId;
        area.ownerFactionName = context.playerFactionName ?? "";
        area.lastCapturedUtc = now.ToString("o");
        area.lastCapturedUtcTicks = now.Ticks;
        area.areaLockedUntilUtcTicks = now.AddHours(areaLockHours).Ticks;

        area.defenderUnitIds.Clear();
        if (context.playerUnitIds != null)
            area.defenderUnitIds.AddRange(context.playerUnitIds);

        string playerId = accountManager != null ? accountManager.UserId : "";
        string playerName = accountManager != null ? accountManager.Username : "";

        area.lastCapturedByPlayerId = playerId;
        area.lastCapturedByPlayerName = playerName;

        area.AddContribution(context.playerFactionId, context.playerFactionName, winContributionAmount);

        save.savedAtUtc = now.ToString("o");
    }

    public void ApplyGlobalSaveToLocalAreaState(ZoidsGlobalAreaOwnershipSave save)
    {
        if (save == null)
            return;

        RefreshRuntimeReferences();

        if (localAreaStateManager == null)
        {
            Debug.LogWarning("[ZoidsGameJoltGlobalAreaManager] Cannot apply global to local. AreaBattleStateManager missing.");
            return;
        }

        save.EnsureValid();

        for (int i = 0; i < save.areas.Count; i++)
        {
            ZoidsGlobalAreaOwnershipData globalArea = save.areas[i];
            if (globalArea == null)
                continue;

            AreaBattleStateData localArea = localAreaStateManager.GetAreaState(globalArea.areaId, true);

            localArea.ownerFactionId = globalArea.ownerFactionId;
            localArea.ownerFactionName = globalArea.ownerFactionName ?? "";

            localArea.defenderUnitIds.Clear();
            if (globalArea.defenderUnitIds != null)
                localArea.defenderUnitIds.AddRange(globalArea.defenderUnitIds);

            localArea.areaLockedUntilUtcTicks = globalArea.areaLockedUntilUtcTicks;
            localArea.lastBattleUtc = globalArea.lastCapturedUtc ?? "";
            localArea.lastPlayerWon = false;

            // Important:
            // Do NOT touch localArea.playerAttemptLockedUntilUtcTicks.
            // That remains private per-player cooldown.
        }

        localAreaStateManager.Save();

        if (debugLog)
            Debug.Log("[ZoidsGameJoltGlobalAreaManager] Applied global ownership to local area state. Count=" + save.areas.Count);
    }
}

[Serializable]
public class ZoidsGlobalAreaOwnershipSave
{
    public int version = 1;
    public string savedAtUtc = "";
    public List<ZoidsGlobalAreaOwnershipData> areas = new List<ZoidsGlobalAreaOwnershipData>();

    public void EnsureValid()
    {
        if (areas == null)
            areas = new List<ZoidsGlobalAreaOwnershipData>();
    }

    public ZoidsGlobalAreaOwnershipData GetArea(int areaId, bool createIfMissing)
    {
        EnsureValid();

        for (int i = 0; i < areas.Count; i++)
        {
            if (areas[i] != null && areas[i].areaId == areaId)
                return areas[i];
        }

        if (!createIfMissing)
            return null;

        ZoidsGlobalAreaOwnershipData area = new ZoidsGlobalAreaOwnershipData();
        area.areaId = areaId;
        areas.Add(area);
        return area;
    }
}

[Serializable]
public class ZoidsGlobalAreaOwnershipData
{
    public int areaId = -1;
    public string areaName = "";

    public int ownerFactionId = -1;
    public string ownerFactionName = "";

    public List<int> defenderUnitIds = new List<int>();

    public long areaLockedUntilUtcTicks = 0;
    public string lastCapturedUtc = "";
    public long lastCapturedUtcTicks = 0;

    public string lastCapturedByPlayerId = "";
    public string lastCapturedByPlayerName = "";

    public List<ZoidsFactionContributionData> factionContributions = new List<ZoidsFactionContributionData>();

    public void AddContribution(int factionId, string factionName, int amount)
    {
        if (factionContributions == null)
            factionContributions = new List<ZoidsFactionContributionData>();

        for (int i = 0; i < factionContributions.Count; i++)
        {
            if (factionContributions[i] != null && factionContributions[i].factionId == factionId)
            {
                factionContributions[i].factionName = factionName ?? "";
                factionContributions[i].score += amount;
                return;
            }
        }

        ZoidsFactionContributionData data = new ZoidsFactionContributionData();
        data.factionId = factionId;
        data.factionName = factionName ?? "";
        data.score = amount;
        factionContributions.Add(data);
    }
}

[Serializable]
public class ZoidsFactionContributionData
{
    public int factionId = -1;
    public string factionName = "";
    public int score = 0;
}
