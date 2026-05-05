using System;

[System.Serializable]
public class LevelData
{
    public int ID;
    public int gridX;
    public int gridY;
    public CellRow[] cellMatrix;
    public BusConfig[] busConfigs;
    public CameraPreset cameraPreset;
    public float levelTime;


    public int TotalPassengerCount()
    {
        int count = 0;
        if (cellMatrix == null)
            return count;

        foreach (var row in cellMatrix)
        {
            if (row == null || row.cellArray == null)
                continue;

            foreach (var tile in row.cellArray)
            {
                if (tile != null && tile.passengers != null && tile.passengers.Length > 0)
                    count++;
            }
        }

        return count;
    }

    public void NormalizeForPrototype()
    {
        if (cellMatrix == null)
        {
            cellMatrix = Array.Empty<CellRow>();
        }
        else
        {
            foreach (var row in cellMatrix)
            {
                if (row == null)
                    continue;

                if (row.cellArray == null)
                {
                    row.cellArray = Array.Empty<Cell>();
                    continue;
                }

                foreach (var cell in row.cellArray)
                {
                    if (cell == null)
                        continue;

                    if (cell.passengers == null)
                        cell.passengers = Array.Empty<PassengerData>();

                    // Some imported levels mark blocked cells with isDisabledTall instead of isDisabled.
                    cell.isDisabled = cell.isDisabled || cell.isDisabledTall;
                }
            }
        }

        if (busConfigs == null)
            busConfigs = Array.Empty<BusConfig>();
    }
}

[System.Serializable]
public class CellRow
{
    public Cell[] cellArray;
}

[System.Serializable]
public class Cell
{
    public PassengerData[] passengers;
    public bool isDisabled;
    public bool isDisabledTall;
}

[System.Serializable]
public class PassengerData
{
    public int color;
    
}

[System.Serializable]
public class BusConfig
{
    public int color;
    public int passengerCapacity;
}

[System.Serializable]
public class CameraPreset
{
    public Vector3Data cameraPosition;
    public Vector3Data cameraRotation;
    public float cameraFov;
}

[System.Serializable]
public class Vector3Data
{
    public float x;
    public float y;
    public float z;
}
