using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColosseumTeamButtonUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image selectedHighlight;
    [SerializeField] private Button button;

    private ColosseumSetupUI owner;
    private int teamIndex = 0;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    public void Setup(ColosseumSetupUI owner, int teamIndex, string teamName, int unitCount, bool selected)
    {
        this.owner = owner;
        this.teamIndex = teamIndex;

        if (label != null)
            label.text = teamName + " - " + (unitCount > 0 ? unitCount + " Zoids" : "Empty");

        SetSelected(selected);

        if (button != null)
            button.interactable = unitCount > 0;
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
            selectedHighlight.gameObject.SetActive(selected);
    }

    private void OnClick()
    {
        if (owner == null)
            return;

        owner.SelectTeam(teamIndex);
    }
}
