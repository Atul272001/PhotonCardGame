using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonSendAndReciveData : MonoBehaviour, IOnEventCallback
{
    public static PhotonSendAndReciveData instance;

    public enum GameEvents : byte
    {
        RevealCard = 1,
        TurnChanged = 2,
        EndTurn = 3,
        GameStart = 5,
        GameReady = 7
    }

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

    private void OnEnable() => PhotonNetwork.AddCallbackTarget(this);
    private void OnDisable() => PhotonNetwork.RemoveCallbackTarget(this);

    public void SendData(GameEvents eventCode, object payload)
    {
        if (payload == null)
        {
            Debug.LogWarning("SendData null payload event " + eventCode);
            return;
        }

        string json = JsonUtility.ToJson(payload);

        PhotonNetwork.RaiseEvent(
            (byte)eventCode,
            json,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable
        );

    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent == null) return;


        if (photonEvent.CustomData == null)
        {
            Debug.LogWarning("photonEvent CustomData is null " + photonEvent.Code);
            return;
        }

        string json = (string)photonEvent.CustomData;

        try
        {
            switch ((GameEvents)photonEvent.Code)
            {
                case GameEvents.RevealCard:
                    OpponentPlayedCard opp = JsonUtility.FromJson<OpponentPlayedCard>(json);
                    Debug.Log("Fetched RevealCard");
                    GameManager.instance?.OnRevealCardChanged(opp);
                    break;

                case GameEvents.EndTurn:
                    EndTurn et = JsonUtility.FromJson<EndTurn>(json);
                    Debug.Log("Fetched EndTurn");
                    GameManager.instance?.OnEndTurnChanged(et);
                    break;

                case GameEvents.GameStart:
                    GameStart gs = JsonUtility.FromJson<GameStart>(json);
                    Debug.Log("Fetched GameStart");
                    GameManager.instance?.OnGameStartChanged(gs);
                    break;

                case GameEvents.TurnChanged:
                    TurnChanged tc = JsonUtility.FromJson<TurnChanged>(json);
                    Debug.Log("Fetched TurnChanged");
                    GameManager.instance?.OnTurnChangedChanged(tc);
                    break;

                case GameEvents.GameReady:
                    GameReady gr = JsonUtility.FromJson<GameReady>(json);
                    Debug.Log("Fetched GameReady");
                    GameManager.instance?.OnPlayerReady(gr);
                    break;

                default:
                    Debug.LogWarning("Unknown event code: " + photonEvent.Code);
                    break;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception try failed: " + ex);
        }
    }

    [System.Serializable]
    public class OpponentPlayedCard
    {
        public string action;
        public string opponentName;
        public int cardId;
    }

    [System.Serializable]
    public class EndTurn
    {
        public string action;
        public string opponentId;
        public int oppRevealCardInCurrnentRound;
    }

    [System.Serializable]
    public class GameStart
    {
        public string action;
        public string opponentId;
        public int totalTurns;
        public string startingPlayerUserId;
    }

    [System.Serializable]
    public class TurnChanged
    {
        public string action;
        public int turnChnaged;
    }

    [System.Serializable]
    public class OppRevealCardInCurrnentRound
    {
        public string action;
        public int count;
    }

    [System.Serializable]
    public class GameReady
    {
        public string action;
        public string userId;
    }
}
