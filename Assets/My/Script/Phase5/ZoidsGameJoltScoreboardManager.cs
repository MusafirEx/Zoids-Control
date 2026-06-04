using System;
using System.Collections.Generic;
using UnityEngine;
using GameJolt.API;
using GameJolt.API.Objects;
using TBTK;

public enum ZoidsScoreboardCategory
{
    AreaBattle = 0,
    VS1 = 1,
    VS2 = 2,
    VS3 = 3,
    VS4 = 4,
    VS5 = 5,
    VS6 = 6,
    VS7 = 7,
    VS8 = 8,
    VS9 = 9,
    VS10 = 10,
    AllVS = 99
}

public class ZoidsGameJoltScoreboardManager : MonoBehaviour
{
    public static ZoidsGameJoltScoreboardManager Instance { get; private set; }

    [Serializable]
    public class VSScoreboardTable
    {
        [Range(1, 10)] public int vsSize = 1;
        public int tableId = 0;
    }

    [Header("Game Jolt Scoreboard Tables")]
    [Tooltip("Area Battle Score Board. Add +1 total win when player wins an area battle.")]
    [SerializeField] private int areaBattleTableId = 0;

    [Tooltip("All VS scoreboard. Add +1 total win for every VS category win.")]
    [SerializeField] private int allVsTableId = 0;

    [Tooltip("Standalone scoreboards for 1vs1 until 10vs10.")]
    [SerializeField] private List<VSScoreboardTable> vsTables = new List<VSScoreboardTable>()
    {
        new VSScoreboardTable(){ vsSize = 1, tableId = 0 },
        new VSScoreboardTable(){ vsSize = 2, tableId = 0 },
        new VSScoreboardTable(){ vsSize = 3, tableId = 0 },
        new VSScoreboardTable(){ vsSize = 4, tableId = 0 },
        new VSScoreboardTable(){ vsSize = 5, tableId = 0 },
        new VSScoreboardTable(){ vsSize = 6, tableId = 0 },
        new VSScoreboardTable(){ vsSize = 7, tableId = 0 },
        new VSScoreboardTable(){ vsSize = 8, tableId = 0 },
        new VSScoreboardTable(){ vsSize = 9, tableId = 0 },
        new VSScoreboardTable(){ vsSize = 10, tableId = 0 },
    };

    [Header("PlayerPrefs")]
    [SerializeField] private string scoreboardProgressKey = "ZOIDS_GAMEJOLT_SCOREBOARD_PROGRESS_V1";

    [Header("References")]
    [SerializeField] private ZoidsGameJoltAccountManager accountManager;

    [Header("Rules")]
    [SerializeField] private bool requireGameJoltLogin = true;
    [SerializeField] private bool submitAreaBattleWins = true;
    [SerializeField] private bool submitVsWins = true;
    [SerializeField] private bool submitAllVsWins = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private ZoidsGameJoltScoreboardProgress progress = new ZoidsGameJoltScoreboardProgress();

    public event Action<ZoidsScoreboardCategory, bool> OnScoreSubmitted;
    public event Action<ZoidsGameJoltScoreboardResult> OnRankingDownloaded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshRuntimeReferences();
        LoadProgress();
    }

    public void RefreshRuntimeReferences()
    {
        if (accountManager == null && ZoidsGameJoltAccountManager.Instance != null)
            accountManager = ZoidsGameJoltAccountManager.Instance;

        if (accountManager == null)
            accountManager = FindManager<ZoidsGameJoltAccountManager>();
    }

    private T FindManager<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return UnityEngine.Object.FindObjectOfType<T>(true);
#endif
    }

    // Called by UIGameOver only when playerWon == true.
    // Area Battle is scored immediately. Colosseum VS is NOT scored here because
    // it must only be awarded when the full run is completed on the final round.
    public void ReportBattleWin(BattleContextData context)
    {
        if (context == null)
        {
            Debug.LogWarning("[ZoidsGameJoltScoreboardManager] Cannot report battle win. BattleContextData is null.");
            return;
        }

        RefreshRuntimeReferences();

        if (IsColosseumBattle(context))
        {
            if (debugLog)
                Debug.Log("[ZoidsGameJoltScoreboardManager] Colosseum round win ignored here. Score is awarded only on full Colosseum clear.");
            return;
        }

        if (IsAreaBattle(context))
        {
            if (submitAreaBattleWins)
                SubmitCategoryWin(ZoidsScoreboardCategory.AreaBattle);
            return;
        }

        int vsSize;
        if (TryGetVsSize(context, out vsSize))
        {
            if (submitVsWins)
                SubmitVsWin(vsSize);
            return;
        }

        if (debugLog)
        {
            Debug.Log("[ZoidsGameJoltScoreboardManager] Battle win not submitted. Unknown battle type=" +
                      context.battleType +
                      " playerUnits=" + SafeCount(context.playerUnitIds) +
                      " enemyUnits=" + SafeCount(context.enemyUnitIds));
        }
    }

    // Called by UIGameOver when Colosseum final round is won.
    // One full clear gives +1 to the selected VS category and +1 to All VS.
    public void ReportColosseumClearWin(ColosseumRunData run)
    {
        if (run == null)
        {
            Debug.LogWarning("[ZoidsGameJoltScoreboardManager] Cannot report Colosseum clear. Run data is null.");
            return;
        }

        if (!run.IsFinalRound())
        {
            if (debugLog)
                Debug.Log("[ZoidsGameJoltScoreboardManager] Colosseum round ignored. Not final round. Round=" + run.currentRound + "/" + run.totalRounds);
            return;
        }

        int vsSize = Mathf.Clamp(run.battleSize, 1, 10);

        if (debugLog)
        {
            Debug.Log("[ZoidsGameJoltScoreboardManager] Colosseum clear score. " +
                      vsSize + "vs" + vsSize +
                      " Round=" + run.currentRound + "/" + run.totalRounds);
        }

        if (submitVsWins)
            SubmitVsWin(vsSize);
    }

    // Optional direct call if another Colosseum script wants to report by size only.
    public void ReportColosseumClearWin(int battleSize)
    {
        battleSize = Mathf.Clamp(battleSize, 1, 10);

        if (debugLog)
            Debug.Log("[ZoidsGameJoltScoreboardManager] Colosseum clear score by size. " + battleSize + "vs" + battleSize);

        if (submitVsWins)
            SubmitVsWin(battleSize);
    }

    public void SubmitAreaBattleWin()
    {
        SubmitCategoryWin(ZoidsScoreboardCategory.AreaBattle);
    }

    public void SubmitVsWin(int vsSize)
    {
        vsSize = Mathf.Clamp(vsSize, 1, 10);
        SubmitCategoryWin(GetVSCategory(vsSize));

        if (submitAllVsWins)
            SubmitCategoryWin(ZoidsScoreboardCategory.AllVS);
    }

    public void SubmitCategoryWin(ZoidsScoreboardCategory category)
    {
        int tableId = GetTableId(category);
        SubmitWinToTable(category, tableId, GetCategoryName(category), GetCategoryExtraMode(category));
    }

    private void SubmitWinToTable(ZoidsScoreboardCategory category, int tableId, string scoreLabel, string extraMode)
    {
        if (tableId <= 0)
        {
            if (debugLog)
                Debug.LogWarning("[ZoidsGameJoltScoreboardManager] Table ID is not assigned for " + scoreLabel + ". Score not submitted.");
            OnScoreSubmitted?.Invoke(category, false);
            return;
        }

        if (requireGameJoltLogin)
        {
            RefreshRuntimeReferences();
            if (accountManager == null || !accountManager.IsLoggedIn)
            {
                if (debugLog)
                    Debug.LogWarning("[ZoidsGameJoltScoreboardManager] Cannot submit " + scoreLabel + ". Game Jolt user not logged in.");
                OnScoreSubmitted?.Invoke(category, false);
                return;
            }
        }

        LoadProgress();
        int totalWins = progress.AddWin(tableId, 1);
        SaveProgress();

        string scoreText = totalWins + (totalWins == 1 ? " win" : " wins");
        string extraData = BuildExtraData(extraMode, totalWins);

        if (debugLog)
            Debug.Log("[ZoidsGameJoltScoreboardManager] Submit scoreboard. Table=" + tableId + " Label=" + scoreLabel + " Total=" + totalWins);

        Scores.Add(totalWins, scoreText, tableId, extraData, success =>
        {
            if (debugLog)
                Debug.Log("[ZoidsGameJoltScoreboardManager] Score submit finished. Table=" + tableId + " Total=" + totalWins + " Success=" + success);

            OnScoreSubmitted?.Invoke(category, success);
        });
    }

    public void DownloadRanking(ZoidsScoreboardCategory category, int limit, Action<ZoidsGameJoltScoreboardResult> callback = null)
    {
        RefreshRuntimeReferences();
        LoadProgress();

        int tableId = GetTableId(category);
        ZoidsGameJoltScoreboardResult result = new ZoidsGameJoltScoreboardResult();
        result.category = category;
        result.tableId = tableId;
        result.title = GetCategoryName(category);
        result.localWins = tableId > 0 ? progress.GetWins(tableId) : 0;
        result.success = false;

        if (tableId <= 0)
        {
            result.message = "Scoreboard table ID is not assigned for " + result.title + ".";
            callback?.Invoke(result);
            OnRankingDownloaded?.Invoke(result);
            return;
        }

        if (debugLog)
            Debug.Log("[ZoidsGameJoltScoreboardManager] Download ranking. Category=" + category + " Table=" + tableId + " Limit=" + limit);

        Scores.Get(scores =>
        {
            result.success = scores != null;
            result.message = result.success ? "Ranking loaded." : "Failed to load ranking.";
            result.rows.Clear();

            if (scores != null)
            {
                string currentUsername = accountManager != null ? accountManager.Username : "";

                for (int i = 0; i < scores.Length; i++)
                {
                    Score score = scores[i];
                    ZoidsGameJoltScoreboardRow row = new ZoidsGameJoltScoreboardRow();
                    row.rank = i + 1;
                    row.playerName = score != null ? score.PlayerName : "";
                    row.scoreText = score != null ? score.Text : "";
                    row.value = score != null ? score.Value : 0;
                    row.time = score != null ? score.Time : "";
                    row.extra = score != null ? score.Extra : "";
                    row.isCurrentPlayer = !string.IsNullOrEmpty(currentUsername) &&
                                          !string.IsNullOrEmpty(row.playerName) &&
                                          string.Equals(row.playerName, currentUsername, StringComparison.OrdinalIgnoreCase);
                    result.rows.Add(row);
                }
            }

            // Game Jolt GetRank returns rank for a score value, not specifically the player's row.
            // We use the player's saved local total wins to estimate/show their rank for this table.
            if (result.localWins > 0)
            {
                Scores.GetRank(result.localWins, tableId, rank =>
                {
                    result.localRank = rank;
                    callback?.Invoke(result);
                    OnRankingDownloaded?.Invoke(result);
                });
            }
            else
            {
                result.localRank = 0;
                callback?.Invoke(result);
                OnRankingDownloaded?.Invoke(result);
            }
        }, tableId, Mathf.Max(1, limit), false);
    }

    public int GetLocalWins(ZoidsScoreboardCategory category)
    {
        LoadProgress();
        int tableId = GetTableId(category);
        if (tableId <= 0) return 0;
        return progress.GetWins(tableId);
    }

    public List<ZoidsScoreboardCategory> GetAllCategories()
    {
        List<ZoidsScoreboardCategory> list = new List<ZoidsScoreboardCategory>();
        list.Add(ZoidsScoreboardCategory.AreaBattle);
        for (int i = 1; i <= 10; i++) list.Add(GetVSCategory(i));
        list.Add(ZoidsScoreboardCategory.AllVS);
        return list;
    }

    public ZoidsScoreboardCategory GetVSCategory(int vsSize)
    {
        vsSize = Mathf.Clamp(vsSize, 1, 10);
        return (ZoidsScoreboardCategory)vsSize;
    }

    public int GetTableId(ZoidsScoreboardCategory category)
    {
        if (category == ZoidsScoreboardCategory.AreaBattle)
            return areaBattleTableId;

        if (category == ZoidsScoreboardCategory.AllVS)
            return allVsTableId;

        int vsSize = (int)category;
        if (vsSize >= 1 && vsSize <= 10)
            return GetVsTableId(vsSize);

        return 0;
    }

    public string GetCategoryName(ZoidsScoreboardCategory category)
    {
        if (category == ZoidsScoreboardCategory.AreaBattle) return "Area Battle";
        if (category == ZoidsScoreboardCategory.AllVS) return "All VS";

        int vsSize = (int)category;
        if (vsSize >= 1 && vsSize <= 10) return vsSize + "vs" + vsSize;

        return category.ToString();
    }

    private string GetCategoryExtraMode(ZoidsScoreboardCategory category)
    {
        if (category == ZoidsScoreboardCategory.AreaBattle) return "AreaBattle";
        if (category == ZoidsScoreboardCategory.AllVS) return "AllVS";

        int vsSize = (int)category;
        if (vsSize >= 1 && vsSize <= 10) return vsSize + "vs" + vsSize;

        return category.ToString();
    }

    private string BuildExtraData(string mode, int totalWins)
    {
        string userId = accountManager != null ? accountManager.UserId : "";
        string username = accountManager != null ? accountManager.Username : "";
        return "mode=" + mode + ";totalWins=" + totalWins + ";userId=" + userId + ";username=" + username + ";utc=" + DateTime.UtcNow.ToString("o");
    }

    private int GetVsTableId(int vsSize)
    {
        if (vsTables == null)
            return 0;

        for (int i = 0; i < vsTables.Count; i++)
        {
            if (vsTables[i] != null && vsTables[i].vsSize == vsSize)
                return vsTables[i].tableId;
        }

        return 0;
    }

    private bool IsAreaBattle(BattleContextData context)
    {
        string type = context.battleType ?? "";
        if (type == "AreaBattle") return true;
        if (type.IndexOf("Area", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return context.areaId >= 0;
    }

    private bool IsColosseumBattle(BattleContextData context)
    {
        string type = context.battleType ?? "";
        return type == "ColosseumBattle" || type.IndexOf("Colosseum", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool TryGetVsSize(BattleContextData context, out int vsSize)
    {
        vsSize = 0;

        string type = context.battleType ?? "";
        for (int i = 1; i <= 10; i++)
        {
            string token = i + "vs" + i;
            string tokenAlt = i + "v" + i;

            if (type.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 ||
                type.IndexOf(tokenAlt, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                vsSize = i;
                return true;
            }
        }

        int playerCount = SafeCount(context.playerUnitIds);
        int enemyCount = SafeCount(context.enemyUnitIds);

        if (playerCount >= 1 && playerCount <= 10 && playerCount == enemyCount)
        {
            vsSize = playerCount;
            return true;
        }

        return false;
    }

    private int SafeCount<T>(List<T> list)
    {
        return list != null ? list.Count : 0;
    }

    public void LoadProgress()
    {
        string json = PlayerPrefs.GetString(scoreboardProgressKey, "");
        if (string.IsNullOrEmpty(json))
        {
            progress = new ZoidsGameJoltScoreboardProgress();
            return;
        }

        try
        {
            progress = JsonUtility.FromJson<ZoidsGameJoltScoreboardProgress>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[ZoidsGameJoltScoreboardManager] Failed to parse progress. Resetting. " + ex.Message);
            progress = new ZoidsGameJoltScoreboardProgress();
        }

        if (progress == null)
            progress = new ZoidsGameJoltScoreboardProgress();

        progress.EnsureValid();
    }

    public void SaveProgress()
    {
        if (progress == null)
            progress = new ZoidsGameJoltScoreboardProgress();

        progress.EnsureValid();
        string json = JsonUtility.ToJson(progress, false);
        PlayerPrefs.SetString(scoreboardProgressKey, json);
        PlayerPrefs.Save();
    }

    public string GetProgressJson()
    {
        LoadProgress();
        return JsonUtility.ToJson(progress, false);
    }

    public void ApplyProgressJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return;

        try
        {
            ZoidsGameJoltScoreboardProgress loaded = JsonUtility.FromJson<ZoidsGameJoltScoreboardProgress>(json);
            if (loaded != null)
            {
                progress = loaded;
                SaveProgress();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[ZoidsGameJoltScoreboardManager] Failed to apply progress json. " + ex.Message);
        }
    }

    public void ResetLocalScoreboardProgress()
    {
        progress = new ZoidsGameJoltScoreboardProgress();
        SaveProgress();
    }
}

[Serializable]
public class ZoidsGameJoltScoreboardResult
{
    public ZoidsScoreboardCategory category;
    public int tableId;
    public string title = "";
    public bool success;
    public string message = "";
    public int localWins;
    public int localRank;
    public List<ZoidsGameJoltScoreboardRow> rows = new List<ZoidsGameJoltScoreboardRow>();
}

[Serializable]
public class ZoidsGameJoltScoreboardRow
{
    public int rank;
    public string playerName = "";
    public string scoreText = "";
    public int value;
    public string time = "";
    public string extra = "";
    public bool isCurrentPlayer;
}

[Serializable]
public class ZoidsGameJoltScoreboardProgress
{
    public int version = 1;
    public List<ZoidsGameJoltScoreboardEntry> entries = new List<ZoidsGameJoltScoreboardEntry>();

    public void EnsureValid()
    {
        if (entries == null)
            entries = new List<ZoidsGameJoltScoreboardEntry>();
    }

    public int GetWins(int tableId)
    {
        EnsureValid();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].tableId == tableId)
                return Mathf.Max(0, entries[i].wins);
        }
        return 0;
    }

    public int AddWin(int tableId, int amount)
    {
        EnsureValid();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].tableId == tableId)
            {
                entries[i].wins = Mathf.Max(0, entries[i].wins + amount);
                return entries[i].wins;
            }
        }

        ZoidsGameJoltScoreboardEntry entry = new ZoidsGameJoltScoreboardEntry();
        entry.tableId = tableId;
        entry.wins = Mathf.Max(0, amount);
        entries.Add(entry);
        return entry.wins;
    }
}

[Serializable]
public class ZoidsGameJoltScoreboardEntry
{
    public int tableId = 0;
    public int wins = 0;
}
