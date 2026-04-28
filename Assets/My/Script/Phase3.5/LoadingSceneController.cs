using UnityEngine;

public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private bool loadBattleOnStart = true;

    private void Start()
    {
        if (!loadBattleOnStart)
            return;

        if (BattleContextManager.Instance == null || !BattleContextManager.Instance.HasContext)
        {
            Debug.LogWarning("[LoadingSceneController] No battle context found.");
            return;
        }

        BattleContextManager.Instance.LoadBattleScene();
    }
}
