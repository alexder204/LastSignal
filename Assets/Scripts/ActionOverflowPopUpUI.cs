using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionOverflowPopupUI : MonoBehaviour
{
    [Header("Popup Root")]
    public GameObject popupRoot;

    [Header("UI")]
    public Image cardImage;
    public TMP_Text titleText;
    public TMP_Text descText;
    public Button trashButton;

    private CardData currentCard;
    private System.Action onTrash;

    void Awake()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);

        if (trashButton != null)
        {
            trashButton.onClick.RemoveAllListeners();
            trashButton.onClick.AddListener(OnTrashClicked);
        }
    }

    public void Show(CardData card, System.Action trashCallback)
    {
        if (card == null || popupRoot == null) return;

        currentCard = card;
        onTrash = trashCallback;

        if (cardImage != null)
        {
            cardImage.sprite = card.cardArt;
            cardImage.enabled = card.cardArt != null;
        }

        if (titleText != null) titleText.text = card.cardName;
        if (descText != null)  descText.text  = card.description;

        popupRoot.SetActive(true);
    }

    public void Hide()
    {
        currentCard = null;
        onTrash = null;

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void OnTrashClicked()
    {
        onTrash?.Invoke();
        Hide();
    }
}
