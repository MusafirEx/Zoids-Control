using System.Collections.Generic;
using UnityEngine;

public class DummyMapSelectorManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DummyAreaDatabase areaDatabase;
    [SerializeField] private PlayerProfileManager profileManager;
    [SerializeField] private BattleContextManager battleContextManager;

    [Header("Fallback Player Setup")]
    [SerializeField] private int fallbackPlayerFactionId = 0;
    [SerializeField] private string fallbackPlayerFactionName = "Player";
    [SerializeField] private List<int> fallbackPlayerUnitIds = new List<int>();

    [Header("Scene Names")]
    [SerializeField] private string loadingSceneName = "LoadingScene";
    [SerializeField] private string battleSceneName = "ZoidsBattleScene_JRPGStyle";

    private void Reset()
    {
        profileManager = FindObjectOfType<PlayerProfileManager>();
        battleContextManager = FindObjectOfType<BattleContextManager>();
    }

    private void Awake()
    {
        if (profileManager == null)
            profileManager = FindObjectOfType<PlayerProfileManager>();

        if (battleContextManager == null)
            battleContextManager = FindObjectOfType<BattleContextManager>();
    }

    public bool TrySelectArea(int areaId)
    {
        if (areaDatabase == null)
        {
            Debug.LogError("[DummyMapSelectorManager] Missing DummyAreaDatabase reference.");
            return false;
        }

        if (battleContextManager == null)
        {
            Debug.LogError("[DummyMapSelectorManager] Missing BattleContextManager reference.");
            return false;
        }

        DummyAreaDefinition area = areaDatabase.GetArea(areaId);
        if (area == null || !area.IsValid())
        {
            Debug.LogWarning("[DummyMapSelectorManager] Invalid area selection. areaId=" + areaId);
            return false;
        }

        List<int> playerUnits = BuildPlayerTeam();
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogWarning("[DummyMapSelectorManager] Player team is empty.");
            return false;
        }

        BattleContextData context = new BattleContextData();
        context.areaId = area.areaId;
        context.areaName = area.areaName;
        context.isNaturalArea = area.isNaturalArea;
        context.battleType = area.battleType;
        context.environmentPrefab = area.environmentPrefab;

        context.enemyFactionId = area.enemyFactionId;
        context.enemyFactionName = area.enemyFactionName;
        context.enemyUnitIds = new List<int>(area.enemyUnitIds);

        FillPlayerContext(context, playerUnits);

        battleContextManager.SetLoadingSceneName(loadingSceneName);
        battleContextManager.SetBattleSceneName(battleSceneName);
        battleContextManager.SetContext(context);
        battleContextManager.LoadLoadingScene();

        return true;
    }

    private void FillPlayerContext(BattleContextData context, List<int> playerUnits)
    {
        if (profileManager != null && profileManager.CurrentProfile != null && profileManager.CurrentProfile.profileInitialized)
        {
            context.playerFactionId = profileManager.CurrentProfile.chosenFactionId;
            context.playerFactionName = profileManager.CurrentProfile.chosenFactionName;
        }
        else
        {
            context.playerFactionId = fallbackPlayerFactionId;
            context.playerFactionName = fallbackPlayerFactionName;
        }

        context.playerUnitIds = new List<int>(playerUnits);
    }

    private List<int> BuildPlayerTeam()
    {
        if (profileManager != null && profileManager.CurrentProfile != null &&
            profileManager.CurrentProfile.activeTeamUnitIds != null &&
            profileManager.CurrentProfile.activeTeamUnitIds.Count > 0)
        {
            return new List<int>(profileManager.CurrentProfile.activeTeamUnitIds);
        }

        return new List<int>(fallbackPlayerUnitIds);
    }
}
