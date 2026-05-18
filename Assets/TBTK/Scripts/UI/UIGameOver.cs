using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace TBTK
{
    public class UIGameOver : UIScreen
    {
        [Header("Default TBTK UI")]
        public Text labelMessage;

        public UIButton buttonContinue;
        public UIButton buttonRestart;
        public UIButton buttonMainMenu;

        [Header("Zoids Result Display")]
        [SerializeField] private TMP_Text labelTitle;
        [SerializeField] private TMP_Text labelDetail;
        [SerializeField] private TMP_Text labelReward;
        [SerializeField] private TMP_Text labelContinueButton;

        [Header("Zoids Reward Hook")]
        [SerializeField] private BattleRewardManager rewardManager;
        [SerializeField] private bool applyBattleReward = true;
        [SerializeField] private bool debugRewardLog = true;

        [Header("Colosseum Hook")]
        [SerializeField] private bool enableColosseumFlow = true;
        [SerializeField] private bool continueColosseumOnContinueButton = true;

        [Header("Area Battle Hook")]
        [SerializeField] private bool applyAreaResult = true;

        private bool rewardApplied = false;
        private bool lastPlayerWon = false;
        private bool showingColosseumResult = false;

        private BattleResultData lastRewardResult;
        private int lastPerkCurrencyReward = 0;

        private static UIGameOver instance;

        public override void Awake()
        {
            base.Awake();

            instance = this;
            RefreshRuntimeReferences();
        }

        public override void Start()
        {
            if (labelMessage == null)
            {
                labelMessage = transform.GetChild(1).GetChild(0).GetComponent<Text>();
            }

            buttonContinue.Init();
            buttonContinue.button.onClick.AddListener(delegate { OnContinueButton(); });

            buttonRestart.Init();
            buttonRestart.button.onClick.AddListener(delegate { OnRestartButton(); });

            buttonMainMenu.Init();
            buttonMainMenu.button.onClick.AddListener(delegate { OnMenuButton(); });

            thisObj.SetActive(false);
        }

        private void RefreshRuntimeReferences()
        {
            if (rewardManager == null)
                rewardManager = FindManager<BattleRewardManager>();

            if (rewardManager != null)
                rewardManager.RefreshRuntimeReferences();
        }

        private T FindManager<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
            return Object.FindObjectOfType<T>(true);
#endif
        }

        public void OnContinueButton()
        {
            if (showingColosseumResult && continueColosseumOnContinueButton)
            {
                ContinueColosseum();
                return;
            }

            UIControl.NextLevel();
        }

        public void OnRestartButton()
        {
            UIControl.RestartLevel();
        }

        public void OnMenuButton()
        {
            if (showingColosseumResult && continueColosseumOnContinueButton)
            {
                ContinueColosseum();
                return;
            }

            UIControl.MainMenu();
        }

        public static void Show(bool playerWon)
        {
            if (instance == null) return;
            instance._Show(playerWon);
        }

        public void _Show(bool playerWon)
        {
            lastPlayerWon = playerWon;
            showingColosseumResult = IsColosseumBattle();

            lastRewardResult = ApplyRewardOnce(playerWon);

            if (!showingColosseumResult && applyAreaResult)
                BattleAreaResultApplier.ApplyResult(playerWon);

            RefreshResultDisplay(playerWon);

            base.Show();

            if (showingColosseumResult)
            {
                Debug.Log("[UIGameOver] Colosseum result shown. PlayerWon=" + playerWon +
                          ". Press Continue to proceed.");
            }
        }

        private void RefreshResultDisplay(bool playerWon)
        {
            BattleContextData context = GetContext();

            if (showingColosseumResult)
                RefreshColosseumDisplay(playerWon, context);
            else
                RefreshAreaDisplay(playerWon, context);
        }

        private void RefreshAreaDisplay(bool playerWon, BattleContextData context)
        {
            string areaName = context != null && !string.IsNullOrEmpty(context.areaName)
                ? context.areaName
                : "Unknown Area";

            string battleType = context != null && !string.IsNullOrEmpty(context.battleType)
                ? context.battleType
                : "Area Battle";

            string title = playerWon ? "AREA BATTLE VICTORY" : "AREA BATTLE DEFEAT";

            string detail = "";
            detail += "Area: " + areaName + "\n";
            detail += "Battle Type: " + battleType + "\n";

            if (playerWon)
            {
                detail += "Result: Area captured / ownership updated.";
            }
            else
            {
                detail += "Result: Area owner unchanged.";
            }

            string rewardText = BuildRewardText(lastRewardResult);

            SetResultText(title, detail, rewardText, "Return To Map");

            // Old single-label fallback.
            if (labelMessage != null && labelTitle == null && labelDetail == null)
                labelMessage.text = title + "\n" + detail + "\n" + rewardText;
        }

        private void RefreshColosseumDisplay(bool playerWon, BattleContextData context)
        {
            ColosseumManager manager = FindManager<ColosseumManager>();
            ColosseumRunData run = manager != null ? manager.CurrentRun : null;

            int currentRound = run != null ? run.currentRound : 1;
            int totalRounds = run != null ? run.totalRounds : 1;
            bool finalRound = run != null && run.IsFinalRound();

            string title;

            if (playerWon && finalRound)
                title = "COLOSSEUM COMPLETE";
            else if (playerWon)
                title = "COLOSSEUM ROUND CLEAR";
            else
                title = "COLOSSEUM FAILED";

            string detail = "";

            if (playerWon && finalRound)
            {
                detail += "Rounds Cleared: " + totalRounds + " / " + totalRounds + "\n";
                detail += "Result: All rounds cleared.";
            }
            else if (playerWon)
            {
                detail += "Round Cleared: " + currentRound + " / " + totalRounds + "\n";
                detail += "Next Round: " + Mathf.Min(currentRound + 1, totalRounds) + " / " + totalRounds;
            }
            else
            {
                detail += "Stopped At Round: " + currentRound + " / " + totalRounds + "\n";
                detail += "Result: Colosseum run ended.";
            }

            string rewardText = BuildRewardText(lastRewardResult);

            string continueText = "Next Battle";
            if (!playerWon || finalRound)
                continueText = "Return to Colosseum";

            SetResultText(title, detail, rewardText, continueText);

            // Old single-label fallback.
            if (labelMessage != null && labelTitle == null && labelDetail == null)
                labelMessage.text = title + "\n" + detail + "\n" + rewardText;
        }

        private void SetResultText(string title, string detail, string reward, string continueText)
        {
            if (labelTitle != null)
                labelTitle.text = title;

            if (labelDetail != null)
                labelDetail.text = detail;

            if (labelReward != null)
                labelReward.text = reward;

            if (labelMessage != null)
                labelMessage.text = title;

            if (labelContinueButton != null)
                labelContinueButton.text = continueText;
        }

        private string BuildRewardText(BattleResultData result)
        {
            string text = "Reward:";

            if (result == null || result.rewards == null || result.rewards.Count == 0)
            {
                text += "\nUnit Data: None";
            }
            else
            {
                for (int i = 0; i < result.rewards.Count; i++)
                {
                    BattleRewardUnitData reward = result.rewards[i];

                    string unitName = "Unit " + reward.unitId;
                    Unit unit = UnitDB.GetPrefab(reward.unitId);
                    if (unit != null)
                    {
                        if (!string.IsNullOrEmpty(unit.itemName))
                            unitName = unit.itemName;
                        else
                            unitName = unit.gameObject.name;
                    }

                    text += "\n" + unitName + ": Data +" + reward.dataAmount ;
                }

                text += "\nTotal Data : +" + result.GetTotalRewardData();
            }

            text += "\n\nSkill Point :+" + lastPerkCurrencyReward;

            return text;
        }

        private void ContinueColosseum()
        {
            ColosseumManager manager = FindManager<ColosseumManager>();

            if (manager == null)
            {
                Debug.LogWarning("[UIGameOver] ColosseumManager missing. Cannot continue Colosseum.");
                UIControl.MainMenu();
                return;
            }

            Hide();
            manager.OnColosseumRoundFinished(lastPlayerWon);
        }

        private bool IsColosseumBattle()
        {
            BattleContextData context = GetContext();

            return context != null &&
                   context.battleType == "ColosseumBattle" &&
                   enableColosseumFlow;
        }

        private BattleContextData GetContext()
        {
            if (BattleContextManager.Instance != null &&
                BattleContextManager.Instance.HasContext &&
                BattleContextManager.Instance.CurrentContext != null)
            {
                return BattleContextManager.Instance.CurrentContext;
            }

            return null;
        }

        private BattleResultData ApplyRewardOnce(bool playerWon)
        {
            if (!applyBattleReward) return null;
            if (rewardApplied) return lastRewardResult;

            rewardApplied = true;

            RefreshRuntimeReferences();

            if (rewardManager == null)
            {
                Debug.LogWarning("[UIGameOver] BattleRewardManager missing. Reward not applied.");
                return null;
            }

            List<int> enemyUnitIds = GetEnemyUnitIdsFromContext();

            if (enemyUnitIds == null || enemyUnitIds.Count == 0)
            {
                Debug.LogWarning("[UIGameOver] No enemy unit IDs found in BattleContext. Reward not applied.");
                return null;
            }

            int perkCurrencyBefore = GetCurrentPerkCurrency();

            BattleResultData result = rewardManager.BuildResult(playerWon, enemyUnitIds);
            rewardManager.ApplyResult(result);

            int perkCurrencyAfter = GetCurrentPerkCurrency();
            lastPerkCurrencyReward = Mathf.Max(0, perkCurrencyAfter - perkCurrencyBefore);

            if (debugRewardLog && result != null)
            {
                string summary = playerWon ? "WIN" : "LOSS";
                summary += IsColosseumBattle() ? " Colosseum rewards:" : " Area rewards:";

                for (int i = 0; i < result.rewards.Count; i++)
                {
                    BattleRewardUnitData reward = result.rewards[i];
                    summary += " [unit " + reward.unitId + " +" + reward.dataAmount + "]";
                }

                Debug.Log("[UIGameOver] " + summary + " | total=" + result.GetTotalRewardData() + " | perkCurrency=+" + lastPerkCurrencyReward);
            }

            return result;
        }

        private int GetCurrentPerkCurrency()
        {
            ZoidsPerkProgressManager progressManager = FindManager<ZoidsPerkProgressManager>();
            if (progressManager == null || progressManager.CurrentData == null)
                return 0;

            return progressManager.CurrentData.currency;
        }

        private List<int> GetEnemyUnitIdsFromContext()
        {
            List<int> ids = new List<int>();

            BattleContextData context = GetContext();
            if (context != null && context.enemyUnitIds != null)
                ids.AddRange(context.enemyUnitIds);

            return ids;
        }
    }
}
