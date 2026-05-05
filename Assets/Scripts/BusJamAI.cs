using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Added an enum to manage the AI state
public enum AISolverState { Idle, Solving, Paused }

public class BusJamAI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameManager gameManager;

    [Header("Settings")]
    [SerializeField] private float actionDelay = 1.1f;
    [SerializeField] private bool autoPlay = false;
    [SerializeField] private float scoreThreshold = 300f;

    // --- NEW STATE VARIABLE ---
    public AISolverState CurrentState { get; private set; } = AISolverState.Idle;

    private bool isProcessing = false;
    private AISolverLogger logger;

    private void Start()
    {
        logger = GetComponent<AISolverLogger>();
        if (logger != null) logger.InitializeLogger(7);
    }

    private void Update()
    {
        // Modified to check if CurrentState is Solving
        if ((Input.GetKeyDown(KeyCode.A) || autoPlay) && !isProcessing && CurrentState == AISolverState.Solving)
        {
            StartCoroutine(ProcessBestMove());
        }
    }

    // --- NEW UI TOGGLE METHOD ---
    public void ToggleSolver()
    {
        if (CurrentState == AISolverState.Solving)
        {
            CurrentState = AISolverState.Paused;
            autoPlay = false;
            Debug.Log("<color=red>AI Solver Paused</color>");
        }
        else
        {
            CurrentState = AISolverState.Solving;
            autoPlay = true;
            Debug.Log("<color=green>AI Solver Activated</color>");
        }
    }

    private IEnumerator ProcessBestMove()
    {
        if (gameManager == null || gameManager.isBusDeparting || gameManager.currentBus == null || gameManager.currentBus.IsFull)
            yield break;

        isProcessing = true;
        Passenger bestPassenger = FindBestPassenger();

        if (bestPassenger != null)
        {
            float score = CalculateScore(bestPassenger);
            float unblockVal = CalculateUnblockValue(bestPassenger);
            int slots = GetFreeWaitingSlotsCount();
            string busColor = gameManager.currentBus.Color.ToString();

            if (score > scoreThreshold)
            {
                if (logger != null)
                {
                    logger.LogAction(bestPassenger.Color.ToString(), score, slots, unblockVal, busColor, "Executed");
                }

                Debug.Log($"AI Level 7 Strategy: Taking {bestPassenger.Color} with score {score}");
                gameManager.OnPassengerClicked(bestPassenger);
                yield return new WaitForSeconds(actionDelay);
            }
            else
            {
                if (logger != null) logger.LogAction(bestPassenger.Color.ToString(), score, slots, unblockVal, busColor, "Rejected_BelowThreshold");
                Debug.LogWarning("AI: No safe move found. Scores are too low to risk clogging.");
            }
        }
        else
        {
            if (logger != null) logger.LogAction("None", 0, GetFreeWaitingSlotsCount(), 0, "Unknown", "NO_PATH_FOUND");
        }

        isProcessing = false;
    }

    private Passenger FindBestPassenger()
    {
        List<Passenger> reachablePassengers = GetAllReachablePassengers();
        Passenger bestMatch = null;
        float highestScore = -1000000f;

        foreach (Passenger p in reachablePassengers)
        {
            float score = CalculateScore(p);
            if (score > highestScore)
            {
                highestScore = score;
                bestMatch = p;
            }
        }
        return bestMatch;
    }

    private List<Passenger> GetAllReachablePassengers()
    {
        List<Passenger> reachable = new List<Passenger>();
        for (int x = 0; x < gameManager.currentLevel.gridX; x++)
        {
            for (int y = 0; y < gameManager.currentLevel.gridY; y++)
            {
                Tile tile = gameManager.grid[x, y];
                if (tile != null && tile.CurrentPassenger != null)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    var path = gameManager.FindPathToTop(pos);
                    if (path != null && path.Count > 0)
                    {
                        reachable.Add(tile.CurrentPassenger);
                    }
                }
            }
        }
        return reachable;
    }

    private float CalculateScore(Passenger p)
    {
        float score = 0;
        int freeSlots = GetFreeWaitingSlotsCount();
        bool isCurrentColor = (p.Color == gameManager.currentBus.Color);
        bool isNextColor = IsMatchingNextBus(p);

        if (isCurrentColor)
        {
            score += 25000f;
        }
        else
        {
            float unblockValue = CalculateUnblockValue(p);
            score += unblockValue;

            if (freeSlots <= 1) return -500000f;

            if (isNextColor)
            {
                score += (freeSlots >= 3) ? 6000f : 1500f;
            }
            else
            {
                if (unblockValue < 1000f) score -= 10000f;
            }

            score += GetColorCountOnBoard(p.Color) * 150f;
        }

        Vector2Int pos = gameManager.FindPassengerPosition(p);
        score += pos.y * 75f;

        return score;
    }

    private float CalculateUnblockValue(Passenger p)
    {
        Vector2Int pos = gameManager.FindPassengerPosition(p);
        float value = 0;

        for (int y = pos.y + 1; y < gameManager.currentLevel.gridY; y++)
        {
            Tile belowTile = gameManager.grid[pos.x, y];
            if (belowTile != null && belowTile.CurrentPassenger != null)
            {
                Color belowColor = belowTile.CurrentPassenger.Color;

                if (belowColor == gameManager.currentBus.Color)
                {
                    value += 18000f;
                    break;
                }
                else if (IsMatchingNextBus(belowTile.CurrentPassenger))
                {
                    value += 6000f;
                    break;
                }
            }
        }
        return value;
    }

    private int GetColorCountOnBoard(Color targetColor)
    {
        int count = 0;
        foreach (Tile t in gameManager.grid)
        {
            if (t != null && t.CurrentPassenger != null && t.CurrentPassenger.Color == targetColor)
                count++;
        }
        return count;
    }

    private bool IsMatchingNextBus(Passenger p)
    {
        int nextIndex = gameManager.currentBusIndex + 1;
        if (nextIndex < gameManager.currentLevel.busConfigs.Length)
        {
            Color nextBusColor = gameManager.possibleColors[gameManager.currentLevel.busConfigs[nextIndex].color];
            return p.Color == nextBusColor;
        }
        return false;
    }

    private int GetFreeWaitingSlotsCount()
    {
        int count = 0;
        if (gameManager.waitingSlots == null) return 0;
        foreach (var slot in gameManager.waitingSlots)
        {
            if (slot.Passenger == null) count++;
        }
        return count;
    }
}