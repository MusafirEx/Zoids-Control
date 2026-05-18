using System.Collections.Generic;
using UnityEngine;

public class DummyMapSelectorManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DummyAreaDatabase areaDatabase;
    [SerializeField] private PlayerProfileManager profileManager;
    [SerializeField] private BattleContextManager battleContextManager;
    [SerializeField] private PlayerZoidTeamManager teamManager;
    [SerializeField] private AreaMapGameJoltSyncController mapSyncController;

    [Header("Fallback Player Setup")]
    [SerializeField] private int fallbackPlayerFactionId = 0;
    [SerializeField] private string fallbackPlayerFactionName = "Player";
    [SerializeField] private List<int> fallbackPlayerUnitIds = new List<int>();

    [Header("Scene Names")]
    [SerializeField] private string loadingSceneName = "LoadingScene";
    [SerializeField] private string battleSceneName = "ZoidsBattleScene_JRPGStyle";

    [Header("Area Rules")]
    [Tooltip("Turn this off while testing so cooldown/lock will not block entering battle.")]
    [SerializeField] private bool enforceAreaCooldown = false;

    [Tooltip("If false, player cannot attack an area already owned by their own faction.")]
    [SerializeField] private bool allowAttackOwnFactionArea = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public GameObject NotificationPanel;

    private void Reset()
    {
        RefreshRuntimeReferences();
    }

    private void Awake()
    {
        RefreshRuntimeReferences();
    }

    private void OnEnable()
    {
        RefreshRuntimeReferences();
    }

    public bool TrySelectArea(int areaId)
    {
        RefreshRuntimeReferences();

        if (mapSyncController != null && mapSyncController.IsSyncing)
        {
            Debug.LogWarning("[DummyMapSelectorManager] Cannot select area while map data is syncing.");
            return false;
        }

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

        if (enforceAreaCooldown && AreaBattleStateManager.Instance != null)
        {
            string reason;
            if (!AreaBattleStateManager.Instance.CanAttemptArea(areaId, out reason))
            {
                Debug.LogWarning("[DummyMapSelectorManager] Cannot attempt area. " + reason);
                return false;
            }
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

        FillPlayerContext(context, playerUnits);

        if (!FillEnemyContextFromAreaOwnership(context, area))
            return false;

        battleContextManager.SetLoadingSceneName(loadingSceneName);
        battleContextManager.SetBattleSceneName(battleSceneName);
        battleContextManager.SetContext(context);
        battleContextManager.LoadLoadingScene();

        return true;
    }

    private bool FillEnemyContextFromAreaOwnership(BattleContextData context, DummyAreaDefinition area)
    {
        AreaBattleStateData state = null;

        if (AreaBattleStateManager.Instance != null)
            state = AreaBattleStateManager.Instance.GetAreaState(area.areaId, false);

        bool areaHasOwner =
            state != null &&
            state.ownerFactionId >= 0 &&
            state.defenderUnitIds != null &&
            state.defenderUnitIds.Count > 0;

        if (areaHasOwner)
        {
            if (!allowAttackOwnFactionArea && state.ownerFactionId == context.playerFactionId)
            {
                Debug.LogWarning("[DummyMapSelectorManager] Cannot attack own faction area. Area=" + area.areaName +
                                 " OwnerFaction=" + state.ownerFactionName);
                return false;
            }

            context.isNaturalArea = false;
            context.battleType = "OwnedAreaBattle";

            context.enemyFactionId = state.ownerFactionId;
            context.enemyFactionName = string.IsNullOrEmpty(state.ownerFactionName) ? "Defender" : state.ownerFactionName;
            context.enemyUnitIds = new List<int>(state.defenderUnitIds);

            if (debugLog)
            {
                Debug.Log("[DummyMapSelectorManager] Owned area battle selected. Area=" + area.areaName +
                          " OwnerFaction=" + context.enemyFactionName +
                          " DefenderUnitCount=" + context.enemyUnitIds.Count);
            }

            return true;
        }

        context.isNaturalArea = area.isNaturalArea;
        context.battleType = area.battleType;

        context.enemyFactionId = area.enemyFactionId;
        context.enemyFactionName = area.enemyFactionName;
        context.enemyUnitIds = new List<int>(area.enemyUnitIds);

        if (debugLog)
        {
            Debug.Log("[DummyMapSelectorManager] Natural area battle selected. Area=" + area.areaName +
                      " EnemyFaction=" + context.enemyFactionName +
                      " EnemyUnitCount=" + context.enemyUnitIds.Count);
        }

        return true;
    }

    private void RefreshRuntimeReferences()
    {
        if (profileManager == null && PlayerProfileManager.Instance != null)
            profileManager = PlayerProfileManager.Instance;

        if (battleContextManager == null && BattleContextManager.Instance != null)
            battleContextManager = BattleContextManager.Instance;

        if (teamManager == null && PlayerZoidTeamManager.Instance != null)
            teamManager = PlayerZoidTeamManager.Instance;

        if (mapSyncController == null && AreaMapGameJoltSyncController.Instance != null)
            mapSyncController = AreaMapGameJoltSyncController.Instance;

        if (profileManager == null)
            profileManager = FindFirstObjectByTypeCompat<PlayerProfileManager>();

        if (battleContextManager == null)
            battleContextManager = FindFirstObjectByTypeCompat<BattleContextManager>();

        if (teamManager == null && PlayerZoidTeamManager.Instance != null)
            teamManager = PlayerZoidTeamManager.Instance;

        if (teamManager == null)
            teamManager = FindFirstObjectByTypeCompat<PlayerZoidTeamManager>();

        if (mapSyncController == null)
            mapSyncController = FindFirstObjectByTypeCompat<AreaMapGameJoltSyncController>();

        if (debugLog)
        {
            if (profileManager == null)
                Debug.LogWarning("[DummyMapSelectorManager] PlayerProfileManager not found during refresh.");

            if (battleContextManager == null)
                Debug.LogWarning("[DummyMapSelectorManager] BattleContextManager not found during refresh.");

            if (teamManager == null)
                Debug.LogWarning("[DummyMapSelectorManager] PlayerZoidTeamManager not found during refresh.");
        }
    }

    private T FindFirstObjectByTypeCompat<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>(true);
#endif
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

        // Runtime battle scene faction slot convention:
        // slot 0 = player, slot 1 = enemy.
        context.playerFactionSlotIndex = 0;
        context.enemyFactionSlotIndex = 1;
    }

    private List<int> BuildPlayerTeam()
    {
        // Area Battle team rule:
        // Use Team 1 first.
        // If Team 1 is empty, use Team 2.
        // If Team 2 is empty, use Team 3.
        // If all teams are empty, block battle.
        if (teamManager == null)
            teamManager = FindFirstObjectByTypeCompat<PlayerZoidTeamManager>();

        if (teamManager == null)
        {
            Debug.LogWarning("[DummyMapSelectorManager] PlayerZoidTeamManager missing. Cannot load player battle team.");
            Debug.LogWarning("[DummyMapSelectorManager] All your team is empty cant go to battle");
            return new List<int>();
        }

        for (int teamIndex = 0; teamIndex < 3; teamIndex++)
        {
            List<int> teamUnits = teamManager.GetTeamUnitIds(teamIndex);

            if (teamUnits != null && teamUnits.Count > 0)
            {
                if (debugLog)
                    Debug.Log("[DummyMapSelectorManager] Using Team " + (teamIndex + 1) + " from PlayerZoidTeamManager. UnitCount=" + teamUnits.Count);

                return new List<int>(teamUnits);
            }
        }

        Debug.LogWarning("[DummyMapSelectorManager] All your team is empty cant go to battle");
        NotificationPanel.GetComponent<Animator>().SetTrigger("Show");
        return new List<int>();
    }
}
