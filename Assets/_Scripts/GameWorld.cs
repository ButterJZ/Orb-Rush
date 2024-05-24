using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;
using System.IO;
using Slider = UnityEngine.UI.Slider;
using Debug = UnityEngine.Debug;
using UnityEngine.SceneManagement;

public class GameWorld : MonoBehaviour {
    // Map Data
    private int[,] map;
    public Tilemap tilemap;
    
    // Map Highlight
    public Tilemap tilemapHighlightL;
    public Tilemap tilemapHighlightR;
    public Tilemap tilemapHighlightT;
    public Tilemap tilemapHighlightB;

    // Tile Pieces
    public TileBase tile;
    public TileBase dot;
    public TileBase puzzle;
    public TileBase highlight;

    // Player
    public GameObject player;
    private Vector3Int playerLocation;
    public GameObject camera;
    private bool isMoving;

    // Game Info
    private float score;
    private int dotCollected;
    private int maxDotCount;
    public int currentLevel;
    public bool mapCreationMode;

    // UI
    public TMP_Text scoreText;
    public Slider timeSlider;
    public float time;
    private bool timerActivated;
    public bool infiniteTime;

    // Touch Imput
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;

    // Countdown
    public TMP_Text countDownText;
    private bool isInCountdown;

    // Debug
    public TMP_Text debugText;
    public PlayerDataManager playerData;

    void Awake() {
        isInCountdown = true;
        playerData = FindObjectOfType<PlayerDataManager>();

        playerData.gameOver = false;
        scoreText.text = "0%";
        BeginLevel();
    }

    void Update() {
        if (!isInCountdown) {
            // Touch Input
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) {
                startTouchPosition = Input.GetTouch(0).position;
            }
            // Finished swiping
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) {
                endTouchPosition = Input.GetTouch(0).position;

                // Swipe displacement
                float xChange = endTouchPosition.x - startTouchPosition.x;
                float yChange = endTouchPosition.y - startTouchPosition.y;

                // Move in X axis
                if (Math.Abs(xChange) >= Math.Abs(yChange)) {
                    if (xChange >= 0)
                        findDestination("right");
                    else
                        findDestination("left");
                }
                // Move in Y axis
                else {
                    if (yChange >= 0) 
                        findDestination("up");
                    else
                        findDestination("down");
                }
            }

            // Keyboard Input
            if (Input.GetKeyDown(KeyCode.W) && !isMoving)
                findDestination("up");
            if (Input.GetKeyDown(KeyCode.A) && !isMoving)
                findDestination("left");
            if (Input.GetKeyDown(KeyCode.S) && !isMoving)
                findDestination("down");
            if (Input.GetKeyDown(KeyCode.D) && !isMoving)
                findDestination("right");

            // Saving function for Map Creation
            if (Input.GetKeyDown(KeyCode.P))
                SaveTilemapToJson();

            // Center the player on screen
            CameraFollow();
        }
        // Debug
        debugText.text = time.ToString();
    }

    /// <summary>
    /// Setup the map when game begins
    /// </summary>
    public void BeginLevel() {
        currentLevel = playerData.currentLevel;
        isMoving = false;
        score = 0;
        dotCollected = 0;
        if (!mapCreationMode) {
            LoadTilemapFromJsonFile("/levels/level" + currentLevel + ".json");
            DrawMap();
        }
        // Three seconds countdown
        StartCoroutine(CountDown());
        // Timer starts and game begins
        StartTimer();
    }

    /// <summary>
    /// Three seconds countdown before game starts
    /// </summary>
    IEnumerator CountDown() {
        float countDownTime = 4f; // Countdown duration
        countDownText.gameObject.SetActive(true);
        while (isInCountdown) {
            countDownTime -= Time.deltaTime;
            yield return new WaitForSeconds(0.001f);

            if (countDownTime <= 0f) {
                countDownText.gameObject.SetActive(false);
                isInCountdown = false;
                break;
            }
            else if (countDownTime <= 1f) 
                countDownText.text = "GO!";
            else if (countDownTime <= 2f)
                countDownText.text = "1";
            else if (countDownTime <= 3f)
                countDownText.text = "2";
            else if (countDownTime <= 4f)
                countDownText.text = "3";
        }
    }

    /// <summary>
    /// Start timer
    /// </summary>
    void StartTimer() {
        timerActivated = true;
        timeSlider.maxValue = time;
        timeSlider.value = time;
        StartCoroutine(StartTicking());
    }

    /// <summary>
    /// Timer
    /// </summary>
    IEnumerator StartTicking() {
        while (timerActivated) {
            if (!infiniteTime && !isInCountdown) {
                time -= Time.deltaTime;
            }
            yield return new WaitForSeconds(0.001f);

            if (time <= 0f) {
                timerActivated = false;
                GameOver();
                break;
            }
            timeSlider.value = time;
        }
    }

    /// <summary>
    /// Gameover when time runs out player collect all orbs & puzzles
    /// </summary>
    void GameOver() {
        int highScore = playerData.levelData[currentLevel - 1].highScore;
        if ((int)score > highScore) {
            playerData.levelData[currentLevel - 1].highScore = (int)score;
            playerData.SaveDataToJson();
        }
        playerData.gameOver = true;
        playerData.currentScore = (int)score;
        SceneManager.LoadScene("Menu");
    }


    /// <summary>
    /// Camera tracking player's live postion with a slight slower speed compare to player
    /// </summary>
    private void CameraFollow() {
        float speed = 5f;
        var dir = (player.transform.position - camera.transform.position);
        var dir2 = new Vector3(dir.x, dir.y, -0.0001f);

        camera.transform.position += dir2 * speed * Time.deltaTime;
    }

    /// <summary>
    /// Draw the map with data in the 2d array
    /// </summary>
    void DrawMap() {
        for (int i = 0; i < map.GetLength(0); i++) {
            for (int j = 0; j < map.GetLength(1); j++) {
                // Player
                if (map[i, j] == -1) {
                    playerLocation = new Vector3Int(i, j, 0);
                    // Fill the player start point as a ground tile
                    FillTile(i, j, 1);
                }
                // Ground
                else if (map[i, j] == 1) {
                    FillTile(i, j, 1);
                }
                // Wall
                else if (map[i, j] == 2) {
                    FillTile(i, j, 2);
                }
                // Puzzle piece
                else if (map[i, j] == 3) {
                    // Check if piece is collected
                    bool pieceFound = false;
                    foreach (Vector2Int piece in playerData.levelData[currentLevel - 1].collectedPieces) {
                        if (piece.x == i && piece.y == j) {
                            map[i, j] = 1;
                            pieceFound = true;
                        }
                    }
                    // Draw piece when it is still not collected
                    if (!pieceFound) {
                        FillTile(i, j, 3);
                    }
                    // Draw ground when it is already collected
                    else {
                        FillTile(i, j, 1);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Set tiles and creating hilight around the map
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="type"></param>
    void FillTile(int x, int y, int type) {
        Vector3Int tilePosition = new Vector3Int(x - 8, y - 15, 0);

        if (type == 0) {
            tilemap.SetTile(tilePosition, null);
            tilemapHighlightL.SetTile(tilePosition, null);
            tilemapHighlightR.SetTile(tilePosition, null);
            tilemapHighlightT.SetTile(tilePosition, null);
            tilemapHighlightB.SetTile(tilePosition, null);
        }

        else if (type == 1 || type == -1) {
            tilemap.SetTile(tilePosition, tile);
            tilemapHighlightL.SetTile(tilePosition, highlight);
            tilemapHighlightR.SetTile(tilePosition, highlight);
            tilemapHighlightT.SetTile(tilePosition, highlight);
            tilemapHighlightB.SetTile(tilePosition, highlight);
        }

        else if (type == 2) {
            tilemap.SetTile(tilePosition, dot);
            tilemapHighlightL.SetTile(tilePosition, highlight);
            tilemapHighlightR.SetTile(tilePosition, highlight);
            tilemapHighlightT.SetTile(tilePosition, highlight);
            tilemapHighlightB.SetTile(tilePosition, highlight);
        }

        else if (type == 3) {
            tilemap.SetTile(tilePosition, puzzle);
            tilemapHighlightL.SetTile(tilePosition, highlight);
            tilemapHighlightR.SetTile(tilePosition, highlight);
            tilemapHighlightT.SetTile(tilePosition, highlight);
            tilemapHighlightB.SetTile(tilePosition, highlight);
        }
    }

    /// <summary>
    /// Calculate the position for player to land and start moving
    /// </summary>
    /// <param name="direction"></param>
    void findDestination(string direction) {
        int x = playerLocation.x;
        int y = playerLocation.y;

        int counter;

        switch (direction) {
            case "up":
                counter = 0;
                for (int i = y + 1; i < map.GetLength(1); i++) {
                    if (map[x, i] != 0) {
                        counter++;
                    }
                    else {
                        break;
                    }
                }
                PlayerMoveTo(x, y + counter, false);
                break;
            case "down":
                counter = 0;
                for (int i = y - 1; i >= 0; i--) {
                    if (map[x, i] != 0) {
                        counter++;
                    }
                    else {
                        break;
                    }
                }
                PlayerMoveTo(x, y - counter, false);
                break;
            case "left":
                counter = 0;
                for (int i = x - 1; i >= 0; i--) {
                    if (map[i, y] != 0) {
                        counter++;
                    }
                    else {
                        break;
                    }
                }
                PlayerMoveTo(x - counter, y, false);
                break;
            case "right":
                counter = 0;
                for (int i = x + 1; i < map.GetLength(0); i++) {
                    if (map[i, y] != 0) {
                        counter++;
                    }
                    else {
                        break;
                    }
                }
                PlayerMoveTo(x + counter, y, false);
                break;
        }
    }

    /// <summary>
    /// Move player to a position
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="instantMove"></param>
    void PlayerMoveTo(int x, int y, bool instantMove) {
        if (playerLocation.x == x && playerLocation.y == y) {
            return;
        }

        StartCoroutine(EatDots(playerLocation.x, x, playerLocation.y, y));
        StartCoroutine(MovePlayer(new Vector3Int(x, y, 0), instantMove));

        playerLocation.x = x;
        playerLocation.y = y;
    }

    /// <summary>
    /// Player movement
    /// </summary>
    private IEnumerator MovePlayer(Vector3Int destination, bool instantMove) {
        isMoving = true;
        float elapsedTime = 0f;

        Vector3 origionalPostion = tilemap.GetCellCenterWorld(new Vector3Int(playerLocation.x - 8, playerLocation.y - 15));
        Vector3 targetPosition = tilemap.GetCellCenterWorld(new Vector3Int(destination.x - 8, destination.y - 15));

        float distance = Math.Abs(origionalPostion.x - targetPosition.x) + Math.Abs(origionalPostion.y - targetPosition.y);

        float timeToMove = 0;

        // Instant move when game begins
        if (!instantMove) {
            timeToMove = 0.1f * distance;
        }

        // Move animation
        while (elapsedTime < timeToMove) {
            player.transform.position = Vector3.Lerp(origionalPostion, targetPosition, (elapsedTime / timeToMove));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        player.transform.position = targetPosition;

        if (instantMove) {
            camera.transform.position = player.transform.position + new Vector3(0, 0, -10);
        }

        isMoving = false;
    }

    /// <summary>
    /// Player eat dots
    /// </summary>
    IEnumerator EatDots(int playerX, int x, int playerY, int y) {
        // Player moving from right to left
        if (playerX > x) {
            for (int i = playerX; i >= x; i--) {
                if (map[i, y] == 2) {
                    map[i, y] = 1;
                    dotCollected++;
                    score = ((float)dotCollected / maxDotCount) * 100;
                    scoreText.text = (int)score + "%";
                }
                else if (map[i, y] == 3) {
                    map[i, y] = 1;
                    playerData.levelData[currentLevel - 1].pieceCount++;
                    playerData.levelData[currentLevel - 1].collectedPieces.Add(new Vector2Int(i, y));
                }
                yield return new WaitForSeconds(0.025f);
                FillTile(i, y, 1);
            }
        }
        // Left to right
        else if (playerX < x) {
            for (int i = playerX; i < x + 1; i++) {
                if (map[i, y] == 2) {
                    map[i, y] = 1;
                    dotCollected++;
                    score = ((float)dotCollected / maxDotCount) * 100;
                    scoreText.text = (int)score + "%";
                }
                else if (map[i, y] == 3) {
                    map[i, y] = 1;
                    playerData.levelData[currentLevel - 1].pieceCount++;
                    playerData.levelData[currentLevel - 1].collectedPieces.Add(new Vector2Int(i, y));
                }
                yield return new WaitForSeconds(0.025f);
                FillTile(i, y, 1);
            }
        }
        // Top to bottom
        else if (playerY > y) {
            for (int i = playerY; i >= y; i--) {
                if (map[x, i] == 2) {
                    map[x, i] = 1;
                    dotCollected++;
                    score = ((float)dotCollected / maxDotCount) * 100;
                    scoreText.text = (int)score + "%";
                }
                else if (map[x, i] == 3) {
                    map[x, i] = 1;
                    playerData.levelData[currentLevel - 1].pieceCount++;
                    playerData.levelData[currentLevel - 1].collectedPieces.Add(new Vector2Int(x, i));
                }
                yield return new WaitForSeconds(0.025f);
                FillTile(x, i, 1);
            }
        }
        // Bottom to top
        else {
            for (int i = playerY; i < y + 1; i++) {
                if (map[x, i] == 2) {
                    map[x, i] = 1;
                    dotCollected++;
                    score = ((float)dotCollected / maxDotCount) * 100;
                    scoreText.text = (int)score + "%";
                }
                else if (map[x, i] == 3) {
                    map[x, i] = 1;
                    playerData.levelData[currentLevel - 1].pieceCount++;
                    playerData.levelData[currentLevel - 1].collectedPieces.Add(new Vector2Int(x, i));
                }
                yield return new WaitForSeconds(0.025f);
                FillTile(x, i, 1);
            }
        }

        // Field Clear!
        if (dotCollected >= maxDotCount && playerData.levelData[currentLevel - 1].pieceCount >= 3) {
            GameOver();
        }
    }


    /// <summary>
    /// Save map as Json for Map Creation
    /// </summary>
    private void SaveTilemapToJson() {
        // Get tile data
        List<TileData> tileDataList = GetTileDataList(tilemap);

        // Convert tile data into Json
        string jsonString = JsonUtility.ToJson(new TilemapDataContainer { Tiles = tileDataList, playTime = time });

        // Save Json to file
        int fileNameCounter = 1;
        while (File.Exists(Application.dataPath + "/StreamingAssets" + "/levels/level" + fileNameCounter + ".json")) {
            fileNameCounter++;
        }
        File.WriteAllText(Application.dataPath + "/StreamingAssets" + "/levels/level" + fileNameCounter + ".json", jsonString);
    }

    /// <summary>
    /// Get tile data
    /// </summary>
    /// <param name="tilemap"></param>
    /// <returns></returns>
    private List<TileData> GetTileDataList(Tilemap tilemap) {
        List<TileData> tileDataList = new List<TileData>();

        BoundsInt bounds = tilemap.cellBounds;

        foreach (var position in bounds.allPositionsWithin) {
            Vector3Int localPlace = new Vector3Int(position.x, position.y, position.z);

            if (tilemap.HasTile(localPlace)) {
                TileBase tile = tilemap.GetTile(localPlace);
                TileData tileData = new TileData {
                    position = localPlace,
                    tileName = tile != null ? tile.name : "null"
                };
                tileDataList.Add(tileData);
            }
        }
        return tileDataList;
    }

    /// <summary>
    /// Load map data from Json
    /// </summary>
    /// <param name="filePath"></param>
    private void LoadTilemapFromJsonFile(string filePath) {
        BetterStreamingAssets.Initialize();
        // Read Json data
        string json = BetterStreamingAssets.ReadAllText(filePath);
        debugText.text = filePath + "\n" + "\"level\" + currentLevel + \".json\"" + BetterStreamingAssets.FileExists("level" + currentLevel + ".json").ToString();


        if (json != null) {
            // Convert Json to tile data
            TilemapDataContainer dataContainer = JsonUtility.FromJson<TilemapDataContainer>(json);
            ApplyTilemapData(dataContainer);
        }
        else {
            Debug.LogError("Failed to load JSON file.");
        }
    }

    private void ApplyTilemapData(TilemapDataContainer dataContainer) {
        time = dataContainer.playTime;
        if (time <= 0) {
            time = 15;
        }

        // Clear the tilemap
        tilemap.ClearAllTiles();
        tilemapHighlightL.ClearAllTiles();
        tilemapHighlightR.ClearAllTiles();
        tilemapHighlightT.ClearAllTiles();
        tilemapHighlightB.ClearAllTiles();

        maxDotCount = 0;

        int largerX = int.MinValue;
        int largerY = int.MinValue;
        int smallerX = int.MaxValue;
        int smallerY = int.MaxValue;

        foreach (var tileData in dataContainer.Tiles) {
            if (tileData.position.x < smallerX) {
                smallerX = tileData.position.x;
            }
            if (tileData.position.x > largerX) {
                largerX = tileData.position.x;
            }
            if (tileData.position.y < smallerY) {
                smallerY = tileData.position.y;
            }
            if (tileData.position.y > largerY) {
                largerY = tileData.position.y;
            }
        }

        map = new int[largerX - smallerX + 1, largerY - smallerY + 1];

        // Setup every tile
        foreach (var tileData in dataContainer.Tiles) {
            int x = tileData.position.x - smallerX;
            int y = tileData.position.y - smallerY;

            if (tileData.tileName == "") {
                map[x, y] = 0;
            }
            else if (tileData.tileName == "BLACK") {
                map[x, y] = 1;
            }
            else if (tileData.tileName == "GREENWITHBLACKBACKGROUND") {
                map[x, y] = 2;
                maxDotCount++;
            }
            else if (tileData.tileName == "PUZZLEPIECE") {
                map[x, y] = 3;
            }
            else if (tileData.tileName == "WHITE") {
                map[x, y] = -1;
                playerLocation = new Vector3Int(x, y);
                StartCoroutine(MovePlayer(playerLocation, true));
            }
        }
    }

    [System.Serializable]
    private class TilemapDataContainer {
        public List<TileData> Tiles;
        public float playTime;
    }

    [System.Serializable]
    private class TileData {
        public Vector3Int position;
        public string tileName;
    }
}