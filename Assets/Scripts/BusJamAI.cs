////using System.Collections;
////using System.Collections.Generic;
////using UnityEngine;
////using System.Linq;

////public enum AISolverState { Idle, Solving, Paused }

////public class BusJamAI : MonoBehaviour
////{
////    [Header("Dependencies")]
////    [SerializeField] private GameManager gameManager;

////    [Header("Settings")]
////    [SerializeField] private float actionDelay = 1.1f;
////    [SerializeField] private bool autoPlay = false;
////    [SerializeField] private float scoreThreshold = 300f;

////    [Header("Hybrid Backtracking Logic")]
////    [SerializeField] private int standardLookAheadDepth = 8;
////    [SerializeField] private int emergencyLookAheadDepth = 12;
////    [SerializeField] private int maxSimulationPerMove = 10;
////    [SerializeField] private int criticalSlotThreshold = 3;

////    public AISolverState CurrentState { get; private set; } = AISolverState.Idle;

////    private bool isProcessing = false;
////    private AISolverLogger logger;

////    private void Start()
////    {
////        logger = GetComponent<AISolverLogger>();
////        if (logger != null) logger.InitializeLogger(7);
////    }

////    private void Update()
////    {
////        if ((Input.GetKeyDown(KeyCode.A) || autoPlay) && !isProcessing && CurrentState == AISolverState.Solving)
////        {
////            StartCoroutine(ProcessBestMove());
////        }
////    }

////    public void ToggleSolver()
////    {
////        if (CurrentState == AISolverState.Solving)
////        {
////            CurrentState = AISolverState.Paused;
////            autoPlay = false;
////            Debug.Log("<color=red>AI Solver Paused</color>");
////        }
////        else
////        {
////            CurrentState = AISolverState.Solving;
////            autoPlay = true;
////            Debug.Log("<color=green>AI Solver Activated</color>");
////        }
////    }

////    private IEnumerator ProcessBestMove()
////    {
////        // GÜNCELLEME: Otobüs kalkarken veya bekleme alanı tam doluyken asla hamle yapma.
////        if (gameManager == null || gameManager.isBusDeparting || GetFreeWaitingSlotsCount() <= 0)
////        {
////            yield break;
////        }

////        isProcessing = true;

////        Passenger priorityPassenger = GetImmediateMatch();
////        Passenger selectedPassenger;
////        float finalScore;

////        if (priorityPassenger != null)
////        {
////            selectedPassenger = priorityPassenger;
////            finalScore = 26000f;
////        }
////        else
////        {
////            selectedPassenger = FindBestPassengerWithHybridBacktracking();
////            finalScore = selectedPassenger != null ? CalculateScore(selectedPassenger) : -1000000f;
////        }

////        if (selectedPassenger != null)
////        {
////            float unblockVal = CalculateUnblockValue(selectedPassenger);
////            int slots = GetFreeWaitingSlotsCount();
////            string busColor = gameManager.currentBus.Color.ToString();

////            // GÜNCELLEME: "Risk Toleransı" - Skor eşiğin altında olsa bile 1'den fazla boş slot varsa hamle yap.
////            if (finalScore > scoreThreshold || (slots > 1 && finalScore > -100000f))
////            {
////                if (logger != null)
////                    logger.LogAction(selectedPassenger.Color.ToString(), finalScore, slots, unblockVal, busColor, "Executed");

////                gameManager.OnPassengerClicked(selectedPassenger);
////                yield return new WaitForSeconds(actionDelay);
////            }
////            else
////            {
////                if (logger != null) logger.LogAction(selectedPassenger.Color.ToString(), finalScore, slots, unblockVal, busColor, "Rejected_SafeLock");
////                Debug.LogWarning("AI: Strategy requires waiting for a safer opening.");
////            }
////        }

////        isProcessing = false;
////    }

////    private Passenger GetImmediateMatch()
////    {
////        return GetAllReachablePassengers()
////            .FirstOrDefault(p => p.Color == gameManager.currentBus.Color);
////    }

////    private Passenger FindBestPassengerWithHybridBacktracking()
////    {
////        List<Passenger> candidates = GetAllReachablePassengers();
////        Passenger bestMatch = null;
////        float highestScore = -1000000f;

////        int currentFreeSlots = GetFreeWaitingSlotsCount();
////        int dynamicDepth = (currentFreeSlots <= criticalSlotThreshold) ? emergencyLookAheadDepth : standardLookAheadDepth;

////        foreach (Passenger p in candidates)
////        {
////            float currentScore = CalculateScore(p);

////            if (currentScore > -1000000f)
////            {
////                bool isSafePath = CheckFutureSafety(p, dynamicDepth);
////                if (!isSafePath) currentScore -= 200000f;
////            }

////            if (currentScore > highestScore)
////            {
////                highestScore = currentScore;
////                bestMatch = p;
////            }
////        }
////        return bestMatch;
////    }

////    private bool CheckFutureSafety(Passenger initialMove, int depth)
////    {
////        int virtualFreeSlots = GetFreeWaitingSlotsCount();
////        if (initialMove.Color == gameManager.currentBus.Color) return true;

////        virtualFreeSlots--;
////        if (virtualFreeSlots <= 0) return false;
////        if (virtualFreeSlots == 1 && CalculateUnblockValue(initialMove) > 15000f) return true;

////        for (int i = 0; i < maxSimulationPerMove; i++)
////        {
////            if (CanCompletePath(virtualFreeSlots, depth)) return true;
////        }
////        return false;
////    }

////    private bool CanCompletePath(int slots, int depth)
////    {
////        int currentSlots = slots;
////        for (int d = 0; d < depth; d++)
////        {
////            if (currentSlots <= 0) return false;
////            if (Random.value < 0.5f) currentSlots = Mathf.Min(7, currentSlots + 1);
////            else currentSlots--;
////        }
////        return currentSlots >= 1;
////    }

////    private List<Passenger> GetAllReachablePassengers()
////    {
////        List<Passenger> reachable = new List<Passenger>();
////        if (gameManager == null || gameManager.grid == null) return reachable;

////        for (int x = 0; x < gameManager.currentLevel.gridX; x++)
////        {
////            for (int y = 0; y < gameManager.currentLevel.gridY; y++)
////            {
////                Tile tile = gameManager.grid[x, y];
////                if (tile != null && tile.CurrentPassenger != null)
////                {
////                    Vector2Int pos = new Vector2Int(x, y);
////                    var path = gameManager.FindPathToTop(pos);
////                    if (path != null && path.Count > 0)
////                        reachable.Add(tile.CurrentPassenger);
////                }
////            }
////        }
////        return reachable;
////    }

////    private float CalculateScore(Passenger p)
////    {
////        float score = 0;
////        int freeSlots = GetFreeWaitingSlotsCount();
////        bool isCurrentColor = (p.Color == gameManager.currentBus.Color);
////        bool isNextColor = IsMatchingNextBus(p);
////        Vector2Int pos = gameManager.FindPassengerPosition(p);

////        if (isCurrentColor) return 25000f + (pos.y * 75f);

////        float unblockValue = CalculateUnblockValue(p);
////        score += unblockValue;

////        if (freeSlots <= 1)
////        {
////            if (unblockValue > 15000f) return unblockValue;
////            return -500000f;
////        }

////        if (isNextColor)
////            score += (freeSlots >= 3) ? 6000f : 1500f;
////        else if (unblockValue < 1000f)
////            score -= 10000f;

////        score += GetColorCountOnBoard(p.Color) * 150f;
////        score += pos.y * 75f;

////        return score;
////    }

////    private float CalculateUnblockValue(Passenger p)
////    {
////        Vector2Int pos = gameManager.FindPassengerPosition(p);
////        float value = 0;
////        for (int y = pos.y + 1; y < gameManager.currentLevel.gridY; y++)
////        {
////            Tile belowTile = gameManager.grid[pos.x, y];
////            if (belowTile != null && belowTile.CurrentPassenger != null)
////            {
////                Color belowColor = belowTile.CurrentPassenger.Color;
////                if (belowColor == gameManager.currentBus.Color) { value += 18000f; break; }
////                else if (IsMatchingNextBus(belowTile.CurrentPassenger)) { value += 6000f; break; }
////            }
////        }
////        return value;
////    }

////    private int GetColorCountOnBoard(Color targetColor)
////    {
////        int count = 0;
////        foreach (Tile t in gameManager.grid)
////            if (t != null && t.CurrentPassenger != null && t.CurrentPassenger.Color == targetColor) count++;
////        return count;
////    }

////    private bool IsMatchingNextBus(Passenger p)
////    {
////        int nextIndex = gameManager.currentBusIndex + 1;
////        if (nextIndex < gameManager.currentLevel.busConfigs.Length)
////        {
////            Color nextBusColor = gameManager.possibleColors[gameManager.currentLevel.busConfigs[nextIndex].color];
////            return p.Color == nextBusColor;
////        }
////        return false;
////    }

////    private int GetFreeWaitingSlotsCount()
////    {
////        int count = 0;
////        if (gameManager.waitingSlots == null) return 0;
////        foreach (var slot in gameManager.waitingSlots)
////            if (slot.Passenger == null) count++;
////        return count;
////    }
////}

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System.Linq;

//public enum AISolverState { Idle, Solving, Paused }

//public class BusJamAI : MonoBehaviour
//{
//    [Header("Dependencies")]
//    [SerializeField] private GameManager gameManager;

//    [Header("Settings")]
//    [SerializeField] private float actionDelay = 1.1f;
//    [SerializeField] private bool autoPlay = false;
//    [SerializeField] private float scoreThreshold = 300f;

//    [Header("Backtracking Logic")]
//    [SerializeField] private int lookAheadDepth = 8;
//    [SerializeField] private int maxSimulationPerMove = 10;

//    public AISolverState CurrentState { get; private set; } = AISolverState.Idle;

//    private bool isProcessing = false;
//    private AISolverLogger logger;

//    private void Start()
//    {
//        logger = GetComponent<AISolverLogger>();
//        if (logger != null) logger.InitializeLogger(7);
//    }

//    private void Update()
//    {
//        if ((Input.GetKeyDown(KeyCode.A) || autoPlay) && !isProcessing && CurrentState == AISolverState.Solving)
//        {
//            StartCoroutine(ProcessBestMove());
//        }
//    }

//    public void ToggleSolver()
//    {
//        if (CurrentState == AISolverState.Solving)
//        {
//            CurrentState = AISolverState.Paused;
//            autoPlay = false;
//            Debug.Log("<color=red>AI Solver Paused</color>");
//        }
//        else
//        {
//            CurrentState = AISolverState.Solving;
//            autoPlay = true;
//            Debug.Log("<color=green>AI Solver Activated</color>");
//        }
//    }

//    private IEnumerator ProcessBestMove()
//    {
//        if (gameManager == null || gameManager.isBusDeparting || gameManager.currentBus == null || gameManager.currentBus.IsFull)
//            yield break;

//        isProcessing = true;

//        Passenger bestPassenger = FindBestPassengerWithBacktracking();

//        if (bestPassenger != null)
//        {
//            float score = CalculateScore(bestPassenger);

//            // EMERGENCY OVERRIDE: If the passenger matches the bus, it's ALWAYS safe 
//            // regardless of the 1-slot penalty in CalculateScore.
//            if (bestPassenger.Color == gameManager.currentBus.Color)
//            {
//                score = Mathf.Max(score, scoreThreshold + 1000f);
//            }

//            float unblockVal = CalculateUnblockValue(bestPassenger);
//            int slots = GetFreeWaitingSlotsCount();
//            string busColor = gameManager.currentBus.Color.ToString();

//            if (score > scoreThreshold)
//            {
//                if (logger != null)
//                    logger.LogAction(bestPassenger.Color.ToString(), score, slots, unblockVal, busColor, "Executed");

//                Debug.Log($"AI Strategy: Executing {bestPassenger.Color}. Future looks safe.");
//                gameManager.OnPassengerClicked(bestPassenger);
//                yield return new WaitForSeconds(actionDelay);
//            }
//            else
//            {
//                if (logger != null) logger.LogAction(bestPassenger.Color.ToString(), score, slots, unblockVal, busColor, "Rejected_BelowThreshold");
//                Debug.LogWarning("AI: No move survived the safety check. Waiting for an opening.");
//            }
//        }

//        isProcessing = false;
//    }

//    private Passenger FindBestPassengerWithBacktracking()
//    {
//        List<Passenger> candidates = GetAllReachablePassengers();
//        Passenger bestMatch = null;
//        float highestScore = -1000000f;

//        foreach (Passenger p in candidates)
//        {
//            float currentScore = CalculateScore(p);

//            // Backtracking Simulation
//            if (currentScore > -1000000f || p.Color == gameManager.currentBus.Color)
//            {
//                bool isSafePath = CheckFutureSafety(p, lookAheadDepth);
//                if (!isSafePath) currentScore -= 200000f;
//            }

//            if (currentScore > highestScore)
//            {
//                highestScore = currentScore;
//                bestMatch = p;
//            }
//        }
//        return bestMatch;
//    }

//    private bool CheckFutureSafety(Passenger initialMove, int depth)
//    {
//        int virtualFreeSlots = GetFreeWaitingSlotsCount();
//        Color busColor = gameManager.currentBus.Color;

//        // If it goes directly to the bus, it doesn't consume a slot.
//        if (initialMove.Color == busColor) return true;

//        virtualFreeSlots--;
//        if (virtualFreeSlots <= 0) return false;

//        // Optimized Unblock check: if it opens a current bus match, it's a high-value safe path.
//        if (virtualFreeSlots == 1 && CalculateUnblockValue(initialMove) > 15000f) return true;

//        for (int i = 0; i < maxSimulationPerMove; i++)
//        {
//            if (CanCompletePath(virtualFreeSlots, depth)) return true;
//        }

//        return false;
//    }

//    private bool CanCompletePath(int slots, int depth)
//    {
//        int currentSlots = slots;
//        for (int d = 0; d < depth; d++)
//        {
//            if (currentSlots <= 0) return false;
//            // Slightly optimistic simulation for Level 7 complexity
//            if (Random.value < 0.5f) currentSlots = Mathf.Min(7, currentSlots + 1);
//            else currentSlots--;
//        }
//        return currentSlots >= 1;
//    }

//    private List<Passenger> GetAllReachablePassengers()
//    {
//        List<Passenger> reachable = new List<Passenger>();
//        for (int x = 0; x < gameManager.currentLevel.gridX; x++)
//        {
//            for (int y = 0; y < gameManager.currentLevel.gridY; y++)
//            {
//                Tile tile = gameManager.grid[x, y];
//                if (tile != null && tile.CurrentPassenger != null)
//                {
//                    Vector2Int pos = new Vector2Int(x, y);
//                    var path = gameManager.FindPathToTop(pos);
//                    if (path != null && path.Count > 0)
//                        reachable.Add(tile.CurrentPassenger);
//                }
//            }
//        }
//        return reachable;
//    }

//    private float CalculateScore(Passenger p)
//    {
//        float score = 0;
//        int freeSlots = GetFreeWaitingSlotsCount();
//        bool isCurrentColor = (p.Color == gameManager.currentBus.Color);
//        bool isNextColor = IsMatchingNextBus(p);

//        if (isCurrentColor)
//        {
//            score += 25000f;
//        }
//        else
//        {
//            float unblockValue = CalculateUnblockValue(p);
//            score += unblockValue;

//            // Deadlock prevention penalty
//            if (freeSlots <= 1) return -500000f;

//            if (isNextColor)
//                score += (freeSlots >= 3) ? 6000f : 1500f;
//            else if (unblockValue < 1000f)
//                score -= 10000f;

//            score += GetColorCountOnBoard(p.Color) * 150f;
//        }

//        Vector2Int pos = gameManager.FindPassengerPosition(p);
//        score += pos.y * 75f;

//        return score;
//    }

//    private float CalculateUnblockValue(Passenger p)
//    {
//        Vector2Int pos = gameManager.FindPassengerPosition(p);
//        float value = 0;
//        for (int y = pos.y + 1; y < gameManager.currentLevel.gridY; y++)
//        {
//            Tile belowTile = gameManager.grid[pos.x, y];
//            if (belowTile != null && belowTile.CurrentPassenger != null)
//            {
//                Color belowColor = belowTile.CurrentPassenger.Color;
//                if (belowColor == gameManager.currentBus.Color) { value += 18000f; break; }
//                else if (IsMatchingNextBus(belowTile.CurrentPassenger)) { value += 6000f; break; }
//            }
//        }
//        return value;
//    }

//    private int GetColorCountOnBoard(Color targetColor)
//    {
//        int count = 0;
//        foreach (Tile t in gameManager.grid)
//            if (t != null && t.CurrentPassenger != null && t.CurrentPassenger.Color == targetColor) count++;
//        return count;
//    }

//    private bool IsMatchingNextBus(Passenger p)
//    {
//        int nextIndex = gameManager.currentBusIndex + 1;
//        if (nextIndex < gameManager.currentLevel.busConfigs.Length)
//        {
//            Color nextBusColor = gameManager.possibleColors[gameManager.currentLevel.busConfigs[nextIndex].color];
//            return p.Color == nextBusColor;
//        }
//        return false;
//    }

//    private int GetFreeWaitingSlotsCount()
//    {
//        int count = 0;
//        if (gameManager.waitingSlots == null) return 0;
//        foreach (var slot in gameManager.waitingSlots)
//            if (slot.Passenger == null) count++;
//        return count;
//    }
//}

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System.Linq;

//public enum AISolverState { Idle, Solving, Paused }

//public class BusJamAI : MonoBehaviour
//{
//    [Header("Dependencies")]
//    [SerializeField] private GameManager gameManager;

//    [Header("Settings")]
//    [SerializeField] private float actionDelay = 1.1f;
//    [SerializeField] private bool autoPlay = false;

//    [Header("Backtracking Logic")]
//    [SerializeField] private int lookAheadDepth = 8;
//    [SerializeField] private int maxSimulationPerMove = 10;

//    public AISolverState CurrentState { get; private set; } = AISolverState.Idle;

//    private bool isProcessing = false;
//    private AISolverLogger logger;

//    private void Start()
//    {
//        logger = GetComponent<AISolverLogger>();
//        if (logger != null) logger.InitializeLogger(7);
//    }

//    private void Update()
//    {
//        if ((Input.GetKeyDown(KeyCode.A) || autoPlay) && !isProcessing && CurrentState == AISolverState.Solving)
//        {
//            StartCoroutine(ProcessBestMove());
//        }
//    }

//    public void ToggleSolver()
//    {
//        if (CurrentState == AISolverState.Solving)
//        {
//            CurrentState = AISolverState.Paused;
//            autoPlay = false;
//            Debug.Log("<color=red>AI Solver Paused</color>");
//        }
//        else
//        {
//            CurrentState = AISolverState.Solving;
//            autoPlay = true;
//            Debug.Log("<color=green>AI Solver Activated</color>");
//        }
//    }

//    private IEnumerator ProcessBestMove()
//    {
//        if (gameManager == null || gameManager.isBusDeparting || gameManager.currentBus == null || gameManager.currentBus.IsFull)
//            yield break;

//        isProcessing = true;

//        Passenger bestPassenger = FindBestPassengerWithBacktracking();

//        if (bestPassenger != null)
//        {
//            float score = CalculateScore(bestPassenger);
//            int slots = GetFreeWaitingSlotsCount();

//            // DİNAMİK RİSK EŞİĞİ: Slotlar azaldıkça AI daha "cesur" (düşük puanlı hamlelere razı) olur.
//            float dynamicThreshold = Mathf.Lerp(100f, 400f, slots / 7.0f);

//            // EMERGENCY OVERRIDE: Otobüsle eşleşiyorsa her zaman güvenlidir.
//            if (bestPassenger.Color == gameManager.currentBus.Color)
//            {
//                score = Mathf.Max(score, dynamicThreshold + 1000f);
//            }

//            if (score > dynamicThreshold)
//            {
//                if (logger != null)
//                    logger.LogAction(bestPassenger.Color.ToString(), score, slots, CalculateUnblockValue(bestPassenger), gameManager.currentBus.Color.ToString(), "Executed");

//                gameManager.OnPassengerClicked(bestPassenger);
//                yield return new WaitForSeconds(actionDelay);
//            }
//            else
//            {
//                if (logger != null) logger.LogAction(bestPassenger.Color.ToString(), score, slots, 0, gameManager.currentBus.Color.ToString(), "Rejected_TooDangerous");
//            }
//        }

//        isProcessing = false;
//    }

//    private Passenger FindBestPassengerWithBacktracking()
//    {
//        List<Passenger> candidates = GetAllReachablePassengers();
//        Passenger bestMatch = null;
//        float highestScore = -1000000f;

//        // Dinamik Derinlik: Slot azaldıkça daha uzağı gör.
//        int currentDepth = GetFreeWaitingSlotsCount() <= 2 ? 12 : lookAheadDepth;

//        foreach (Passenger p in candidates)
//        {
//            float currentScore = CalculateScore(p);

//            if (currentScore > -1000000f || p.Color == gameManager.currentBus.Color)
//            {
//                bool isSafePath = CheckFutureSafety(p, currentDepth);
//                if (!isSafePath) currentScore -= 300000f; // Güvensiz patika cezası
//            }

//            if (currentScore > highestScore)
//            {
//                highestScore = currentScore;
//                bestMatch = p;
//            }
//        }
//        return bestMatch;
//    }

//    private bool CheckFutureSafety(Passenger initialMove, int depth)
//    {
//        int virtualFreeSlots = GetFreeWaitingSlotsCount();
//        if (initialMove.Color == gameManager.currentBus.Color) return true;

//        virtualFreeSlots--;
//        if (virtualFreeSlots <= 0)
//        {
//            // Son slotu kullanıyorsak, sadece altından otobüs rengi çıkacaksa izin ver.
//            return CalculateUnblockValue(initialMove) > 15000f;
//        }

//        for (int i = 0; i < maxSimulationPerMove; i++)
//        {
//            if (CanCompletePath(virtualFreeSlots, depth)) return true;
//        }
//        return false;
//    }

//    private bool CanCompletePath(int slots, int depth)
//    {
//        int currentSlots = slots;
//        for (int d = 0; d < depth; d++)
//        {
//            if (currentSlots <= 0) return false;
//            // Gerçekçi simülasyon: %40 ihtimalle otobüs eşleşmesi olur (slot boşalır).
//            if (Random.value < 0.4f) currentSlots = Mathf.Min(7, currentSlots + 1);
//            else currentSlots--;
//        }
//        return currentSlots >= 1;
//    }

//    private float CalculateScore(Passenger p)
//    {
//        float score = 0;
//        int freeSlots = GetFreeWaitingSlotsCount();
//        bool isCurrentColor = (p.Color == gameManager.currentBus.Color);

//        if (isCurrentColor)
//        {
//            score += 30000f; // Otobüs eşleşmesi en yüksek öncelik
//        }
//        else
//        {
//            // ZİNCİRLEME REAKSİYON BONUSU
//            score += CalculateChainReactionPotential(p);

//            // SLOTLARDAKİ COMBO KONTROLÜ
//            int sameColorInSlots = GetColorCountInSlots(p.Color);
//            if (sameColorInSlots >= 2) score += 8000f; // 3'e tamamlayıp slot boşaltma potansiyeli

//            if (freeSlots <= 1) return -500000f;

//            if (IsMatchingNextBus(p))
//                score += (freeSlots >= 4) ? 7000f : 2000f;

//            score += GetColorCountOnBoard(p.Color) * 200f;
//        }

//        Vector2Int pos = gameManager.FindPassengerPosition(p);
//        score += pos.y * 100f; // Arkadakileri açmak her zaman iyidir

//        return score;
//    }

//    private float CalculateChainReactionPotential(Passenger p)
//    {
//        Vector2Int pos = gameManager.FindPassengerPosition(p);
//        float potential = 0;
//        int depthFound = 0;

//        for (int y = pos.y + 1; y < gameManager.currentLevel.gridY; y++)
//        {
//            Tile belowTile = gameManager.grid[pos.x, y];
//            if (belowTile != null && belowTile.CurrentPassenger != null)
//            {
//                depthFound++;
//                if (belowTile.CurrentPassenger.Color == gameManager.currentBus.Color)
//                    potential += (25000f / depthFound);
//                else if (IsMatchingNextBus(belowTile.CurrentPassenger))
//                    potential += (8000f / depthFound);

//                break; // Sadece ilk engeli/yolcuyu kontrol et (temiz yol için)
//            }
//        }
//        return potential;
//    }

//    private int GetColorCountInSlots(Color targetColor)
//    {
//        int count = 0;
//        foreach (var slot in gameManager.waitingSlots)
//            if (slot.Passenger != null && slot.Passenger.Color == targetColor) count++;
//        return count;
//    }

//    private float CalculateUnblockValue(Passenger p)
//    {
//        Vector2Int pos = gameManager.FindPassengerPosition(p);
//        for (int y = pos.y + 1; y < gameManager.currentLevel.gridY; y++)
//        {
//            Tile belowTile = gameManager.grid[pos.x, y];
//            if (belowTile != null && belowTile.CurrentPassenger != null)
//            {
//                if (belowTile.CurrentPassenger.Color == gameManager.currentBus.Color) return 20000f;
//                break;
//            }
//        }
//        return 0;
//    }

//    private List<Passenger> GetAllReachablePassengers()
//    {
//        List<Passenger> reachable = new List<Passenger>();
//        for (int x = 0; x < gameManager.currentLevel.gridX; x++)
//        {
//            for (int y = 0; y < gameManager.currentLevel.gridY; y++)
//            {
//                Tile tile = gameManager.grid[x, y];
//                if (tile != null && tile.CurrentPassenger != null)
//                {
//                    var path = gameManager.FindPathToTop(new Vector2Int(x, y));
//                    if (path != null && path.Count > 0) reachable.Add(tile.CurrentPassenger);
//                }
//            }
//        }
//        return reachable;
//    }

//    private int GetColorCountOnBoard(Color targetColor)
//    {
//        int count = 0;
//        foreach (Tile t in gameManager.grid)
//            if (t != null && t.CurrentPassenger != null && t.CurrentPassenger.Color == targetColor) count++;
//        return count;
//    }

//    private bool IsMatchingNextBus(Passenger p)
//    {
//        int nextIndex = gameManager.currentBusIndex + 1;
//        if (nextIndex < gameManager.currentLevel.busConfigs.Length)
//        {
//            Color nextBusColor = gameManager.possibleColors[gameManager.currentLevel.busConfigs[nextIndex].color];
//            return p.Color == nextBusColor;
//        }
//        return false;
//    }

//    private int GetFreeWaitingSlotsCount()
//    {
//        int count = 0;
//        if (gameManager.waitingSlots == null) return 0;
//        foreach (var slot in gameManager.waitingSlots)
//            if (slot.Passenger == null) count++;
//        return count;
//    }
//}

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System.Linq;

//public enum AISolverState { Idle, Solving, Paused }

//public class BusJamAI : MonoBehaviour
//{
//    [Header("Dependencies")]
//    [SerializeField] private GameManager gameManager;

//    [Header("Engine Settings")]
//    [SerializeField] private float actionDelay = 1.2f; // Loglara göre senkronizasyon için hafif artırıldı
//    [SerializeField] private bool autoPlay = false;

//    public AISolverState CurrentState { get; private set; } = AISolverState.Idle;
//    private bool isProcessing = false;
//    private AISolverLogger logger;

//    private int consecutiveWaitCount = 0;

//    private void Start()
//    {
//        logger = GetComponent<AISolverLogger>();
//        if (logger != null) logger.InitializeLogger(7);
//    }

//    private void Update()
//    {
//        if ((Input.GetKeyDown(KeyCode.A) || autoPlay) && !isProcessing && CurrentState == AISolverState.Solving)
//        {
//            StartCoroutine(ProcessBestMove());
//        }
//    }

//    public void ToggleSolver()
//    {
//        CurrentState = (CurrentState == AISolverState.Solving) ? AISolverState.Paused : AISolverState.Solving;
//        autoPlay = (CurrentState == AISolverState.Solving);
//    }

//    private IEnumerator ProcessBestMove()
//    {
//        if (gameManager == null || gameManager.isBusDeparting || gameManager.currentBus == null || gameManager.currentBus.IsFull)
//            yield break;

//        isProcessing = true;
//        Passenger bestPassenger = FindBestPassengerMaster();

//        if (bestPassenger != null)
//        {
//            float score = CalculateMasterScore(bestPassenger);
//            int slots = GetFreeWaitingSlotsCount();

//            // DINAMIK EŞIK: Bekleme arttıkça AI'yı hamle yapmaya zorlar
//            float baseThreshold = (slots <= 3) ? 500000f : 300f;
//            float dynamicThreshold = baseThreshold - (consecutiveWaitCount * 800000f);

//            // Seviyeyi bitirmek için skor barajını genişletiyoruz
//            if (score > dynamicThreshold && score > -40000000f)
//            {
//                consecutiveWaitCount = 0;
//                if (logger != null)
//                    logger.LogAction(bestPassenger.Color.ToString(), score, slots, 0, gameManager.currentBus.Color.ToString(), "Executed");

//                gameManager.OnPassengerClicked(bestPassenger);
//                yield return new WaitForSeconds(actionDelay);
//            }
//            else
//            {
//                consecutiveWaitCount++;
//                if (logger != null) logger.LogAction(bestPassenger.Color.ToString(), score, slots, 0, "Wait", "Rejected_Critical_Safety");
//            }
//        }

//        isProcessing = false;
//    }

//    private Passenger FindBestPassengerMaster()
//    {
//        List<Passenger> candidates = GetAllReachablePassengers();
//        Passenger bestMatch = null;
//        float highestScore = -100000000f;

//        foreach (Passenger p in candidates)
//        {
//            float score = CalculateMasterScore(p);
//            if (score > highestScore)
//            {
//                highestScore = score;
//                bestMatch = p;
//            }
//        }
//        return bestMatch;
//    }

//    private float CalculateMasterScore(Passenger p)
//    {
//        int freeSlots = GetFreeWaitingSlotsCount();
//        bool isBusColor = (p.Color == gameManager.currentBus.Color);
//        int sameInSlots = GetColorCountInSlots(p.Color);
//        float gridFreedom = CalculateGridFreedom(p);

//        // VICTORY MODE: Tahtada çok az yolcu kaldıysa saldır!
//        List<Passenger> allPassengers = GetAllReachablePassengers();
//        if (allPassengers.Count <= freeSlots + 1)
//        {
//            if (isBusColor || sameInSlots >= 1 || gridFreedom > 0)
//                return 100000000f; // Bitirici hamle bonusu
//        }

//        // --- HARD CONSTRAINT ---
//        if (freeSlots <= 0) return -60000000f;

//        if (freeSlots == 1)
//        {
//            if (isBusColor) return 30000000f;
//            if (sameInSlots == 2) return 25000000f;
//            if (gridFreedom > 0) return 12000000f; // Altı açılıyorsa riske değer

//            return -50000000f;
//        }

//        float score = 0;

//        // 1. ÖNCELİK: TAHLİYE
//        if (isBusColor) score += 15000000f;
//        if (sameInSlots == 2) score += 10000000f;
//        if (sameInSlots == 1) score += 5000000f;

//        // 2. GELECEK PLANI
//        float futureWeight = CalculateFutureWeight(p.Color);
//        float multiplier = (freeSlots > 3) ? 2500f : 500f;
//        score += futureWeight * multiplier;

//        // 3. GRID FREEDOM
//        score += gridFreedom * 12000f;

//        // 4. DERİNLİK
//        Vector2Int pos = gameManager.FindPassengerPosition(p);
//        score += pos.y * 800f;

//        // 5. AGRESİF SLOT CEZASI (16:42 Hatasını Önlemek İçin)
//        if (freeSlots <= 2 && !isBusColor && sameInSlots == 0)
//        {
//            // Slotlar doluyken alakasız taş almanın cezası artırıldı
//            score -= 20000000f;
//        }

//        return score;
//    }

//    private float CalculateGridFreedom(Passenger p)
//    {
//        Vector2Int pos = gameManager.FindPassengerPosition(p);
//        float val = 0;
//        if (pos.y + 1 < gameManager.currentLevel.gridY)
//        {
//            Tile below = gameManager.grid[pos.x, pos.y + 1];
//            if (below != null && below.CurrentPassenger != null)
//            {
//                if (below.CurrentPassenger.Color == gameManager.currentBus.Color) val += 6000f;
//                else if (GetColorCountInSlots(below.CurrentPassenger.Color) >= 1) val += 3000f;
//            }
//        }
//        return val;
//    }

//    private float CalculateFutureWeight(Color col)
//    {
//        float weight = 0;
//        for (int i = 1; i <= 5; i++)
//        {
//            int idx = gameManager.currentBusIndex + i;
//            if (idx < gameManager.currentLevel.busConfigs.Length)
//            {
//                if (col == gameManager.possibleColors[gameManager.currentLevel.busConfigs[idx].color])
//                    weight += (6 - i) * 1500f;
//            }
//        }
//        return weight;
//    }

//    private int GetColorCountInSlots(Color targetColor)
//    {
//        if (gameManager.waitingSlots == null) return 0;
//        return gameManager.waitingSlots.Count(s => s.Passenger != null && s.Passenger.Color == targetColor);
//    }

//    private int GetFreeWaitingSlotsCount()
//    {
//        if (gameManager.waitingSlots == null) return 0;
//        return gameManager.waitingSlots.Count(s => s.Passenger == null);
//    }

//    private List<Passenger> GetAllReachablePassengers()
//    {
//        List<Passenger> reachable = new List<Passenger>();
//        for (int x = 0; x < gameManager.currentLevel.gridX; x++)
//        {
//            for (int y = 0; y < gameManager.currentLevel.gridY; y++)
//            {
//                Tile tile = gameManager.grid[x, y];
//                if (tile != null && tile.CurrentPassenger != null)
//                {
//                    var path = gameManager.FindPathToTop(new Vector2Int(x, y));
//                    if (path != null && path.Count > 0) reachable.Add(tile.CurrentPassenger);
//                }
//            }
//        }
//        return reachable;
//    }
//}

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