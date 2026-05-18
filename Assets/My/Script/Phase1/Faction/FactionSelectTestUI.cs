using UnityEngine;

public class FactionSelectTestUI : MonoBehaviour
{
    [SerializeField] private FactionSelectionManager factionSelectionManager;

    private void Reset()
    {
        factionSelectionManager = FindFirstObjectByTypeCompat<FactionSelectionManager>();
    }

    private void Awake()
    {
        if (factionSelectionManager == null)
            factionSelectionManager = FindFirstObjectByTypeCompat<FactionSelectionManager>();
    }

    public void ChooseFaction0(int faction)
    {
        if (factionSelectionManager == null)
        {
            Debug.LogWarning("[FactionSelectTestUI] Missing FactionSelectionManager.");
            return;
        }

        bool success = factionSelectionManager.TryChooseFaction(faction);
        Debug.Log("Choose faction " + faction + " result: " + success);
    }

    public void ClearProfile()
    {
        if (PlayerProfileManager.Instance != null)
        {
            PlayerProfileManager.Instance.ClearProfile();
            Debug.Log("Profile cleared");
        }

        if (factionSelectionManager != null)
            factionSelectionManager.RefreshProfileAndPanels();
    }

    public void RefreshPanels()
    {
        if (factionSelectionManager != null)
            factionSelectionManager.RefreshProfileAndPanels();
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

    private T FindFirstObjectByTypeCompat<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
