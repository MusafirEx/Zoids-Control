using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSliderSetting : MonoBehaviour
{

    public Slider BGMSlider;
    public Slider SFXSlider;
    public GameObject volumePanel;
    private MapSFX soundManager;

    // Start is called before the first frame update
    void Start()
    {
        soundManager = MapSFX.Instance;

    }

    private void Update()
    {
        if (BGMSlider != null)
        {
            soundManager.Bgm.volume = BGMSlider.value;
        }
        if (SFXSlider != null) 
        {
            soundManager.sfx.volume = SFXSlider.value;
        }

    }

    public void ToggleVolumePanel()
    {
        volumePanel.SetActive(!volumePanel.activeSelf);
        soundManager.volumeSave();
    }

}
