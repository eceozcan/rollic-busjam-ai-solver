using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class AISolverUI : MonoBehaviour
{
    [SerializeField] private BusJamAI aiSolver;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image statusIcon;

    private void Start()
    {
        actionButton.onClick.AddListener(OnButtonClicked);
    }

    private void Update()
    {
        if (aiSolver == null) return;

        if (statusText != null)
            statusText.text = $"AI STATUS: {aiSolver.CurrentState}";

        if (statusIcon != null)
            statusIcon.color = aiSolver.CurrentState == AISolverState.Solving
                ? Color.green
                : Color.red;
    }

    private void OnButtonClicked()
    {
        aiSolver.ToggleSolver();
    }
}