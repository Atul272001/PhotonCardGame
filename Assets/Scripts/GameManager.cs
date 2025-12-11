using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEditor;

/// <summary>
/// Central game state and network-driven callbacks.
/// Receives parsed events from PhotonSendAndReciveData and drives UI through uiManager.
/// NOTE: OppRevealedCardInCurrnentRound removed entirely.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    public enum TurnState { PlayerTurn = 1, OpponentTurn = 2, RandomTurn = 3 }

    public string playerConnectionStatus;

    [Header("State")]
    public TurnState currentTurnState = TurnState.RandomTurn;
    public int turnLeft = 0; // increments per round
    public int playerScore = 0;
    public int opponentScore = 0;

    [Header("Players")]
    public string playerUserName;
    public string opponentUserName;
    public string opponentConnectionStatus;
    public bool isPlayerOnline = false;

    [Header("Round flags")]
    public bool isPlayerTurnEndded = false;
    public bool isOpponnetTurnEndded = false;

    [Header("Reveal lists (cleared each round)")]
    public List<GameObject> tempPlayerCardGameObject = new List<GameObject>();
    public List<GameObject> tempOpponentCardGameObject = new List<GameObject>();

    [Header("References")]
    public UiManager uiManager;

    // track ready users for initial handshake
    private HashSet<string> playersReady = new HashSet<string>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnRevealCardChanged(PhotonSendAndReciveData.OpponentPlayedCard payload)
    {
        if (payload.opponentName == PhotonNetwork.LocalPlayer.UserId)
        {
            Debug.Log("Ignore because dis iz my card.");
            return;
        }
        if (payload == null) return;

        Card cardModel = CardManager.instance.allCards.Find(c => c.id == payload.cardId);
        if (cardModel == null)
        {
            return;
        }

        CardManager.instance.card = cardModel;
        CardManager.instance.opponentReveledCards.Add(cardModel);

        GameObject revealGO = CardManager.instance.AddCardToGame(CardManager.instance.OpponentReveledCardsPos, CardManager.instance.revealCardPrefab);
        if (revealGO != null)
        {
            tempOpponentCardGameObject.Add(revealGO);
            var disp = revealGO.GetComponent<DisplayCardData>();
            if (disp != null)
            {
                // keep face-down until unfold
                disp.cardBack.SetActive(true);
                disp.cardBtn.onClick.RemoveAllListeners();
            }
        }
    }

    public void OnEndTurnChanged(PhotonSendAndReciveData.EndTurn payload)
    {
        if (payload == null)
        {
            Debug.LogWarning("OnEndTurnChanged called with null payload");
            return;
        }

        if (payload.opponentId != PhotonNetwork.LocalPlayer.UserId)
        {
            isOpponnetTurnEndded = true;

            uiManager?.DelayedAttemptUnfold();
        }
    }

    public void OnGameStartChanged(PhotonSendAndReciveData.GameStart payload)
    {
        if (payload == null)
        {
            Debug.LogWarning("OnGameStartChanged payload null");
            return;
        }

        opponentUserName = payload.opponentId;

        if (payload.totalTurns > 0) turnLeft = payload.totalTurns;
        else if (turnLeft <= 0) turnLeft = 1;

        if (!string.IsNullOrEmpty(payload.startingPlayerUserId))
        {
            bool amIStarter = PhotonNetwork.LocalPlayer.UserId == payload.startingPlayerUserId;
            currentTurnState = amIStarter ? TurnState.PlayerTurn : TurnState.OpponentTurn;
        }
        else
        {
            currentTurnState = (Random.value > 0.5f) ? TurnState.PlayerTurn : TurnState.OpponentTurn;
        }

        isPlayerTurnEndded = false;
        isOpponnetTurnEndded = false;
        tempPlayerCardGameObject.Clear();
        tempOpponentCardGameObject.Clear();

        if (uiManager != null)
        {
            uiManager.Init();
        }
        else
        {
            Debug.LogWarning("uiManager is nullt");
        }
    }

    public void OnTurnChangedChanged(PhotonSendAndReciveData.TurnChanged payload)
    {
        if (payload == null) return;
        currentTurnState = (TurnState)payload.turnChnaged;
        Debug.Log("OnTurnChangedChanged : " + currentTurnState);
    }

    public void OnPlayerReady(PhotonSendAndReciveData.GameReady payload)
    {
        if (payload == null) return;
        Debug.Log(" OnPlayerReady userId:" + payload.userId);
        playersReady.Add(payload.userId);

        if (PhotonNetwork.IsMasterClient && playersReady.Count >= 2)
        {
            Debug.Log("Both players ready - Master will send initial GameStart");

            var list = PhotonNetwork.PlayerList;
            var pick = list[Random.Range(0, list.Length)];
            string starterId = pick.UserId;

            var gs = new PhotonSendAndReciveData.GameStart
            {
                action = "GameStart",
                totalTurns = 1,
                opponentId = PhotonNetwork.LocalPlayer.UserId,
                startingPlayerUserId = starterId
            };
            PhotonSendAndReciveData.instance?.SendData(PhotonSendAndReciveData.GameEvents.GameStart, gs);
        }
    }

    public void MasterStartNextRound()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("MasterStartNextRound called on non-master");
            return;
        }

        string nextStarterId = null;
        if (playerScore > opponentScore)
        {
            nextStarterId = PhotonNetwork.LocalPlayer.UserId;
        }
        else if (opponentScore > playerScore)
        {
            foreach (var p in PhotonNetwork.PlayerList)
            {
                if (p.UserId != PhotonNetwork.LocalPlayer.UserId) { nextStarterId = p.UserId; break; }
            }
        }
        else
        {
            var list = PhotonNetwork.PlayerList;
            nextStarterId = list[Random.Range(0, list.Length)].UserId;
        }

        var gs = new PhotonSendAndReciveData.GameStart
        {
            action = "GameStart",
            totalTurns = turnLeft,
            opponentId = PhotonNetwork.LocalPlayer.UserId,
            startingPlayerUserId = nextStarterId
        };

        PhotonSendAndReciveData.instance?.SendData(PhotonSendAndReciveData.GameEvents.GameStart, gs);

        turnLeft += 1;
    }
}
