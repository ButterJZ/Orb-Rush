using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour {
    public int currentLevel;
    public int levelCount;
    public List<LevelData> levelData;

    public bool titlePageActive;

    private static PlayerDataManager original;

    public MenuUIManager menuUIManager;

    // After Game
    public bool gameOver;
    public int currentScore;


    void Start() {
        titlePageActive = true;
        levelCount = 15;
        levelData = new List<LevelData>();
    }

    void Awake() {
        DontDestroyOnLoad(this);

        titlePageActive = true;

        if (original == null) {
            original = this;
        }
        else {
            DestroyObject(gameObject);
        }
    }

    /// <summary>
    /// Load player data
    /// </summary>
    public void LoadDataFromJsonFile() {
        BetterStreamingAssets.Initialize();
        if (BetterStreamingAssets.FileExists("/Playerdata.json")) {
            string json = BetterStreamingAssets.ReadAllText("/Playerdata.json");
            if (json != null) {
                PlayerDataContainer dataContainer = JsonUtility.FromJson<PlayerDataContainer>(json);
                levelData = dataContainer.levelProgression;
            }
        }

        else {
            for (int i = 0; i < levelCount; i++) {
                LevelData level = new LevelData();
                level.highScore = 0;
                level.pieceCount = 0;
                level.collectedPieces = new List<Vector2Int>();

                levelData.Add(level);
            }
            string jsonString = JsonUtility.ToJson(new PlayerDataContainer { levelProgression = levelData });
            File.WriteAllText(Application.streamingAssetsPath + "/Playerdata.json", jsonString);
        }

        menuUIManager = FindObjectOfType<MenuUIManager>();

        // Debug
        menuUIManager.debugText.text = BetterStreamingAssets.ReadAllText("/Playerdata.json");
        menuUIManager.debugText.text = currentLevel + " " + levelData[currentLevel].highScore + " " + levelData[currentLevel].pieceCount;

        // Update UI for every level
        for (int i = 0; i < levelCount; i++) {
            menuUIManager.UpdateUI(i, levelData[i].highScore, levelData[i].pieceCount);
        }
    }

    /// <summary>
    /// Save player data
    /// </summary>
    public void SaveDataToJson() {
        string jsonString = JsonUtility.ToJson(new PlayerDataContainer { levelProgression = levelData });
        File.WriteAllText(Application.streamingAssetsPath + "/Playerdata.json", jsonString);
    }

    [System.Serializable]
    private class PlayerDataContainer {
        public List<LevelData> levelProgression;
    }

    [System.Serializable]
    public class LevelData {
        public int highScore;
        public int pieceCount;
        public List<Vector2Int> collectedPieces;
    }
}
