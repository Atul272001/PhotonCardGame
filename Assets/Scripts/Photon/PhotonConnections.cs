using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PhotonConnections : MonoBehaviourPunCallbacks
{
    public static PhotonConnections Instance { get; private set; }

    [Header("UI")]
    public InputField playerNameInput;
    public Button startBtn;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (startBtn != null) startBtn.interactable = false;

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            if (startBtn != null) startBtn.interactable = true;
        }
    }

    public void StartMatchmaking()
    {
        if (startBtn != null) startBtn.interactable = false;
        PhotonNetwork.NickName = (playerNameInput != null && playerNameInput.text != "") ? playerNameInput.text : "Player_" + Random.Range(1000, 9999);
        PhotonNetwork.JoinLobby();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        if (startBtn != null) startBtn.interactable = true;
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        PhotonNetwork.JoinRoom("Game");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("JoinRoom failed (" + returnCode + "): " + message + " -> Creating 'Game'");
        PhotonNetwork.JoinOrCreateRoom("Game", new RoomOptions { MaxPlayers = 2 }, TypedLobby.Default);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room created");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom");

        if (GameManager.instance != null)
        {
            GameManager.instance.playerUserName = PhotonNetwork.LocalPlayer.NickName;
            GameManager.instance.isPlayerOnline = true;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Game");
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("OnPlayerEnteredRoom");

        GameManager.instance.opponentUserName = newPlayer.NickName;
        //GameManager.instance.opponentConnectionStatus = "Online";
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Game");
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("OnPlayerLeftRoom");
        //GameManager.instance.opponentConnectionStatus = "Disconnected";
    }

    public override void OnLeftRoom()
    {
        Debug.Log("OnLeftRoom");
        if (GameManager.instance != null) GameManager.instance.isPlayerOnline = false;
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("OnDisconnected: " + cause);
        StartCoroutine(TryReconnect());
    }

    IEnumerator TryReconnect()
    {
        Debug.Log("Attempting reconnect...");
        int attempts = 0;
        if (GameManager.instance != null) GameManager.instance.playerConnectionStatus = "Reconnecting";
        while (attempts < 20)
        {
            if (PhotonNetwork.IsConnectedAndReady)
            {
                Debug.Log("Reconnected successfully");
                GameManager.instance.playerConnectionStatus = "Connected";
                yield break;
            }

            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
            }

            attempts++;
            yield return new WaitForSeconds(1f);
        }

        Debug.LogError("Reconnect failed");
        SceneManager.LoadScene("MainMenu");
    }
}
