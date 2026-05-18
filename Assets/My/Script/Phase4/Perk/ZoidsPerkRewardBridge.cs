using UnityEngine;
using TBTK;

public class ZoidsPerkRewardBridge : MonoBehaviour
{
    public static ZoidsPerkRewardBridge Instance { get; private set; }

    [Header("References")]
    [SerializeField] private ZoidsPerkProgressManager perkProgressManager;

    [Header("Area Battle Perk Currency")]
    [SerializeField] private bool enableAreaBattlePerkReward = true;
    [SerializeField] private int areaWinCurrency = 1;
    [SerializeField] private int areaLoseCurrency = 0;

    [Header("Colosseum Perk Currency")]
    [SerializeField] private bool enableColosseumRoundPerkReward = true;
    [SerializeField] private int colosseumRoundWinCurrency = 1;
    [SerializeField] private int colosseumRoundLoseCurrency = 0;

    [Header("Colosseum Clear Bonus Perk Currency")]
    [SerializeField] private bool enableColosseumClearBonusCurrency = true;
    [SerializeField] private int colosseumClearBonusCurrency = 3;
    [SerializeField] private bool scaleClearBonusByBattleSize = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshRuntimeReferences();
    }

    private void OnEnable()
    {
        RefreshRuntimeReferences();
    }

    public void RefreshRuntimeReferences()
    {
        if (perkProgressManager == null && ZoidsPerkProgressManager.Instance != null)
            perkProgressManager = ZoidsPerkProgressManager.Instance;

        if (perkProgressManager == null)
            perkProgressManager = FindManager<ZoidsPerkProgressManager>();

        if (perkProgressManager == null)
        {
            GameObject obj = new GameObject("ZoidsPerkProgressManager_AUTO");
            perkProgressManager = obj.AddComponent<ZoidsPerkProgressManager>();

            if (debugLog)
                Debug.Log("[ZoidsPerkRewardBridge] Created ZoidsPerkProgressManager_AUTO.");
        }
    }

    private T FindManager<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>(true);
#endif
    }

    public int AwardBattlePerkCurrency(bool playerWon, string battleType)
    {
        bool isColosseum = battleType == "ColosseumBattle";

        if (isColosseum)
            return AwardColosseumRoundCurrency(playerWon);

        return AwardAreaBattleCurrency(playerWon);
    }

    public int AwardAreaBattleCurrency(bool playerWon)
    {
        if (!enableAreaBattlePerkReward)
            return 0;

        int amount = playerWon ? areaWinCurrency : areaLoseCurrency;
        return AddCurrency(amount, playerWon ? "Area battle win" : "Area battle loss");
    }

    public int AwardColosseumRoundCurrency(bool playerWon)
    {
        if (!enableColosseumRoundPerkReward)
            return 0;

        int amount = playerWon ? colosseumRoundWinCurrency : colosseumRoundLoseCurrency;
        return AddCurrency(amount, playerWon ? "Colosseum round win" : "Colosseum round loss");
    }

    public int AwardColosseumClearBonusCurrency(ColosseumRunData run)
    {
        if (!enableColosseumClearBonusCurrency)
            return 0;

        int amount = colosseumClearBonusCurrency;

        if (scaleClearBonusByBattleSize && run != null)
            amount *= Mathf.Max(1, run.battleSize);

        return AddCurrency(amount, "Colosseum clear bonus");
    }

    public int AddCurrency(int amount, string reason)
    {
        if (amount <= 0)
            return 0;

        RefreshRuntimeReferences();

        if (perkProgressManager == null)
        {
            Debug.LogWarning("[ZoidsPerkRewardBridge] Cannot add perk currency. ZoidsPerkProgressManager missing.");
            return 0;
        }

        perkProgressManager.AddCurrency(amount, reason);

        if (debugLog)
            Debug.Log("[ZoidsPerkRewardBridge] +" + amount + " perk currency. Reason=" + reason);

        return amount;
    }
}
