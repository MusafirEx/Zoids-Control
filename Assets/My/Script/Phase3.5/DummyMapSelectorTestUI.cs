using UnityEngine;

public class DummyMapSelectorTestUI : MonoBehaviour
{
    [SerializeField] private DummyMapSelectorManager selectorManager;

    public void SelectArea(int areaId)
    {
        if (selectorManager == null)
            return;

        bool success = selectorManager.TrySelectArea(areaId);
        Debug.Log("Select area " + areaId + " result=" + success);
    }

    public void SelectArea0()
    {
        SelectArea(0);
    }

    public void SelectArea1()
    {
        SelectArea(1);
    }

    public void SelectArea2()
    {
        SelectArea(2);
    }
}
