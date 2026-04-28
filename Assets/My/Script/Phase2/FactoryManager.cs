using System;
using UnityEngine;

public class FactoryManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnitProgressManager progressManager;
    [SerializeField] private FactoryUnitDatabase factoryDatabase;

    public event Action<int> OnManufactureSuccess;
    public event Action<int> OnManufactureFailed;

    private void Reset()
    {
        progressManager = FindObjectOfType<UnitProgressManager>();
    }

    private void Awake()
    {
        if (progressManager == null)
            progressManager = FindObjectOfType<UnitProgressManager>();
    }

    public int GetManufactureCost(int unitId)
    {
        if (factoryDatabase == null)
            return 100;

        return factoryDatabase.GetManufactureCost(unitId);
    }

    public bool CanManufacture(int unitId)
    {
        if (progressManager == null)
            return false;

        int cost = GetManufactureCost(unitId);
        return progressManager.GetUnitData(unitId) >= cost;
    }

    public int GetCurrentData(int unitId)
    {
        if (progressManager == null)
            return 0;

        return progressManager.GetUnitData(unitId);
    }

    public int GetOwnedCount(int unitId)
    {
        if (progressManager == null)
            return 0;

        return progressManager.GetOwnedCount(unitId);
    }

    public bool TryManufacture(int unitId, int quantity = 1)
    {
        if (progressManager == null)
        {
            Debug.LogError("[FactoryManager] Missing UnitProgressManager reference.");
            OnManufactureFailed?.Invoke(unitId);
            return false;
        }

        quantity = Mathf.Max(1, quantity);

        int costPerUnit = GetManufactureCost(unitId);
        int totalCost = costPerUnit * quantity;

        if (!progressManager.SpendUnitData(unitId, totalCost, false))
        {
            OnManufactureFailed?.Invoke(unitId);
            return false;
        }

        progressManager.AddOwnedCount(unitId, quantity, false);
        progressManager.SaveProgress();

        if (PlayerProfileManager.Instance != null)
            PlayerProfileManager.Instance.SaveProfile();

        OnManufactureSuccess?.Invoke(unitId);
        return true;
    }
}
