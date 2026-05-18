using System.Collections.Generic;
using UnityEngine;
using TBTK;

public class PlayerZoidTeamManager : MonoBehaviour
{
    public static PlayerZoidTeamManager Instance { get; private set; }

    [Header("Save")]
    [SerializeField] private string playerPrefsKey = "ZOIDS_PLAYER_TEAMS_V1";

    [Header("Team Rules")]
    [SerializeField] private int teamCount = 3;
    [SerializeField] private int maxUnitsPerTeam = 10;

    [Header("References")]
    [SerializeField] private UnitProgressManager progressManager;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private PlayerZoidTeamSaveData saveData = new PlayerZoidTeamSaveData();

    public int TeamCount { get { return teamCount; } }
    public int MaxUnitsPerTeam { get { return maxUnitsPerTeam; } }

    private void Reset(){ RefreshRuntimeReferences(); }

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
        LoadTeams();
    }

    private void OnEnable(){ RefreshRuntimeReferences(); }

    public void RefreshRuntimeReferences()
    {
        if (progressManager == null && UnitProgressManager.Instance != null)
            progressManager = UnitProgressManager.Instance;

        if (progressManager == null)
            progressManager = FindFirstObjectByTypeCompat<UnitProgressManager>();

        if (debugLog && progressManager == null)
            Debug.LogWarning("[PlayerZoidTeamManager] UnitProgressManager not found during refresh.");
    }

    private T FindFirstObjectByTypeCompat<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>(true);
#endif
    }

    public void LoadTeams()
    {
        if (!PlayerPrefs.HasKey(playerPrefsKey))
        {
            saveData = CreateDefaultTeams();
            SaveTeams();
            return;
        }

        string json = PlayerPrefs.GetString(playerPrefsKey, "");
        if (string.IsNullOrEmpty(json))
        {
            saveData = CreateDefaultTeams();
            SaveTeams();
            return;
        }

        try { saveData = JsonUtility.FromJson<PlayerZoidTeamSaveData>(json); }
        catch { saveData = CreateDefaultTeams(); }

        if (saveData == null)
            saveData = CreateDefaultTeams();

        saveData.EnsureTeamCount(teamCount);
        ValidateAllTeams(false);

        if (debugLog)
            Debug.Log("[PlayerZoidTeamManager] Loaded teams.");
    }

    public void SaveTeams()
    {
        if (saveData == null)
            saveData = CreateDefaultTeams();

        saveData.EnsureTeamCount(teamCount);

        string json = JsonUtility.ToJson(saveData, true);
        PlayerPrefs.SetString(playerPrefsKey, json);
        PlayerPrefs.Save();

        if (debugLog)
            Debug.Log("[PlayerZoidTeamManager] Saved teams.");
    }

    private PlayerZoidTeamSaveData CreateDefaultTeams()
    {
        PlayerZoidTeamSaveData data = new PlayerZoidTeamSaveData();
        data.EnsureTeamCount(teamCount);
        return data;
    }

    public PlayerZoidTeamData GetTeam(int teamIndex)
    {
        if (saveData == null)
            LoadTeams();

        saveData.EnsureTeamCount(teamCount);

        if (teamIndex < 0 || teamIndex >= saveData.teams.Count)
            return null;

        return saveData.teams[teamIndex];
    }

    public List<int> GetTeamUnitIds(int teamIndex)
    {
        PlayerZoidTeamData team = GetTeam(teamIndex);
        if (team == null || team.unitIds == null)
            return new List<int>();

        return new List<int>(team.unitIds);
    }

    public bool IsTeamValidForBattle(int teamIndex, int requiredUnitCount, out string reason)
    {
        reason = "";

        PlayerZoidTeamData team = GetTeam(teamIndex);
        if (team == null)
        {
            reason = "Team does not exist.";
            return false;
        }

        ValidateTeam(teamIndex, false);

        if (team.unitIds.Count <= 0)
        {
            reason = "Team is empty.";
            return false;
        }

        if (team.unitIds.Count < requiredUnitCount)
        {
            reason = "Team has only " + team.unitIds.Count + " Zoids. Required=" + requiredUnitCount;
            return false;
        }

        if (!ValidateTeamOwnership(team.unitIds, out reason))
            return false;

        return true;
    }

    public bool CanAddUnitToTeam(int teamIndex, int unitId, out string reason)
    {
        reason = "";
        RefreshRuntimeReferences();

        PlayerZoidTeamData team = GetTeam(teamIndex);
        if (team == null)
        {
            reason = "Team does not exist.";
            return false;
        }

        if (team.unitIds == null)
            team.unitIds = new List<int>();

        if (team.unitIds.Count >= maxUnitsPerTeam)
        {
            reason = "Team is full. Max=" + maxUnitsPerTeam;
            return false;
        }

        if (progressManager == null)
        {
            reason = "UnitProgressManager missing.";
            return false;
        }

        int owned = progressManager.GetOwnedCount(unitId);
        if (owned <= 0)
        {
            reason = "You do not own this Zoid.";
            return false;
        }

        int alreadyUsed = CountUnitInTeam(team.unitIds, unitId);
        if (alreadyUsed >= owned)
        {
            reason = "Not enough owned copies. Owned=" + owned + " UsedInTeam=" + alreadyUsed;
            return false;
        }

        Unit unit = UnitDB.GetPrefab(unitId);
        if (unit == null)
        {
            reason = "UnitDB missing unit id=" + unitId;
            return false;
        }

        return true;
    }

    public bool AddUnitToTeam(int teamIndex, int unitId)
    {
        string reason;
        if (!CanAddUnitToTeam(teamIndex, unitId, out reason))
        {
            Debug.LogWarning("[PlayerZoidTeamManager] Cannot add unit. " + reason);
            return false;
        }

        PlayerZoidTeamData team = GetTeam(teamIndex);
        team.unitIds.Add(unitId);
        SaveTeams();
        return true;
    }

    public bool RemoveUnitFromTeamAt(int teamIndex, int slotIndex)
    {
        PlayerZoidTeamData team = GetTeam(teamIndex);
        if (team == null || team.unitIds == null)
            return false;

        if (slotIndex < 0 || slotIndex >= team.unitIds.Count)
            return false;

        team.unitIds.RemoveAt(slotIndex);
        SaveTeams();
        return true;
    }

    public void ClearTeam(int teamIndex)
    {
        PlayerZoidTeamData team = GetTeam(teamIndex);
        if (team == null) return;

        team.unitIds.Clear();
        SaveTeams();
    }

    public void RenameTeam(int teamIndex, string newName)
    {
        PlayerZoidTeamData team = GetTeam(teamIndex);
        if (team == null) return;

        team.teamName = string.IsNullOrEmpty(newName) ? "Team " + (teamIndex + 1) : newName;
        SaveTeams();
    }

    public bool ValidateTeam(int teamIndex, bool saveAfterValidation)
    {
        RefreshRuntimeReferences();

        PlayerZoidTeamData team = GetTeam(teamIndex);
        if (team == null || team.unitIds == null)
            return false;

        bool changed = false;

        for (int i = team.unitIds.Count - 1; i >= 0; i--)
        {
            if (UnitDB.GetPrefab(team.unitIds[i]) == null)
            {
                team.unitIds.RemoveAt(i);
                changed = true;
            }
        }

        while (team.unitIds.Count > maxUnitsPerTeam)
        {
            team.unitIds.RemoveAt(team.unitIds.Count - 1);
            changed = true;
        }

        if (progressManager != null)
        {
            Dictionary<int, int> used = new Dictionary<int, int>();

            for (int i = team.unitIds.Count - 1; i >= 0; i--)
            {
                int unitId = team.unitIds[i];

                if (!used.ContainsKey(unitId))
                    used.Add(unitId, 0);

                used[unitId]++;

                int owned = progressManager.GetOwnedCount(unitId);
                if (used[unitId] > owned)
                {
                    team.unitIds.RemoveAt(i);
                    changed = true;
                }
            }
        }

        if (changed && saveAfterValidation)
            SaveTeams();

        return !changed;
    }

    public void ValidateAllTeams(bool saveAfterValidation)
    {
        for (int i = 0; i < teamCount; i++)
            ValidateTeam(i, false);

        if (saveAfterValidation)
            SaveTeams();
    }

    private bool ValidateTeamOwnership(List<int> unitIds, out string reason)
    {
        reason = "";
        RefreshRuntimeReferences();

        if (progressManager == null)
        {
            reason = "UnitProgressManager missing.";
            return false;
        }

        Dictionary<int, int> used = new Dictionary<int, int>();

        for (int i = 0; i < unitIds.Count; i++)
        {
            int unitId = unitIds[i];

            if (!used.ContainsKey(unitId))
                used.Add(unitId, 0);

            used[unitId]++;

            int owned = progressManager.GetOwnedCount(unitId);
            if (used[unitId] > owned)
            {
                reason = "Team uses unit " + unitId + " " + used[unitId] + " times but owns " + owned;
                return false;
            }
        }

        return true;
    }

    private int CountUnitInTeam(List<int> unitIds, int unitId)
    {
        if (unitIds == null) return 0;

        int count = 0;
        for (int i = 0; i < unitIds.Count; i++)
        {
            if (unitIds[i] == unitId)
                count++;
        }

        return count;
    }

    public void ClearAllTeams()
    {
        saveData = CreateDefaultTeams();
        SaveTeams();
    }
}
