using UnityEngine;

public class FactionSelectTestUI : MonoBehaviour
{
    [SerializeField] private FactionSelectionManager factionSelectionManager;

    public void ChooseFaction0(int faction)
    {
        bool success = factionSelectionManager.TryChooseFaction(faction);
        Debug.Log("Choose faction "+faction+" result: " + success);
    }

    public void ClearProfile()
    {
        if (PlayerProfileManager.Instance != null)
        {
            PlayerProfileManager.Instance.ClearProfile();
            Debug.Log("Profile cleared");
        }
    }

    public void PrintProfile()
    {
        if (PlayerProfileManager.Instance == null || PlayerProfileManager.Instance.CurrentProfile == null)
        {
            Debug.Log("No profile loaded");
            return;
        }

        var p = PlayerProfileManager.Instance.CurrentProfile;
        Debug.Log("ProfileInitialized=" + p.profileInitialized +
                  " | Faction=" + p.chosenFactionName +
                  " | ActiveTeamCount=" + p.activeTeamUnitIds.Count);
    }
}