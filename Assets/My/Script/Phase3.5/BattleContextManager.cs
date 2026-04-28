using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleContextManager : MonoBehaviour
{
    public static BattleContextManager Instance { get; private set; }

    [SerializeField] private string loadingSceneName = "LoadingScene";
    [SerializeField] private string battleSceneName = "ZoidsBattleScene_JRPGStyle";

    public BattleContextData CurrentContext { get; private set; }

    public bool HasContext => CurrentContext != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetContext(BattleContextData context)
    {
        CurrentContext = context;
        Debug.Log("[BattleContextManager] Context set. Area=" + context.areaName + " | battleType=" + context.battleType);
    }

    public void ClearContext()
    {
        CurrentContext = null;
    }

    public void LoadLoadingScene()
    {
        SceneManager.LoadScene(loadingSceneName);
    }

    public void LoadBattleScene()
    {
        SceneManager.LoadScene(battleSceneName);
    }

    public void SetLoadingSceneName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            loadingSceneName = sceneName;
    }

    public void SetBattleSceneName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            battleSceneName = sceneName;
    }
}
