using UnityEngine;

public class ColosseumRewardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnitProgressManager progressManager;
    [SerializeField] private ZoidsPerkRewardBridge perkRewardBridge;

    [Header("Clear Bonus Rules")]
    [SerializeField] private bool enableClearBonus = true;

    [Tooltip("Final bonus = battleSize x totalRounds x this value.")]
    [SerializeField] private int dataBonusPerBattleSizeRound = 10;

    [Tooltip("If true, bonus goes to a random unit from the player's selected colosseum team.")]
    [SerializeField] private bool rewardRandomPlayerTeamUnit = true;

    [Tooltip("If random player team unit is false, use this unit ID as fallback reward target.")]
    [SerializeField] private int fallbackRewardUnitId = 0;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public ColosseumBonusRewardResult LastBonusResult { get; private set; }

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

    public void RefreshRuntimeReferences()
    {
        if (progressManager == null)
            progressManager = FindManager<UnitProgressManager>();

        if (perkRewardBridge == null)
            perkRewardBridge = FindManager<ZoidsPerkRewardBridge>();

        if (perkRewardBridge == null)
        {
            GameObject obj = new GameObject("ZoidsPerkRewardBridge_AUTO");
            perkRewardBridge = obj.AddComponent<ZoidsPerkRewardBridge>();

            if (debugLog)
                Debug.Log("[ColosseumRewardManager] Created ZoidsPerkRewardBridge_AUTO.");
        }

        if (debugLog && progressManager == null)
            Debug.LogWarning("[ColosseumRewardManager] UnitProgressManager not found.");

        if (debugLog && perkRewardBridge == null)
            Debug.LogWarning("[ColosseumRewardManager] ZoidsPerkRewardBridge not found.");
    }

    private T FindManager<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>(true);
#endif
    }

    public ColosseumBonusRewardResult ApplyClearBonus(ColosseumRunData run)
    {
        LastBonusResult = new ColosseumBonusRewardResult();

        if (!enableClearBonus)
        {
            LastBonusResult.message = "Clear bonus disabled.";
            return LastBonusResult;
        }

        RefreshRuntimeReferences();

        if (progressManager == null)
        {
            LastBonusResult.message = "UnitProgressManager missing. Bonus not applied.";
            Debug.LogWarning("[ColosseumRewardManager] " + LastBonusResult.message);
            return LastBonusResult;
        }

        if (run == null)
        {
            LastBonusResult.message = "ColosseumRunData missing. Bonus not applied.";
            Debug.LogWarning("[ColosseumRewardManager] " + LastBonusResult.message);
            return LastBonusResult;
        }

        int rewardUnitId = SelectRewardUnitId(run);
        if (rewardUnitId < 0)
        {
            LastBonusResult.message = "No valid reward unit ID. Bonus not applied.";
            Debug.LogWarning("[ColosseumRewardManager] " + LastBonusResult.message);
            return LastBonusResult;
        }

        int amount = Mathf.Max(1, run.battleSize * run.totalRounds * dataBonusPerBattleSizeRound);

        progressManager.AddUnitData(rewardUnitId, amount, false);
        progressManager.SaveProgress();

        int perkCurrencyBonus = 0;
        if (perkRewardBridge != null)
            perkCurrencyBonus = perkRewardBridge.AwardColosseumClearBonusCurrency(run);

        LastBonusResult.applied = true;
        LastBonusResult.rewardUnitId = rewardUnitId;
        LastBonusResult.dataAmount = amount;
        LastBonusResult.battleSize = run.battleSize;
        LastBonusResult.totalRounds = run.totalRounds;
        LastBonusResult.perkCurrencyAmount = perkCurrencyBonus;
        LastBonusResult.message = "Colosseum clear bonus applied.";

        if (debugLog)
        {
            Debug.Log("[ColosseumRewardManager] Clear bonus applied. unitId=" + rewardUnitId +
                      " amount=" + amount +
                      " battleSize=" + run.battleSize +
                      " totalRounds=" + run.totalRounds);
        }

        return LastBonusResult;
    }

    private int SelectRewardUnitId(ColosseumRunData run)
    {
        if (rewardRandomPlayerTeamUnit &&
            run.fullTeamUnitIds != null &&
            run.fullTeamUnitIds.Count > 0)
        {
            int index = Random.Range(0, run.fullTeamUnitIds.Count);
            return run.fullTeamUnitIds[index];
        }

        if (fallbackRewardUnitId >= 0)
            return fallbackRewardUnitId;

        return -1;
    }
}

[System.Serializable]
public class ColosseumBonusRewardResult
{
    public bool applied = false;
    public int rewardUnitId = -1;
    public int dataAmount = 0;
    public int battleSize = 0;
    public int totalRounds = 0;
    public int perkCurrencyAmount = 0;
    public string message = "";
}
