/* * DEVELOPMENT HISTORY:
 * v1.0: Basic score calculation based on current bus match.
 * v2.0: Added Grid Depth bonus and Next Bus look-ahead.
 * v3.0: Implemented Backtracking simulation to prevent deadlocks.
 * v4.0 (Current): Added Dynamic Risk Thresholding to prevent 'Wait' loops in complex levels.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum AISolverState { Idle, Solving, Paused }

public class BusJamAI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameManager gameManager;

    [Header("Engine Settings")]
    [SerializeField] private float actionDelay = 1.1f;
    [SerializeField] private bool autoPlay = false;

    [Header("Backtracking Logic")]
    [SerializeField] private int standardLookAhead = 8;
    [SerializeField] private int criticalLookAhead = 12;
    [SerializeField] private int maxSimulations = 10;

    public AISolverState CurrentState { get; private set; } = AISolverState.Idle;
    private bool isProcessing = false;
    private AISolverLogger logger;
    private int consecutiveWaitCount = 0;

    private void Start()
    {
        logger = GetComponent<AISolverLogger>();
        if (logger != null) logger.InitializeLogger(7);
    }

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.A) || autoPlay) && !isProcessing && CurrentState == AISolverState.Solving)
        {
            StartCoroutine(ProcessBestMove());
        }
    }

    public void ToggleSolver()
    {
        if (CurrentState == AISolverState.Solving)
        {
            CurrentState = AISolverState.Paused;
            autoPlay = false;
        }
        else
        {
            CurrentState = AISolverState.Solving;
            autoPlay = true;
        }
    }

    private IEnumerator ProcessBestMove()
    {
        if (gameManager == null || gameManager.isBusDeparting || gameManager.currentBus == null)
            yield break;

        isProcessing = true;

        Passenger bestPassenger = FindBestPassengerWithBacktracking();

        if (bestPassenger != null)
        {
            float score = CalculateMasterScore(bestPassenger);
            int slots = GetFreeWaitingSlotsCount();

            // DYNAMIC RISK THRESHOLD: The longer it waits, the more risk the AI ​​takes (Stuck prevention)
            float threshold = Mathf.Max(200f, 600f - (consecutiveWaitCount * 150f));

            if (score > threshold || bestPassenger.Color == gameManager.currentBus.Color)
            {
                consecutiveWaitCount = 0;
                if (logger != null)
                    logger.LogAction(bestPassenger.Color.ToString(), score, slots, CalculateUnblockValue(bestPassenger), gameManager.currentBus.Color.ToString(), "Executed");

                gameManager.OnPassengerClicked(bestPassenger);
                yield return new WaitForSeconds(actionDelay);
            }
            else
            {
                consecutiveWaitCount++;
                if (logger != null && consecutiveWaitCount % 5 == 0)
                    logger.LogAction(bestPassenger.Color.ToString(), score, slots, 0, "Wait", "Rejected_Safety");
            }
        }

        isProcessing = false;
    }

    private Passenger FindBestPassengerWithBacktracking()
    {
        List<Passenger> candidates = GetAllReachablePassengers();
        Passenger bestMatch = null;
        float highestScore = -100000000f;

        int slots = GetFreeWaitingSlotsCount();
        int depth = (slots <= 2) ? criticalLookAhead : standardLookAhead;

        foreach (Passenger p in candidates)
        {
            float score = CalculateMasterScore(p);

            // Backtracking: Simulate the future if the move doesn't lead to the bus.
            if (p.Color != gameManager.currentBus.Color)
            {
                if (!CheckFutureSafety(p, depth))
                {
                    score -= 250000f; // Security penalty
                }
            }

            if (score > highestScore)
            {
                highestScore = score;
                bestMatch = p;
            }
        }
        return bestMatch;
    }

    private float CalculateMasterScore(Passenger p)
    {
        float score = 0;
        int slots = GetFreeWaitingSlotsCount();
        bool isCurrentBus = (p.Color == gameManager.currentBus.Color);

        // 1. BASIC PRIORITY: Bus Matching
        if (isCurrentBus) return 50000f;

        // 2. CRITICAL CONDITION: Slot occupancy (Deadlock Prevention)
        if (slots <= 1)
        {
            // Only perform this action if the passenger below will board the bus
            return CalculateUnblockValue(p) > 15000f ? 20000f : -1000000f;
        }

        // 3. STRATEGIC SCORING (16:52 logic)
        score += CalculateUnblockValue(p); // Clearing the way
        if (IsMatchingNextBus(p))
            score += (slots >= 4) ? 8000f : 2000f;

        // Increase the score if there are slots with the same color waiting (to clear them by 3)
        int sameInSlots = GetColorCountInSlots(p.Color);
        score += (sameInSlots * 4000f);

        // Grid depth bonus (Get rid of those behind early)
        Vector2Int pos = gameManager.FindPassengerPosition(p);
        score += (pos.y * 150f);

        return score;
    }

    private bool CheckFutureSafety(Passenger initialMove, int depth)
    {
        int virtualSlots = GetFreeWaitingSlotsCount() - 1;
        if (virtualSlots <= 0) return CalculateUnblockValue(initialMove) > 15000f;

        for (int i = 0; i < maxSimulations; i++)
        {
            int tempSlots = virtualSlots;
            for (int d = 0; d < depth; d++)
            {
                if (tempSlots <= 0) break; // Simulation that there is a 45% chance that a bus will arrive and free up a slot
                if (Random.value < 0.45f) tempSlots = Mathf.Min(7, tempSlots + 1);
                else tempSlots--;
            }
            if (tempSlots >= 1) return true;
        }
        return false;
    }

    private float CalculateUnblockValue(Passenger p)
    {
        Vector2Int pos = gameManager.FindPassengerPosition(p);
        for (int y = pos.y + 1; y < gameManager.currentLevel.gridY; y++)
        {
            Tile below = gameManager.grid[pos.x, y];
            if (below != null && below.CurrentPassenger != null)
            {
                if (below.CurrentPassenger.Color == gameManager.currentBus.Color) return 22000f;
                if (IsMatchingNextBus(below.CurrentPassenger)) return 7000f;
                break;
            }
        }
        return 0;
    }

    private int GetColorCountInSlots(Color targetColor)
    {
        return gameManager.waitingSlots.Count(s => s.Passenger != null && s.Passenger.Color == targetColor);
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
                    if (gameManager.FindPathToTop(new Vector2Int(x, y))?.Count > 0)
                        reachable.Add(tile.CurrentPassenger);
                }
            }
        }
        return reachable;
    }

    private bool IsMatchingNextBus(Passenger p)
    {
        int next = gameManager.currentBusIndex + 1;
        if (next < gameManager.currentLevel.busConfigs.Length)
            return p.Color == gameManager.possibleColors[gameManager.currentLevel.busConfigs[next].color];
        return false;
    }

    private int GetFreeWaitingSlotsCount()
    {
        return gameManager.waitingSlots.Count(s => s.Passenger == null);
    }
}

