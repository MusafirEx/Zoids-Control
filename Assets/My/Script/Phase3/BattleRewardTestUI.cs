using System.Collections.Generic;
using UnityEngine;

public class BattleRewardTestUI : MonoBehaviour
{
    [SerializeField] private BattleRewardManager rewardManager;
    [SerializeField] private UnitProgressManager progressManager;

    [Header("Test Unit IDs")]
    public int testUnitA = 101;
    public int testUnitB = 102;
    public int testUnitC = 103;

    public void SimulateWinOneA()
    {
        ApplyAndPrint(true, new List<int> { testUnitA });
    }

    public void SimulateWinABC()
    {
        ApplyAndPrint(true, new List<int> { testUnitA, testUnitB, testUnitC });
    }

    public void SimulateLossOneA()
    {
        ApplyAndPrint(false, new List<int> { testUnitA });
    }

    public void SimulateLossABC()
    {
        ApplyAndPrint(false, new List<int> { testUnitA, testUnitB, testUnitC });
    }

    public void PrintUnitDataA()
    {
        if (progressManager == null) return;
        Debug.Log("Unit " + testUnitA + " data=" + progressManager.GetUnitData(testUnitA));
    }

    private void ApplyAndPrint(bool playerWon, List<int> defeatedIds)
    {
        if (rewardManager == null) return;

        BattleResultData result = rewardManager.BuildResult(playerWon, defeatedIds);
        rewardManager.ApplyResult(result);

        string summary = playerWon ? "WIN" : "LOSS";
        summary += " rewards:";
        for (int i = 0; i < result.rewards.Count; i++)
        {
            summary += " [unit " + result.rewards[i].unitId + " +" + result.rewards[i].dataAmount + "]";
        }

        Debug.Log(summary + " | total=" + result.GetTotalRewardData());
    }
}
