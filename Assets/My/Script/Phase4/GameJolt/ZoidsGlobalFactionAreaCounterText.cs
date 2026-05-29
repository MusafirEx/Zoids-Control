using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class ZoidsGlobalFactionAreaCounterText : MonoBehaviour
{
    [System.Serializable]
    public class FactionDisplayInfo
    {
        public int factionId = -1;
        public string factionName = "";
        
    }

    [Header("References")]
    [SerializeField] private ZoidsGameJoltGlobalAreaManager globalAreaManager;
    [SerializeField] private TMP_Text targetText;
    public int currentTotalArea = 15;

    [Header("Faction List")]
    [Tooltip("Optional. Add known factions here if you want to show factions with 0 area too.")]
    [SerializeField] private List<FactionDisplayInfo> knownFactions = new List<FactionDisplayInfo>();

    [Header("Display")]
    [SerializeField] private bool showZeroCountFactions = true;
    [SerializeField] private bool showUnownedAreas = true;
    [SerializeField] private string title = "<color=#A17400>AREA OWNERSHIP</color>";
    [SerializeField] private string emptyText = "No global area data loaded.";
    [SerializeField] private string unownedLabel = "Unowned";

    [Header("Auto Refresh")]
    [SerializeField] private bool refreshOnEnable = true;
    [SerializeField] private bool downloadIfNoData = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private void Reset()
    {
        targetText = GetComponent<TMP_Text>();
    }

    private void Awake()
    {
        RefreshReferences();
    }

    private void OnEnable()
    {
        RefreshReferences();

        if (globalAreaManager != null)
            globalAreaManager.OnGlobalDownloadFinished += OnGlobalDownloadFinished;

        if (refreshOnEnable)
            RefreshDisplay();

        if (downloadIfNoData && globalAreaManager != null && globalAreaManager.LastGlobalSave == null)
            globalAreaManager.DownloadGlobalAreas(true);
    }

    private void OnDisable()
    {
        if (globalAreaManager != null)
            globalAreaManager.OnGlobalDownloadFinished -= OnGlobalDownloadFinished;
    }

    private void OnGlobalDownloadFinished(bool success, ZoidsGlobalAreaOwnershipSave save)
    {
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        RefreshReferences();

        if (targetText == null)
        {
            Debug.LogWarning("[ZoidsGlobalFactionAreaCounterText] TMP_Text reference is missing.");
            return;
        }

        if (globalAreaManager == null)
        {
            targetText.text = "Global area manager missing.";
            return;
        }

        ZoidsGlobalAreaOwnershipSave save = globalAreaManager.LastGlobalSave;

        if (save == null || save.areas == null || save.areas.Count == 0)
        {
            targetText.text = emptyText;
            return;
        }

        Dictionary<int, int> countByFactionId = new Dictionary<int, int>();
        Dictionary<int, string> nameByFactionId = new Dictionary<int, string>();

        int unownedCount = 0;

        for (int i = 0; i < save.areas.Count; i++)
        {
            ZoidsGlobalAreaOwnershipData area = save.areas[i];
            if (area == null)
                continue;

            int factionId = area.ownerFactionId;

            if (factionId < 0)
            {
                unownedCount++;
                continue;
            }

            if (!countByFactionId.ContainsKey(factionId))
                countByFactionId.Add(factionId, 0);

            countByFactionId[factionId]++;

            if (!nameByFactionId.ContainsKey(factionId))
                nameByFactionId.Add(factionId, area.ownerFactionName);

            if (!string.IsNullOrEmpty(area.ownerFactionName))
                nameByFactionId[factionId] = area.ownerFactionName;
        }

        // Ensure known factions appear even if they currently own 0 area.
        for (int i = 0; i < knownFactions.Count; i++)
        {
            FactionDisplayInfo faction = knownFactions[i];
            if (faction == null || faction.factionId < 0)
                continue;

            if (!countByFactionId.ContainsKey(faction.factionId))
                countByFactionId.Add(faction.factionId, 0);

            if (!nameByFactionId.ContainsKey(faction.factionId))
                nameByFactionId.Add(faction.factionId, faction.factionName);

            if (!string.IsNullOrEmpty(faction.factionName))
                nameByFactionId[faction.factionId] = faction.factionName;
        }

        List<int> factionIds = new List<int>(countByFactionId.Keys);
        factionIds.Sort();

        StringBuilder sb = new StringBuilder();

        if (!string.IsNullOrEmpty(title))
        {
            sb.AppendLine(title);
            sb.AppendLine();
        }

        for (int i = 0; i < factionIds.Count; i++)
        {
            int factionId = factionIds[i];
            int count = countByFactionId[factionId];

            if (!showZeroCountFactions && count <= 0)
                continue;

            string factionName = nameByFactionId.ContainsKey(factionId) && !string.IsNullOrEmpty(nameByFactionId[factionId])
                ? nameByFactionId[factionId]
                : "Faction " + factionId;

            sb.AppendLine(factionName + ": " + count +"/"+currentTotalArea);
        }

        if (showUnownedAreas && unownedCount > 0)
            sb.AppendLine(unownedLabel + ": " + unownedCount);

        targetText.text = sb.ToString();

        if (debugLog)
            Debug.Log("[ZoidsGlobalFactionAreaCounterText] Refreshed faction area count.");
    }

    private void RefreshReferences()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        if (globalAreaManager == null && ZoidsGameJoltGlobalAreaManager.Instance != null)
            globalAreaManager = ZoidsGameJoltGlobalAreaManager.Instance;

        if (globalAreaManager == null)
            globalAreaManager = FindManager<ZoidsGameJoltGlobalAreaManager>();
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