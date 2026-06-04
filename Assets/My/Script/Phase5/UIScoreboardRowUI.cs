using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIScoreboardRowUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] public Image badge;


    [Header("Current Player Highlight")]
    [SerializeField] private GameObject currentPlayerMarker;

    public void Setup(ZoidsGameJoltScoreboardRow row)
    {
        if (row == null)
            return;

        if (rankText != null)
            rankText.text =  row.rank.ToString();

        if (playerNameText != null)
            playerNameText.text = string.IsNullOrEmpty(row.playerName) ? "Unknown" : row.playerName;

        if (scoreText != null)
            scoreText.text = string.IsNullOrEmpty(row.scoreText) ? row.value.ToString() : row.scoreText;

        if (timeText != null)
            timeText.text = row.time ?? "";

        if (currentPlayerMarker != null)
            currentPlayerMarker.SetActive(row.isCurrentPlayer);

        int t = row.rank;
        switch (t) 
        {
            case 1:
                badge.color = new Color(1f, 0.7815f, 0.43f, 1f);
                break;
            case 2:
                badge.color = new Color(1f, 1f, 1f, 1f);
                break;
            case 3: 
                badge.color = new Color(0.56f, 0.34f, 0.25f, 1f);
                break;
            default:
                badge.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                break;
        }
    }
}
