using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUIManager : MonoBehaviour {
    public PlayerDataManager dataManager;

    // Connect to Level Menu
    public List<TMP_Text> levelBest;
    public List<TMP_Text> levelPiece;

    public int levelCount;

    public GameObject levelPages;
    public GameObject titlePage;

    // Game Summary
    public GameObject gameSummaryPage;
    public TMP_Text levelText;
    public TMP_Text scoreText;
    public TMP_Text pieceText;
    public List<GameObject> fullStars;

    // Shop
    public GameObject shopPage;

    // Debug
    public TMP_Text debugText;

    void Awake() {
        dataManager = FindObjectOfType<PlayerDataManager>();
        levelPages = GameObject.Find("Level Pages");

        Application.targetFrameRate = 120;
    }

    void Start() {
        levelCount = 15;

        gameSummaryPage.SetActive(true);
        titlePage.SetActive(true);
        shopPage.SetActive(false);

        if (!dataManager.titlePageActive) {
            titlePage.SetActive(false);
        }

        if (dataManager.gameOver) {
            AfterGameSummary(dataManager.currentLevel, dataManager.currentScore);
        }
        if (!dataManager.gameOver) {
            gameSummaryPage.SetActive(false);
        }

        dataManager.LoadDataFromJsonFile();
    }

    public void UpdateUI(int level, int best, int piece) {
        levelBest[level].text = "BEST: " + best + "%";
        levelPiece[level].text = "PIECE: " + piece + "/3";
    }

    /// <summary>
    /// Home page -> Game scene
    /// </summary>
    /// <param name="level"></param>
    public void NextScene(int level) {
        dataManager = FindObjectOfType<PlayerDataManager>();
        dataManager.currentLevel = level;
        SceneManager.LoadScene("Game");
    }

    /// <summary>
    /// Menu page -> Home page
    /// </summary>
    public void TapToContinue() {
        titlePage.SetActive(false);
        dataManager.titlePageActive = false;
    }

    /// <summary>
    /// Update summary page
    /// </summary>
    /// <param name="level"></param>
    /// <param name="score"></param>
    public void AfterGameSummary(int level, int score) {
        levelText.text = "LEVEL " + level;
        dataManager = FindObjectOfType<PlayerDataManager>();
        scoreText.text = " " + score + "<size=70%>%";

        pieceText.text = dataManager.levelData[level - 1].pieceCount + " / 3";
        gameSummaryPage.SetActive(true);

        // Display stars
        fullStars[0].gameObject.SetActive(false);
        fullStars[1].gameObject.SetActive(false);
        fullStars[2].gameObject.SetActive(false);

        if (score > 50) {
            fullStars[0].gameObject.SetActive(true);
        }
        if (score > 75) {
            fullStars[1].gameObject.SetActive(true);
        }
        if (score >= 100) {
            fullStars[2].gameObject.SetActive(true);
        }
    }

    public void AfterGameSummaryClose() {
        gameSummaryPage.SetActive(false);
    }

    public void ShopOpen() {
        shopPage.SetActive(true);
    }

    public void ShopClose() {
        shopPage.SetActive(false);
    }
}