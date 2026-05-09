using UnityEngine;
using System.Collections;

public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private bool loadBattleOnStart = true;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(5);
        if (!loadBattleOnStart)
            yield break;

        if (BattleContextManager.Instance == null || !BattleContextManager.Instance.HasContext)
        {
            Debug.LogWarning("[LoadingSceneController] No battle context found.");
            yield break ;
        }
        yield return new WaitForSeconds(3);
        BattleContextManager.Instance.LoadBattleScene();
    }
}
