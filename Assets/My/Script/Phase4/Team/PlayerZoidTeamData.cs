using System;
using System.Collections.Generic;

[Serializable]
public class PlayerZoidTeamData
{
    public string teamName = "Team";
    public List<int> unitIds = new List<int>();

    public PlayerZoidTeamData() { }

    public PlayerZoidTeamData(string teamName)
    {
        this.teamName = teamName;
        unitIds = new List<int>();
    }

    public int Count
    {
        get { return unitIds != null ? unitIds.Count : 0; }
    }

    public bool IsEmpty()
    {
        return unitIds == null || unitIds.Count == 0;
    }
}
