using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuPerkSceneButton : MonoBehaviour
{
    [SerializeField] private string perkSceneName = "PerkScene";

    public void OpenPerkScene()
    {
        if (string.IsNullOrEmpty(perkSceneName))
        {
            Debug.LogWarning("[MainMenuPerkSceneButton] Perk scene name is empty.");
            return;
        }

        SceneManager.LoadScene(perkSceneName);
    }
}
