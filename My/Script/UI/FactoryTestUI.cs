using UnityEngine;

public class FactoryTestUI : MonoBehaviour
{
    [SerializeField] private UnitProgressManager progressManager;
    [SerializeField] private FactoryManager factoryManager;

    public int testUnitId = 101;

    public void Add25Data()
    {
        AddUnitDataInternal(testUnitId, 25);
    }

    public void Add50Data()
    {
        AddUnitDataInternal(testUnitId, 50);
    }

    public void Add100Data()
    {
        AddUnitDataInternal(testUnitId, 100);
    }

    public void ManufactureOne()
    {
        if (factoryManager == null) return;

        bool success = factoryManager.TryManufacture(testUnitId, 1);
        Debug.Log("Manufacture unit " + testUnitId + " success=" + success +
                  " | data=" + factoryManager.GetCurrentData(testUnitId) +
                  " | owned=" + factoryManager.GetOwnedCount(testUnitId));
    }

    public void PrintUnitState()
    {
        if (progressManager == null) return;

        Debug.Log("Unit " + testUnitId +
                  " | data=" + progressManager.GetUnitData(testUnitId) +
                  " | owned=" + progressManager.GetOwnedCount(testUnitId));
    }

    public void ClearUnitProgress()
    {
        if (progressManager == null) return;

        progressManager.ClearProgress();
        Debug.Log("Unit progress cleared");
    }

    private void AddUnitDataInternal(int unitId, int amount)
    {
        if (progressManager == null) return;

        progressManager.AddUnitData(unitId, amount);
        Debug.Log("Added data to unit " + unitId + " amount=" + amount +
                  " | currentData=" + progressManager.GetUnitData(unitId));
    }
}