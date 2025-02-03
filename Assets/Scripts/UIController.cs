using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // 用來操作 UI 元素

public class UIController : MonoBehaviour
{
    // 設置兩個 Panel，分別是 StartPanel 和 SetPanel
    public GameObject startPanel;
    public GameObject modePanel;
    public GameObject setPanel;
    public AudioSource audioSource;
    public AudioClip menuMusic;
    public AudioClip roundMusic;
    
    public Image SingleBackground;
    public Image DoubleBackground;
    
    // 紀錄選擇的比賽局數
    public static int matchRounds;
    public static bool isSingle;

    void Start()
    {
        // 檢查場景是否已經加載
        if (!SceneManager.GetSceneByName("UI").isLoaded)
        {
            SceneManager.LoadScene("UI", LoadSceneMode.Additive);
        }

        // 確保 StartPanel 顯示並 SetPanel 隱藏
        startPanel.SetActive(true);
        modePanel.SetActive(false);
        setPanel.SetActive(false);

        audioSource.PlayOneShot(menuMusic);
    }

    // 當按鈕被按下時，這個方法會被調用
    public void OnStartButtonClick()
    {
        startPanel.SetActive(false);
        modePanel.SetActive(true);
        setPanel.SetActive(false);
        Debug.Log("Start button clicked!");
    }

    public void onePeopleButtonClick()
    {
        startPanel.SetActive(false);
        modePanel.SetActive(false);
        setPanel.SetActive(true);
        isSingle = true;
        Debug.Log("Mode select : Single Mode");
        SingleBackground.enabled = true;
        DoubleBackground.enabled = false;
        audioSource.PlayOneShot(roundMusic);
    }

    public void twoPeopleButtonClick()
    {
        startPanel.SetActive(false);
        modePanel.SetActive(false);
        setPanel.SetActive(true);
        isSingle = false;
        SingleBackground.enabled = false;
        DoubleBackground.enabled = true;
        Debug.Log("Mode select : Double Mode");
        audioSource.PlayOneShot(roundMusic);
    }
    // 設置局數並切換到 MainScene
    public void OnSetRoundsButtonClick(int rounds)
    {
        // 記錄選擇的局數
        matchRounds = rounds;
        Debug.Log("Selected match rounds: " + matchRounds);

        // 切換到 MainScene
        if(isSingle == true){
            SceneManager.LoadScene("SingleMainScene");
        }else{
            SceneManager.LoadScene("MainScene");
        }
        
    }
}
