using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIScoreboard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ZoidsGameJoltScoreboardManager scoreboardManager;

    [Header("Text UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text localPlayerText;
    [SerializeField] private TMP_Text listFallbackText;

    [Header("Ranking Rows")]
    [SerializeField] private Transform rowParent;
    [SerializeField] private UIScoreboardRowUI rowPrefab;
    [SerializeField] private bool clearRowsOnLoad = true;

    [Header("Row Parent Auto Resize")]
    [SerializeField] private bool autoResizeRowParent = true;
    [SerializeField] private float rowSpacing = 8f;
    [SerializeField] private float fallbackRowHeight = 40f;
    [SerializeField] private bool updateVerticalLayoutGroupSpacing = true;

    [Header("Buttons")]
    [SerializeField] private Button areaBattleButton;
    [SerializeField] private Button allVsButton;
    [SerializeField] private List<Button> vsButtons = new List<Button>();

    [Header("Options")]
    [SerializeField] private int rankingLimit = 10;
    [SerializeField] private bool autoLoadOnStart = false;
    [SerializeField] private ZoidsScoreboardCategory defaultCategory = ZoidsScoreboardCategory.AreaBattle;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private ZoidsScoreboardCategory currentCategory = ZoidsScoreboardCategory.AreaBattle;

    private void Awake()
    {
        RefreshReferences();
        HookButtons();
    }

    private void Start()
    {
        if (autoLoadOnStart)
            ShowCategory(defaultCategory);
    }

    private void RefreshReferences()
    {
        if (scoreboardManager == null && ZoidsGameJoltScoreboardManager.Instance != null)
            scoreboardManager = ZoidsGameJoltScoreboardManager.Instance;

        if (scoreboardManager == null)
            scoreboardManager = FindManager<ZoidsGameJoltScoreboardManager>();
    }

    private T FindManager<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>(true);
#endif
    }

    private void HookButtons()
    {
        if (areaBattleButton != null)
            areaBattleButton.onClick.AddListener(ShowAreaBattle);

        if (allVsButton != null)
            allVsButton.onClick.AddListener(ShowAllVS);

        if (vsButtons != null)
        {
            for (int i = 0; i < vsButtons.Count; i++)
            {
                int vsSize = i + 1;
                if (vsButtons[i] == null) continue;
                vsButtons[i].onClick.AddListener(() => ShowVS(vsSize));
            }
        }
    }

    public void ShowAreaBattle()
    {
        ShowCategory(ZoidsScoreboardCategory.AreaBattle);
    }

    public void ShowAllVS()
    {
        ShowCategory(ZoidsScoreboardCategory.AllVS);
    }

    public void ShowVS(int vsSize)
    {
        RefreshReferences();
        if (scoreboardManager == null)
        {
            SetStatus("Scoreboard manager missing.");
            return;
        }

        ShowCategory(scoreboardManager.GetVSCategory(vsSize));
    }

    public void Show1VS1(){ ShowVS(1); }
    public void Show2VS2(){ ShowVS(2); }
    public void Show3VS3(){ ShowVS(3); }
    public void Show4VS4(){ ShowVS(4); }
    public void Show5VS5(){ ShowVS(5); }
    public void Show6VS6(){ ShowVS(6); }
    public void Show7VS7(){ ShowVS(7); }
    public void Show8VS8(){ ShowVS(8); }
    public void Show9VS9(){ ShowVS(9); }
    public void Show10VS10(){ ShowVS(10); }

    public void RefreshCurrent()
    {
        ShowCategory(currentCategory);
    }

    public void ShowCategory(ZoidsScoreboardCategory category)
    {
        RefreshReferences();
        currentCategory = category;

        if (scoreboardManager == null)
        {
            SetStatus("Scoreboard manager missing.");
            return;
        }

        ClearRows();

        string title = scoreboardManager.GetCategoryName(category) + " Ranking";
        if (titleText != null)
            titleText.text = title;

        SetStatus("Loading " + title + "...");

        scoreboardManager.DownloadRanking(category, rankingLimit, OnRankingLoaded);
    }

    private void OnRankingLoaded(ZoidsGameJoltScoreboardResult result)
    {
        if (result == null)
        {
            SetStatus("Ranking failed.");
            return;
        }

        if (titleText != null)
            titleText.text = result.title + " Ranking";

        string localLine = "Your Wins: " + result.localWins;
        if (result.localRank > 0)
        {
            localLine += " | Your Rank: #" + result.localRank;
            localPlayerText.gameObject.SetActive(true);
        }
        else
        {
            localLine += " | Your Rank: -";
            localPlayerText.gameObject.SetActive(false);
        }
        if (localPlayerText != null)
            localPlayerText.text = localLine;

        if (!result.success)
        {
            SetStatus(result.message);
            SetFallbackText(result.message);
            ResizeRowParent();
            return;
        }

        if (result.rows == null || result.rows.Count == 0)
        {
            SetStatus("No ranking data yet.");
            SetFallbackText("No ranking data yet.");
            ResizeRowParent();
            return;
        }

        SetStatus("Loaded " + result.rows.Count + " ranking rows.");
        BuildRows(result.rows);
        BuildFallbackText(result);
    }

    private void BuildRows(List<ZoidsGameJoltScoreboardRow> rows)
    {
        if (rowParent == null || rowPrefab == null)
        {
            ResizeRowParent();
            return;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            UIScoreboardRowUI row = Instantiate(rowPrefab, rowParent);
            row.gameObject.SetActive(true);
            row.Setup(rows[i]);
        }

        ResizeRowParent();
    }

    private void BuildFallbackText(ZoidsGameJoltScoreboardResult result)
    {
        if (listFallbackText == null)
            return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(result.title + " Ranking");
        sb.AppendLine("Your Wins: " + result.localWins + (result.localRank > 0 ? " | Rank #" + result.localRank : ""));
        sb.AppendLine();

        for (int i = 0; i < result.rows.Count; i++)
        {
            ZoidsGameJoltScoreboardRow row = result.rows[i];
            sb.AppendLine("#" + row.rank + "  " + row.playerName + "  " + row.scoreText);
        }

        listFallbackText.text = sb.ToString();
    }

    private void SetFallbackText(string text)
    {
        if (listFallbackText != null)
            listFallbackText.text = text;
    }

    private void ClearRows()
    {
        if (!clearRowsOnLoad || rowParent == null)
            return;

        for (int i = rowParent.childCount - 1; i >= 0; i--)
        {
            Transform child = rowParent.GetChild(i);
            if (rowPrefab != null && child == rowPrefab.transform)
                continue;

            Destroy(child.gameObject);
        }

        if (listFallbackText != null)
            listFallbackText.text = "";

        ResizeRowParent();
    }

    private void ResizeRowParent()
    {
        if (!autoResizeRowParent || rowParent == null)
            return;

        RectTransform parentRect = rowParent as RectTransform;
        if (parentRect == null)
            parentRect = rowParent.GetComponent<RectTransform>();

        if (parentRect == null)
            return;

        float itemHeight = GetRowHeight();
        int activeChildCount = GetActiveRankingRowCount();

        float totalHeight = 0f;
        if (activeChildCount > 0)
            totalHeight = (activeChildCount * itemHeight) + ((activeChildCount - 1) * Mathf.Max(0f, rowSpacing));

        parentRect.sizeDelta = new Vector2(parentRect.sizeDelta.x, totalHeight);

        if (updateVerticalLayoutGroupSpacing)
        {
            VerticalLayoutGroup layoutGroup = rowParent.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
                layoutGroup.spacing = rowSpacing;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);

        if (debugLog)
            Debug.Log("[UIScoreboard] Row parent resized. Rows=" + activeChildCount + " Height=" + totalHeight);
    }

    private int GetActiveRankingRowCount()
    {
        if (rowParent == null)
            return 0;

        int count = 0;

        for (int i = 0; i < rowParent.childCount; i++)
        {
            Transform child = rowParent.GetChild(i);
            if (child == null)
                continue;

            if (rowPrefab != null && child == rowPrefab.transform)
                continue;

            if (!child.gameObject.activeSelf)
                continue;

            count++;
        }

        return count;
    }

    private float GetRowHeight()
    {
        RectTransform prefabRect = rowPrefab != null ? rowPrefab.GetComponent<RectTransform>() : null;
        if (prefabRect != null && prefabRect.rect.height > 0f)
            return prefabRect.rect.height;

        if (rowParent != null && rowParent.childCount > 0)
        {
            for (int i = 0; i < rowParent.childCount; i++)
            {
                RectTransform childRect = rowParent.GetChild(i) as RectTransform;
                if (childRect != null && childRect.rect.height > 0f)
                    return childRect.rect.height;
            }
        }

        return Mathf.Max(1f, fallbackRowHeight);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        if (debugLog)
            Debug.Log("[UIScoreboard] " + message);
    }
}
