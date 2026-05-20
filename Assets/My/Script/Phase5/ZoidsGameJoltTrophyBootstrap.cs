using UnityEngine;

public class ZoidsGameJoltTrophyBootstrap : MonoBehaviour
{
    [SerializeField] private bool createTrophyManagerIfMissing = true;

    private void Awake()
    {
        if (!createTrophyManagerIfMissing)
            return;

        if (ZoidsGameJoltTrophyManager.Instance != null)
            return;

        ZoidsGameJoltTrophyManager found = FindManager<ZoidsGameJoltTrophyManager>();
        if (found != null)
            return;

        GameObject obj = new GameObject("ZoidsGameJoltTrophyManager_AUTO");
        obj.AddComponent<ZoidsGameJoltTrophyManager>();
    }

    private T FindManager<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>(true);
#endif
    }
}
