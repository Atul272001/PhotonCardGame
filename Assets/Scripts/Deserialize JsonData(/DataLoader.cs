using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.GPUSort;

public class DataLoader : MonoBehaviour
{
    private const string JsonFileName = "cards";

    private void Start()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>(JsonFileName);

        if (jsonAsset == null)
        {
            Debug.Log("json file not found");
            return;
        }

        DeserializeJsonData(jsonAsset.text, CardManager.instance.allCards);
    }

    public void DeserializeJsonData(string jsonData, List<Card> cards)
    {
        CardListWrapper cardList = JsonUtility.FromJson<CardListWrapper>(jsonData);

        if (cardList.cards != null)
        {
            foreach (Card card in cardList.cards)
            {
                cards.Add(card);
            }
            Debug.Log("Total cards count: " + cards.Count);
        }
    }

}
