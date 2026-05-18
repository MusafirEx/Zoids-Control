using UnityEngine;

class BattleAreaResultApplier : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    [Header("Game Jolt Global Area")]
    [SerializeField] private bool updateGameJoltGlobalAreaOnWin = true;

    private static bool resultAppliedThisBattle = false;

    private void Awake()
    {
        resultAppliedThisBattle = false;
    }

    public static void ApplyResult(bool playerWon)
    {
        BattleAreaResultApplier applier = FindObjectOfType<BattleAreaResultApplier>();
        if (applier != null)
        {
            applier.Apply(playerWon);
            return;
        }

        if (AreaBattleStateManager.Instance == null)
        {
            Debug.LogWarning("[BattleAreaResultApplier] Missing AreaBattleStateManager. Area result not applied.");
            return;
        }

        if (BattleContextManager.Instance == null || !BattleContextManager.Instance.HasContext)
        {
            Debug.LogWarning("[BattleAreaResultApplier] Missing battle context. Area result not applied.");
            return;
        }

        BattleContextData context = BattleContextManager.Instance.CurrentContext;
        AreaBattleStateManager.Instance.ApplyBattleResult(context, playerWon);

        if (playerWon && ZoidsGameJoltGlobalAreaManager.Instance != null)
            ZoidsGameJoltGlobalAreaManager.Instance.ApplyAreaBattleWinToGlobal(context);
    }

    public void Apply(bool playerWon)
    {
        if (resultAppliedThisBattle)
            return;

        resultAppliedThisBattle = true;

        if (AreaBattleStateManager.Instance == null)
        {
            Debug.LogWarning("[BattleAreaResultApplier] Missing AreaBattleStateManager. Area result not applied.");
            return;
        }

        if (BattleContextManager.Instance == null || !BattleContextManager.Instance.HasContext)
        {
            Debug.LogWarning("[BattleAreaResultApplier] Missing battle context. Area result not applied.");
            return;
        }

        BattleContextData context = BattleContextManager.Instance.CurrentContext;

        AreaBattleStateManager.Instance.ApplyBattleResult(context, playerWon);

        if (playerWon && updateGameJoltGlobalAreaOnWin)
        {
            if (ZoidsGameJoltGlobalAreaManager.Instance != null)
            {
                ZoidsGameJoltGlobalAreaManager.Instance.ApplyAreaBattleWinToGlobal(context);
            }
            else
            {
                Debug.LogWarning("[BattleAreaResultApplier] ZoidsGameJoltGlobalAreaManager missing. Global ownership not updated.");
            }
        }

        if (debugLog)
            Debug.Log("[BattleAreaResultApplier] Applied area result. PlayerWon=" + playerWon);
    }
}
