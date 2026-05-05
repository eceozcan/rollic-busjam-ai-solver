using System.IO;
using System.Text;
using UnityEngine;

public class AISolverLogger : MonoBehaviour
{
    private string filePath;
    private int moveCounter = 0;

    public void InitializeLogger(int levelNumber)
    {
        // Generates a file name with a unique timestamp for each run
        // AI_Log_Lvl7_20260505_1430.csv
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmm");
        string fileName = $"AI_Log_Level_{levelNumber}_{timestamp}.csv";

        filePath = System.IO.Path.Combine(Application.dataPath, fileName);

        if (!System.IO.File.Exists(filePath))
        {
            string header = "Timestamp;MoveID;SelectedColor;Score;FreeSlots;UnblockValue;TargetBus;Status\n";
            System.IO.File.WriteAllText(filePath, header, System.Text.Encoding.UTF8);
        }
    }

    public void LogAction(string color, float score, int freeSlots, float unblockVal, string targetBus, string status)
    {
        moveCounter++;
        string time = System.DateTime.Now.ToString("HH:mm:ss");

        // Prepare data in CSV format (separated by semicolons)
        string row = $"{time};{moveCounter};{color};{score:F2};{freeSlots};{unblockVal:F2};{targetBus};{status}\n";

        // Add to the end of the file
        File.AppendAllText(filePath, row, Encoding.UTF8);
    }
}