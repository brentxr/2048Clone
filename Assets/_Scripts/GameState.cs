using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

public class GameState 
{
    public int[,] tileValues = new int[TileManager.gridSize, TileManager.gridSize];
    public int score;
    public int moveCount;
}
