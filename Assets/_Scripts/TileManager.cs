using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ScriptableObjects;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class TileManager : MonoBehaviour
{

    public static int gridSize = 4;

    private readonly Transform[,] _tilePositions = new Transform[gridSize, gridSize];
    private readonly Tile[,] _tiles = new Tile[gridSize, gridSize];
    private bool _isAnimating;
    private bool _tilesUpdated;
    private int _lastXInput;
    private int _lastYInput;
    private int _score;
    private int _bestScore;
    private int _moveCount;
    private Stopwatch _gameStopWatch = new Stopwatch();
    private IInputManager _inputManager = new MultipleInputManager(new KeybordInputManager(), new SwipeInputManager());
    
    

    private Stack<GameState> _gameStates = new Stack<GameState>();
    

    [SerializeField] private Tile tilePrefab;
    [SerializeField] private TileSettings tileSettings;

    [SerializeField] private GameOverScreen gameOverScreen;
    
    [SerializeField] private UnityEvent<int> scoreUpdated;
    [SerializeField] private UnityEvent<int> bestScoreUpdated;
    [SerializeField] private UnityEvent<int> moveCountUpdate;
    [SerializeField] private UnityEvent<TimeSpan> gameTimeUpdated;
    
    // Start is called before the first frame update
    void Start()
    {
        GetTilePositions();
        TrySpawnTile();
        TrySpawnTile();
        UpdateTilePositions(true);

        _gameStopWatch.Start();
        _bestScore = PlayerPrefs.GetInt("BestScore", 0);
        bestScoreUpdated.Invoke(_bestScore);
    }

    // Update is called once per frame
    void Update()
    {
        gameTimeUpdated.Invoke(_gameStopWatch.Elapsed);

        InputResult input = _inputManager.GetInput();
        
        if (!_isAnimating)
            Trymove(input.XInput, input.YInput);

    }

    public void AddSore(int value)
    {
        _score += value;
        scoreUpdated.Invoke(_score);

        if (_score > _bestScore)
        {
            _bestScore = _score;
            bestScoreUpdated.Invoke(_bestScore);
            PlayerPrefs.SetInt("BestScore", _bestScore);
        }
    }

    private void GetTilePositions()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        int x = 0;
        int y = 0;
        foreach (Transform t in this.transform)
        {
            _tilePositions[x, y] = t;
            x++;
            if (x >= gridSize)
            {
                x = 0;
                y++;
            }
        }
    }

    private bool TrySpawnTile()
    {
        List<Vector2Int> availableSpots = new List<Vector2Int>();
        
        for (int x = 0; x < gridSize; x++)
        for (int y = 0; y < gridSize; y++)
        {
            if (_tiles[x, y] == null)
                availableSpots.Add(new Vector2Int(x,y));
        }

        if (!availableSpots.Any())
            return false;

        int randomIndex = Random.Range(0, availableSpots.Count);
        Vector2Int spot = availableSpots[randomIndex];

        var tile = Instantiate(tilePrefab, transform.parent);
        tile.SetValue(GetRandomValue());
        _tiles[spot.x, spot.y] = tile;

        return true;

    }

    private int GetRandomValue()
    {
        var rand = Random.Range(0f, 1f);

        if (rand <= .8f)
            return 2;
        else
            return 4;
    }

    private void UpdateTilePositions(bool instant = false)
    {
        if (!instant)
        {
            _isAnimating = true;
            StartCoroutine(WaitForTileAnimation());
        }
        for (int x = 0; x < gridSize; x++)
        for (int y = 0; y < gridSize; y++)
        {
            if (_tiles[x, y] != null)
                _tiles[x, y].SetPosition(_tilePositions[x, y].position, instant);
        }
    }

    private IEnumerator WaitForTileAnimation()
    {
        yield return new WaitForSeconds(tileSettings.AnimationTime);

        if (!TrySpawnTile())
        {
            Debug.LogError("Unable to spawn Tile!!!");
        }

        UpdateTilePositions(true);

        if (!AnyMovesLeft())
        {
            gameOverScreen.SetGameOver(true);
        }
        
        _isAnimating = false;
    }

    private bool AnyMovesLeft()
    {
        return CanMoveRight() || CanMoveLeft() || CanMoveUp() || CanMoveDown();
    }

    private void Trymove(int x, int y)
    {
        if (x == 0 && y == 0)
            return;

        if (Mathf.Abs(x) == 1 && Mathf.Abs(y) == 1)
        {
            Debug.Log($"Invalid Move {x}, {y}");
        }

        _tilesUpdated = false;
        int[,] preMoveTileValues = GetCurrentTileValues();

        if (x == 0)
        {
            if (y > 0)
                TrymoveUp();
            else
                TryMoveDown();
        }
        else
        {
            if (x < 0)
                TryMoveLeft();
            else
                TryMoveRight();
        }
        
        if (_tilesUpdated)
        {
            _gameStates.Push(new GameState() { tileValues = preMoveTileValues, score = _score, moveCount = _moveCount});
            _moveCount++;
            moveCountUpdate.Invoke(_moveCount);
            UpdateTilePositions();
        }
    }

    private int[,] GetCurrentTileValues()
    {
        int[,] result = new int[gridSize, gridSize];
        for (int x = 0; x < gridSize; x++)
        for (int y = 0; y < gridSize; y++)
        {
            if (_tiles[x, y] != null)
                result[x, y] = _tiles[x, y].GetValues();
        }

        return result;
    }

    public void LoadLastGameState()
    {
        if (_isAnimating)
            return;

        if (!_gameStates.Any())
            return;

        GameState previousGameState = _gameStates.Pop();
        
        gameOverScreen.SetGameOver(false);

        _score = previousGameState.score;
        scoreUpdated.Invoke(_score);

        _moveCount = previousGameState.moveCount;
        moveCountUpdate.Invoke(_moveCount);

        foreach (Tile t in _tiles)
        {
            if (t != null)
                Destroy(t.gameObject);
        }

        for (int x = 0; x < gridSize; x++)
        for (int y = 0; y < gridSize; y++)
        {
            _tiles[x, y] = null;
            if (previousGameState.tileValues[x, y] == 0)
                continue;

            Tile tile = Instantiate(tilePrefab, transform.parent);
            tile.SetValue(previousGameState.tileValues[x, y]);
            _tiles[x, y] = tile;
        }

        UpdateTilePositions(true);
    }

    private bool TileExistsBetween(int x, int y, int x2, int y2)
    {
        if (x == x2)
            return TileExistsBetweenVerticle(x, y, y2);
        else if (y == y2)
            return TileExistsBetweenHorizontal(x, x2, y);
        
        Debug.LogError($"BETWEEN CHECK - INVALID PARAMETERS ({x}, {y}) ({x2}, {y2})");
        return true;
    }

    private bool TileExistsBetweenHorizontal(int x, int x2, int y)
    {
        int minX = Mathf.Min(x, x2);
        int maxX = Mathf.Max(x, x2);
        for (int xIndex = minX + 1; xIndex < maxX; xIndex++)
        {
            if (_tiles[xIndex, y] != null)
                return true;
        }

        return false;
    }

    private bool TileExistsBetweenVerticle(int x, int y, int y2)
    {
        int minY = Mathf.Min(y, y2);
        int maxY = Mathf.Max(y, y2);
        for (int yIndex = minY + 1; yIndex < maxY; yIndex++)
        {
            if (_tiles[x, yIndex] != null)
                return true;
        }

        return false;
    }

    private void TryMoveRight()
    {
        for (int y = 0; y < gridSize; y++)
        for (int x = gridSize - 1; x >= 0; x--)
        {
            if (_tiles[x,y] == null) continue;

            for (int x2 = gridSize - 1; x2 > x; x2--)
            {
                if (_tiles[x2, y] != null)
                {
                    if (TileExistsBetween(x, y, x2, y))
                        continue;
                    
                    if (_tiles[x2, y].Merge(_tiles[x, y]))
                    {
                        _tiles[x, y] = null;
                        _tilesUpdated = true;
                        break;
                    }
                    continue;
                }
                

                _tilesUpdated = true;
                _tiles[x2, y] = _tiles[x, y];
                _tiles[x, y] = null;
                break;
            }
        }
    }

    private void TryMoveLeft()
    {
        for (int y = 0; y < gridSize; y++)
        for (int x = 0; x < gridSize; x++)
        {
            if (_tiles[x, y] == null) continue;

            for (int x2 = 0; x2 < x; x2++)
            {
                if (_tiles[x2, y] != null)
                {
                    if (TileExistsBetween(x, y, x2, y))
                        continue;
                    
                    if (_tiles[x2, y].Merge(_tiles[x, y]))
                    {
                        _tiles[x, y] = null;
                        _tilesUpdated = true;
                        break;
                    }
                    continue;
                }
                
                
                _tilesUpdated = true;

                _tiles[x2, y] = _tiles[x, y];
                _tiles[x, y] = null;
                break;
            }
        }
    }

    private void TryMoveDown()
    {
        for (int x = 0; x < gridSize; x++)
        for (int y = gridSize - 1; y >= 0; y--)
        {
            if (_tiles[x, y] == null) continue;

            for (int y2 = gridSize - 1; y2 > y; y2--)
            {
                if (_tiles[x, y2] != null)
                {
                    if (TileExistsBetween(x, y, x, y2))
                        continue;
                    
                    if (_tiles[x, y2].Merge(_tiles[x, y]))
                    {
                        _tiles[x, y] = null;
                        _tilesUpdated = true;
                        break;
                    }
                    continue;
                }
                
                _tilesUpdated = true;

                _tiles[x, y2] = _tiles[x, y];
                _tiles[x, y] = null;
                break;
            }
        }
    }
    
    private void TrymoveUp()
    {
        for (int x = 0; x < gridSize; x++)
        for (int y = 0; y < gridSize; y++)
        {
            if (_tiles[x, y] == null) continue;

            for (int y2 = 0; y2 < y; y2++)
            {
                if (_tiles[x, y2] != null)
                {
                    if (TileExistsBetween(x, y, x, y2))
                        continue;

                    if (_tiles[x, y2].Merge(_tiles[x, y]))
                    {
                        _tiles[x, y] = null;
                        _tilesUpdated = true;
                        break;
                    }
                    continue;
                }
                
                _tilesUpdated = true;

                _tiles[x, y2] = _tiles[x, y];
                _tiles[x, y] = null;
                break;
            }
        }
    }
    
        private bool CanMoveRight()
    {
        for (int y = 0; y < gridSize; y++)
        for (int x = gridSize - 1; x >= 0; x--)
        {
            if (_tiles[x,y] == null) continue;

            for (int x2 = gridSize - 1; x2 > x; x2--)
            {
                if (_tiles[x2, y] != null)
                {
                    if (TileExistsBetween(x, y, x2, y))
                        continue;
                    
                    if (_tiles[x2, y].CanMerge(_tiles[x, y]))
                    {
                        return true;
                    }
                    continue;
                }
                
                return true;
            }
        }

        return false;
    }

    private bool CanMoveLeft()
    {
        for (int y = 0; y < gridSize; y++)
        for (int x = 0; x < gridSize; x++)
        {
            if (_tiles[x, y] == null) continue;

            for (int x2 = 0; x2 < x; x2++)
            {
                if (_tiles[x2, y] != null)
                {
                    if (TileExistsBetween(x, y, x2, y))
                        continue;
                    
                    if (_tiles[x2, y].CanMerge(_tiles[x, y]))
                    {
                        return true;
                    }
                    continue;
                }
                
                
                return true;
            }
        }

        return false;
    }

    private bool CanMoveDown()
    {
        for (int x = 0; x < gridSize; x++)
        for (int y = gridSize - 1; y >= 0; y--)
        {
            if (_tiles[x, y] == null) continue;

            for (int y2 = gridSize - 1; y2 > y; y2--)
            {
                if (_tiles[x, y2] != null)
                {
                    if (TileExistsBetween(x, y, x, y2))
                        continue;
                    
                    if (_tiles[x, y2].CanMerge(_tiles[x, y]))
                    {
                        return true;
                    }
                    continue;
                }
                
                return true;
            }
        }

        return false;
    }
    
    private bool CanMoveUp()
    {
        for (int x = 0; x < gridSize; x++)
        for (int y = 0; y < gridSize; y++)
        {
            if (_tiles[x, y] == null) continue;

            for (int y2 = 0; y2 < y; y2++)
            {
                if (_tiles[x, y2] != null)
                {
                    if (TileExistsBetween(x, y, x, y2))
                        continue;

                    if (_tiles[x, y2].CanMerge(_tiles[x, y]))
                    {
                        return true;
                    }
                    continue;
                }
                
                return true;
            }
        }

        return false;
    }

    public void RestartGame()
    {
        var activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }
}
