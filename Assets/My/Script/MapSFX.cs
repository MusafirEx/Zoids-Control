using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapSFX : MonoBehaviour/*, IPointerEnterHandler*/
{
    public static MapSFX Instance { get; private set; }

    public AudioSource Bgm;
    public AudioSource sfx;
    //public Slider bgmVolumeSlider;
    //public Slider sfxVolumeSlider;
    [SerializeField] private BGMSceneSetting[] theScene;

    // Start is called before the first frame update

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {

    }

    /*public void OnPointerEnter(PointerEventData eventData)
    {
        sfx.PlayOneShot(sfxSound);
    }*/

    [Serializable]
    public class BGMSceneSetting
    {
        public string CurrentScene;
        public AudioClip[] SceneBGM;
    }

    private void OnEnable()
    {
        // Subscribe to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Always unsubscribe when the object is disabled/destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Loaded scene: " + scene.name);
        foreach (BGMSceneSetting bss in theScene)
        {
            if (bss.CurrentScene == SceneManager.GetActiveScene().name)
            {
                Bgm.clip = bss.SceneBGM[UnityEngine.Random.Range(0, bss.SceneBGM.Length)];
                Bgm.loop = true;
                Bgm.Play();
            }
        }

        volumeLoad();
    }

    public void volumeSave()
    {
        PlayerPrefs.SetFloat("BGM_Volume", Bgm.volume);
        PlayerPrefs.SetFloat("SFX_Volume", sfx.volume);
        PlayerPrefs.Save();
    }

    public void volumeLoad()
    {
        if (PlayerPrefs.HasKey("BGM_Volume")) Bgm.volume = PlayerPrefs.GetFloat("BGM_Volume");
        else 
        {
            Bgm.volume = 0.3f ;
            PlayerPrefs.SetFloat("BGM_Volume", Bgm.volume);
        }

        if (PlayerPrefs.HasKey("SFX_Volume")) sfx.volume = PlayerPrefs.GetFloat("SFX_Volume");
        else
        {
            sfx.volume = 0.3f;
            PlayerPrefs.SetFloat("SFX_Volume", sfx.volume);
        }
        PlayerPrefs.Save();
    }

}
