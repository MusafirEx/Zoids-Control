using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ZoidsGameJoltLoginSceneController : MonoBehaviour
{
    [Header("Scene Flow")]
    [SerializeField] private string menuSceneName = "MainMenuScene";
    [SerializeField] private bool requireGameJoltLogin = true;

    [Tooltip("If true, offline/local play button can enter menu without Game Jolt login.")]
    [SerializeField] private bool allowOfflinePlay = false;

    [Header("Auto Login Check")]
    [SerializeField] private bool autoCheckOnStart = true;

    [Tooltip("How long to wait for GameJoltAPI auto-login before showing manual login option.")]
    [SerializeField] private float autoLoginWaitSeconds = 2f;

    [Tooltip("Check interval while waiting for auto-login.")]
    [SerializeField] private float checkInterval = 0.25f;

    [Header("References")]
    [SerializeField] private ZoidsGameJoltAccountManager accountManager;
    [SerializeField] private ZoidsGameJoltProfileBridge profileBridge;
    [SerializeField] private ZoidsGameJoltCloudSaveManager cloudSaveManager;

    [Header("UI Panels")]
    [SerializeField] private GameObject checkingPanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject loggedInPanel;
    [SerializeField] private GameObject offlinePanel;

    [Header("UI Text")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text loggedInText;

    [Header("Cloud Save")]
    [SerializeField] private bool downloadCloudSaveBeforeMenu = true;
    [SerializeField] private bool createProfileBeforeMenu = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool isChecking;
    private bool isEnteringMenu;

    private void Awake()
    {
        RefreshReferences();
    }

    private void OnEnable()
    {
        RefreshReferences();

        if (accountManager != null)
            accountManager.OnLoginStateChanged += OnLoginStateChanged;

        if (cloudSaveManager != null)
            cloudSaveManager.OnDownloadFinished += OnCloudDownloadFinished;
    }

    private void OnDisable()
    {
        if (accountManager != null)
            accountManager.OnLoginStateChanged -= OnLoginStateChanged;

        if (cloudSaveManager != null)
            cloudSaveManager.OnDownloadFinished -= OnCloudDownloadFinished;
    }

    private void Start()
    {
        if (autoCheckOnStart)
            StartCoroutine(AutoLoginCheckRoutine());
        else
            RefreshLoginUI();
    }

    private void RefreshReferences()
    {
        if (accountManager == null && ZoidsGameJoltAccountManager.Instance != null)
            accountManager = ZoidsGameJoltAccountManager.Instance;

        if (profileBridge == null && ZoidsGameJoltProfileBridge.Instance != null)
            profileBridge = ZoidsGameJoltProfileBridge.Instance;

        if (cloudSaveManager == null && ZoidsGameJoltCloudSaveManager.Instance != null)
            cloudSaveManager = ZoidsGameJoltCloudSaveManager.Instance;

        if (accountManager == null)
            accountManager = FindManager<ZoidsGameJoltAccountManager>();

        if (profileBridge == null)
            profileBridge = FindManager<ZoidsGameJoltProfileBridge>();

        if (cloudSaveManager == null)
            cloudSaveManager = FindManager<ZoidsGameJoltCloudSaveManager>();
    }

    private T FindManager<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>(true);
#endif
    }

    private IEnumerator AutoLoginCheckRoutine()
    {
        isChecking = true;
        SetStatus("Checking Game Jolt login...");
        ShowPanel(checkingPanel);

        float endTime = Time.time + Mathf.Max(0.1f, autoLoginWaitSeconds);

        while (Time.time < endTime)
        {
            RefreshReferences();

            if (accountManager != null)
                accountManager.RefreshLoginState();

            if (accountManager != null && accountManager.IsLoggedIn)
            {
                isChecking = false;
                OnLoginConfirmed();
                yield break;
            }

            yield return new WaitForSeconds(checkInterval);
        }

        isChecking = false;

        RefreshReferences();

        if (accountManager != null)
            accountManager.RefreshLoginState();

        RefreshLoginUI();
    }

    private void RefreshLoginUI()
    {
        RefreshReferences();

        bool loggedIn = accountManager != null && accountManager.IsLoggedIn;

        if (loggedIn)
        {
            ShowPanel(loggedInPanel);
            string username = accountManager.Username;
            SetStatus("Logged in as " + username);

            if (loggedInText != null)
                loggedInText.text = "Logged in as " + username;
        }
        else
        {
            if (allowOfflinePlay)
                ShowPanel(offlinePanel);
            else
                ShowPanel(loginPanel);

            SetStatus(requireGameJoltLogin
                ? "Please login with Game Jolt to continue."
                : "Game Jolt login optional.");
        }
    }

    private void ShowPanel(GameObject targetPanel)
    {
        if (checkingPanel != null)
            checkingPanel.SetActive(targetPanel == checkingPanel);

        if (loginPanel != null)
            loginPanel.SetActive(targetPanel == loginPanel);

        if (loggedInPanel != null)
            loggedInPanel.SetActive(targetPanel == loggedInPanel);

        if (offlinePanel != null)
            offlinePanel.SetActive(targetPanel == offlinePanel);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        if (debugLog)
            Debug.Log("[ZoidsGameJoltLoginSceneController] " + message);
    }

    private void OnLoginStateChanged(bool loggedIn)
    {
        if (isChecking)
            return;

        if (loggedIn)
            OnLoginConfirmed();
        else
            RefreshLoginUI();
    }

    private void OnLoginConfirmed()
    {
        RefreshReferences();

        if (accountManager != null)
            accountManager.OpenSession();

        // Do not create profile here before cloud save is downloaded.
        // In WebGL this can create a fresh uninitialized profile and make Main Menu ask faction again.
        RefreshLoginUI();

        if (debugLog && accountManager != null)
            Debug.Log("[ZoidsGameJoltLoginSceneController] Login confirmed: " + accountManager.Username);
    }

    public void ManualLogin()
    {
        RefreshReferences();

        if (accountManager == null)
        {
            SetStatus("Game Jolt account manager missing.");
            return;
        }

        SetStatus("Opening Game Jolt login...");
        accountManager.ShowSignIn();
    }

    public void ContinueToMenu()
    {
        RefreshReferences();

        if (isEnteringMenu)
            return;

        bool loggedIn = accountManager != null && accountManager.IsLoggedIn;

        if (requireGameJoltLogin && !loggedIn)
        {
            SetStatus("Login required before entering menu.");
            RefreshLoginUI();
            return;
        }

        BeginEnterMenu();
    }

    public void PlayOffline()
    {
        if (!allowOfflinePlay)
        {
            SetStatus("Offline play is disabled.");
            return;
        }

        BeginEnterMenu();
    }

    private void BeginEnterMenu()
    {
        RefreshReferences();

        isEnteringMenu = true;

        if (createProfileBeforeMenu && profileBridge != null)
            profileBridge.EnsureProfileFromCurrentAccount();

        if (downloadCloudSaveBeforeMenu && cloudSaveManager != null && accountManager != null && accountManager.IsLoggedIn)
        {
            SetStatus("Downloading private cloud save before menu...");
            cloudSaveManager.DownloadCloudSave();
            return;
        }

        LoadMenuScene();
    }

    private void OnCloudDownloadFinished(bool success, ZoidsGameJoltSavePayload payload)
    {
        if (!isEnteringMenu)
            return;

        if (success && payload != null && cloudSaveManager != null)
        {
            cloudSaveManager.ApplyPayloadToLocal(payload, true);
            SetStatus("Cloud save loaded.");
        }
        else
        {
            SetStatus("No private cloud save found. Continuing as new/local player.");

            if (createProfileBeforeMenu && profileBridge != null)
                profileBridge.EnsureProfileFromCurrentAccount();
        }

        LoadMenuScene();
    }

    private void LoadMenuScene()
    {
        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogWarning("[ZoidsGameJoltLoginSceneController] Menu scene name is empty.");
            isEnteringMenu = false;
            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }

    public void RefreshButton()
    {
        StartCoroutine(AutoLoginCheckRoutine());
    }
}
