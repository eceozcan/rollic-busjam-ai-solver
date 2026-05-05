using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BusJamAI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameManager gameManager;

    [Header("Settings")]
    [SerializeField] private float actionDelay = 1.1f;
    [SerializeField] private bool autoPlay = false;

    private bool isProcessing = false;
    private AISolverLogger logger; // Reference for the logging system

    private void Start()
    {
        // Get the logger component and initialize it for the session
        logger = GetComponent<AISolverLogger>();
        if (logger != null) logger.InitializeLogger(7); // Initializing for Level 7 Analysis
    }

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.A) || autoPlay) && !isProcessing)
        {
            StartCoroutine(ProcessBestMove());
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

            // Threshold is 500. AI will perform moves it deems beneficial more easily.
            if (score > 500f)
            {
                // Record the decision to the CSV before execution
                if (logger != null)
                {
                    logger.LogAction(
                        bestPassenger.Color.ToString(),
                        score,
                        slots,
                        unblockVal,
                        busColor,
                        "Executed"
                    );
                }

                Debug.Log($"AI Balanced Attack: Taking {bestPassenger.Color}");
                gameManager.OnPassengerClicked(bestPassenger);
                yield return new WaitForSeconds(actionDelay);
            }
            else
            {
                // Log the rejection due to failing the threshold
                if (logger != null) logger.LogAction(bestPassenger.Color.ToString(), score, slots, unblockVal, busColor, "Rejected_BelowThreshold");
                Debug.LogWarning("AI: No meaningful move. If stuck, clear one blocker manually.");
            }
        }
        else
        {
            // Log when no reachable path is found for any passenger
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

        // --- 1. CURRENT BUS COLOR ---
        if (isCurrentColor)
        {
            score += 20000f;
        }
        else
        {
            // --- 2. UNBLOCK ANALYSIS (LEVEL 7 BALANCED) ---
            float unblockValue = CalculateUnblockValue(p);
            score += unblockValue;

            // --- 3. AREA MANAGEMENT ---
            if (freeSlots <= 1) return -500000f; // Absolute deadlock state.

            if (isNextColor)
            {
                // If it's the next bus color, add points if there is room (at least 3 slots).
                score += (freeSlots >= 3) ? 3000f : 500f;
            }
            else
            {
                // Penalize if the move doesn't unblock anything and is the wrong color.
                if (unblockValue < 1000f) score -= 4000f;
            }
        }

        // --- 4. POSITION ---
        Vector2Int pos = gameManager.FindPassengerPosition(p);
        score += pos.y * 50f;

        return score;
    }

    private float CalculateUnblockValue(Passenger p)
    {
        Vector2Int pos = gameManager.FindPassengerPosition(p);
        float value = 0;

        // Check the entire column below this passenger.
        for (int y = pos.y + 1; y < gameManager.currentLevel.gridY; y++)
        {
            Tile belowTile = gameManager.grid[pos.x, y];
            if (belowTile != null && belowTile.CurrentPassenger != null)
            {
                Color belowColor = belowTile.CurrentPassenger.Color;

                // Clearing the path for the current bus color is best.
                if (belowColor == gameManager.currentBus.Color)
                {
                    value += 15000f;
                    break;
                }
                // Clearing for the next bus color is also good.
                else if (IsMatchingNextBus(belowTile.CurrentPassenger))
                {
                    value += 5000f;
                    break;
                }
            }
        }
        return value;
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