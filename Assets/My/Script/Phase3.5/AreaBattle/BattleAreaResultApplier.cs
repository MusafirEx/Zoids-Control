using UnityEngine;

public class BattleAreaResultApplier : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

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

        AreaBattleStateManager.Instance.ApplyBattleResult(BattleContextManager.Instance.CurrentContext, playerWon);
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

        AreaBattleStateManager.Instance.ApplyBattleResult(BattleContextManager.Instance.CurrentContext, playerWon);

        if (debugLog)
            Debug.Log("[BattleAreaResultApplier] Applied area result. PlayerWon=" + playerWon);
    }
}
