using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace TBTK
{

    public class UIGameOver : UIScreen
    {

        public Text labelMessage;

        public UIButton buttonContinue;
        public UIButton buttonRestart;
        public UIButton buttonMainMenu;

        [Header("Zoids Reward Hook")]
        [SerializeField] private BattleRewardManager rewardManager;
        [SerializeField] private bool applyBattleReward = true;
        [SerializeField] private bool debugRewardLog = true;

        private bool rewardApplied = false;

        private static UIGameOver instance;

        public override void Awake()
        {
            base.Awake();

            instance = this;

            if (rewardManager == null)
                rewardManager = FindObjectOfType<BattleRewardManager>();
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


        public void OnContinueButton()
        {
            UIControl.NextLevel();
        }
        public void OnRestartButton()
        {
            UIControl.RestartLevel();
        }
        public void OnMenuButton()
        {
            UIControl.MainMenu();
        }


        public static void Show(bool playerWon)
        {
            if (instance == null) return;
            instance._Show(playerWon);
        }

        public void _Show(bool playerWon)
        {
            ApplyRewardOnce(playerWon);
            BattleAreaResultApplier.ApplyResult(playerWon);

            if (labelMessage != null && playerWon) labelMessage.text = "You Have Won!";
            else if (labelMessage != null && !playerWon) labelMessage.text = "You Have Lost!";

            base.Show();
        }

        private void ApplyRewardOnce(bool playerWon)
        {
            if (!applyBattleReward) return;
            if (rewardApplied) return;

            rewardApplied = true;

            if (rewardManager == null)
                rewardManager = FindObjectOfType<BattleRewardManager>();

            if (rewardManager == null)
            {
                Debug.LogWarning("[UIGameOver] BattleRewardManager missing. Reward not applied.");
                return;
            }

            List<int> enemyUnitIds = GetEnemyUnitIdsFromContext();

            if (enemyUnitIds == null || enemyUnitIds.Count == 0)
            {
                Debug.LogWarning("[UIGameOver] No enemy unit IDs found in BattleContext. Reward not applied.");
                return;
            }

            BattleResultData result = rewardManager.BuildResult(playerWon, enemyUnitIds);
            rewardManager.ApplyResult(result);

            if (debugRewardLog && result != null)
            {
                string summary = playerWon ? "WIN" : "LOSS";
                summary += " rewards:";

                for (int i = 0; i < result.rewards.Count; i++)
                {
                    BattleRewardUnitData reward = result.rewards[i];
                    summary += " [unit " + reward.unitId + " +" + reward.dataAmount + "]";
                }

                Debug.Log("[UIGameOver] " + summary + " | total=" + result.GetTotalRewardData());
            }
        }

        private List<int> GetEnemyUnitIdsFromContext()
        {
            List<int> ids = new List<int>();

            if (BattleContextManager.Instance != null &&
               BattleContextManager.Instance.HasContext &&
               BattleContextManager.Instance.CurrentContext != null &&
               BattleContextManager.Instance.CurrentContext.enemyUnitIds != null)
            {

                ids.AddRange(BattleContextManager.Instance.CurrentContext.enemyUnitIds);
            }

            return ids;
        }
    }
}