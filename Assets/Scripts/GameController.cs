using UnityEngine;
using UnityEngine.UI; // 為了操作 Image
using TMPro;
using System; // TextMeshPro 的命名空間
using System.Collections;
using UnityEngine.SceneManagement;


public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public Animator winAnimator;
    public Animator youwinAnimator;
    public Animator youLoseAnimator;
    public Animator winloseAnimator;
    public Animator losewinAnimator;

    #region UI
    // 燈和基座的 Image 引用
    public Image lightOneP;
    public Image lightOneO;
    public Image lightTwoP;
    public Image lightTwoO;
    public Image lightThreeP;
    public Image lightThreeO;
    public Image lightBaseOneP;
    public Image lightBaseOneO;
    public Image lightBaseTwoP;
    public Image lightBaseTwoO;
    public Image lightBaseThreeP;
    public Image lightBaseThreeO;

    public Image WinningBackground;

    public TMP_Text scoreText; // 使用 TextMeshPro

    public AudioClip zero_fifteen;
    public AudioClip zero_thirty;
    public AudioClip zero_forty;
    public AudioClip fifteen_zero;
    public AudioClip fifteen_fifteen;
    public AudioClip fifteen_thirty;
    public AudioClip fifteen_forty;
    public AudioClip thirty_zero;
    public AudioClip thirty_fifteen;
    public AudioClip thirty_thirty;
    public AudioClip thirty_forty;
    public AudioClip fourty_zero;
    public AudioClip fourty_thirty;
    public AudioClip Deuce;

    public AudioClip gamePoint;
    public AudioClip matchPoint;
    public AudioClip breakPoint;

    public AudioClip gameWin;
    public AudioClip matchWin;
    public AudioClip matchLose;
    public AudioSource audioSource;
    public Button restartButton;
    public Button quitButton;




    // 比賽局數及玩家分數
    private int playerScoreIndex = 0; // 玩家得分索引
    private int opponentScoreIndex = 0; // 對手得分索引
    private string[] scoreSteps = { "0", "15", "30", "40", "Win" };

    private int playerDeuce = 0;
    private int opponentDeuce = 0;
    private int isDeuce = 0;

    // 燈號狀態
    private int playerWins = 0;
    private int opponentWins = 0;
    private int matchRounds = 0;
    private int maxWins;
    #endregion
    public enum GameState{
        NotStarted,
        Going,
        Interval,
        Over
    }
    [NonSerialized] public int servePlayer=1;    
    public enum PlayerTurn{
        P1,
        P2
    }
    [NonSerialized] public GameState game=GameState.NotStarted;
    [NonSerialized] public PlayerTurn turn = PlayerTurn.P1;
    [NonSerialized] public  int bounceTime=0;

    [NonSerialized] public bool gameOver = false;

    void Start()
        {
            // 獲取比賽局數
            matchRounds = UIController.matchRounds;
            if(matchRounds == 1) maxWins = 1;
            else if(matchRounds == 3) maxWins = 2;
            else maxWins = 3;

            // 初始化燈和基座的狀態
            SetLightsAndBases(matchRounds);
            restartButton.gameObject.SetActive(false);
            quitButton.gameObject.SetActive(false);

            // 初始化分數
            UpdateScoreText();
        }
    private void switchServer(){
        //if single, might need adjustment
        if(servePlayer == 1) {
            servePlayer=2;
        }
        else servePlayer=1;

    }
    public void AddPlayerScore()
    {
        if (gameOver) return;

        // 玩家得分邏輯
        if (playerScoreIndex == 3 && opponentScoreIndex == 3)
        {
            // Deuce 情況
            playerDeuce++;
            if(playerDeuce == 2){
                playerWins++;
                switchServer();
                ResetRound();
                UpdateLightsAndCheckGameOver();
            }else{opponentDeuce = 0;}
        }
        else if (playerScoreIndex == 4 || (playerScoreIndex == 3 && opponentScoreIndex < 3))
        {
            // 玩家獲勝一局
            playerWins++;
            switchServer();
            ResetRound();
            UpdateLightsAndCheckGameOver();
        }
        else
        {
            // 普通得分
            playerScoreIndex++;
            if(playerScoreIndex == 3 && opponentScoreIndex == 3){
                isDeuce = 1;
            }
        }

        UpdateScoreText();
        scoreSound();
    }

    public void AddOpponentScore()
    {
        if (gameOver) return;

        // 玩家得分邏輯
        if (playerScoreIndex == 3 && opponentScoreIndex == 3)
        {
            // Deuce 情況
            isDeuce = 1;
            opponentDeuce++;
            if(opponentDeuce == 2){
                opponentWins++;
                switchServer();
                ResetRound();
                UpdateLightsAndCheckGameOver();
            }else{playerDeuce = 0;}
            scoreSound();
        }
        else if (opponentScoreIndex == 4 || (opponentScoreIndex == 3 && playerScoreIndex < 3))
        {
            // 玩家獲勝一局
            opponentWins++;
            switchServer();
            ResetRound();
            UpdateLightsAndCheckGameOver();
            
        }
        else
        {
            // 普通得分
            opponentScoreIndex++;
            if(playerScoreIndex == 3 && opponentScoreIndex == 3){
                isDeuce = 1;
            }
            scoreSound();
        }

        UpdateScoreText();
    }

    private void ResetRound()
    {
        playerScoreIndex = 0;
        opponentScoreIndex = 0;
        isDeuce = 0;
        scoreText.fontSize = 50; 
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if(isDeuce == 0){
            string playerScore = (playerScoreIndex < 4) ? scoreSteps[playerScoreIndex] : "Adv";
            string opponentScore = (opponentScoreIndex < 4) ? scoreSteps[opponentScoreIndex] : "Adv";
            scoreText.text = $"{playerScore} - {opponentScore}";
        }else{
            scoreText.fontSize = 36; 
            if(playerDeuce == 0 && opponentDeuce == 0){
                scoreText.text = "DEUCE";
            }else if(playerDeuce > 0){
                scoreText.text = "ADVANTAGE PLAYER";
            }else{
                scoreText.text = "ADVANTAGE OPPONENT";
            }
        }
        
    }

    void SetLightsAndBases(int rounds)
    {
        // 根據局數設定燈和基座的啟用狀態
        lightOneP.enabled = (rounds >= 1);
        lightBaseOneP.enabled = (rounds >= 1);
        lightOneO.enabled = (rounds >= 1);
        lightBaseOneO.enabled = (rounds >= 1);

        lightTwoP.enabled = (rounds >= 3);
        lightBaseTwoP.enabled = (rounds >= 3);
        lightTwoO.enabled = (rounds >= 3);
        lightBaseTwoO.enabled = (rounds >= 3);

        lightThreeP.enabled = (rounds >= 5);
        lightBaseThreeP.enabled = (rounds >= 5);
        lightThreeO.enabled = (rounds >= 5);
        lightBaseThreeO.enabled = (rounds >= 5);
    }

    private void UpdateLightsAndCheckGameOver()
    {
        if (playerWins == 1)
        {
            lightOneP.color = Color.green; // 顯示亮燈
        }
        else if (playerWins == 2)
        {
            lightTwoP.color = Color.green;
        }
        else if (playerWins == 3)
        {
            lightThreeP.color = Color.green;
        }

        if (opponentWins == 1)
        {
            lightOneO.color = Color.green; // 顯示亮燈
        }
        else if (opponentWins == 2)
        {
            lightTwoO.color = Color.green;
        }
        else if (opponentWins == 3)
        {
            lightThreeO.color = Color.green;
        }
        if(!CheckGameOver()){audioSource.PlayOneShot(gameWin);}
    }
    private void scoreSound()
    {
        StartCoroutine(PlayScoreSequence());
    }

    private IEnumerator PlayScoreSequence()
    {
        AudioClip clipToPlay = null;

        if (playerScoreIndex == 0)
        {
            if (opponentScoreIndex == 1) clipToPlay = zero_fifteen;
            else if (opponentScoreIndex == 2) clipToPlay = zero_thirty;
            else if (opponentScoreIndex == 3) clipToPlay = zero_forty;
        }
        else if (playerScoreIndex == 1)
        {
            if(opponentScoreIndex == 0) clipToPlay = fifteen_zero;
            else if (opponentScoreIndex == 1) clipToPlay = fifteen_fifteen;
            else if (opponentScoreIndex == 2) clipToPlay = fifteen_thirty;
            else if (opponentScoreIndex == 3) clipToPlay = fifteen_forty;
        }
        else if (playerScoreIndex == 2)
        {
            if(opponentScoreIndex == 0) clipToPlay = thirty_zero;
            else if (opponentScoreIndex == 1) clipToPlay = thirty_fifteen;
            else if (opponentScoreIndex == 2) clipToPlay = thirty_thirty;
            else if (opponentScoreIndex == 3) clipToPlay = thirty_forty;
        }
        else if (playerScoreIndex == 3)
        {
            if(opponentScoreIndex == 0) clipToPlay = fourty_zero;
            else if (opponentScoreIndex == 2) clipToPlay = fourty_thirty;
            else if (opponentScoreIndex == 3) clipToPlay = Deuce;
        }
        if((playerScoreIndex == 3 || opponentScoreIndex == 3) && isDeuce == 0){
            yield return PlayAndWait(clipToPlay);
            if(playerWins == maxWins - 1 || opponentWins == maxWins - 1) clipToPlay = matchPoint; 
            else{ clipToPlay = gamePoint; }
        }else if(isDeuce == 1 && (opponentDeuce == 1 && playerDeuce == 1)){
            yield return PlayAndWait(clipToPlay);
            clipToPlay = breakPoint;
        }
        if (clipToPlay != null)
        {
            yield return PlayAndWait(clipToPlay);
        }
    }

    private IEnumerator PlayAndWait(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
        yield return new WaitForSeconds(clip.length);
    }


    public bool CheckGameOver()
    {

        if (playerWins >= maxWins)
        {
            Debug.Log("Game Over player win");
            winAnimator.SetTrigger("replay");
            if(UIController.isSingle){
                youwinAnimator.SetTrigger("replay");
            }else{
                winloseAnimator.SetTrigger("replay");
            }
            gameOver = true;
            audioSource.PlayOneShot(matchWin);
            return true;
        }else if (opponentWins >= maxWins)
        {
            Debug.Log("Game Over opponent win");
            winAnimator.SetTrigger("replay");
            if(UIController.isSingle){
                youLoseAnimator.SetTrigger("replay");
            }else{
                losewinAnimator.SetTrigger("replay");
            }
            gameOver = true;
            if(UIController.isSingle){
                audioSource.PlayOneShot(matchLose);
            }else{
                audioSource.PlayOneShot(matchWin);
            }
            
            return true;
        }
        return false;
    }
    public void showButton(){
        restartButton.gameObject.SetActive(true);
        quitButton.gameObject.SetActive(true);
    }

    public void GameRestart()
    {
        // Clear the instance before loading new scene
        Instance = null;
        
        // Ensure all game objects are cleaned up
        foreach (GameObject obj in GameObject.FindObjectsOfType<GameObject>())
        {
            Destroy(obj);
        }
        
        // Load the UI scene
        SceneManager.LoadScene("UI");
    }
    // 退出遊戲
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 在編輯器中停止遊戲
        #else
        Application.Quit(); // 在打包後退出遊戲
        #endif
    }
}
