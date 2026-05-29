using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ColosseumBattleSizeButtonUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image selectedHighlight;
    [SerializeField] private Image lockedOverlay;
    [SerializeField] private Button button;
    private MapSFX soundManager;
    public AudioClip buttonHoverFx;

    private ColosseumSetupUI owner;
    private int battleSize = 1;
    private bool unlocked = true;

    public int BattleSize { get { return battleSize; } }

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    public void Start()
    {
        soundManager = MapSFX.Instance;
    }

    public void Setup(ColosseumSetupUI owner, int battleSize, bool unlocked, bool selected)
    {
        this.owner = owner;
        this.battleSize = battleSize;
        this.unlocked = unlocked;

        if (label != null)
            label.text = battleSize + " VS " + battleSize + (unlocked ? "" : " LOCKED");

        if (button != null)
            button.interactable = unlocked;

        if (lockedOverlay != null)
            lockedOverlay.gameObject.SetActive(!unlocked);

        SetSelected(selected);
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
            selectedHighlight.gameObject.SetActive(selected);
    }

    private void OnClick()
    {
        if (!unlocked || owner == null)
            return;

        owner.SelectBattleSize(battleSize);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        soundManager.sfx.PlayOneShot(buttonHoverFx);
    }
}
