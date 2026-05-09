using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MapSFX : MonoBehaviour, IPointerEnterHandler

{
    [SerializeField] private AudioSource Bgm;
    [SerializeField] private AudioClip BgmSound;
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip sfxSound;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        sfx.PlayOneShot(sfxSound);
    }

   
}
