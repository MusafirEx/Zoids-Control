using System;
using UnityEngine;
using GameJolt.API;
using GameJolt.API.Objects;
using GameJolt.UI;

public class ZoidsGameJoltAccountManager : MonoBehaviour
{
    public static ZoidsGameJoltAccountManager Instance { get; private set; }

    [Header("Session")]
    [SerializeField] private bool openSessionOnStart = true;
    [SerializeField] private bool pingSession = true;
    [SerializeField] private float pingInterval = 30f;

    [Header("Fallback")]
    [SerializeField] private string localUserId = "LOCAL_PLAYER";
    [SerializeField] private string localUsername = "Local Player";

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private float nextPingTime;

    public bool IsLoggedIn
    {
        get
        {
            return GameJoltAPI.Instance != null &&
                   GameJoltAPI.Instance.HasSignedInUser &&
                   GameJoltAPI.Instance.CurrentUser != null;
        }
    }

    public User CurrentUser
    {
        get
        {
            if (!IsLoggedIn)
                return null;

            return GameJoltAPI.Instance.CurrentUser;
        }
    }

    public string UserId
    {
        get
        {
            if (IsLoggedIn)
                return GameJoltAPI.Instance.CurrentUser.ID.ToString();

            return localUserId;
        }
    }

    public string Username
    {
        get
        {
            if (IsLoggedIn)
                return GameJoltAPI.Instance.CurrentUser.Name;

            return localUsername;
        }
    }

    public event Action<bool> OnLoginStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        RefreshLoginState();

        if (openSessionOnStart && IsLoggedIn)
            OpenSession();
    }

    private void Update()
    {
        if (!pingSession || !IsLoggedIn)
            return;

        if (Time.time < nextPingTime)
            return;

        nextPingTime = Time.time + pingInterval;
        Sessions.Ping(SessionStatus.Active);

        if (debugLog)
            Debug.Log("[ZoidsGameJoltAccountManager] Session ping.");
    }

    public void ShowSignIn()
    {
        if (GameJoltUI.Instance == null)
        {
            Debug.LogWarning("[ZoidsGameJoltAccountManager] GameJoltUI.Instance missing.");
            return;
        }

        GameJoltUI.Instance.ShowSignIn(
            signedIn =>
            {
                RefreshLoginState();

                if (signedIn)
                    OpenSession();

                if (debugLog)
                    Debug.Log("[ZoidsGameJoltAccountManager] Sign in result=" + signedIn);
            },
            userFetched =>
            {
                RefreshLoginState();

                if (debugLog)
                    Debug.Log("[ZoidsGameJoltAccountManager] User fetched=" + userFetched);
            }
        );
    }

    public void SignOut()
    {
        if (IsLoggedIn)
        {
            GameJoltAPI.Instance.CurrentUser.SignOut();
        }

        RefreshLoginState();
    }

    public void RefreshLoginState()
    {
        bool logged = IsLoggedIn;

        if (debugLog)
        {
            Debug.Log("[ZoidsGameJoltAccountManager] Login state. LoggedIn=" + logged +
                      " UserId=" + UserId +
                      " Username=" + Username);
        }

        OnLoginStateChanged?.Invoke(logged);
    }

    public void OpenSession()
    {
        if (!IsLoggedIn)
        {
            if (debugLog)
                Debug.Log("[ZoidsGameJoltAccountManager] Cannot open session. Not logged in.");
            return;
        }

        Sessions.Open(success =>
        {
            if (debugLog)
                Debug.Log("[ZoidsGameJoltAccountManager] Open session success=" + success);
        });
    }

    public void CloseSession()
    {
        if (!IsLoggedIn)
            return;

        Sessions.Close(success =>
        {
            if (debugLog)
                Debug.Log("[ZoidsGameJoltAccountManager] Close session success=" + success);
        });
    }

    private void OnApplicationQuit()
    {
        CloseSession();
    }
}
