using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ColosseumRunData
{
    public bool active = false;

    public int selectedTeamIndex = 0;
    public int battleSize = 1;

    public int currentRound = 1;
    public int totalRounds = 5;

    public List<int> fullTeamUnitIds = new List<int>();
    public List<int> currentPlayerUnitIds = new List<int>();
    public List<int> currentEnemyUnitIds = new List<int>();

    public string colosseumSceneName = "ColosseumScene";
    public string loadingSceneName = "LoadingScene";
    public string battleSceneName = "ZoidsBattleScene_JRPGStyle";

    public void Reset()
    {
        active = false;
        selectedTeamIndex = 0;
        battleSize = 1;
        currentRound = 1;
        totalRounds = 5;

        fullTeamUnitIds.Clear();
        currentPlayerUnitIds.Clear();
        currentEnemyUnitIds.Clear();
    }

    public bool IsFinalRound()
    {
        return currentRound >= totalRounds;
    }
}
