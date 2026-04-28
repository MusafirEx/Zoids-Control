using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DummyAreaDatabase", menuName = "Zoids/Dummy Area Database")]
public class DummyAreaDatabase : ScriptableObject
{
    public List<DummyAreaDefinition> areas = new List<DummyAreaDefinition>();

    public DummyAreaDefinition GetArea(int areaId)
    {
        for (int i = 0; i < areas.Count; i++)
        {
            if (areas[i] != null && areas[i].areaId == areaId)
                return areas[i];
        }

        return null;
    }
}
