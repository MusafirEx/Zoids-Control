using System.Collections.Generic;
using UnityEngine;
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

    private readonly List<FactoryUnitButtonUI> spawnedButtons = new List<FactoryUnitButtonUI>();

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

        if (buildOnEnable)
            BuildList();
    }

    public void RefreshReferences()
    {
        if (factoryManager == null)
            factoryManager = FindFirstObjectByTypeCompat<FactoryManager>();
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

        // Requested formula:
        // buttonParent height = (button height + spacing) x number of buttons
        float height = (buttonHeight + buttonSpacing) * buttonCount;

        parentRect.sizeDelta = new Vector2(parentRect.sizeDelta.x, height);
    }

    public void SelectUnit(int unitId)
    {
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

    private T FindFirstObjectByTypeCompat<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
