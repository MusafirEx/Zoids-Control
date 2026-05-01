using UnityEngine;

public class BattleEnvironmentSpawner : MonoBehaviour
{
    private GameObject spawnedEnvironment;

    public void Spawn(GameObject environmentPrefab)
    {
        if (spawnedEnvironment != null)
            Destroy(spawnedEnvironment);

        if (environmentPrefab == null)
        {
            Debug.Log("No environment prefab assigned in battle context.");
            return;
        }

        spawnedEnvironment = Instantiate(environmentPrefab, Vector3.zero, Quaternion.identity);
        spawnedEnvironment.name = environmentPrefab.name + "_Runtime";
    }
}
