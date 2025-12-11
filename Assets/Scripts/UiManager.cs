using DG.Tweening.Core.Easing;
using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    [Header("UI")]
    public TMPro.TextMeshProUGUI turnsLeft;
    public TMPro.TextMeshProUGUI countDownText;

    [Header("Opponent UI")]
    public TMPro.TextMeshProUGUI opponentUserName;
    public Text opponentOnlineStatus;
    public Text opponentScore;

    [Header("Player UI")]
    public TMPro.TextMeshProUGUI playerUserName;
    public Text playerScore;
    public GameObject[] playerTotalCardPower;
    public Text playerTotalCardPowerNum;

    [Header("Transforms")]
    public Transform playerHandCardsPos;
    public Transform PlayeReveledCardsPos;
    public Transform OpponentReveledCardsPos;

    [Header("Result")]
    public GameObject gameResultPanel;
    public Text gameResult;
    public Text playerScoreResult;
    public Text opponentScoreResult;

    [Header("Buttons")]
    public Button playCardBtn;
    public Button endTurnBtn;

    // Timer
    private Coroutine timerCoroutine;
    private int turnTimerLength = 30;

    // Round play state local
    private int cumulativePowerThisRound = 0;
    private bool overplayUsed = false;    // single overplay allowed
    private bool firstPlayThisRound = true;
    private bool hasPlayedCardThisRound = false;

    // prevent re-entrance to unfolding
    private bool isUnfolding = false;

    private void Awake()
    {
        Debug.Log("active scene: " + SceneManager.GetActiveScene().name);
    }

    private void Start()
    {
        playerUserName.text = GameManager.instance.playerUserName;

        if (GameManager.instance != null)
        {
            GameManager.instance.uiManager = this;
            CardManager.instance.playerHandCardsPos = playerHandCardsPos;
            CardManager.instance.PlayeReveledCardsPos = PlayeReveledCardsPos;
            CardManager.instance.OpponentReveledCardsPos = OpponentReveledCardsPos;
        }

        CardManager.instance?.DrawRandomCard(1);
        CardManager.instance?.DrawRandomCard(1);

        SendGameReady();
        opponentUserName.text = "Player Two";
    }

    private void SendGameReady()
    {
        var payload = new PhotonSendAndReciveData.GameReady { action = "ready", userId = PhotonNetwork.LocalPlayer.UserId };
        PhotonSendAndReciveData.instance?.SendData(PhotonSendAndReciveData.GameEvents.GameReady, payload);
    }

    public void Init()
    {
        cumulativePowerThisRound = 0;
        overplayUsed = false;
        firstPlayThisRound = true;
        hasPlayedCardThisRound = false;

        GameManager.instance.tempPlayerCardGameObject.Clear();
        GameManager.instance.tempOpponentCardGameObject.Clear();
        GameManager.instance.isPlayerTurnEndded = false;
        GameManager.instance.isOpponnetTurnEndded = false;

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        RoundStart();
        StartTimer();
    }

    private void RoundStart()
    {
        //if (GameManager.instance != null) GameManager.instance.turnLeft++;
        if (GameManager.instance != null && GameManager.instance.turnLeft >= 6)
        {
            FinishGame();
            return;
        }

        playerTotalCardPowerNum.text = (GameManager.instance.turnLeft).ToString() + "/5";
        playerTotalCardPower[GameManager.instance.turnLeft - 1].SetActive(true);
        if (playCardBtn != null) playCardBtn.interactable = true;

        CardManager.instance?.DrawRandomCard();

        if (playerHandCardsPos != null)
            CardManager.instance.playerHandCardsData = playerHandCardsPos.GetComponentsInChildren<DisplayCardData>();

        if (turnsLeft != null) turnsLeft.text = "Turn " + (GameManager.instance != null ? GameManager.instance.turnLeft.ToString() : "0") + "/6";

        EnablePlayableCards();
    }

    private void EnablePlayableCards()
    {
        int round = GameManager.instance != null ? GameManager.instance.turnLeft : 1;
        if (CardManager.instance.playerHandCardsData == null) return;

        foreach (var d in CardManager.instance.playerHandCardsData)
        {
            if (d == null || d.cardBtn == null) continue;
            int cost = 0; int.TryParse(d.cardCost.text, out cost);
            bool allowed = cost <= round;
            d.cardBtn.interactable = allowed;
        }
    }

    public void StartTimer()
    {
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(TurnTimerRoutine());
    }

    private IEnumerator TurnTimerRoutine()
    {
        int timer = turnTimerLength;
        if (countDownText != null) countDownText.text = timer.ToString();

        while (timer > 0)
        {
            yield return new WaitForSeconds(1f);
            timer--;
            if (countDownText != null) countDownText.text = timer.ToString();
            Debug.Log("Timer tick: " + timer);

            if (GameManager.instance.isPlayerTurnEndded)
            {
                Debug.Log("Timer stopped early because player ended turn");
                yield break;
            }
        }

        Debug.Log("Timer finished so auto EndPlayerTurn()");
        if (!GameManager.instance.isPlayerTurnEndded)
            EndPlayerTurn();
    }

    public void PlayCardFromDeck()
    {
        Debug.Log("PlayCardFromDeck called");
        if (CardManager.instance.selectedCard == null)
        {
            Debug.LogWarning("No selected card");
            return;
        }

        var selected = CardManager.instance.selectedCard;
        int power = 0; int.TryParse(selected.cardPower.text, out power);
        int cost = 0; int.TryParse(selected.cardCost.text, out cost);
        int round = GameManager.instance != null ? GameManager.instance.turnLeft : 1;

        if (cost > round)
        {
            return;
        }

        if (cumulativePowerThisRound + power <= round)
        {
            Debug.Log("[UiManager] Play allowed - within cumulative limit");
            CommitPlay(selected);
            cumulativePowerThisRound += power;
            hasPlayedCardThisRound = true;
            firstPlayThisRound = false;
            playerTotalCardPowerNum.text = cumulativePowerThisRound.ToString() + "/6";
            if (power == round) DisableHand();
            return;
        }

        if (firstPlayThisRound && !hasPlayedCardThisRound)
        {
            Debug.Log("[UiManager] First-play-overpower allowed - committing play");
            CommitPlay(selected);
            cumulativePowerThisRound += power;
            hasPlayedCardThisRound = true;
            firstPlayThisRound = false;
            overplayUsed = true;
            playerTotalCardPowerNum.text = cumulativePowerThisRound.ToString() + "/6";
            DisableHand();
            return;
        }

        // allow single final overplay if not used and cumulative before card < round
        if (!overplayUsed && cumulativePowerThisRound < round)
        {
            CommitPlay(selected);
            cumulativePowerThisRound += power;
            hasPlayedCardThisRound = true;
            overplayUsed = true;
            DisableHand();
            return;
        }

    }

    private void DisableHand()
    {
        Debug.Log("no more plays allowed this round");
        if (CardManager.instance.playerHandCardsData == null) return;
        foreach (var d in CardManager.instance.playerHandCardsData)
        {
            if (d != null && d.cardBtn != null) d.cardBtn.interactable = false;
        }
    }

    private void CommitPlay(DisplayCardData selected)
    {
        if (selected == null) return;

        var payload = new PhotonSendAndReciveData.OpponentPlayedCard
        {
            action = "reveal",
            opponentName = PhotonNetwork.LocalPlayer.UserId,
            cardId = selected.cardId
        };
        PhotonSendAndReciveData.instance?.SendData(PhotonSendAndReciveData.GameEvents.RevealCard, payload);
        Debug.Log("Sent RevealCard for cardId=" + selected.cardId);

        if (CardManager.instance != null)
        {
            CardManager.instance.card = CardManager.instance.allCards.Find(c => c.id == selected.cardId);
            if (CardManager.instance.card != null)
            {
                CardManager.instance.playerReveledCards.Add(CardManager.instance.card);
            }

            selected.cardBtn.onClick.RemoveAllListeners();
            Destroy(selected.gameObject);
            if (playerHandCardsPos != null) CardManager.instance.playerHandCardsData = playerHandCardsPos.GetComponentsInChildren<DisplayCardData>();

            GameObject reveal = CardManager.instance.AddCardToGame(PlayeReveledCardsPos, CardManager.instance.revealCardPrefab);
            if (reveal != null) GameManager.instance.tempPlayerCardGameObject.Add(reveal);
        }

        CardManager.instance?.DeselectCard();
    }

    public void EndPlayerTurn()
    {
        if (playCardBtn != null) playCardBtn.interactable = false;
        Debug.Log("EndPlayerTurn called");
        if (GameManager.instance != null && GameManager.instance.isPlayerTurnEndded)
        {
            Debug.Log("Already ended this round so ignoring");
            return;
        }

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
            Debug.Log("[Timer coroutine stopped by EndPlayerTurn");
        }

        var payload = new PhotonSendAndReciveData.EndTurn
        {
            action = "endTurn",
            opponentId = PhotonNetwork.LocalPlayer.UserId,
            oppRevealCardInCurrnentRound = 0
        };

        PhotonSendAndReciveData.instance?.SendData(PhotonSendAndReciveData.GameEvents.EndTurn, payload);
        Debug.Log("[UiManager] Sent EndTurn event");

        GameManager.instance.isPlayerTurnEndded = true;

        DisableHand();

        foreach (var go in GameManager.instance.tempPlayerCardGameObject)
        {
            if (go == null) continue;
            var disp = go.GetComponent<DisplayCardData>();
            if (disp != null)
            {
                disp.cardBack.SetActive(true);
                disp.cardBtn.onClick.RemoveAllListeners();
            }
        }

        DelayedAttemptUnfold();
    }

    public void DelayedAttemptUnfold()
    {
        StartCoroutine(DelayedAttemptUnfoldCoroutine());
    }

    private IEnumerator DelayedAttemptUnfoldCoroutine()
    {
        yield return null;
        AttemptUnfold();
    }

    private void AttemptUnfold()
    {
        if (GameManager.instance.isPlayerTurnEndded && GameManager.instance.isOpponnetTurnEndded)
        {
            if (isUnfolding)
            {
                Debug.Log("already unfolding");
                return;
            }
            StartCoroutine(CardUnfoldRoutine());
        }
        else
        {
            Debug.Log("waiting for both players to end or timer expiry");
        }
    }

    private IEnumerator CardUnfoldRoutine()
    {
        isUnfolding = true;
        Debug.Log("CardUnfoldRoutine started");

        // ensure timer stopped
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
            Debug.Log("Timer stopped before unfolding");
        }

        List<GameObject> pCards = new List<GameObject>(GameManager.instance.tempPlayerCardGameObject);
        List<GameObject> oCards = new List<GameObject>(GameManager.instance.tempOpponentCardGameObject);

        int p = 0, o = 0;
        while (p < pCards.Count || o < oCards.Count)
        {
            if (p < pCards.Count)
            {
                Debug.Log("Revealing player card index " + p);
                yield return RevealCard(pCards[p], true);
                p++;
            }
            if (o < oCards.Count)
            {
                Debug.Log("Revealing opponent card index " + o);
                yield return RevealCard(oCards[o], false);
                o++;
            }
        }

        yield return new WaitForSeconds(0.5f);

        if (PhotonNetwork.IsMasterClient)
        {
            GameManager.instance.turnLeft++;
            string nextStarter = "";
            if (GameManager.instance.playerScore > GameManager.instance.opponentScore)
                nextStarter = PhotonNetwork.LocalPlayer.UserId;
            else if (GameManager.instance.opponentScore > GameManager.instance.playerScore)
            {
                foreach (var pinfo in PhotonNetwork.PlayerList)
                {
                    if (pinfo.UserId != PhotonNetwork.LocalPlayer.UserId) { nextStarter = pinfo.UserId; break; }
                }
            }
            else
            {
                nextStarter = PhotonNetwork.PlayerList[Random.Range(0, PhotonNetwork.PlayerList.Length)].UserId;
            }

            var gs = new PhotonSendAndReciveData.GameStart
            {
                action = "GameStart",
                totalTurns = GameManager.instance != null ? GameManager.instance.turnLeft : 0,
                opponentId = PhotonNetwork.LocalPlayer.UserId,
                startingPlayerUserId = nextStarter
            };
            PhotonSendAndReciveData.instance?.SendData(PhotonSendAndReciveData.GameEvents.GameStart, gs);
        }
        else
        {
            Debug.Log("Client finished reveal and waiting for master GameStart");
        }

        cumulativePowerThisRound = 0;
        overplayUsed = false;
        firstPlayThisRound = true;
        hasPlayedCardThisRound = false;
        isUnfolding = false;
    }

    private IEnumerator RevealCard(GameObject cardGO, bool isPlayer)
    {
        if (cardGO == null)
        {
            Debug.LogWarning("RevealCard null GO");
            yield break;
        }

        var disp = cardGO.GetComponent<DisplayCardData>();
        if (disp != null)
            disp.cardBack.SetActive(false);

        int power = 0;
        if (disp != null) int.TryParse(disp.cardPower.text, out power);

        if (isPlayer)
        {
            GameManager.instance.playerScore += power;
            UpdateScore(GameManager.instance.playerScore, playerScore);
        }
        else
        {
            GameManager.instance.opponentScore += power;
            UpdateScore(GameManager.instance.opponentScore, opponentScore);
        }

        yield return new WaitForSeconds(1f);
    }

    private void UpdateScore(int score, Text ui)
    {
        if (ui != null) ui.text = "Score: " + score;
    }

    public void OpenMainMenu()
    {
        Debug.Log("OpenMainMenu called");
        if (GameManager.instance != null)
        {
            GameManager.instance.turnLeft = 0;
            GameManager.instance.playerScore = 0;
            GameManager.instance.opponentScore = 0;
        }
        SceneManager.LoadScene("MainMenu");
    }

    public void FinishGame()
    {
        Debug.Log("FinishGame called");
        if (gameResultPanel != null) gameResultPanel.SetActive(true);
        if (GameManager.instance == null) return;
        playerScoreResult.text =  "Your Score : " + GameManager.instance.playerScore.ToString();
        opponentScoreResult.text = "PLayerTwo Score : " + GameManager.instance.opponentScore.ToString();
        if (GameManager.instance.playerScore > GameManager.instance.opponentScore)
        {
            gameResult.text = "You Win!";
        }
        else if (GameManager.instance.opponentScore > GameManager.instance.playerScore)
        {
            gameResult.text = "You Lose!";
        }
        else
        {
            gameResult.text = "It's a Draw!";
        }
    }
}
