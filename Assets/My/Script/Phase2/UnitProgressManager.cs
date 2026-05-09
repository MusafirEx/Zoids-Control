using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UnitDataProgressEntry
{
    public int unitId;
    public int dataAmount;

    public UnitDataProgressEntry(int unitId, int dataAmount)
    {
        this.unitId = unitId;
        this.dataAmount = dataAmount;
    }
}

[Serializable]
public class UnitDataProgressSave
{
    public List<UnitDataProgressEntry> unitData = new List<UnitDataProgressEntry>();
}

public class UnitProgressManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerProfileManager profileManager;

    [Header("Save")]
    [SerializeField] private string playerPrefsKey = "ZOIDS_UNIT_DATA_PROGRESS_V1";

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private UnitDataProgressSave progress = new UnitDataProgressSave();

    private void Reset()
    {
        RefreshRuntimeReferences();
    }

    private void Awake()
    {
        RefreshRuntimeReferences();
        LoadProgress();
    }

    private void OnEnable()
    {
        RefreshRuntimeReferences();
    }

    public void RefreshRuntimeReferences()
    {
        if (profileManager == null)
            profileManager = FindFirstObjectByTypeCompat<PlayerProfileManager>();

        if (debugLog && profileManager == null)
            Debug.LogWarning("[UnitProgressManager] PlayerProfileManager not found during refresh.");
    }

    private T FindFirstObjectByTypeCompat<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<T>();
#else
        return UnityEngine.Object.FindObjectOfType<T>();
#endif
    }

    private PlayerProfileData GetProfile()
    {
        RefreshRuntimeReferences();

        if (profileManager == null)
            return null;

        PlayerProfileData profile = profileManager.EnsureProfile();
        if (profile == null)
            Debug.LogWarning("[UnitProgressManager] Player profile is null.");

        return profile;
    }

    // ---------------------------------------------------------
    // Unit data progress
    // Stored here because current PlayerProfileData has no
    // GetUnitData / SetUnitData methods.
    // ---------------------------------------------------------

    public int GetUnitData(int unitId)
    {
        UnitDataProgressEntry entry = GetUnitDataEntry(unitId, false);
        return entry != null ? entry.dataAmount : 0;
    }

    public void SetUnitData(int unitId, int amount, bool save = true)
    {
        UnitDataProgressEntry entry = GetUnitDataEntry(unitId, true);
        entry.dataAmount = Mathf.Max(0, amount);

        TouchProfile();

        if (save)
            SaveProgress();
    }

    public void AddUnitData(int unitId, int amount, bool save = true)
    {
        if (amount == 0)
            return;

        UnitDataProgressEntry entry = GetUnitDataEntry(unitId, true);
        int oldAmount = entry.dataAmount;
        entry.dataAmount = Mathf.Max(0, entry.dataAmount + amount);

        TouchProfile();

        if (debugLog)
            Debug.Log("[UnitProgressManager] Unit " + unitId + " data " + oldAmount + " -> " + entry.dataAmount);

        if (save)
            SaveProgress();
    }

    public bool HasEnoughUnitData(int unitId, int amount)
    {
        return GetUnitData(unitId) >= amount;
    }

    public bool SpendUnitData(int unitId, int amount, bool save = true)
    {
        if (amount <= 0)
            return true;

        UnitDataProgressEntry entry = GetUnitDataEntry(unitId, true);
        if (entry.dataAmount < amount)
            return false;

        entry.dataAmount -= amount;

        TouchProfile();

        if (debugLog)
            Debug.Log("[UnitProgressManager] Spent " + amount + " data from unit " + unitId + ". Remaining=" + entry.dataAmount);

        if (save)
            SaveProgress();

        return true;
    }

    private UnitDataProgressEntry GetUnitDataEntry(int unitId, bool createIfMissing)
    {
        if (progress == null)
            progress = new UnitDataProgressSave();

        if (progress.unitData == null)
            progress.unitData = new List<UnitDataProgressEntry>();

        for (int i = 0; i < progress.unitData.Count; i++)
        {
            if (progress.unitData[i] != null && progress.unitData[i].unitId == unitId)
                return progress.unitData[i];
        }

        if (!createIfMissing)
            return null;

        UnitDataProgressEntry entry = new UnitDataProgressEntry(unitId, 0);
        progress.unitData.Add(entry);
        return entry;
    }

    // ---------------------------------------------------------
    // Owned unit count
    // FactoryManager expects these methods on UnitProgressManager.
    // These use PlayerProfileData because it already has ownedUnits.
    // ---------------------------------------------------------

    public int GetOwnedCount(int unitId)
    {
        PlayerProfileData profile = GetProfile();
        if (profile == null)
            return 0;

        return profile.GetOwnedCount(unitId);
    }

    public void SetOwnedCount(int unitId, int amount, bool save = true)
    {
        PlayerProfileData profile = GetProfile();
        if (profile == null)
            return;

        profile.SetOwnedCount(unitId, Mathf.Max(0, amount));
        profile.Touch();

        if (save)
            SaveProgress();
    }

    public void AddOwnedCount(int unitId, int amount, bool save = true)
    {
        PlayerProfileData profile = GetProfile();
        if (profile == null)
            return;

        profile.AddOwnedCount(unitId, amount);
        profile.Touch();

        if (save)
            SaveProgress();
    }

    public bool OwnsUnit(int unitId)
    {
        return GetOwnedCount(unitId) > 0;
    }

    // ---------------------------------------------------------
    // Save / Load
    // ---------------------------------------------------------

    public void SaveProgress()
    {
        RefreshRuntimeReferences();

        if (progress == null)
            progress = new UnitDataProgressSave();

        string json = JsonUtility.ToJson(progress);
        PlayerPrefs.SetString(playerPrefsKey, json);
        PlayerPrefs.Save();

        if (profileManager != null)
            profileManager.SaveProfile();

        if (debugLog)
            Debug.Log("[UnitProgressManager] Progress saved.");
    }

    public void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(playerPrefsKey))
        {
            progress = new UnitDataProgressSave();
            return;
        }

        string json = PlayerPrefs.GetString(playerPrefsKey, "");
        if (string.IsNullOrEmpty(json))
        {
            progress = new UnitDataProgressSave();
            return;
        }

        try
        {
            progress = JsonUtility.FromJson<UnitDataProgressSave>(json);
            if (progress == null)
                progress = new UnitDataProgressSave();
        }
        catch
        {
            progress = new UnitDataProgressSave();
        }

        if (progress.unitData == null)
            progress.unitData = new List<UnitDataProgressEntry>();

        if (debugLog)
            Debug.Log("[UnitProgressManager] Progress loaded. Count=" + progress.unitData.Count);
    }

    public void ClearProgress()
    {
        progress = new UnitDataProgressSave();
        PlayerPrefs.DeleteKey(playerPrefsKey);
        PlayerPrefs.Save();

        if (debugLog)
            Debug.Log("[UnitProgressManager] Progress cleared.");
    }

    private void TouchProfile()
    {
        PlayerProfileData profile = GetProfile();
        if (profile != null)
            profile.Touch();
    }
}
