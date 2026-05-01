using System;
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

    public UnitProgressData CurrentData { get; private set; }

    public event Action<UnitProgressData> OnProgressLoaded;
    public event Action<UnitProgressData> OnProgressSaved;
    public event Action<int, int> OnUnitDataChanged;
    public event Action<int, int> OnUnitOwnedChanged;

    private void Reset()
    {
        profileManager = FindObjectOfType<PlayerProfileManager>();
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

        if (profileManager == null)
            profileManager = FindObjectOfType<PlayerProfileManager>();

        if (autoLoadOnAwake)
            LoadProgress();
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

        OnProgressSaved?.Invoke(CurrentData);
    }

    public void ClearProgress()
    {
        CurrentData = new UnitProgressData();
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
    }

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
        if (amount <= 0)
            return;

        EnsureLoaded();
        CurrentData.AddDataAmount(unitId, amount);

        OnUnitDataChanged?.Invoke(unitId, CurrentData.GetDataAmount(unitId));

        if (autoSave)
            SaveProgress();
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

        if (autoSave)
            SaveProgress();

        return true;
    }

    public int GetOwnedCount(int unitId)
    {
        if (profileManager == null || profileManager.CurrentProfile == null)
            return 0;

        return profileManager.CurrentProfile.GetOwnedCount(unitId);
    }

    public void AddOwnedCount(int unitId, int amount, bool autoSave = true)
    {
        if (amount == 0)
            return;

        if (profileManager == null)
            profileManager = FindObjectOfType<PlayerProfileManager>();

        if (profileManager == null)
        {
            Debug.LogError("[UnitProgressManager] Missing PlayerProfileManager reference.");
            return;
        }

        if (profileManager.CurrentProfile == null)
            profileManager.CreateNewProfile();

        profileManager.CurrentProfile.AddOwnedCount(unitId, amount);
        OnUnitOwnedChanged?.Invoke(unitId, profileManager.CurrentProfile.GetOwnedCount(unitId));

        if (autoSave)
            profileManager.SaveProfile();
    }

    private void EnsureLoaded()
    {
        if (CurrentData == null)
            LoadProgress();
    }
}
