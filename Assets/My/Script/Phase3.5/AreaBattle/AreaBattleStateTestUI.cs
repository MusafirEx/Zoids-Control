using UnityEngine;

public class AreaBattleStateTestUI : MonoBehaviour
{
    public int testAreaId = 1;

    public void PrintAreaState()
    {
        if (AreaBattleStateManager.Instance == null)
        {
            Debug.Log("AreaBattleStateManager missing.");
            return;
        }

        AreaBattleStateData state = AreaBattleStateManager.Instance.GetAreaState(testAreaId, false);
        if (state == null)
        {
            Debug.Log("Area " + testAreaId + " has no saved state.");
            return;
        }

        Debug.Log("Area " + testAreaId +
                  " OwnerFaction=" + state.ownerFactionName +
                  " AreaLocked=" + state.IsAreaLocked() +
                  " AreaRemain=" + AreaBattleStateManager.Instance.FormatTimeSpan(state.GetAreaLockRemaining()) +
                  " PlayerAttemptLocked=" + state.IsPlayerAttemptLocked() +
                  " PlayerRemain=" + AreaBattleStateManager.Instance.FormatTimeSpan(state.GetPlayerAttemptRemaining()) +
                  " DefenderCount=" + (state.defenderUnitIds != null ? state.defenderUnitIds.Count : 0));
    }

    public void ClearAreaState()
    {
        if (AreaBattleStateManager.Instance == null)
            return;

        AreaBattleStateManager.Instance.ClearAllAreaState();
    }
}
