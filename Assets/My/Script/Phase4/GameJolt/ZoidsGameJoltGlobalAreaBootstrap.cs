using UnityEngine;

public class ZoidsGameJoltGlobalAreaBootstrap : MonoBehaviour
{
    [SerializeField] private bool createGlobalAreaManagerIfMissing = true;
    [SerializeField] private bool downloadGlobalAreaOnStart = true;

    private void Awake()
    {
        if (createGlobalAreaManagerIfMissing && ZoidsGameJoltGlobalAreaManager.Instance == null)
        {
            ZoidsGameJoltGlobalAreaManager found = FindManager<ZoidsGameJoltGlobalAreaManager>();
            if (found == null)
            {
                GameObject obj = new GameObject("ZoidsGameJoltGlobalAreaManager_AUTO");
                obj.AddComponent<ZoidsGameJoltGlobalAreaManager>();
            }
        }
    }

    private void Start()
    {
        if (downloadGlobalAreaOnStart && ZoidsGameJoltGlobalAreaManager.Instance != null)
            ZoidsGameJoltGlobalAreaManager.Instance.DownloadGlobalAreas(true);
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
