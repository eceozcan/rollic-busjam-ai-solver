using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private const int DefaultWaitingSlots = 5;
    private const int ExtendedWaitingSlots = 7;
    private const string ExtendedWaitingLevelName = "9472";
    private const float PreviewBusOffsetX = -3.3f;
    private const float PreviewBusOffsetZ = 0.85f;

    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject levelArea;
    [SerializeField] private GameObject passengerPrefab;
    [SerializeField] private GameObject busPrefab;
    [SerializeField] private GameObject waitingAreaPrefab;
    [SerializeField] private Color[] possibleColors;
    [SerializeField] private string levelsFolderName = "Levels";
    [SerializeField] private TextAsset[] levelDataFile;
    
    public Camera mainCamera;
    private float busDistance = 4f;
    private Tile[,] grid;
    private Bus currentBus;
    private Bus nextBusPreview;
    private int remainingPassengers;
    private int currentBusIndex;
    private LevelData currentLevel;
    private const float TileSpacing = 1.1f;
    private List<Vector2Int> currentPath;
    private WaitingSlot[] waitingSlots;
    private int currentLevelIndex;
    private string currentLevelName;
    private bool isBusDeparting;
    private bool isLevelTransitioning;
    private readonly List<LevelSource> loadedLevels = new();

    private struct LevelSource
    {
        public string Name;
        public string Json;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        LoadAvailableLevels();
        SetupLevel();
    }

    private void SetupLevel()
    {
        ClearLevel();
        LoadLevel();
        if (!enabled || currentLevel == null)
            return;

        CreateGrid();
        CreateWaitingArea();
        SpawnNewBus();
        PositionCamera();
    }
    
    private Vector3 CalculateBusStartPosition()
    {
        float gridCenterX = (currentLevel.gridX - 1) * TileSpacing * 0.5f;
        float gridTopEdgeZ = busDistance;
        return new Vector3(gridCenterX, 0, gridTopEdgeZ);
    }

    private void ClearLevel()
    {
        foreach (Transform child in levelArea.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void LoadLevel()
    {
        if (loadedLevels.Count == 0)
        {
            Debug.LogError("No level JSON files were found.");
            enabled = false;
            return;
        }

        currentLevel = JsonUtility.FromJson<LevelData>(loadedLevels[currentLevelIndex].Json);
        if (currentLevel == null)
        {
            Debug.LogError($"Failed to parse level json for {loadedLevels[currentLevelIndex].Name}.");
            enabled = false;
            return;
        }

        currentLevel.NormalizeForPrototype();
        currentLevelName = loadedLevels[currentLevelIndex].Name;
        currentBusIndex = 0;
        remainingPassengers = currentLevel.TotalPassengerCount();
        isBusDeparting = false;
        isLevelTransitioning = false;
    }

    private void LoadNextLevel()
    {
        currentLevelIndex++;
        if(currentLevelIndex >= loadedLevels.Count)
            currentLevelIndex = 0;
        SetupLevel();
    }

    private void LoadAvailableLevels()
    {
        loadedLevels.Clear();

        string levelsDirectory = Path.Combine(Application.dataPath, levelsFolderName);
        if (Directory.Exists(levelsDirectory))
        {
            string[] jsonPaths = Directory.GetFiles(levelsDirectory, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(jsonPaths, CompareLevelPaths);

            foreach (string jsonPath in jsonPaths)
            {
                try
                {
                    loadedLevels.Add(new LevelSource
                    {
                        Name = Path.GetFileNameWithoutExtension(jsonPath),
                        Json = File.ReadAllText(jsonPath)
                    });
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Failed to read level json at {jsonPath}: {exception.Message}");
                }
            }
        }

        if (loadedLevels.Count > 0)
        {
            Debug.Log($"Loaded {loadedLevels.Count} levels from {levelsDirectory}.");
            return;
        }

        if (levelDataFile == null || levelDataFile.Length == 0)
            return;

        TextAsset[] fallbackLevels = (TextAsset[])levelDataFile.Clone();
        Array.Sort(fallbackLevels, CompareTextAssets);

        foreach (TextAsset levelTextAsset in fallbackLevels)
        {
            if (levelTextAsset == null || string.IsNullOrWhiteSpace(levelTextAsset.text))
                continue;

            loadedLevels.Add(new LevelSource
            {
                Name = levelTextAsset.name,
                Json = levelTextAsset.text
            });
        }
    }

    private static int CompareLevelPaths(string leftPath, string rightPath)
    {
        return CompareLevelNames(Path.GetFileNameWithoutExtension(leftPath), Path.GetFileNameWithoutExtension(rightPath));
    }

    private static int CompareTextAssets(TextAsset left, TextAsset right)
    {
        string leftName = left == null ? string.Empty : left.name;
        string rightName = right == null ? string.Empty : right.name;
        return CompareLevelNames(leftName, rightName);
    }

    private static int CompareLevelNames(string leftName, string rightName)
    {
        bool leftIsNumber = int.TryParse(leftName, out int leftNumber);
        bool rightIsNumber = int.TryParse(rightName, out int rightNumber);

        if (leftIsNumber && rightIsNumber)
            return leftNumber.CompareTo(rightNumber);

        if (leftIsNumber != rightIsNumber)
            return leftIsNumber ? -1 : 1;

        return string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase);
    }

    private void CreateGrid()
    {
        grid = new Tile[currentLevel.gridX, currentLevel.gridY];

        
            for (int y = 0; y < currentLevel.gridY; y++)
            {
                for (int x = 0; x < currentLevel.gridX; x++)
                {
                    var cell = currentLevel.cellMatrix[y].cellArray[x];
                    Vector3 position = new Vector3(x * 1.1f, 0, y * -1.1f);
                    GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity);
                    tileObj.transform.SetParent(levelArea.transform);
                    grid[x, y] = tileObj.GetComponent<Tile>();

                    grid[x, y].SetDisabled(cell.isDisabled);
                }
        }

        SpawnPassengers();
    }

    private void SpawnPassengers()
    {
        for(int y = 0; y < currentLevel.gridY; y++)
        {
            for(int x = 0; x < currentLevel.gridX; x++)
            {
                var cell = currentLevel.cellMatrix[y].cellArray[x];
                if (cell.passengers.Length > 0)
                {
                    var passengerData = cell.passengers[0];
                    Tile tile = grid[x, y];
                    GameObject passengerObj = Instantiate(passengerPrefab, tile.PassengerAnchor.position, Quaternion.identity);
                    Passenger passenger = passengerObj.GetComponent<Passenger>();
                    passenger.Initialize(possibleColors[passengerData.color]);
                    tile.SetPassenger(passenger);
                }
                
            }
           
        }
    }


    private Bus SpawnBus(int busConfigIndex, Vector3 position)
    {
        if (busConfigIndex < 0 || busConfigIndex >= currentLevel.busConfigs.Length)
            return null;
        
        GameObject busObj = Instantiate(busPrefab, position, Quaternion.identity);
        busObj.transform.SetParent(levelArea.transform);
        Bus bus = busObj.GetComponent<Bus>();
        
        BusConfig busConfig = currentLevel.busConfigs[busConfigIndex];
        Color busColor = possibleColors[busConfig.color];
        bus.Initialize(busColor, busConfig.passengerCapacity);

        return bus;
    }

    private void SpawnNewBus()
    {
        currentBus = SpawnBus(currentBusIndex, CalculateBusStartPosition());
        nextBusPreview = SpawnBus(currentBusIndex + 1, CalculatePreviewBusPosition());
        MoveMatchingPassengersFromWaiting();
    }
    
    private void CreateWaitingArea()
    {
        int waitingSlotCount = GetWaitingSlotCount();
        waitingSlots = new WaitingSlot[waitingSlotCount];
        float startX = (currentLevel.gridX - waitingSlotCount) * TileSpacing * 0.5f;
        float waitingAreaZ = 1 + TileSpacing;

        for (int i = 0; i < waitingSlotCount; i++)
        {
            Vector3 position = new Vector3(startX + i * TileSpacing, 0, waitingAreaZ);
            GameObject slotObj = Instantiate(waitingAreaPrefab, position, Quaternion.identity);
            slotObj.transform.SetParent(levelArea.transform);
            waitingSlots[i] = new WaitingSlot { Position = position, Passenger = null};
        }
    }
    
    private int GetAvailableWaitingSlot()
    {
        for (int i = 0; i < waitingSlots.Length; i++)
        {
            if (waitingSlots[i].Passenger == null)
                return i;
        }
        return -1;
    }

    public void OnPassengerClicked(Passenger passenger)
    {
        if (currentPath != null || isLevelTransitioning) return;

        Vector2Int passengerPos = FindPassengerPosition(passenger);
        List<Vector2Int> path = FindPathToTop(passengerPos);
        
        if (path != null)
        {
            currentPath = path;
            if (passenger.Color == currentBus.Color)
            {
                StartCoroutine(MovePassengerToBus(passenger));
            }
            else
            {
                int slot = GetAvailableWaitingSlot();
                if (slot != -1)
                {
                    StartCoroutine(MovePassengerToWaitingArea(passenger, slot));
                }
            }
            MoveMatchingPassengersFromWaiting();
            
        }
    }
    
    private System.Collections.IEnumerator MovePassengerToWaitingArea(Passenger passenger, int slotIndex)
    {
        Vector2Int startPos = FindPassengerPosition(passenger);
        Tile startTile = grid[startPos.x, startPos.y];
        startTile.ClearPassenger();

        foreach (Vector2Int pathPos in currentPath)
        {
            yield return StartCoroutine(MovePassengerToPosition(passenger, 
                new Vector3(pathPos.x * TileSpacing, passenger.transform.position.y, -pathPos.y * TileSpacing)));
        }

        yield return StartCoroutine(MovePassengerToPosition(passenger, waitingSlots[slotIndex].Position));
        
        passenger.MakeNonPlayable();
        waitingSlots[slotIndex].Passenger = passenger;
        currentPath = null;

        if (GetAvailableWaitingSlot() == -1)
        {
            GameOver(false);
        }
    }
    
    private System.Collections.IEnumerator MovePassengerToBus(Passenger passenger)
    {
        Vector2Int startPos = FindPassengerPosition(passenger);
        Tile startTile = grid[startPos.x, startPos.y];
        startTile.ClearPassenger();

        foreach (Vector2Int pathPos in currentPath)
        {
            yield return StartCoroutine(MovePassengerToPosition(passenger, 
                new Vector3(pathPos.x * TileSpacing, passenger.transform.position.y, -pathPos.y * TileSpacing)));
        }

        currentBus.AddPassenger(passenger);
        passenger.MakeNonPlayable();
        
        remainingPassengers--;
        currentPath = null;

        CheckEnd();
    }

    public void CheckEnd()
    {
      
        if (currentBus != null && currentBus.IsFull && !isBusDeparting)
        {
            isBusDeparting = true;
            StartCoroutine(BusDepartureSequence());
          
        }

        if (remainingPassengers <= 0 || currentBusIndex >= currentLevel.busConfigs.Length)
        {
            GameOver(true);
        }
        
    }

    private System.Collections.IEnumerator MovePassengerToPosition(Passenger passenger, Vector3 targetPos)
    {
        float moveTime = 0.05f;
        float elapsed = 0;
        Vector3 startPos = passenger.transform.position;

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;
            passenger.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
    }
    
    private void MoveMatchingPassengersFromWaiting()
    {
        if (currentBus == null)
            return;

        for (int i = 0; i < waitingSlots.Length; i++)
        {
            if (waitingSlots[i].Passenger != null && 
                waitingSlots[i].Passenger.Color == currentBus.Color && 
                !currentBus.IsFull)
            {
                var passenger = waitingSlots[i].Passenger;
                waitingSlots[i].Passenger = null;
                StartCoroutine(MoveWaitingPassengerToBus(passenger));
            }
        }
    }

    private System.Collections.IEnumerator MoveWaitingPassengerToBus(Passenger passenger)
    {
        yield return StartCoroutine(MovePassengerToPosition(passenger, currentBus.transform.position));
        
        currentBus.AddPassenger(passenger);
        remainingPassengers--;

        CheckEnd();
    }


    private Vector2Int FindPassengerPosition(Passenger passenger)
    {
        for (int x = 0; x < currentLevel.gridX; x++)
        {
            for (int y = 0; y < currentLevel.gridY; y++)
            {
                if (grid[x, y].CurrentPassenger == passenger)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return Vector2Int.zero;
    }

    private List<Vector2Int> FindPathToTop(Vector2Int start)
    {
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        
        queue.Enqueue(start);
        visited.Add(start);
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            
            if (current.y == 0) // Reached top row
            {
                return ReconstructPath(start, current, cameFrom);
            }
            
            foreach (Vector2Int next in GetValidMoves(current))
            {
                if (!visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                    cameFrom[next] = current;
                }
            }
        }
        
        return null;
    }

    private List<Vector2Int> GetValidMoves(Vector2Int pos)
    {
        var moves = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        foreach (Vector2Int dir in directions)
        {
            Vector2Int next = pos + dir;
            if (IsValidPosition(next) && grid[next.x, next.y].CurrentPassenger == null && !grid[next.x, next.y].IsDisabled)
            {
                moves.Add(next);
            }
        }
        
        return moves;
    }

    private bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < currentLevel.gridX && 
               pos.y >= 0 && pos.y < currentLevel.gridY;
    }

    private List<Vector2Int> ReconstructPath(Vector2Int start, Vector2Int end, Dictionary<Vector2Int, Vector2Int> cameFrom)
    {
        var path = new List<Vector2Int>();
        Vector2Int current = end;
        
        while (!current.Equals(start))
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    

    private System.Collections.IEnumerator BusDepartureSequence()
    {
        yield return new WaitForSeconds(0.2f);
        
        Vector3 startPos = currentBus.transform.position;
        Vector3 exitPosition = new Vector3(startPos.x + 10f, 0, startPos.z);
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            currentBus.transform.position = Vector3.Lerp(startPos, exitPosition, t);
            yield return null;
        }
        
        Destroy(currentBus.gameObject);
        
        currentBusIndex++;
        currentBus = nextBusPreview;
        nextBusPreview = null;

        if (currentBus == null)
        {
            isBusDeparting = false;
            GameOver(remainingPassengers <= 0);
            yield break;
        }

        if (currentBus != null)
        {
            yield return StartCoroutine(MoveBus(currentBus.transform, CalculateBusStartPosition(), 0.2f));
        }

        nextBusPreview = SpawnBus(currentBusIndex + 1, CalculatePreviewBusPosition());
        isBusDeparting = false;
        MoveMatchingPassengersFromWaiting();
    
    }

    private System.Collections.IEnumerator MoveBus(Transform busTransform, Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = busTransform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            busTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        busTransform.position = targetPosition;
    }

    private Vector3 CalculatePreviewBusPosition()
    {
        return CalculateBusStartPosition() + new Vector3(PreviewBusOffsetX, 0, PreviewBusOffsetZ);
    }

    private int GetWaitingSlotCount()
    {
        return currentLevelName == ExtendedWaitingLevelName ? ExtendedWaitingSlots : DefaultWaitingSlots;
    }
    
    private void PositionCamera()
    {
        // yield return new WaitForEndOfFrame();
        FocusTarget(levelArea);
    }
    
    private Bounds CalculateBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds();

        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            combinedBounds.Encapsulate(renderer.bounds);
        }

        return combinedBounds;
    }
    
    public void FocusTarget(GameObject target)
    {
        if (target == null || mainCamera == null) return;

        Bounds bounds = CalculateBounds(target);
        if (bounds.size == Vector3.zero) return;
        
        float objectSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float distance = objectSize / (2.0f * Mathf.Tan(Mathf.Deg2Rad * mainCamera.fieldOfView / 2.0f));
        distance *= 1.4f; // Add padding to the distance

        //Vector3 direction = (mainCamera.transform.position - bounds.center).normalized;
        Vector3 targetPosition = bounds.center + levelArea.transform.up * distance;


        StartCoroutine(SmoothMoveCamera(mainCamera.transform.position, targetPosition, 0.01f));
    }
    
    private System.Collections.IEnumerator SmoothMoveCamera(Vector3 startPos, Vector3 endPos, float duration)
    {
        float elapsedTime = 0f;
        Vector3 velocity = Vector3.zero;
        while (elapsedTime < duration)
        {
            mainCamera.transform.position = Vector3.SmoothDamp(startPos, endPos, ref velocity, duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Finalize position
        mainCamera.transform.position = endPos;
    }

    private void GameOver(bool won)
    {
        if (isLevelTransitioning)
            return;

        isLevelTransitioning = true;
        StartCoroutine(GameOverSequence(won));
    }

    private System.Collections.IEnumerator GameOverSequence(bool won)
    {
        yield return new WaitForSeconds(1f);
        
        Debug.Log(won ? "Level Complete!" : "Level Failed - Waiting Area Full!");

        if (won)
        {
            LoadNextLevel();
        }
        else
        {
            SetupLevel();
        }
    }
}
