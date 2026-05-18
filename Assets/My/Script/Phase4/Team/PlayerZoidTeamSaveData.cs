using System;
using System.Collections.Generic;

[Serializable]
public class PlayerZoidTeamSaveData
{
    public List<PlayerZoidTeamData> teams = new List<PlayerZoidTeamData>();

    public void EnsureTeamCount(int count)
    {
        if (teams == null)
            teams = new List<PlayerZoidTeamData>();

        while (teams.Count < count)
            teams.Add(new PlayerZoidTeamData("Team " + (teams.Count + 1)));

        while (teams.Count > count)
            teams.RemoveAt(teams.Count - 1);

        for (int i = 0; i < teams.Count; i++)
        {
            if (teams[i] == null)
                teams[i] = new PlayerZoidTeamData("Team " + (i + 1));

            if (string.IsNullOrEmpty(teams[i].teamName))
                teams[i].teamName = "Team " + (i + 1);

            if (teams[i].unitIds == null)
                teams[i].unitIds = new List<int>();
        }
    }
}
