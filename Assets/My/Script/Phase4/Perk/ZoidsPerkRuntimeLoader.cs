using System.Collections;
using UnityEngine;
using TBTK;

public class ZoidsPerkRuntimeLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ZoidsPerkProgressManager progressManager;
    [SerializeField] private PerkManager perkManager;

    [Header("Options")]
    [SerializeField] private bool createProgressManagerIfMissing = true;
    [SerializeField] private bool createPerkManagerIfMissing = false;

    [Tooltip("Set true for battle scene so Perk.Activate() applies unit/ability/stat modifiers.")]
    [SerializeField] private bool inGameScene = false;

    [SerializeField] private bool loadProgressOnStart = true;
    [SerializeField] private bool applyProgressOnStart = true;

    [Tooltip("Extra frames to wait before applying progress. Useful when PerkManager is created by another object.")]
    [SerializeField] private int delayFrames = 1;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private IEnumerator Start()
    {
        RefreshRuntimeReferences();

        if (progressManager == null && createProgressManagerIfMissing)
        {
            GameObject obj = new GameObject("ZoidsPerkProgressManager_AUTO");
            progressManager = obj.AddComponent<ZoidsPerkProgressManager>();

            if (debugLog)
                Debug.Log("[ZoidsPerkRuntimeLoader] Created ZoidsPerkProgressManager_AUTO.");
        }

        if (perkManager == null && createPerkManagerIfMissing)
        {
            GameObject obj = new GameObject("PerkManager_AUTO");
            perkManager = obj.AddComponent<PerkManager>();
            perkManager.inGameScene = inGameScene;
            perkManager.loadProgressFromCache = true;
            perkManager.saveProgressToCache = true;

            if (debugLog)
                Debug.Log("[ZoidsPerkRuntimeLoader] Created PerkManager_AUTO.");
        }

        for (int i = 0; i < Mathf.Max(0, delayFrames); i++)
            yield return null;

        RefreshRuntimeReferences();

        if (perkManager != null)
            perkManager.inGameScene = inGameScene;

        if (progressManager != null && loadProgressOnStart)
            progressManager.LoadProgress();

        if (progressManager != null && applyProgressOnStart)
            progressManager.ApplyProgressToPerkManager();

        if (debugLog)
        {
            Debug.Log("[ZoidsPerkRuntimeLoader] Runtime perk load complete. " +
                      "progressManager=" + (progressManager != null) +
                      " perkManager=" + (perkManager != null) +
                      " inGameScene=" + inGameScene +
                      " perkSystemEnabled=" + PerkManager.PerkSystemEnabled());
        }
    }

    private void OnEnable()
    {
        RefreshRuntimeReferences();
    }

    public void RefreshRuntimeReferences()
    {
        if (progressManager == null && ZoidsPerkProgressManager.Instance != null)
            progressManager = ZoidsPerkProgressManager.Instance;

        if (progressManager == null)
            progressManager = FindManager<ZoidsPerkProgressManager>();

        if (perkManager == null)
            perkManager = FindManager<PerkManager>();
    }

    public void ReloadAndApplyNow()
    {
        RefreshRuntimeReferences();

        if (progressManager == null)
        {
            Debug.LogWarning("[ZoidsPerkRuntimeLoader] Cannot reload perks. ZoidsPerkProgressManager missing.");
            return;
        }

        if (perkManager == null)
        {
            Debug.LogWarning("[ZoidsPerkRuntimeLoader] Cannot reload perks. PerkManager missing.");
            return;
        }

        perkManager.inGameScene = inGameScene;

        progressManager.LoadProgress();
        progressManager.ApplyProgressToPerkManager();

        if (debugLog)
            Debug.Log("[ZoidsPerkRuntimeLoader] Reloaded and applied perk progress manually.");
    }

    public bool IsPerkUnlocked(int perkId)
    {
        if (PerkManager.PerkSystemEnabled())
            return PerkManager.IsPerkUnlocked(perkId);

        if (progressManager != null)
            return progressManager.IsPerkUnlocked(perkId);

        return false;
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
