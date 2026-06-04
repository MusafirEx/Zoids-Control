using System;
using System.Collections.Generic;
using UnityEngine;
using TBTK;

public class FactoryManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnitProgressManager progressManager;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public event Action<int> OnManufactureSuccess;
    public event Action<int> OnManufactureFailed;

    private void Reset()
    {
        RefreshRuntimeReferences();
    }

    private void Awake()
    {
        RefreshRuntimeReferences();
    }

    private void OnEnable()
    {
        RefreshRuntimeReferences();
    }

    public void RefreshRuntimeReferences()
    {
        // Always prefer persistent instance to avoid stale scene reference in WebGL scene changes.
        if (UnitProgressManager.Instance != null)
            progressManager = UnitProgressManager.Instance;

        if (progressManager == null)
            progressManager = FindFirstObjectByTypeCompat<UnitProgressManager>();

        if (debugLog && progressManager == null)
            Debug.LogWarning("[FactoryManager] UnitProgressManager not found during refresh.");
    }

    private T FindFirstObjectByTypeCompat<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return UnityEngine.Object.FindObjectOfType<T>(true);
#endif
    }

    public Unit GetUnitDefinition(int unitId)
    {
        return UnitDB.GetPrefab(unitId);
    }

    public List<Unit> GetAllFactoryUnits()
    {
        List<Unit> list = UnitDB.GetList();
        return list != null ? list : new List<Unit>();
    }

    public int GetManufactureCost(int unitId)
    {
        Unit unit = GetUnitDefinition(unitId);
        if (unit == null)
            return 100;

        return Mathf.Max(1, unit.factoryCost);
    }

    public UnitRarity GetRarity(int unitId)
    {
        Unit unit = GetUnitDefinition(unitId);
        if (unit == null)
            return UnitRarity.Common;

        return unit.rarity;
    }

    public string GetUnitName(int unitId)
    {
        Unit unit = GetUnitDefinition(unitId);
        if (unit == null)
            return "Unit " + unitId;

        if (!string.IsNullOrEmpty(unit.itemName))
            return unit.itemName;

        return unit.gameObject.name;
    }

    public string GetUnitDescription(int unitId)
    {
        Unit unit = GetUnitDefinition(unitId);
        if (unit == null)
            return "";

        return unit.unitDescription;
    }

    public Sprite GetUnitIcon(int unitId)
    {
        Unit unit = GetUnitDefinition(unitId);
        if (unit == null)
            return null;

        return unit.icon;
    }

    public bool CanManufacture(int unitId)
    {
        return CanManufacture(unitId, 1);
    }

    public bool CanManufacture(int unitId, int quantity)
    {
        RefreshRuntimeReferences();

        if (progressManager == null)
            return false;

        quantity = Mathf.Max(1, quantity);

        if (!CanOwnMore(unitId, quantity))
            return false;

        return progressManager.GetUnitData(unitId) >= GetManufactureCost(unitId) * quantity;
    }

    public int GetCurrentData(int unitId)
    {
        RefreshRuntimeReferences();

        if (progressManager == null)
            return 0;

        return progressManager.GetUnitData(unitId);
    }

    public int GetRemainingDataNeeded(int unitId)
    {
        return Mathf.Max(0, GetManufactureCost(unitId) - GetCurrentData(unitId));
    }

    public int GetOwnedCount(int unitId)
    {
        RefreshRuntimeReferences();

        if (progressManager == null)
            return 0;

        return progressManager.GetOwnedCount(unitId);
    }

    public bool IsOwnedLimited(int unitId)
    {
        Unit unit = GetUnitDefinition(unitId);
        return unit != null && unit.limitedOwned;
    }

    public int GetOwnedLimit(int unitId)
    {
        Unit unit = GetUnitDefinition(unitId);
        if (unit == null || !unit.limitedOwned)
            return int.MaxValue;

        return Mathf.Max(1, unit.ownedLimit);
    }

    public string GetOwnedLimitLabel(int unitId)
    {
        return IsOwnedLimited(unitId) ? GetOwnedLimit(unitId).ToString() : "Unlimited";
    }

    public bool IsUpgradedUnit(int unitId)
    {
        Unit unit = GetUnitDefinition(unitId);
        return unit != null && unit.isUpgradedUnit;
    }

    public bool ShouldShowInFactoryList(int unitId)
    {
        // Factory list visibility rules:
        // 1) hide units with 0 data
        // 2) hide upgraded units, even if data exists
        // 3) hide limited units when player already reached the owned limit
        if (GetCurrentData(unitId) <= 0)
            return false;

        if (IsUpgradedUnit(unitId))
            return false;

        if (!CanOwnMore(unitId, 1))
            return false;

        return true;
    }

    public int GetRemainingOwnedCapacity(int unitId)
    {
        if (!IsOwnedLimited(unitId))
            return int.MaxValue;

        return Mathf.Max(0, GetOwnedLimit(unitId) - GetOwnedCount(unitId));
    }

    public bool CanOwnMore(int unitId)
    {
        return CanOwnMore(unitId, 1);
    }

    public bool CanOwnMore(int unitId, int quantity)
    {
        quantity = Mathf.Max(1, quantity);

        if (!IsOwnedLimited(unitId))
            return true;

        return GetOwnedCount(unitId) + quantity <= GetOwnedLimit(unitId);
    }

    public bool TryManufacture(int unitId)
    {
        return TryManufacture(unitId, 1);
    }

    public bool TryManufacture(int unitId, int quantity)
    {
        RefreshRuntimeReferences();

        if (progressManager == null)
        {
            Debug.LogError("[FactoryManager] Missing UnitProgressManager reference.");
            OnManufactureFailed?.Invoke(unitId);
            return false;
        }

        quantity = Mathf.Max(1, quantity);
        int totalCost = GetManufactureCost(unitId) * quantity;

        if (!CanOwnMore(unitId, quantity))
        {
            if (debugLog)
                Debug.LogWarning("[FactoryManager] Manufacture failed. Owned limit reached. unitId=" + unitId +
                                 " owned=" + GetOwnedCount(unitId) +
                                 " limit=" + GetOwnedLimitLabel(unitId) +
                                 " requested=" + quantity);

            OnManufactureFailed?.Invoke(unitId);
            return false;
        }

        if (!progressManager.SpendUnitData(unitId, totalCost, false))
        {
            if (debugLog)
                Debug.LogWarning("[FactoryManager] Manufacture failed. Not enough data. unitId=" + unitId +
                                 " required=" + totalCost +
                                 " current=" + progressManager.GetUnitData(unitId));

            OnManufactureFailed?.Invoke(unitId);
            return false;
        }

        progressManager.AddOwnedCount(unitId, quantity, false);
        progressManager.SaveProgress();

        if (debugLog)
            Debug.Log("[FactoryManager] Manufactured unitId=" + unitId +
                      " quantity=" + quantity +
                      " owned=" + progressManager.GetOwnedCount(unitId));

        OnManufactureSuccess?.Invoke(unitId);
        return true;
    }

    public bool TryFakenufacture(int unitId, int quantity)
    {
        RefreshRuntimeReferences();

        progressManager.AddOwnedCount(unitId, quantity, false);
        progressManager.SaveProgress();

        if (debugLog)
            Debug.Log("[FactoryManager] Manufactured unitId=" + unitId +
                      " quantity=" + quantity +
                      " owned=" + progressManager.GetOwnedCount(unitId));

        OnManufactureSuccess?.Invoke(unitId);
        return true;
    }

    public void AddUnitDataDebug(int unitId, int amount)
    {
        RefreshRuntimeReferences();

        if (progressManager == null)
            return;

        progressManager.AddUnitData(unitId, amount, true);
    }
}
