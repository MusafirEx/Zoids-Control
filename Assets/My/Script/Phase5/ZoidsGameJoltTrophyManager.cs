using System;
using System.Collections.Generic;
using UnityEngine;
using GameJolt.API;

public class ZoidsGameJoltTrophyManager : MonoBehaviour
{
    public static ZoidsGameJoltTrophyManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private ZoidsGameJoltAccountManager accountManager;
    [SerializeField] private PlayerProfileManager profileManager;
    [SerializeField] private UnitProgressManager unitProgressManager;

    [Header("Trophy Rules")]
    [SerializeField] private List<ZoidsGameJoltTrophyRule> trophyRules = new List<ZoidsGameJoltTrophyRule>();

    [Header("Options")]
    [SerializeField] private bool checkOnStart = false;
    [SerializeField] private bool checkOnEnable = false;
    [SerializeField] private bool debugLog = true;

    private bool isChecking = false;

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
        if (checkOnStart)
            CheckAllTrophies();
    }

    private void OnEnable()
    {
        RefreshRuntimeReferences();

        if (checkOnEnable)
            CheckAllTrophies();
    }

    public void RefreshRuntimeReferences()
    {
        if (accountManager == null && ZoidsGameJoltAccountManager.Instance != null)
            accountManager = ZoidsGameJoltAccountManager.Instance;

        if (profileManager == null && PlayerProfileManager.Instance != null)
            profileManager = PlayerProfileManager.Instance;

        if (unitProgressManager == null && UnitProgressManager.Instance != null)
            unitProgressManager = UnitProgressManager.Instance;

        if (accountManager == null)
            accountManager = FindManager<ZoidsGameJoltAccountManager>();

        if (profileManager == null)
            profileManager = FindManager<PlayerProfileManager>();

        if (unitProgressManager == null)
            unitProgressManager = FindManager<UnitProgressManager>();
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

    public void CheckAllTrophies()
    {
        if (isChecking)
        {
            if (debugLog)
                Debug.Log("[ZoidsGameJoltTrophyManager] Check ignored. Already checking.");
            return;
        }

        if (!IsLoggedIn())
        {
            if (debugLog)
                Debug.Log("[ZoidsGameJoltTrophyManager] Skip trophy check. Game Jolt user not logged in.");
            return;
        }

        isChecking = true;

        if (trophyRules == null)
            trophyRules = new List<ZoidsGameJoltTrophyRule>();

        for (int i = 0; i < trophyRules.Count; i++)
        {
            ZoidsGameJoltTrophyRule rule = trophyRules[i];

            if (rule == null || !rule.enabled)
                continue;

            if (IsRuleConditionMet(rule))
                TryUnlockTrophy(rule.trophyId, rule.debugName);
        }

        isChecking = false;
    }

    public void CheckOwnedUnitTrophies()
    {
        if (!IsLoggedIn())
            return;

        for (int i = 0; i < trophyRules.Count; i++)
        {
            ZoidsGameJoltTrophyRule rule = trophyRules[i];

            if (rule == null || !rule.enabled)
                continue;

            if (rule.conditionType != ZoidsTrophyConditionType.OwnTotalUnits &&
                rule.conditionType != ZoidsTrophyConditionType.OwnSpecificUnitCount &&
                rule.conditionType != ZoidsTrophyConditionType.OwnUniqueUnitTypes)
                continue;

            if (IsRuleConditionMet(rule))
                TryUnlockTrophy(rule.trophyId, rule.debugName);
        }
    }

    public void CheckSpecificEvent(ZoidsTrophyConditionType eventType)
    {
        if (!IsLoggedIn())
            return;

        for (int i = 0; i < trophyRules.Count; i++)
        {
            ZoidsGameJoltTrophyRule rule = trophyRules[i];

            if (rule == null || !rule.enabled)
                continue;

            if (rule.conditionType != eventType)
                continue;

            if (IsRuleConditionMet(rule))
                TryUnlockTrophy(rule.trophyId, rule.debugName);
        }
    }

    public void TryUnlockTrophyByRuleIndex(int ruleIndex)
    {
        if (trophyRules == null || ruleIndex < 0 || ruleIndex >= trophyRules.Count)
        {
            Debug.LogWarning("[ZoidsGameJoltTrophyManager] Invalid rule index=" + ruleIndex);
            return;
        }

        ZoidsGameJoltTrophyRule rule = trophyRules[ruleIndex];

        if (rule == null)
            return;

        if (IsRuleConditionMet(rule))
            TryUnlockTrophy(rule.trophyId, rule.debugName);
    }

    public void TryUnlockTrophy(int trophyId, string debugName = "")
    {
        if (trophyId <= 0)
        {
            Debug.LogWarning("[ZoidsGameJoltTrophyManager] Invalid Game Jolt trophy ID=" + trophyId);
            return;
        }

        if (!IsLoggedIn())
        {
            if (debugLog)
                Debug.Log("[ZoidsGameJoltTrophyManager] Cannot unlock trophy. Not logged in. trophyId=" + trophyId);

            return;
        }

        Trophies.TryUnlock(trophyId, result =>
        {
            if (debugLog)
            {
                Debug.Log("[ZoidsGameJoltTrophyManager] Trophy result. ID=" + trophyId +
                          " Name=" + debugName +
                          " Result=" + result);
            }
        });
    }

    private bool IsRuleConditionMet(ZoidsGameJoltTrophyRule rule)
    {
        if (rule == null)
            return false;

        switch (rule.conditionType)
        {
            case ZoidsTrophyConditionType.ManualOnly:
                return false;

            case ZoidsTrophyConditionType.OwnTotalUnits:
                return GetTotalOwnedUnitCount() >= rule.requiredAmount;

            case ZoidsTrophyConditionType.OwnSpecificUnitCount:
                return GetOwnedCount(rule.unitId) >= rule.requiredAmount;

            case ZoidsTrophyConditionType.OwnUniqueUnitTypes:
                return GetUniqueOwnedUnitTypeCount() >= rule.requiredAmount;

            case ZoidsTrophyConditionType.ProfileFactionChosen:
                return profileManager != null &&
                       profileManager.CurrentProfile != null &&
                       profileManager.CurrentProfile.profileInitialized;

            case ZoidsTrophyConditionType.FirstAreaWin:
            case ZoidsTrophyConditionType.FirstColosseumClear:
            case ZoidsTrophyConditionType.FirstPerkUnlock:
            case ZoidsTrophyConditionType.FirstFactoryManufacture:
                // These should be checked by event call after the event happens.
                return true;

            default:
                return false;
        }
    }

    private int GetTotalOwnedUnitCount()
    {
        RefreshRuntimeReferences();

        if (unitProgressManager == null)
            return 0;

        int total = 0;
        List<IntValueEntry> ownedEntries = unitProgressManager.GetOwnedUnitEntries();

        if (ownedEntries == null)
            return 0;

        for (int i = 0; i < ownedEntries.Count; i++)
        {
            if (ownedEntries[i] == null)
                continue;

            total += Mathf.Max(0, ownedEntries[i].value);
        }

        return total;
    }

    private int GetUniqueOwnedUnitTypeCount()
    {
        RefreshRuntimeReferences();

        if (unitProgressManager == null)
            return 0;

        int count = 0;
        List<IntValueEntry> ownedEntries = unitProgressManager.GetOwnedUnitEntries();

        if (ownedEntries == null)
            return 0;

        for (int i = 0; i < ownedEntries.Count; i++)
        {
            if (ownedEntries[i] == null)
                continue;

            if (ownedEntries[i].value > 0)
                count++;
        }

        return count;
    }

    private int GetOwnedCount(int unitId)
    {
        RefreshRuntimeReferences();

        if (unitProgressManager == null)
            return 0;

        return unitProgressManager.GetOwnedCount(unitId);
    }

    // ---------------------------------------------------------
    // Convenience calls for gameplay scripts
    // ---------------------------------------------------------

    public void NotifyFactoryManufacture()
    {
        CheckSpecificEvent(ZoidsTrophyConditionType.FirstFactoryManufacture);
        CheckOwnedUnitTrophies();
    }

    public void NotifyAreaBattleWin()
    {
        CheckSpecificEvent(ZoidsTrophyConditionType.FirstAreaWin);
    }

    public void NotifyColosseumClear()
    {
        CheckSpecificEvent(ZoidsTrophyConditionType.FirstColosseumClear);
    }

    public void NotifyPerkUnlocked()
    {
        CheckSpecificEvent(ZoidsTrophyConditionType.FirstPerkUnlock);
    }

    public void NotifyFactionChosen()
    {
        CheckSpecificEvent(ZoidsTrophyConditionType.ProfileFactionChosen);
    }
}

[Serializable]
public class ZoidsGameJoltTrophyRule
{
    public bool enabled = true;

    [Tooltip("For your own inspector note only, eg: Own 5 Zoids, First Area Win.")]
    public string debugName = "New Trophy";

    [Tooltip("Game Jolt Trophy ID from your Game Jolt dashboard.")]
    public int trophyId = 0;

    public ZoidsTrophyConditionType conditionType = ZoidsTrophyConditionType.ManualOnly;

    [Tooltip("Used by OwnTotalUnits, OwnSpecificUnitCount, OwnUniqueUnitTypes.")]
    public int requiredAmount = 1;

    [Tooltip("Used only by OwnSpecificUnitCount.")]
    public int unitId = -1;
}

public enum ZoidsTrophyConditionType
{
    ManualOnly = 0,

    // Unit ownership trophies
    OwnTotalUnits = 10,
    OwnSpecificUnitCount = 11,
    OwnUniqueUnitTypes = 12,

    // Event trophies
    ProfileFactionChosen = 100,
    FirstAreaWin = 101,
    FirstColosseumClear = 102,
    FirstPerkUnlock = 103,
    FirstFactoryManufacture = 104
}
