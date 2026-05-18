using UnityEngine;

public class PlayerZoidTeamTestUI : MonoBehaviour
{
    [SerializeField] private PlayerZoidTeamManager teamManager;

    public int teamIndex = 0;
    public int testUnitId = 0;
    public int requiredBattleSize = 1;

    private void Reset()
    {
        teamManager = FindFirstObjectByTypeCompat<PlayerZoidTeamManager>();
    }

    private void Awake()
    {
        if (teamManager == null)
            teamManager = FindFirstObjectByTypeCompat<PlayerZoidTeamManager>();
    }

    public void AddTestUnit()
    {
        if (teamManager == null) return;
        teamManager.AddUnitToTeam(teamIndex, testUnitId);
    }

    public void RemoveFirstUnit()
    {
        if (teamManager == null) return;
        teamManager.RemoveUnitFromTeamAt(teamIndex, 0);
    }

    public void ClearTeam()
    {
        if (teamManager == null) return;
        teamManager.ClearTeam(teamIndex);
    }

    public void PrintTeam()
    {
        if (teamManager == null) return;

        PlayerZoidTeamData team = teamManager.GetTeam(teamIndex);
        if (team == null)
        {
            Debug.Log("Team not found.");
            return;
        }

        string msg = team.teamName + " count=" + team.Count + " units=";
        for (int i = 0; i < team.unitIds.Count; i++)
            msg += team.unitIds[i] + (i < team.unitIds.Count - 1 ? "," : "");

        Debug.Log(msg);
    }

    public void CheckBattleSize()
    {
        if (teamManager == null) return;

        string reason;
        bool valid = teamManager.IsTeamValidForBattle(teamIndex, requiredBattleSize, out reason);
        Debug.Log("Team valid for " + requiredBattleSize + "v" + requiredBattleSize + " = " + valid + " reason=" + reason);
    }

    public void ClearAllTeams()
    {
        if (teamManager == null) return;
        teamManager.ClearAllTeams();
    }

    private T FindFirstObjectByTypeCompat<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
