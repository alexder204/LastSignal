using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventPopupUI : MonoBehaviour
{
    [Header("Popup Root")]
    public GameObject popupRoot;

    [Header("UI")]
    public Image cardImage;
    public TMP_Text titleText;
    public TMP_Text descText;

    [Header("Timing")]
    public float showSeconds = 2f;

    Coroutine currentRoutine;

    void Awake()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void Show(CardData card)
    {
        if (card == null || popupRoot == null) return;

        if (cardImage != null)
        {
            cardImage.sprite = card.cardArt;
            cardImage.enabled = card.cardArt != null;
        }

        if (titleText != null) titleText.text = card.cardName;
        if (descText != null)  descText.text  = card.description;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        popupRoot.SetActive(true);
        yield return new WaitForSeconds(showSeconds);
        popupRoot.SetActive(false);
        currentRoutine = null;
    }
}
