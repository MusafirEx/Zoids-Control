using System;
using System.Collections.Generic;
using UnityEngine;

public class FactionSelectionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerProfileManager profileManager;
    [SerializeField] private FactionStarterDatabase starterDatabase;
    [SerializeField] private ZoidsGameJoltCloudSaveManager cloudSaveManager;

    [Header("Panels")]
    [SerializeField] private GameObject factionSelectionPanel;
    [SerializeField] private GameObject mainMenuSelectionPanel;

    [Header("Profile Creation")]
    [SerializeField] private bool createLocalProfileIfMissing = true;

    [Tooltip("Temporary local/offline player id. Later this can be replaced by Game Jolt user id.")]
    [SerializeField] private string offlinePlayerId = "LOCAL_PLAYER";

    [Tooltip("Temporary local/offline player name. Later this can be replaced by Game Jolt username.")]
    [SerializeField] private string offlinePlayerName = "Local Player";

    [Header("Game Jolt Cloud Save")]
    [SerializeField] private bool uploadToGameJoltAfterFactionChosen = true;

    [Tooltip("Panel that blocks UI while profile/starter save is uploading.")]
    [SerializeField] private GameObject canvasBlockerPanel;

    [SerializeField] private TMPro.TMP_Text blockerStatusLabel;

    [Header("Auto Refresh")]
    [SerializeField] private bool refreshPanelsOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public event Action<PlayerProfileData> OnFactionChosen;
    public event Action<int> OnFactionChoiceRejected;

    private void Reset()
    {
        RefreshRuntimeReferences();
    }

    private void Awake()
    {
        RefreshRuntimeReferences();
    }

    private void Start()
    {
        if (refreshPanelsOnStart)
            RefreshProfileAndPanels();
    }

    private void OnEnable()
    {
        RefreshRuntimeReferences();
        SubscribeCloudEvents();

        if (refreshPanelsOnStart)
            RefreshProfileAndPanels();
    }

    private void OnDisable()
    {
        UnsubscribeCloudEvents();
        SetCanvasBlocker(false, "");
    }

    public void RefreshRuntimeReferences()
    {
        if (profileManager == null && PlayerProfileManager.Instance != null)
            profileManager = PlayerProfileManager.Instance;

        if (cloudSaveManager == null && ZoidsGameJoltCloudSaveManager.Instance != null)
            cloudSaveManager = ZoidsGameJoltCloudSaveManager.Instance;

        if (profileManager == null)
            profileManager = FindFirstObjectByTypeCompat<PlayerProfileManager>();

        if (cloudSaveManager == null)
            cloudSaveManager = FindFirstObjectByTypeCompat<ZoidsGameJoltCloudSaveManager>();
    }

    private T FindFirstObjectByTypeCompat<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<T>();
#else
        return UnityEngine.Object.FindObjectOfType<T>();
#endif
    }

    public void RefreshProfileAndPanels()
    {
        RefreshRuntimeReferences();

        if (profileManager == null)
        {
            Debug.LogError("[FactionSelectionManager] Missing PlayerProfileManager reference.");
            ShowFactionSelectionPanel(true);
            return;
        }

        // Later Game Jolt flow:
        // Replace this with EnsureProfile(gameJoltUserId, gameJoltUsername).
        if (createLocalProfileIfMissing && profileManager.CurrentProfile == null)
        {
            profileManager.EnsureProfile(offlinePlayerId, offlinePlayerName);

            if (debugLog)
                Debug.Log("[FactionSelectionManager] Created/loaded temporary local profile placeholder.");
        }

        bool profileExistsAndInitialized = HasInitializedProfile();

        if (debugLog)
        {
            Debug.Log("[FactionSelectionManager] Profile check. initialized=" + profileExistsAndInitialized);
        }

        ShowFactionSelectionPanel(!profileExistsAndInitialized);
    }

    public bool HasInitializedProfile()
    {
        RefreshRuntimeReferences();

        if (profileManager == null)
            return false;

        if (profileManager.HasInitializedProfile)
            return true;

        if (profileManager.CurrentProfile != null && profileManager.CurrentProfile.profileInitialized)
            return true;

        return false;
    }

    public bool NeedsFactionSelection()
    {
        return !HasInitializedProfile();
    }

    private void ShowFactionSelectionPanel(bool showFactionSelection)
    {
        if (factionSelectionPanel != null)
            factionSelectionPanel.SetActive(showFactionSelection);

        if (mainMenuSelectionPanel != null)
            mainMenuSelectionPanel.SetActive(!showFactionSelection);
    }

    public bool TryChooseFaction(int factionId, string playerId = "", string playerName = "")
    {
        RefreshRuntimeReferences();

        if (profileManager == null)
        {
            Debug.LogError("[FactionSelectionManager] Missing PlayerProfileManager reference.");
            OnFactionChoiceRejected?.Invoke(factionId);
            RefreshProfileAndPanels();
            return false;
        }

        if (starterDatabase == null)
        {
            Debug.LogError("[FactionSelectionManager] Missing FactionStarterDatabase reference.");
            OnFactionChoiceRejected?.Invoke(factionId);
            RefreshProfileAndPanels();
            return false;
        }

        FactionStarterData starter = starterDatabase.GetFaction(factionId);
        if (starter == null || !starter.IsValid())
        {
            Debug.LogWarning("[FactionSelectionManager] Invalid faction choice: " + factionId);
            OnFactionChoiceRejected?.Invoke(factionId);
            RefreshProfileAndPanels();
            return false;
        }

        // Later Game Jolt:
        // If playerId/playerName are empty, use Game Jolt account data here.
        if (string.IsNullOrEmpty(playerId))
            playerId = offlinePlayerId;

        if (string.IsNullOrEmpty(playerName))
            playerName = offlinePlayerName;

        PlayerProfileData profile = profileManager.EnsureProfile(playerId, playerName);

        if (profile.profileInitialized)
        {
            Debug.LogWarning("[FactionSelectionManager] Profile is already initialized. Clear profile first if you want to reselect faction.");
            OnFactionChoiceRejected?.Invoke(factionId);
            RefreshProfileAndPanels();
            return false;
        }

        ApplyStarter(profile, starter);
        profileManager.SaveProfile();

        RefreshProfileAndPanels();

        OnFactionChosen?.Invoke(profile);

        if (uploadToGameJoltAfterFactionChosen)
            UploadProfileAndStarterToGameJolt();

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


    // ---------------------------------------------------------
    // Game Jolt cloud save
    // ---------------------------------------------------------

    private void SubscribeCloudEvents()
    {
        if (cloudSaveManager == null)
            return;

        cloudSaveManager.OnUploadFinished -= OnCloudUploadFinished;
        cloudSaveManager.OnUploadFinished += OnCloudUploadFinished;
    }

    private void UnsubscribeCloudEvents()
    {
        if (cloudSaveManager == null)
            return;

        cloudSaveManager.OnUploadFinished -= OnCloudUploadFinished;
    }

    private void UploadProfileAndStarterToGameJolt()
    {
        RefreshRuntimeReferences();
        SubscribeCloudEvents();

        if (cloudSaveManager == null)
        {
            if (debugLog)
                Debug.LogWarning("[FactionSelectionManager] Cloud save manager missing. Profile/starter saved locally only.");
            return;
        }

        if (cloudSaveManager.IsBusy)
        {
            if (debugLog)
                Debug.LogWarning("[FactionSelectionManager] Cloud save manager busy. Upload skipped.");
            return;
        }

        SetCanvasBlocker(true, "Uploading profile and starter Zoids...");
        cloudSaveManager.UploadLocalSaveToCloud();
    }

    private void OnCloudUploadFinished(bool success)
    {
        SetCanvasBlocker(false, "");

        if (debugLog)
            Debug.Log("[FactionSelectionManager] Game Jolt profile/starter upload success=" + success);
    }

    private void SetCanvasBlocker(bool active, string message)
    {
        if (canvasBlockerPanel != null)
            canvasBlockerPanel.SetActive(active);

        if (blockerStatusLabel != null)
            blockerStatusLabel.text = message;
    }

    public void DebugRefreshPanels()
    {
        RefreshProfileAndPanels();
    }
}
