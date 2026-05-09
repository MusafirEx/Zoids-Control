using System;
using System.Collections.Generic;
using UnityEngine;

public class FactionSelectionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerProfileManager profileManager;
    [SerializeField] private FactionStarterDatabase starterDatabase;

    public event Action<PlayerProfileData> OnFactionChosen;
    public event Action<int> OnFactionChoiceRejected;

    private void Reset()
    {
        profileManager = FindObjectOfType<PlayerProfileManager>();
    }

    private void Awake()
    {
        if (profileManager == null)
            profileManager = FindObjectOfType<PlayerProfileManager>();
    }

    public bool NeedsFactionSelection()
    {
        if (profileManager == null)
        {
            Debug.LogError("[FactionSelectionManager] Missing PlayerProfileManager reference.");
            return false;
        }

        return !profileManager.HasInitializedProfile;
    }

    public bool TryChooseFaction(int factionId, string playerId = "", string playerName = "")
    {
        if (profileManager == null)
        {
            Debug.LogError("[FactionSelectionManager] Missing PlayerProfileManager reference.");
            OnFactionChoiceRejected?.Invoke(factionId);
            return false;
        }

        if (starterDatabase == null)
        {
            Debug.LogError("[FactionSelectionManager] Missing FactionStarterDatabase reference.");
            OnFactionChoiceRejected?.Invoke(factionId);
            return false;
        }

        FactionStarterData starter = starterDatabase.GetFaction(factionId);
        if (starter == null || !starter.IsValid())
        {
            Debug.LogWarning("[FactionSelectionManager] Invalid faction choice: " + factionId);
            OnFactionChoiceRejected?.Invoke(factionId);
            return false;
        }

        PlayerProfileData profile = profileManager.EnsureProfile(playerId, playerName);

        if (profile.profileInitialized)
        {
            Debug.LogWarning("[FactionSelectionManager] Profile is already initialized. Clear profile first if you want to reselect faction.");
            OnFactionChoiceRejected?.Invoke(factionId);
            return false;
        }

        ApplyStarter(profile, starter);
        profileManager.SaveProfile();

        OnFactionChosen?.Invoke(profile);
        return true;
    }

    private void ApplyStarter(PlayerProfileData profile, FactionStarterData starter)
    {
        profile.profileInitialized = true;
        profile.chosenFactionId = starter.factionId;
        profile.chosenFactionName = starter.factionName ?? "";
        profile.activeTeamUnitIds = new List<int>();

        for (int i = 0; i < starter.starterUnitIds.Count; i++)
        {
            int unitId = starter.starterUnitIds[i];

            profile.AddOwnedCount(unitId, starter.starterOwnedCountPerUnit);
            profile.activeTeamUnitIds.Add(unitId);
        }

        profile.Touch();
    }
}
