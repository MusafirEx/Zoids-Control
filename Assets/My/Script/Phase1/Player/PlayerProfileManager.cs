using System;
using UnityEngine;

public class PlayerProfileManager : MonoBehaviour
{
    public static PlayerProfileManager Instance { get; private set; }

    public const string DefaultSaveKey = "zoids_player_profile_main";

    [Header("Local Save")]
    [SerializeField] private string saveKey = DefaultSaveKey;
    [SerializeField] private bool autoLoadOnAwake = true;

    public PlayerProfileData CurrentProfile { get; private set; }

    public bool HasLoadedProfile => CurrentProfile != null;
    public bool HasInitializedProfile => CurrentProfile != null && CurrentProfile.profileInitialized;

    public event Action<PlayerProfileData> OnProfileLoaded;
    public event Action<PlayerProfileData> OnProfileCreated;
    public event Action<PlayerProfileData> OnProfileSaved;
    public event Action OnProfileCleared;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (autoLoadOnAwake)
            LoadProfile();
    }

    public bool HasSavedProfile()
    {
        return PlayerPrefs.HasKey(saveKey) && !string.IsNullOrEmpty(PlayerPrefs.GetString(saveKey, ""));
    }

    public PlayerProfileData LoadProfile()
    {
        if (!HasSavedProfile())
        {
            CurrentProfile = null;
            return null;
        }

        string json = PlayerPrefs.GetString(saveKey, "");
        if (string.IsNullOrEmpty(json))
        {
            CurrentProfile = null;
            return null;
        }

        try
        {
            CurrentProfile = JsonUtility.FromJson<PlayerProfileData>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("[PlayerProfileManager] Failed to parse profile JSON: " + ex.Message);
            CurrentProfile = null;
            return null;
        }

        if (CurrentProfile == null)
            CurrentProfile = new PlayerProfileData();

        OnProfileLoaded?.Invoke(CurrentProfile);
        return CurrentProfile;
    }

    public void SaveProfile()
    {
        if (CurrentProfile == null)
        {
            Debug.LogWarning("[PlayerProfileManager] SaveProfile called but CurrentProfile is null.");
            return;
        }

        CurrentProfile.Touch();

        string json = JsonUtility.ToJson(CurrentProfile, true);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();

        OnProfileSaved?.Invoke(CurrentProfile);
    }

    public PlayerProfileData CreateNewProfile(string playerId = "", string playerName = "")
    {
        CurrentProfile = new PlayerProfileData
        {
            playerId = playerId ?? "",
            playerName = playerName ?? "",
            profileInitialized = false
        };

        CurrentProfile.Touch();
        SaveProfile();
        OnProfileCreated?.Invoke(CurrentProfile);

        return CurrentProfile;
    }

    public PlayerProfileData EnsureProfile(string playerId = "", string playerName = "")
    {
        if (LoadProfile() != null)
            return CurrentProfile;

        return CreateNewProfile(playerId, playerName);
    }

    public void ClearProfile()
    {
        CurrentProfile = null;
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
        OnProfileCleared?.Invoke();
    }

    public bool CanAttemptAreaBattle(long currentUnixTime)
    {
        if (CurrentProfile == null)
            return false;

        return currentUnixTime >= CurrentProfile.nextAreaBattleUnix;
    }

    public void SetAreaBattleCooldownHours(int hours)
    {
        if (CurrentProfile == null)
            return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        CurrentProfile.nextAreaBattleUnix = now + (hours * 3600L);
        SaveProfile();
    }
}
