using System.Collections.Generic;
using UnityEngine;
using TBTK;

public class ZoidsPerkProgressManager : MonoBehaviour
{
    public static ZoidsPerkProgressManager Instance { get; private set; }

    [Header("Save")]
    [SerializeField] private string playerPrefsKey = "ZOIDS_PERK_PROGRESS_V1";
    [SerializeField] private bool loadOnAwake = true;

    [Tooltip("Safe mode: OnDisable saves current data only, it does not pull 0 from a newly-created PerkManager.")]
    [SerializeField] private bool saveDataOnlyOnDisable = true;

    [Header("Default")]
    [SerializeField] private int defaultCurrency = 0;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private ZoidsPerkProgressData data = new ZoidsPerkProgressData();

    // Critical safety:
    // When a duplicate object is destroyed, Unity still calls OnDisable().
    // Without this flag, the duplicate can save default currency 0 and overwrite real progress.
    private bool isDuplicateBeingDestroyed = false;
    private bool hasLoadedFromPrefs = false;

    public ZoidsPerkProgressData CurrentData { get { return data; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            isDuplicateBeingDestroyed = true;

            if (debugLog)
                Debug.LogWarning("[ZoidsPerkProgressManager] Duplicate detected. Destroying duplicate without saving.");

            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadOnAwake)
            LoadProgress();
    }

    private void OnEnable()
    {
        if (Instance == this && loadOnAwake && !hasLoadedFromPrefs)
            LoadProgress();
    }

    private void OnDisable()
    {
        if (isDuplicateBeingDestroyed)
            return;

        if (Instance != this)
            return;

        if (!hasLoadedFromPrefs)
            return;

        if (saveDataOnlyOnDisable)
            SaveProgressDataOnly();
        else
            SaveProgressFromPerkManagerSafe();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnApplicationQuit()
    {
        if (Instance != this)
            return;

        SaveProgressDataOnly();
    }

    public void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(playerPrefsKey))
        {
            data = new ZoidsPerkProgressData();
            data.currency = defaultCurrency;
            hasLoadedFromPrefs = true;
            SaveProgressDataOnly();

            if (debugLog)
                Debug.Log("[ZoidsPerkProgressManager] Created new perk progress. Currency=" + data.currency);

            return;
        }

        string json = PlayerPrefs.GetString(playerPrefsKey, "");
        if (string.IsNullOrEmpty(json))
        {
            data = new ZoidsPerkProgressData();
            data.currency = defaultCurrency;
            hasLoadedFromPrefs = true;
            SaveProgressDataOnly();
            return;
        }

        try
        {
            data = JsonUtility.FromJson<ZoidsPerkProgressData>(json);
        }
        catch
        {
            data = new ZoidsPerkProgressData();
            data.currency = defaultCurrency;
        }

        EnsureDataValid();
        hasLoadedFromPrefs = true;

        if (debugLog)
            Debug.Log("[ZoidsPerkProgressManager] Loaded perk progress. Currency=" + data.currency +
                      " Unlocked=" + data.unlockedPerkIds.Count);
    }

    private void EnsureDataValid()
    {
        if (data == null)
            data = new ZoidsPerkProgressData();

        if (data.unlockedPerkIds == null)
            data.unlockedPerkIds = new List<int>();
    }

    public void ApplyProgressToPerkManager()
    {
        EnsureDataValid();

        if (!PerkManager.PerkSystemEnabled())
        {
            if (debugLog)
                Debug.LogWarning("[ZoidsPerkProgressManager] Cannot apply progress. PerkManager is missing.");
            return;
        }

        PerkManager.cacheCurrency = data.currency;
        PerkManager.cacheUnlockedIDList = new List<int>(data.unlockedPerkIds);

        PerkManager manager = FindManager<PerkManager>();
        if (manager != null)
        {
            manager.currency = data.currency;
            manager.unlockedIDList = new List<int>(data.unlockedPerkIds);

            List<Perk> perks = PerkManager.GetPerkList();
            for (int i = 0; i < perks.Count; i++)
            {
                if (data.unlockedPerkIds.Contains(perks[i].prefabID) && !perks[i].IsUnlocked())
                    PerkManager.UnlockPerk(perks[i].prefabID, false);
            }
        }

        if (debugLog)
            Debug.Log("[ZoidsPerkProgressManager] Applied progress to PerkManager. Currency=" + data.currency +
                      " Unlocked=" + data.unlockedPerkIds.Count);
    }

    public void SaveProgressFromPerkManager()
    {
        SaveProgressFromPerkManagerSafe();
    }

    public void SaveProgressFromPerkManagerSafe()
    {
        if (isDuplicateBeingDestroyed || Instance != this)
            return;

        EnsureDataValid();

        if (!PerkManager.PerkSystemEnabled())
        {
            SaveProgressDataOnly();
            return;
        }

        int oldCurrency = data.currency;
        List<int> oldUnlocked = new List<int>(data.unlockedPerkIds);

        int managerCurrency = PerkManager.GetPerkCurrency();

        if (oldCurrency > 0 && managerCurrency == 0)
        {
            if (debugLog)
                Debug.LogWarning("[ZoidsPerkProgressManager] Ignored PerkManager currency 0. Keeping saved currency=" + oldCurrency);
        }
        else
        {
            data.currency = managerCurrency;
        }

        data.unlockedPerkIds.Clear();

        List<Perk> perks = PerkManager.GetPerkList();
        for (int i = 0; i < perks.Count; i++)
        {
            if (perks[i] != null && perks[i].IsUnlocked())
                data.unlockedPerkIds.Add(perks[i].prefabID);
        }

        if (data.unlockedPerkIds.Count == 0 && oldUnlocked.Count > 0)
            data.unlockedPerkIds = oldUnlocked;

        SaveProgressDataOnly();

        if (debugLog)
            Debug.Log("[ZoidsPerkProgressManager] Saved from PerkManager safely. Currency=" + data.currency +
                      " Unlocked=" + data.unlockedPerkIds.Count);
    }

    public void SaveProgressDataOnly()
    {
        if (isDuplicateBeingDestroyed || Instance != this)
            return;

        EnsureDataValid();

        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(playerPrefsKey, json);
        PlayerPrefs.Save();

        hasLoadedFromPrefs = true;

        if (debugLog)
            Debug.Log("[ZoidsPerkProgressManager] Saved data only. Currency=" + data.currency +
                      " Unlocked=" + data.unlockedPerkIds.Count);
    }

    public bool IsPerkUnlocked(int perkId)
    {
        EnsureDataValid();
        return data.unlockedPerkIds.Contains(perkId);
    }

    public void AddCurrencyDebug(int amount)
    {
        AddCurrency(amount, "Debug");
    }

    public void AddCurrency(int amount, string reason = "")
    {
        if (isDuplicateBeingDestroyed)
            return;

        if (Instance != this && Instance != null)
        {
            Instance.AddCurrency(amount, reason);
            return;
        }

        EnsureDataValid();

        // Make sure we add on top of latest PlayerPrefs data, not stale scene data.
        if (!hasLoadedFromPrefs)
            LoadProgress();

        if (amount == 0)
            return;

        data.currency = Mathf.Max(0, data.currency + amount);

        if (PerkManager.PerkSystemEnabled())
        {
            PerkManager manager = FindManager<PerkManager>();
            if (manager != null)
                manager.currency = data.currency;
        }

        PerkManager.cacheCurrency = data.currency;
        SaveProgressDataOnly();

        if (debugLog)
            Debug.Log("[ZoidsPerkProgressManager] Add currency: " + amount +
                      " NewCurrency=" + data.currency +
                      " Reason=" + reason);
    }

    public bool SpendCurrency(int amount, string reason = "")
    {
        if (Instance != this && Instance != null)
            return Instance.SpendCurrency(amount, reason);

        EnsureDataValid();

        if (!hasLoadedFromPrefs)
            LoadProgress();

        if (amount <= 0)
            return true;

        if (data.currency < amount)
            return false;

        data.currency -= amount;

        if (PerkManager.PerkSystemEnabled())
        {
            PerkManager manager = FindManager<PerkManager>();
            if (manager != null)
                manager.currency = data.currency;
        }

        PerkManager.cacheCurrency = data.currency;
        SaveProgressDataOnly();

        if (debugLog)
            Debug.Log("[ZoidsPerkProgressManager] Spend currency: " + amount +
                      " NewCurrency=" + data.currency +
                      " Reason=" + reason);

        return true;
    }

    public void RegisterUnlockedPerk(int perkId)
    {
        if (Instance != this && Instance != null)
        {
            Instance.RegisterUnlockedPerk(perkId);
            return;
        }

        EnsureDataValid();

        if (!hasLoadedFromPrefs)
            LoadProgress();

        if (!data.unlockedPerkIds.Contains(perkId))
            data.unlockedPerkIds.Add(perkId);

        SaveProgressDataOnly();

        if (debugLog)
            Debug.Log("[ZoidsPerkProgressManager] Registered unlocked perkId=" + perkId);
    }

    public void ClearProgress()
    {
        if (Instance != this && Instance != null)
        {
            Instance.ClearProgress();
            return;
        }

        data = new ZoidsPerkProgressData();
        data.currency = defaultCurrency;
        hasLoadedFromPrefs = true;
        SaveProgressDataOnly();

        if (PerkManager.PerkSystemEnabled())
        {
            PerkManager manager = FindManager<PerkManager>();
            if (manager != null)
            {
                manager.currency = data.currency;
                manager.unlockedIDList.Clear();
            }
        }

        PerkManager.cacheCurrency = data.currency;
        PerkManager.cacheUnlockedIDList.Clear();

        if (debugLog)
            Debug.Log("[ZoidsPerkProgressManager] Cleared perk progress.");
    }

    public void DebugPrintSavedJson()
    {
        string json = PlayerPrefs.GetString(playerPrefsKey, "NO SAVE FOUND");
        Debug.Log("[ZoidsPerkProgressManager] PlayerPrefs JSON: " + json);
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

[System.Serializable]
public class ZoidsPerkProgressData
{
    public int currency = 0;
    public List<int> unlockedPerkIds = new List<int>();
}
