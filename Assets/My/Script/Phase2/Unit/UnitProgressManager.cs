using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitProgressManager : MonoBehaviour
{
    public static UnitProgressManager Instance { get; private set; }

    public const string DefaultSaveKey = "zoids_unit_progress_main";

    [Header("Local Save")]
    [SerializeField] private string saveKey = DefaultSaveKey;
    [SerializeField] private bool autoLoadOnAwake = true;

    [Header("References")]
    [SerializeField] private PlayerProfileManager profileManager;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public UnitProgressData CurrentData { get; private set; }

    public event Action<UnitProgressData> OnProgressLoaded;
    public event Action<UnitProgressData> OnProgressSaved;
    public event Action<int, int> OnUnitDataChanged;
    public event Action<int, int> OnUnitOwnedChanged;

    private void Reset()
    {
        RefreshRuntimeReferences();
    }

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

        if (autoLoadOnAwake)
            LoadProgress();
    }

    private void OnEnable()
    {
        RefreshRuntimeReferences();
    }

    public void RefreshRuntimeReferences()
    {
        if (profileManager == null && PlayerProfileManager.Instance != null)
            profileManager = PlayerProfileManager.Instance;

        if (profileManager == null)
            profileManager = FindFirstObjectByTypeCompat<PlayerProfileManager>();

        if (debugLog && profileManager == null)
            Debug.LogWarning("[UnitProgressManager] PlayerProfileManager not found during refresh.");
    }

    private T FindFirstObjectByTypeCompat<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return UnityEngine.Object.FindObjectOfType<T>(true);
#endif
    }

    public bool HasSavedProgress()
    {
        return PlayerPrefs.HasKey(saveKey) && !string.IsNullOrEmpty(PlayerPrefs.GetString(saveKey, ""));
    }

    public UnitProgressData LoadProgress()
    {
        if (!HasSavedProgress())
        {
            CurrentData = new UnitProgressData();
            CurrentData.Touch();
            return CurrentData;
        }

        string json = PlayerPrefs.GetString(saveKey, "");
        if (string.IsNullOrEmpty(json))
        {
            CurrentData = new UnitProgressData();
            CurrentData.Touch();
            return CurrentData;
        }

        try
        {
            CurrentData = JsonUtility.FromJson<UnitProgressData>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("[UnitProgressManager] Failed to parse unit progress JSON: " + ex.Message);
            CurrentData = new UnitProgressData();
        }

        if (CurrentData == null)
            CurrentData = new UnitProgressData();

        if (CurrentData.unitData == null)
            CurrentData.unitData = new List<UnitProgressEntry>();

        OnProgressLoaded?.Invoke(CurrentData);
        return CurrentData;
    }

    public void SaveProgress()
    {
        if (CurrentData == null)
            CurrentData = new UnitProgressData();

        CurrentData.Touch();

        string json = JsonUtility.ToJson(CurrentData, true);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();

        RefreshRuntimeReferences();
        if (profileManager != null)
            profileManager.SaveProfile();

        OnProgressSaved?.Invoke(CurrentData);

        if (debugLog)
            Debug.Log("[UnitProgressManager] Progress saved.");
    }

    public void ClearProgress()
    {
        CurrentData = new UnitProgressData();
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();

        if (debugLog)
            Debug.Log("[UnitProgressManager] Unit data progress cleared.");
    }

    // ---------------------------------------------------------
    // Unit data used for Factory manufacture cost.
    // ---------------------------------------------------------

    public int GetUnitData(int unitId)
    {
        EnsureLoaded();
        return CurrentData.GetDataAmount(unitId);
    }

    public void SetUnitData(int unitId, int amount, bool autoSave = true)
    {
        EnsureLoaded();
        CurrentData.SetDataAmount(unitId, amount);

        OnUnitDataChanged?.Invoke(unitId, CurrentData.GetDataAmount(unitId));

        if (autoSave)
            SaveProgress();
    }

    public void AddUnitData(int unitId, int amount, bool autoSave = true)
    {
        if (amount == 0)
            return;

        EnsureLoaded();

        int oldAmount = CurrentData.GetDataAmount(unitId);
        CurrentData.AddDataAmount(unitId, amount);
        int newAmount = CurrentData.GetDataAmount(unitId);

        OnUnitDataChanged?.Invoke(unitId, newAmount);

        if (debugLog)
            Debug.Log("[UnitProgressManager] Unit " + unitId + " data " + oldAmount + " -> " + newAmount);

        if (autoSave)
            SaveProgress();
    }

    public bool HasEnoughUnitData(int unitId, int amount)
    {
        return GetUnitData(unitId) >= amount;
    }

    public bool SpendUnitData(int unitId, int amount, bool autoSave = true)
    {
        if (amount <= 0)
            return true;

        EnsureLoaded();

        int current = CurrentData.GetDataAmount(unitId);
        if (current < amount)
            return false;

        CurrentData.SetDataAmount(unitId, current - amount);
        OnUnitDataChanged?.Invoke(unitId, CurrentData.GetDataAmount(unitId));

        if (debugLog)
            Debug.Log("[UnitProgressManager] Spent " + amount + " unit data from unit " + unitId + ". Remaining=" + CurrentData.GetDataAmount(unitId));

        if (autoSave)
            SaveProgress();

        return true;
    }

    // ---------------------------------------------------------
    // Owned unit count used by Factory and Team Manager.
    // Stored in PlayerProfileData.ownedUnits.
    // ---------------------------------------------------------

    public int GetOwnedCount(int unitId)
    {
        PlayerProfileData profile = GetProfile(false);
        if (profile == null)
            return 0;

        return profile.GetOwnedCount(unitId);
    }

    public bool OwnsUnit(int unitId)
    {
        return GetOwnedCount(unitId) > 0;
    }

    public void SetOwnedCount(int unitId, int count, bool autoSave = true)
    {
        PlayerProfileData profile = GetProfile(true);
        if (profile == null)
            return;

        profile.SetOwnedCount(unitId, Mathf.Max(0, count));
        profile.Touch();

        OnUnitOwnedChanged?.Invoke(unitId, profile.GetOwnedCount(unitId));

        if (autoSave)
            SaveProgress();
    }

    public void AddOwnedCount(int unitId, int amount, bool autoSave = true)
    {
        if (amount == 0)
            return;

        PlayerProfileData profile = GetProfile(true);
        if (profile == null)
            return;

        profile.AddOwnedCount(unitId, amount);
        profile.Touch();

        OnUnitOwnedChanged?.Invoke(unitId, profile.GetOwnedCount(unitId));

        if (debugLog)
            Debug.Log("[UnitProgressManager] Unit " + unitId + " owned count=" + profile.GetOwnedCount(unitId));

        if (autoSave)
            SaveProgress();
    }

    public List<IntValueEntry> GetOwnedUnitEntries()
    {
        PlayerProfileData profile = GetProfile(false);
        if (profile == null || profile.ownedUnits == null)
            return new List<IntValueEntry>();

        List<IntValueEntry> copy = new List<IntValueEntry>();
        for (int i = 0; i < profile.ownedUnits.Count; i++)
        {
            if (profile.ownedUnits[i] == null) continue;
            if (profile.ownedUnits[i].value <= 0) continue;

            copy.Add(new IntValueEntry(profile.ownedUnits[i].id, profile.ownedUnits[i].value));
        }

        return copy;
    }

    public int GetTotalOwnedUnitCount()
    {
        List<IntValueEntry> owned = GetOwnedUnitEntries();

        int total = 0;
        for (int i = 0; i < owned.Count; i++)
            total += owned[i].value;

        return total;
    }

    private PlayerProfileData GetProfile(bool createIfMissing)
    {
        RefreshRuntimeReferences();

        if (profileManager == null)
        {
            Debug.LogWarning("[UnitProgressManager] Missing PlayerProfileManager.");
            return null;
        }

        if (profileManager.CurrentProfile == null && createIfMissing)
            profileManager.CreateNewProfile();

        return profileManager.CurrentProfile;
    }

    private void EnsureLoaded()
    {
        if (CurrentData == null)
            LoadProgress();

        if (CurrentData.unitData == null)
            CurrentData.unitData = new List<UnitProgressEntry>();
    }
}
