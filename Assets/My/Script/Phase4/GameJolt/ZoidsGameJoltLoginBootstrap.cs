using UnityEngine;

public class ZoidsGameJoltLoginBootstrap : MonoBehaviour
{
    [SerializeField] private bool createAccountManagerIfMissing = true;
    [SerializeField] private bool createCloudSaveManagerIfMissing = true;
    [SerializeField] private bool createProfileBridgeIfMissing = true;

    private void Awake()
    {
        if (createAccountManagerIfMissing && ZoidsGameJoltAccountManager.Instance == null)
        {
            ZoidsGameJoltAccountManager found = FindManager<ZoidsGameJoltAccountManager>();
            if (found == null)
            {
                GameObject obj = new GameObject("ZoidsGameJoltAccountManager_AUTO");
                obj.AddComponent<ZoidsGameJoltAccountManager>();
            }
        }

        if (createCloudSaveManagerIfMissing && ZoidsGameJoltCloudSaveManager.Instance == null)
        {
            ZoidsGameJoltCloudSaveManager found = FindManager<ZoidsGameJoltCloudSaveManager>();
            if (found == null)
            {
                GameObject obj = new GameObject("ZoidsGameJoltCloudSaveManager_AUTO");
                obj.AddComponent<ZoidsGameJoltCloudSaveManager>();
            }
        }

        if (createProfileBridgeIfMissing && ZoidsGameJoltProfileBridge.Instance == null)
        {
            ZoidsGameJoltProfileBridge found = FindManager<ZoidsGameJoltProfileBridge>();
            if (found == null)
            {
                GameObject obj = new GameObject("ZoidsGameJoltProfileBridge_AUTO");
                obj.AddComponent<ZoidsGameJoltProfileBridge>();
            }
        }
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
