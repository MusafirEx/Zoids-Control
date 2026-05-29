using UnityEngine;
using UnityEngine.EventSystems;

public class FactionSelectButtonBridge : MonoBehaviour,IPointerEnterHandler
{
    [SerializeField] private int factionId = 0;
    private MapSFX soundManager;
    public AudioClip buttonHoverFx;

    public void Start()
    {
        soundManager = MapSFX.Instance;
    }

    public void ChooseFaction()
    {
        if (ZoidsGameJoltProfileBridge.Instance == null)
        {
            Debug.LogWarning("[FactionSelectButtonBridge] ZoidsGameJoltProfileBridge.Instance missing.");
            return;
        }

        ZoidsGameJoltProfileBridge.Instance.ChooseFactionWithCurrentAccount(factionId);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        soundManager.sfx.PlayOneShot(buttonHoverFx);
    }
}