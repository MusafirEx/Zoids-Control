using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ZoidsGameJoltMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ZoidsGameJoltAccountManager accountManager;
    [SerializeField] private ZoidsGameJoltCloudSaveManager cloudSaveManager;
    [SerializeField] private Image ProfileLogo;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        RefreshReferences();
    }

    private void OnEnable()
    {
        RefreshReferences();

        if (cloudSaveManager != null)
        {
            cloudSaveManager.OnUploadFinished += OnUploadFinished;
            cloudSaveManager.OnDownloadFinished += OnDownloadFinished;
            cloudSaveManager.OnPayloadApplied += OnPayloadApplied;
        }

        RefreshStatus();
    }

    private void OnDisable()
    {
        if (cloudSaveManager != null)
        {
            cloudSaveManager.OnUploadFinished -= OnUploadFinished;
            cloudSaveManager.OnDownloadFinished -= OnDownloadFinished;
            cloudSaveManager.OnPayloadApplied -= OnPayloadApplied;
        }
    }

    private void RefreshReferences()
    {
        if (accountManager == null && ZoidsGameJoltAccountManager.Instance != null)
            accountManager = ZoidsGameJoltAccountManager.Instance;

        if (cloudSaveManager == null && ZoidsGameJoltCloudSaveManager.Instance != null)
            cloudSaveManager = ZoidsGameJoltCloudSaveManager.Instance;

        if (accountManager == null)
            accountManager = FindManager<ZoidsGameJoltAccountManager>();

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

    public void RefreshStatus()
    {
        RefreshReferences();

        if (accountManager == null)
        {
            SetStatus("Game Jolt account manager missing.");
            return;
        }

        if (accountManager.IsLoggedIn)
        {
            SetStatus(accountManager.Username);
            LogoChanger();
        }
        else
            SetStatus("Not logged in");
    }

    public void SignIn()
    {
        RefreshReferences();

        if (accountManager == null)
        {
            SetStatus("Game Jolt account manager missing.");
            return;
        }

        SetStatus("Opening Game Jolt sign in...");
        accountManager.ShowSignIn();
    }

    public void SignOut()
    {
        RefreshReferences();

        if (accountManager == null)
            return;

        accountManager.SignOut();
        RefreshStatus();
    }

    public void UploadSave()
    {
        RefreshReferences();

        if (cloudSaveManager == null)
        {
            SetStatus("Cloud save manager missing.");
            return;
        }

        SetStatus("Uploading save to Game Jolt...");
        cloudSaveManager.UploadLocalSaveToCloud();
    }

    public void DownloadSave()
    {
        RefreshReferences();

        if (cloudSaveManager == null)
        {
            SetStatus("Cloud save manager missing.");
            return;
        }

        SetStatus("Downloading save from Game Jolt...");
        cloudSaveManager.DownloadCloudSave();
    }

    public void DownloadAndApplySave()
    {
        RefreshReferences();

        if (cloudSaveManager == null)
        {
            SetStatus("Cloud save manager missing.");
            return;
        }

        SetStatus("Downloading and applying save...");
        cloudSaveManager.DownloadAndApplyCloudSave();
    }

    public void DeleteCloudSave()
    {
        RefreshReferences();

        if (cloudSaveManager == null)
        {
            SetStatus("Cloud save manager missing.");
            return;
        }

        SetStatus("Deleting Game Jolt cloud save...");
        cloudSaveManager.DeleteCloudSave();
    }

    private void OnUploadFinished(bool success)
    {
        SetStatus("Upload finished. Success=" + success);
    }

    private void OnDownloadFinished(bool success, ZoidsGameJoltSavePayload payload)
    {
        if (success && payload != null)
            SetStatus("Download finished. Save from " + payload.savedAtUtc);
        else
            SetStatus("Download failed or no cloud save found.");
    }

    private void OnPayloadApplied(ZoidsGameJoltSavePayload payload)
    {
        SetStatus("Cloud save applied. SavedAt=" + payload.savedAtUtc);
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;

        Debug.Log("[ZoidsGameJoltMenuUI] " + text);
    }

    public void LogoChanger()
    {
      ProfileLogo.sprite = FactionSelectionManager.instance.starterDatabase.GetFaction(PlayerProfileManager.Instance.CurrentProfile.chosenFactionId).FactionMainLogo;
        
    }
}
