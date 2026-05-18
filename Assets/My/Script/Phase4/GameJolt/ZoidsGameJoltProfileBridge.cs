using UnityEngine;

public class ZoidsGameJoltProfileBridge : MonoBehaviour
{
    public static ZoidsGameJoltProfileBridge Instance { get; private set; }

    [Header("References")]
    [SerializeField] private ZoidsGameJoltAccountManager gameJoltAccountManager;
    [SerializeField] private PlayerProfileManager profileManager;
    [SerializeField] private FactionSelectionManager factionSelectionManager;
    [SerializeField] private ZoidsGameJoltCloudSaveManager cloudSaveManager;

    [Header("Fallback Local Profile")]
    [SerializeField] private string localPlayerId = "LOCAL_PLAYER";
    [SerializeField] private string localPlayerName = "Local Player";

    [Header("Options")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool ensureProfileOnStart = false;
    [SerializeField] private bool refreshFactionSelectionAfterProfile = true;

    [Tooltip("If true, an existing local placeholder profile can be renamed to Game Jolt ID/name before faction selection is completed.")]
    [SerializeField] private bool replaceUninitializedLocalProfileWithGameJolt = true;

    [Tooltip("After choosing faction successfully, upload private save to Game Jolt immediately.")]
    [SerializeField] private bool autoUploadCloudAfterFactionChoice = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    public string CurrentPlayerId
    {
        get
        {
            RefreshRuntimeReferences();

            if (gameJoltAccountManager != null && gameJoltAccountManager.IsLoggedIn)
                return gameJoltAccountManager.UserId;

            return localPlayerId;
        }
    }

    public string CurrentPlayerName
    {
        get
        {
            RefreshRuntimeReferences();

            if (gameJoltAccountManager != null && gameJoltAccountManager.IsLoggedIn)
                return gameJoltAccountManager.Username;

            return localPlayerName;
        }
    }

    public bool IsUsingGameJolt
    {
        get
        {
            RefreshRuntimeReferences();
            return gameJoltAccountManager != null && gameJoltAccountManager.IsLoggedIn;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        RefreshRuntimeReferences();
    }

    private void Start()
    {
        if (ensureProfileOnStart)
            EnsureProfileFromCurrentAccount();
    }

    private void OnEnable()
    {
        RefreshRuntimeReferences();

        if (gameJoltAccountManager != null)
            gameJoltAccountManager.OnLoginStateChanged += OnGameJoltLoginStateChanged;
    }

    private void OnDisable()
    {
        if (gameJoltAccountManager != null)
            gameJoltAccountManager.OnLoginStateChanged -= OnGameJoltLoginStateChanged;
    }

    public void RefreshRuntimeReferences()
    {
        if (gameJoltAccountManager == null && ZoidsGameJoltAccountManager.Instance != null)
            gameJoltAccountManager = ZoidsGameJoltAccountManager.Instance;

        if (profileManager == null && PlayerProfileManager.Instance != null)
            profileManager = PlayerProfileManager.Instance;

        if (cloudSaveManager == null && ZoidsGameJoltCloudSaveManager.Instance != null)
            cloudSaveManager = ZoidsGameJoltCloudSaveManager.Instance;

        if (gameJoltAccountManager == null)
            gameJoltAccountManager = FindManager<ZoidsGameJoltAccountManager>();

        if (profileManager == null)
            profileManager = FindManager<PlayerProfileManager>();

        if (cloudSaveManager == null)
            cloudSaveManager = FindManager<ZoidsGameJoltCloudSaveManager>();

        if (factionSelectionManager == null)
            factionSelectionManager = FindManager<FactionSelectionManager>();
    }

    private T FindManager<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>(true);
#endif
    }

    private void OnGameJoltLoginStateChanged(bool loggedIn)
    {
        // Do not create a profile here. Login scene must download cloud save first.
        if (debugLog)
            Debug.Log("[ZoidsGameJoltProfileBridge] Game Jolt login state changed. loggedIn=" + loggedIn);
    }

    public PlayerProfileData EnsureProfileFromCurrentAccount()
    {
        RefreshRuntimeReferences();

        if (profileManager == null)
        {
            Debug.LogWarning("[ZoidsGameJoltProfileBridge] PlayerProfileManager missing.");
            return null;
        }

        string playerId = CurrentPlayerId;
        string playerName = CurrentPlayerName;

        PlayerProfileData profile = profileManager.CurrentProfile;

        if (profile == null)
        {
            profile = profileManager.EnsureProfile(playerId, playerName);

            if (debugLog)
                Debug.Log("[ZoidsGameJoltProfileBridge] Created/loaded profile for " + playerName + " (" + playerId + ")");
        }
        else
        {
            bool shouldReplace =
                replaceUninitializedLocalProfileWithGameJolt &&
                IsUsingGameJolt &&
                !profile.profileInitialized &&
                (profile.playerId == localPlayerId || string.IsNullOrEmpty(profile.playerId));

            bool shouldFillEmpty =
                string.IsNullOrEmpty(profile.playerId) ||
                string.IsNullOrEmpty(profile.playerName);

            if (shouldReplace || shouldFillEmpty)
            {
                profile.playerId = playerId;
                profile.playerName = playerName;
                profile.Touch();
                profileManager.SaveProfile();

                if (debugLog)
                    Debug.Log("[ZoidsGameJoltProfileBridge] Updated profile identity to " + playerName + " (" + playerId + ")");
            }
        }

        if (refreshFactionSelectionAfterProfile)
            RefreshFactionSelection();

        return profileManager.CurrentProfile;
    }

    public bool TryChooseFactionWithCurrentAccount(int factionId)
    {
        RefreshRuntimeReferences();

        if (factionSelectionManager == null)
        {
            Debug.LogWarning("[ZoidsGameJoltProfileBridge] FactionSelectionManager missing.");
            return false;
        }

        string playerId = CurrentPlayerId;
        string playerName = CurrentPlayerName;

        bool success = factionSelectionManager.TryChooseFaction(factionId, playerId, playerName);

        if (success && autoUploadCloudAfterFactionChoice)
            UploadPrivateCloudSaveNow();

        return success;
    }

    public void ChooseFactionWithCurrentAccount(int factionId)
    {
        bool success = TryChooseFactionWithCurrentAccount(factionId);

        if (debugLog)
            Debug.Log("[ZoidsGameJoltProfileBridge] Choose faction " + factionId + " success=" + success);
    }

    public void UploadPrivateCloudSaveNow()
    {
        RefreshRuntimeReferences();

        if (cloudSaveManager == null)
        {
            Debug.LogWarning("[ZoidsGameJoltProfileBridge] Cannot upload private save. Cloud save manager missing.");
            return;
        }

        if (gameJoltAccountManager == null || !gameJoltAccountManager.IsLoggedIn)
        {
            if (debugLog)
                Debug.Log("[ZoidsGameJoltProfileBridge] Skip private cloud upload. Not logged in.");
            return;
        }

        cloudSaveManager.UploadLocalSaveToCloud();
    }

    public void RefreshFactionSelection()
    {
        RefreshRuntimeReferences();

        if (factionSelectionManager != null)
            factionSelectionManager.RefreshProfileAndPanels();
    }

    public void DebugPrintCurrentIdentity()
    {
        Debug.Log("[ZoidsGameJoltProfileBridge] Current identity: " +
                  CurrentPlayerName + " (" + CurrentPlayerId + ") GameJolt=" + IsUsingGameJolt);
    }
}
