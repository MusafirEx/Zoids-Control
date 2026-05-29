using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapSFX : MonoBehaviour/*, IPointerEnterHandler*/
{
    public static MapSFX Instance { get; private set; }

    [SerializeField] private AudioSource Bgm;
    public AudioSource sfx;
    public Slider volumeSlider;
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

    // Update is called once per frame
    void Update()
    {
       if(volumeSlider!=null)
        Bgm.volume = volumeSlider.value; ;
    }

    /*public void OnPointerEnter(PointerEventData eventData)
    {
        sfx.PlayOneShot(sfxSound);
    }*/

    [Serializable]
    public class BGMSceneSetting
    {
        public SceneAsset CurrentScene;
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
            if (bss.CurrentScene.name == SceneManager.GetActiveScene().name)
            {
                Bgm.clip = bss.SceneBGM[UnityEngine.Random.Range(0,bss.SceneBGM.Length)];
                Bgm.loop=true;
                Bgm.Play();
            }
        }
    }

}
