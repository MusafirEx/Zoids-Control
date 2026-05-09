using System.Collections.Generic;
using UnityEngine;

public class AreaOwnershipTestUI : MonoBehaviour
{
    [Header("Area")]
    [SerializeField] private int areaId = 0;

    [Header("Manual Owner Test")]
    [SerializeField] private int testOwnerFactionId = 0;
    [SerializeField] private string testOwnerFactionName = "Test Faction";
    [SerializeField] private List<int> testDefenderUnitIds = new List<int>();

    [Header("References")]
    [SerializeField] private PlayerProfileManager profileManager;

    private void Reset()
    {
        RefreshReferences();
    }

    private void Awake()
    {
        RefreshReferences();
    }

    private void RefreshReferences()
    {
        if (profileManager == null)
            profileManager = FindFirstObjectByTypeCompat<PlayerProfileManager>();
    }

    public void SetAreaOwnedByManualFaction()
    {
        if (!CheckManager())
            return;

        AreaBattleStateData state = AreaBattleStateManager.Instance.GetAreaState(areaId, true);

        state.ownerFactionId = testOwnerFactionId;
        state.ownerFactionName = testOwnerFactionName;

        state.defenderUnitIds.Clear();
        if (testDefenderUnitIds != null)
            state.defenderUnitIds.AddRange(testDefenderUnitIds);

        AreaBattleStateManager.Instance.Save();

        Debug.Log("[AreaOwnershipTestUI] Area " + areaId +
                  " set owned by " + testOwnerFactionName +
                  " defenderCount=" + state.defenderUnitIds.Count);
    }

    public void SetAreaOwnedByCurrentPlayerFaction()
    {
        if (!CheckManager())
            return;

        RefreshReferences();

        if (profileManager == null || profileManager.CurrentProfile == null)
        {
            Debug.LogWarning("[AreaOwnershipTestUI] Missing PlayerProfileManager or profile.");
            return;
        }

        AreaBattleStateData state = AreaBattleStateManager.Instance.GetAreaState(areaId, true);

        state.ownerFactionId = profileManager.CurrentProfile.chosenFactionId;
        state.ownerFactionName = profileManager.CurrentProfile.chosenFactionName;

        state.defenderUnitIds.Clear();

        if (profileManager.CurrentProfile.activeTeamUnitIds != null)
            state.defenderUnitIds.AddRange(profileManager.CurrentProfile.activeTeamUnitIds);

        AreaBattleStateManager.Instance.Save();

        Debug.Log("[AreaOwnershipTestUI] Area " + areaId +
                  " set owned by current player faction " + state.ownerFactionName +
                  " defenderCount=" + state.defenderUnitIds.Count);
    }

    public void SetAreaNatural()
    {
        if (!CheckManager())
            return;

        AreaBattleStateData state = AreaBattleStateManager.Instance.GetAreaState(areaId, true);

        state.ownerFactionId = -1;
        state.ownerFactionName = "";
        state.defenderUnitIds.Clear();

        AreaBattleStateManager.Instance.Save();

        Debug.Log("[AreaOwnershipTestUI] Area " + areaId + " set back to natural.");
    }

    public void PrintAreaState()
    {
        if (!CheckManager())
            return;

        AreaBattleStateData state = AreaBattleStateManager.Instance.GetAreaState(areaId, false);

        if (state == null)
        {
            Debug.Log("[AreaOwnershipTestUI] Area " + areaId + " has no saved state. Natural/default.");
            return;
        }

        Debug.Log("[AreaOwnershipTestUI] Area=" + areaId +
                  " ownerFactionId=" + state.ownerFactionId +
                  " ownerFactionName=" + state.ownerFactionName +
                  " defenderCount=" + (state.defenderUnitIds != null ? state.defenderUnitIds.Count : 0) +
                  " areaLocked=" + state.IsAreaLocked() +
                  " playerAttemptLocked=" + state.IsPlayerAttemptLocked());
    }

    public void ClearCooldownsOnly()
    {
        if (!CheckManager())
            return;

        AreaBattleStateManager.Instance.ClearAllCooldownsOnly();
    }

    public void ClearAllAreaState()
    {
        if (!CheckManager())
            return;

        AreaBattleStateManager.Instance.ClearAllAreaState();
    }

    private bool CheckManager()
    {
        if (AreaBattleStateManager.Instance == null)
        {
            Debug.LogWarning("[AreaOwnershipTestUI] Missing AreaBattleStateManager.");
            return false;
        }

        return true;
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
