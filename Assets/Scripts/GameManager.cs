using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public Transform[] bots;
    public Transform zone;
    private Camera cam;
    private CameraFollow cameraFollow;
    private List<Transform> sortedList = new List<Transform>();
    public List<Transform> playerInZone = new List<Transform>();
    public Color[] playerColors;
    public Transform respawnT;
    public Player[] players;
    public bool gameStarted;
    private int respawnTries;

    [Space]

    [Header("UI Reference")]
    public float fTimer;
    public TextMeshProUGUI timerLabel;
    public NameManager nameManager;
    public TMP_InputField nameField;
    public Transform loadingImage;
    public TextMeshProUGUI playerCountLabel;
    public TextMeshProUGUI hintLabel;
    public GameObject menuPanel;
    public GameObject matchMakingPanel;
    public GameObject gamePanel;
    public Vector3 inGameCameraPos;
    public Vector3 matchmakingCameraPos;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverPosLabel;
    public GameObject respawnPanel;
    public TextMeshProUGUI respawnTimerLabel;
    
    public string[] hints;
    private string userName;

    [Space]

    [Header("Leaderboard Reference")]
    public Transform[] gameoverScoreCard;

    private void Start()
    {
        loadingImage.DORotate(new Vector3(0, 0, -1), .005f).SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);

        if (PlayerPrefs.HasKey("username"))
        {
            userName = PlayerPrefs.GetString("username");
        }
        else
        {
            userName = "Player";
        }

        cam = Camera.main;
        cameraFollow = cam.GetComponent<CameraFollow>();
        nameField.text = userName;
        hintLabel.text = hints[Random.Range(0, hints.Length)];

        SetupPlayers();
    }

    private void Update()
    {
        foreach (Player p in players)
        {
            if (playerInZone.Contains(p.transform))
            {
                if (!p.isInZone)
                {
                    p.isInZone = true;
                }
            }
            else
            {
                if (p.isInZone)
                {
                    p.isInZone = false;
                }
            }
        }

        if (gameStarted)
        {
            if (fTimer > 0)
            {
                if (fTimer < 10 && timerLabel.color != Color.red)
                {
                    timerLabel.color = Color.red;
                }

                fTimer -= Time.deltaTime;
                timerLabel.text = (int)fTimer / 60 + ":" + ((int)fTimer % 60).ToString("00");

                UpdateScores();
            }
            else
            {
                StartCoroutine(GameOver());
            }
        }
    }

    public IEnumerator Respawn(Transform t, float delay)
    {
        if (!t.GetComponent<Player>().isAi)
        {
            respawnPanel.SetActive(true);
            gamePanel.SetActive(false);

            yield return new WaitForSeconds(delay / 3);
            respawnTimerLabel.text = "2";

            yield return new WaitForSeconds(delay / 3);
            respawnTimerLabel.text = "1";
        }
        else
        {
            yield return new WaitForSeconds(delay);
        }

        if (!t.GetComponent<Player>().isAi)
        {
            respawnPanel.SetActive(false);
            gamePanel.SetActive(true);
        }

        if (gameStarted)
        {
            Spawn(t);
        }
    }

    private void Spawn(Transform t)
    {
        respawnT.eulerAngles = new Vector3(0, Random.Range(0, 359), 0);
        Collider[] cols = Physics.OverlapSphere(respawnT.GetChild(0).position, 5);

        bool playerNearby = false;

        foreach (Collider col in cols)
        {
            if (col.CompareTag("Player"))
            {
                playerNearby = true;
            }
        }

        if (!playerNearby)
        {
            t.position = respawnT.GetChild(0).position;
            t.gameObject.SetActive(true);
            respawnTries = 0;
        }
        else
        {
            if (respawnTries < 10)
            {
                respawnTries++;
                Spawn(t);
            }
            else
            {
                t.position = respawnT.GetChild(0).position;
                t.gameObject.SetActive(true);
                respawnTries = 0;
            }
        }
    }

    private void SetupPlayers()
    {
        int[] index = { 1, 2, 3, 4, 5 };
        System.Random rnd = new System.Random();
        int[] randomIndex = index.OrderBy(x => rnd.Next()).ToArray();

        for (int i = 0; i < players.Length; i++)
        {
            string name = "";

            if (i == 0)
            {
                name = userName;
            }
            else
            {
                name = nameManager.names[Random.Range(0, nameManager.name.Length)];
            }

            players[i].scoreCard.GetComponent<Image>().color = playerColors[i];
            players[i].scoreCard.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (i + 1) + ".";
            players[i].scoreCard.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = name;
            players[i].scoreCard.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = ". 0";

            Material playerMat = players[i].transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().material;
            playerMat.color = playerColors[i];

            players[i].playerName.text = name;
            players[i].playerName.color = playerColors[i];
        }
    }

    private void UpdateScores()
    {
        sortedList.Clear();

        Dictionary<Transform, int> unsortedDic = new Dictionary<Transform, int>();

        foreach (Player p in players)
        {
            unsortedDic.Add(p.transform, p.iScore);
        }

        foreach (KeyValuePair<Transform, int> item in unsortedDic.OrderByDescending(i => i.Value))
        {
            sortedList.Add(item.Key);
        }

        for (int i = 0; i < players.Length; i++)
        {
            Player tempPlayerScript = sortedList[i].GetComponent<Player>();

            if (i == 0)
            {
                tempPlayerScript.crown.SetActive(true);
            }
            else
            {
                tempPlayerScript.crown.SetActive(false);
            }

            tempPlayerScript.scoreCard.SetSiblingIndex(i);
            tempPlayerScript.scoreCard.GetChild(0).GetComponent<TextMeshProUGUI>().text = (i + 1) + ".";
            tempPlayerScript.scoreCard.GetChild(2).GetComponent<TextMeshProUGUI>().text = "- " + tempPlayerScript.iScore.ToString();
        }
    }

    private IEnumerator GameOver()
    {
        gameStarted = false;

        for (int i = 0; i < sortedList.Count; i++)
        {
            if (i == 0)
            {
                cameraFollow.FocusOnWinner(sortedList[i]);
                sortedList[i].GetComponent<Player>().Won(true);
            }
            else
            {
                sortedList[i].GetComponent<Player>().Won(false);
            }
        }

        for (int i = 0; i < players.Length; i++)
        {
            Player tempPlayer = sortedList[i].GetComponent<Player>();
            gameoverScoreCard[i].GetChild(0).GetComponent<TextMeshProUGUI>().text = tempPlayer.scoreCard.GetSiblingIndex() + 1 + ".";
            gameoverScoreCard[i].GetChild(1).GetComponent<TextMeshProUGUI>().text = tempPlayer.playerName.text;
            gameoverScoreCard[i].GetChild(2).GetComponent<TextMeshProUGUI>().text = "- " + tempPlayer.iScore.ToString();
            gameoverScoreCard[i].GetComponent<Image>().color = tempPlayer.scoreCard.GetComponent<Image>().color;

            if (!tempPlayer.isAi)
            {
                gameOverPosLabel.text = "You Finised " + i + 1 + GetRankOrdinal(i + 1);
            }
        }


        yield return new WaitForSeconds(2f);

        gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SaveUserName()
    {
        userName = nameField.text;
        PlayerPrefs.SetString("username", userName);
        players[0].playerName.text = userName;
        players[0].scoreCard.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = userName;
    }

    private IEnumerator DoMatchMaking()
    {
        cameraFollow.enabled = false;
        cam.transform.DOMove(matchmakingCameraPos, .5f);

        playerCountLabel.text = "1/5";
        yield return new WaitForSeconds(Random.Range(.25f, .7f));

        playerCountLabel.text = "2/5";
        bots[0].gameObject.SetActive(true);
        yield return new WaitForSeconds(Random.Range(.25f, .7f));

        playerCountLabel.text = "3/5";
        bots[1].gameObject.SetActive(true);
        yield return new WaitForSeconds(Random.Range(.25f, .7f));

        playerCountLabel.text = "4/5";
        bots[2].gameObject.SetActive(true);
        yield return new WaitForSeconds(Random.Range(.25f, .7f));

        playerCountLabel.text = "Starting game...";
        bots[3].gameObject.SetActive(true);
        yield return new WaitForSeconds(Random.Range(.25f, .7f));

        zone.gameObject.SetActive(true);
        bots[4].gameObject.SetActive(true); // bomber

        matchMakingPanel.SetActive(false);
        gamePanel.SetActive(true);

        cam.transform.DOMove(inGameCameraPos, .5f).OnComplete(() =>
        {
            cameraFollow.enabled = true;
        });
    }

    public void SearchMatch()
    {
        menuPanel.SetActive(false);
        matchMakingPanel.SetActive(true);
        StartCoroutine(DoMatchMaking());
    }  

    public void StartGame()
    {
        gameStarted = true;
    }

    private string GetRankOrdinal(int rank)
    {
        string ordinal = "";

        switch (rank)
        {
            case 1:
                ordinal = "st";
                break;

            case 2:
                ordinal = "nd";
                break;

            case 3:
                ordinal = "rd";
                break;

            case 4:
                ordinal = "th";
                break;

            case 5:
                ordinal = "th";
                break;
        }

        return ordinal;
    }
}






