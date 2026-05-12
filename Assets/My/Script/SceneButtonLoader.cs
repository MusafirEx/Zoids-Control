using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SceneButtonLoader : MonoBehaviour,IPointerEnterHandler
{
    [Header("Scene Names")]
    public string sceneNames;
    public AudioSource buttonFxSource;
    public AudioClip buttonFxClip;

    /// <summary>
    /// Load scene using array index number.
    /// Example: LoadSceneByArrayNumber(0)
    /// </summary>
    public void LoadScene()
    {
        SceneManager.LoadScene(sceneNames);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        buttonFxSource.PlayOneShot(buttonFxClip);
    }
}