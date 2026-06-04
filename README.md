# Bus Jam AI Solver 🚌🤖

An autonomous AI agent developed for a custom **Unity 6** prototype of the popular puzzle game *Bus Jam*. This project demonstrates a hybrid approach between traditional search algorithms and heuristic-based strategic reasoning to solve complex resource management problems in real-time mobile environments.

## 🎮 Gameplay

<img src="aisolver.png" width="100%" alt="Bus Jam AI Solver Gameplay"/>

---

## 🚀 Key Results
* **Level Success:** Successfully solves **6 out of 7** levels fully autonomously.
* **Performance:** Highly optimized to run at **60 FPS** on the Unity Main Thread.
* **Engineering Evolution:** Iterative development from a reactive baseline (v1.0) to a predictive strategic agent (v4.0).

## 🧠 The Approach: Heuristic-based Probabilistic Backtracking
Instead of using resource-heavy brute-force search (like BFS/DFS), I developed a **Heuristic-based Simulation** model. This allows the AI to mimic high-level human strategic thinking while respecting mobile hardware constraints.

### Core Features:
* **Master Heuristic Scoring:** Evaluates every possible move based on **Unblock Value** (prioritizing tiles that hide critical colors) and **Predictive Matching** (accounting for the next 2 buses in the queue).
* **Probabilistic Look-ahead:** When slot capacity becomes critical, the agent simulates moves ahead to calculate the survival probability of a dynamic path.
* **Dynamic Risk Thresholding:** A custom mechanism (`consecutiveWaitCount`) that increases risk tolerance when a stalemate is detected, allowing for "calculated aggression" to unblock the grid.

## 📈 Technical Report & Evolution Analysis

The comprehensive engineering journey, algorithmic trade-offs, and empirical data analysis of this project are documented in the official technical report.

### 📑 [Read the Full Technical Case Study Report (PDF)](https://github.com/eceozcan/rollic-busjam-ai-solver/blob/main/AIBusJam%20Case%20Study%20Report.docx_4.pdf)

### 📊 Key Highlights from the Report:
* **Algorithmic Evolution (v1.0 to v4.0):** Iterative refinement of heuristic scoring shifted the agent from trial-and-error to surgical precision, boosting decision efficiency from **13.89% to 54.30%**.
* **The 16.6ms Challenge (Hardware Trade-offs):** Rather than using CPU-heavy algorithms like $A^*$ or Minimax which cause overheating and frame drops on mobile devices, the engine utilizes a optimized heuristic model to strictly maintain Unity's main thread at **60 FPS**.
* **Level Design Validator (The Level 7 Case):** Used the AI agent as an automated QA tool to identify mathematical deadlocks and level design boundaries where human logic and standard reactive models fail.
* **Technological Evolution:** Represents a major paradigm shift in my development journey—moving from a purely *reactive* architecture (e.g., the [Unreal Engine Swamp Monster AI](https://github.com/eceozcan/unrealmechanic_rose/tree/main) developed 2 years ago using Behavior Trees) to a *predictive*, constraint-aware decision engine.

## 🛠 Tech Stack
* **Engine:** Unity 6
* **Language:** C#
* **Architecture:** State-driven Autonomous Agent (`AISolverState`)
* **Analytics:** Custom CSV Logging System (`AISolverLogger`) for move efficiency and heuristic stability analysis.

---

## 🕹 How to Run
1. Open the project in **Unity 6**.
2. Locate the **"AI Solver"** button in the custom Inspector/Editor UI.
3. Click to activate the autonomous mode and watch the agent analyze the grid and solve levels in real-time.

---

**Author:** Ece Özcan  
**Focus:** AI Engineering & Game Development
