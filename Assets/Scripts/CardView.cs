using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text descText;
    public Button button;
    public Image artImage;

    private CardData card;
    private System.Action<CardData> onClicked;

    public void Bind(CardData data, System.Action<CardData> clickCallback)
    {
        card = data;
        onClicked = clickCallback;

        if (titleText) titleText.text = data != null ? data.cardName : "NULL";
        if (descText)  descText.text  = data != null ? data.description : "";

        if (button == null) button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked?.Invoke(card));
        }
        if (artImage != null)
        {
            artImage.sprite = data != null ? data.cardArt : null;
            artImage.enabled = artImage.sprite != null;
        }
    }
}
