using UnityEngine;

public class FactionSelectButtonBridge : MonoBehaviour
{
    [SerializeField] private int factionId = 0;

    public void ChooseFaction()
    {
        if (ZoidsGameJoltProfileBridge.Instance == null)
        {
            Debug.LogWarning("[FactionSelectButtonBridge] ZoidsGameJoltProfileBridge.Instance missing.");
            return;
        }

        ZoidsGameJoltProfileBridge.Instance.ChooseFactionWithCurrentAccount(factionId);
    }
}