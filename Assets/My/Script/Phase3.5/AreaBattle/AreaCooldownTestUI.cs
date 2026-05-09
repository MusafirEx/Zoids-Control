using UnityEngine;

public class AreaCooldownTestUI : MonoBehaviour
{
    [SerializeField] private int areaId = 0;

    public void ClearAreaCooldown()
    {
        if (AreaBattleStateManager.Instance == null) return;
        AreaBattleStateManager.Instance.ClearAreaCooldown(areaId);
    }

    public void ClearPlayerAttemptCooldown()
    {
        if (AreaBattleStateManager.Instance == null) return;
        AreaBattleStateManager.Instance.ClearPlayerAttemptCooldown(areaId);
    }

    public void ClearGlobalPlayerAttemptCooldown()
    {
        if (AreaBattleStateManager.Instance == null) return;
        AreaBattleStateManager.Instance.ClearGlobalPlayerAttemptCooldown();
    }

    public void ClearAllCooldownsForArea()
    {
        if (AreaBattleStateManager.Instance == null) return;
        AreaBattleStateManager.Instance.ClearAllCooldownsForArea(areaId);
    }

    public void ClearAllCooldownsOnly()
    {
        if (AreaBattleStateManager.Instance == null) return;
        AreaBattleStateManager.Instance.ClearAllCooldownsOnly();
    }

    public void ClearAllAreaState()
    {
        if (AreaBattleStateManager.Instance == null) return;
        AreaBattleStateManager.Instance.ClearAllAreaState();
    }

    public void PrintCanAttempt()
    {
        if (AreaBattleStateManager.Instance == null)
        {
            Debug.Log("[AreaCooldownTestUI] AreaBattleStateManager missing.");
            return;
        }

        string reason;
        bool canAttempt = AreaBattleStateManager.Instance.CanAttemptArea(areaId, out reason);
        Debug.Log("[AreaCooldownTestUI] Area=" + areaId + " CanAttempt=" + canAttempt + " Reason=" + reason);
    }
}
