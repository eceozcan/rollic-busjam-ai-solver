using System.Collections.Generic;
using UnityEngine;

public struct GameStateSnapshot
{
    public int[,] Grid;
    public List<int> WaitingSlots;
    public int CurrentBusColorIndex;
    public int CurrentBusCapacity;
    public int CurrentBusIndex;
    public int RemainingTotalPassengers;

    public GameStateSnapshot Clone()
    {
        return new GameStateSnapshot
        {
            Grid = (int[,])this.Grid.Clone(),
            WaitingSlots = new List<int>(this.WaitingSlots),
            CurrentBusColorIndex = this.CurrentBusColorIndex,
            CurrentBusCapacity = this.CurrentBusCapacity,
            CurrentBusIndex = this.CurrentBusIndex,
            RemainingTotalPassengers = this.RemainingTotalPassengers
        };
    }
}