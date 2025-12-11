using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CardManager : MonoBehaviour
{
    public static CardManager instance;

    public List<Card> allCards;

    public List<Card> playerReveledCards;
    public List<Card> opponentReveledCards;
    public List<Card> playerDeckCards;

    public GameObject playerCardPrefab;
    public Card card;
    public Transform playerHandCardsPos;
    public Transform PlayeReveledCardsPos;
    public Transform OpponentReveledCardsPos;
    public GameObject revealCardPrefab;

    public DisplayCardData[] playerHandCardsData;
    public DisplayCardData selectedCard;

    string JsonData;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public GameObject AddCardToGame(Transform cardPos, GameObject cardObject, bool activate = true)
    {
        GameObject cardGameObject = Instantiate(cardObject);
        cardGameObject.SetActive(false);
        cardGameObject.transform.SetParent(cardPos, false);
        DisplayCardData cardData = cardGameObject.GetComponent<DisplayCardData>();
        cardData.cardBtn.onClick.AddListener(() => SelectAndDeselectCard(cardData.cardBorder));
        cardData.cardBtn.GetComponent<Image>().color = GetColorFromHex(card.Color);
        cardData.cardName.text = card.name;
        cardData.cardCost.text = card.cost.ToString();
        cardData.cardPower.text = card.power.ToString();
        cardData.cardId = card.id;
        cardGameObject.SetActive(activate);
        return cardGameObject;
    }

    public void SelectAndDeselectCard(Image image)
    {
        playerHandCardsData = null;
        selectedCard = image.gameObject.GetComponentInParent<DisplayCardData>();
        playerHandCardsData = playerHandCardsPos.gameObject.GetComponentsInChildren<DisplayCardData>();
        image.gameObject.SetActive(!image.gameObject.activeSelf);
        if (image.gameObject.activeSelf)
        {
            for (int i = 0; i < playerHandCardsData.Length; i++)
            {
                if (playerHandCardsData[i].cardId != selectedCard.cardId)
                    playerHandCardsData[i].GetComponent<Button>().interactable = false;
            }
        }
        else
        {
            DeselectCard();
        }
        
    }

    public void DeselectCard()
    {
        for (int i = 0; i < playerHandCardsData.Length; i++)
        {
            playerHandCardsData[i].GetComponent<Button>().interactable = true;
        }
    }

    public Color GetColorFromHex(string hex)
    {
        Color color;
        if (UnityEngine.ColorUtility.TryParseHtmlString(hex, out color))
        {
            Debug.Log("Color Changed");
            return color;
        }
        Debug.Log("Invalid Hex Color");
        return Color.blue;
    }

    public void DrawRandomCard(int gameTurns = 6)
    {
        int randomCard;
        List<Card> cards = new List<Card>();
        if(gameTurns <= 3)
        {
            foreach(Card card in allCards)
            {
                if(card.cost <= gameTurns)
                    cards.Add(card);
            }

            randomCard = UnityEngine.Random.Range(0, cards.Count);
        }
        else
        {
            randomCard = UnityEngine.Random.Range(0, allCards.Count);
        }

        card = allCards[randomCard];
        AddCardToGame(playerHandCardsPos, playerCardPrefab);
    }

}
