using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TBTK;

public class FactoryListUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FactoryManager factoryManager;
    [SerializeField] private FactoryUnitButtonUI buttonPrefab;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private FactoryUnitDetailUI detailUI;

    [Header("Options")]
    [SerializeField] private bool buildOnEnable = true;
    [SerializeField] private bool selectFirstOnBuild = true;

    [Header("Button Parent Resize")]
    [SerializeField] private bool autoResizeButtonParent = true;
    [SerializeField] private float buttonSpacing = 10f;
    [SerializeField] private float fallbackButtonHeight = 60f;

    [Header("Game Jolt Cloud Sync")]
    [SerializeField] private ZoidsGameJoltCloudSaveManager cloudSaveManager;

    [Tooltip("Panel that blocks the canvas while downloading/uploading. Put this panel above all Factory UI.")]
    [SerializeField] private GameObject canvasBlockerPanel;

    [Tooltip("Optional TMP text inside the blocker panel.")]
    [SerializeField] private TMP_Text blockerStatusLabel;

    [SerializeField] private bool downloadLatestCloudSaveOnEnable = true;
    [SerializeField] private bool uploadCloudSaveAfterManufacture = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private readonly List<FactoryUnitButtonUI> spawnedButtons = new List<FactoryUnitButtonUI>();
    private bool isCloudSyncing = false;

    public bool IsCloudSyncing { get { return isCloudSyncing; } }

    private void Reset()
    {
        factoryManager = FindFirstObjectByTypeCompat<FactoryManager>();
    }

    private void Awake()
    {
        RefreshReferences();
    }

    private void OnEnable()
    {
        RefreshReferences();
        SubscribeCloudEvents();

        if (downloadLatestCloudSaveOnEnable)
            DownloadLatestDataFromGameJolt();
        else if (buildOnEnable)
            BuildList();
    }

    private void OnDisable()
    {
        UnsubscribeCloudEvents();
        SetCanvasBlocker(false, "");
    }

    public void RefreshReferences()
    {
        if (factoryManager == null)
            factoryManager = FindFirstObjectByTypeCompat<FactoryManager>();

        if (cloudSaveManager == null && ZoidsGameJoltCloudSaveManager.Instance != null)
            cloudSaveManager = ZoidsGameJoltCloudSaveManager.Instance;

        if (cloudSaveManager == null)
            cloudSaveManager = FindFirstObjectByTypeCompat<ZoidsGameJoltCloudSaveManager>();
    }

    public void BuildList()
    {
        ClearList();

        if (factoryManager == null)
        {
            Debug.LogWarning("[FactoryListUI] Missing FactoryManager.");
            return;
        }

        if (buttonPrefab == null || buttonParent == null)
        {
            Debug.LogWarning("[FactoryListUI] Missing button prefab or button parent.");
            return;
        }

        List<Unit> units = factoryManager.GetAllFactoryUnits();
        FactoryUnitButtonUI firstButton = null;

        for (int i = 0; i < units.Count; i++)
        {
            Unit unit = units[i];
            if (unit == null)
                continue;

            if (!factoryManager.ShouldShowInFactoryList(unit.prefabID))
                continue;

            FactoryUnitButtonUI button = Instantiate(buttonPrefab, buttonParent);
            button.gameObject.SetActive(true);
            button.Setup(unit, factoryManager, this);
            spawnedButtons.Add(button);

            if (firstButton == null)
                firstButton = button;
        }

        ResizeButtonParent();

        if (selectFirstOnBuild && firstButton != null)
            SelectUnit(firstButton.UnitId);

        RefreshAll();
    }

    public void ClearList()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
                Destroy(spawnedButtons[i].gameObject);
        }

        spawnedButtons.Clear();
    }

    private void ResizeButtonParent()
    {
        if (!autoResizeButtonParent)
            return;

        if (buttonParent == null)
            return;

        RectTransform parentRect = buttonParent as RectTransform;
        if (parentRect == null)
            parentRect = buttonParent.GetComponent<RectTransform>();

        if (parentRect == null)
            return;

        int buttonCount = spawnedButtons.Count;
        if (buttonCount <= 0)
        {
            parentRect.sizeDelta = new Vector2(parentRect.sizeDelta.x, 0f);
            return;
        }

        float buttonHeight = fallbackButtonHeight;

        if (buttonPrefab != null)
        {
            RectTransform prefabRect = buttonPrefab.GetComponent<RectTransform>();
            if (prefabRect != null && prefabRect.rect.height > 0)
                buttonHeight = prefabRect.rect.height;
        }

        float height = (buttonHeight + buttonSpacing) * buttonCount;
        parentRect.sizeDelta = new Vector2(parentRect.sizeDelta.x, height);
    }

    public void SelectUnit(int unitId)
    {
        if (isCloudSyncing)
            return;

        if (detailUI != null)
            detailUI.ShowUnit(unitId);

        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] == null) continue;
            spawnedButtons[i].SetSelected(spawnedButtons[i].UnitId == unitId);
        }
    }

    public void RefreshAll()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
                spawnedButtons[i].Refresh();
        }

        if (detailUI != null)
            detailUI.Refresh();
    }

    public void NotifyManufactureSuccess()
    {
        RefreshAll();

        if (uploadCloudSaveAfterManufacture)
            UploadFactoryDataToGameJolt("Uploading factory data...");
    }

    public void DownloadLatestDataFromGameJolt()
    {
        RefreshReferences();
        SubscribeCloudEvents();

        if (cloudSaveManager == null)
        {
            if (debugLog)
                Debug.LogWarning("[FactoryListUI] Cloud save manager missing. Using local factory data.");

            if (buildOnEnable)
                BuildList();
            return;
        }

        if (cloudSaveManager.IsBusy)
        {
            SetCanvasBlocker(true, "Syncing...");
            return;
        }

        SetCanvasBlocker(true, "Downloading latest factory data...");
        cloudSaveManager.DownloadAndApplyCloudSave();
    }

    public void UploadFactoryDataToGameJolt(string blockerMessage)
    {
        RefreshReferences();
        SubscribeCloudEvents();

        if (cloudSaveManager == null)
        {
            if (debugLog)
                Debug.LogWarning("[FactoryListUI] Cloud save manager missing. Factory saved locally only.");
            return;
        }

        if (cloudSaveManager.IsBusy)
        {
            SetCanvasBlocker(true, "Syncing...");
            return;
        }

        SetCanvasBlocker(true, blockerMessage);
        cloudSaveManager.UploadLocalSaveToCloud();
    }

    private void SubscribeCloudEvents()
    {
        if (cloudSaveManager == null)
            return;

        cloudSaveManager.OnDownloadFinished -= OnCloudDownloadFinished;
        cloudSaveManager.OnUploadFinished -= OnCloudUploadFinished;
        cloudSaveManager.OnPayloadApplied -= OnCloudPayloadApplied;

        cloudSaveManager.OnDownloadFinished += OnCloudDownloadFinished;
        cloudSaveManager.OnUploadFinished += OnCloudUploadFinished;
        cloudSaveManager.OnPayloadApplied += OnCloudPayloadApplied;
    }

    private void UnsubscribeCloudEvents()
    {
        if (cloudSaveManager == null)
            return;

        cloudSaveManager.OnDownloadFinished -= OnCloudDownloadFinished;
        cloudSaveManager.OnUploadFinished -= OnCloudUploadFinished;
        cloudSaveManager.OnPayloadApplied -= OnCloudPayloadApplied;
    }

    private void OnCloudDownloadFinished(bool success, ZoidsGameJoltSavePayload payload)
    {
        // DownloadAndApplyCloudSave already applies payload and reloads local managers when successful.
        RefreshReferences();

        if (UnitProgressManager.Instance != null)
            UnitProgressManager.Instance.LoadProgress();

        if (PlayerProfileManager.Instance != null)
            PlayerProfileManager.Instance.LoadProfile();

        if (buildOnEnable)
            BuildList();
        else
            RefreshAll();

        SetCanvasBlocker(false, "");

        if (debugLog)
            Debug.Log("[FactoryListUI] Cloud download finished. success=" + success);
    }

    private void OnCloudPayloadApplied(ZoidsGameJoltSavePayload payload)
    {
        RefreshReferences();

        if (buildOnEnable)
            BuildList();
        else
            RefreshAll();
    }

    private void OnCloudUploadFinished(bool success)
    {
        SetCanvasBlocker(false, "");

        if (debugLog)
            Debug.Log("[FactoryListUI] Cloud upload finished. success=" + success);

        RefreshAll();
    }

    private void SetCanvasBlocker(bool active, string message)
    {
        isCloudSyncing = active;

        if (canvasBlockerPanel != null)
            canvasBlockerPanel.SetActive(active);

        if (blockerStatusLabel != null)
            blockerStatusLabel.text = message;

        RefreshManufactureButtonState();
    }

    private void RefreshManufactureButtonState()
    {
        if (detailUI != null)
            detailUI.Refresh();
    }

    private T FindFirstObjectByTypeCompat<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>(true);
#endif
    }
}
