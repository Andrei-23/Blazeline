using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteInEditMode]
public class GenerationController : MonoBehaviour
{
    public static GenerationController Instance { get; private set; }

    [SerializeField] public List<BaseGenerator> generators;

    [Header("Level Area")]
    public int radiusCells = 225;
    public float cellSize = 2f;
    [HideInInspector] public int levelR { get; private set; }
    [HideInInspector] public int levelW { get; private set; }
    [HideInInspector] public Vector2Int levelCenter { get; private set; }

    [Header("Seed")]
    [SerializeField] private bool useFixedSeed = true;
    [SerializeField] private int fixedSeed = 12345;
    [SerializeField] private int lastUsedSeed;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap areaTilemap;
    [SerializeField] private TileBase areaTileBase;

    [Header("Debug")]
    [SerializeField] private bool drawAreaTiles = false; 

    // public ObstacleGenerator obstacleGenerator;
    // public List<PoissonRandomSpawner> poissonSpawners;

    public bool[,] solidCells { get; private set; }
    public bool[,] areaCells { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
    #if UNITY_EDITOR
            DestroyImmediate(this);
    #else
            Destroy(this);
    #endif
            return;
        }

        Instance = this;
    }

    void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple GenerationController instances!");
            return;
        }

        Instance = this;
    }

    void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // if (!Application.isPlaying && autoGenerate)
        // {
        //     autoGenerate = false;
        //     GenerateAll();
        // }
    }

    [ContextMenu("Generate All")]
    public void GenerateAll()
    {
        Clear();

        UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
        lastUsedSeed = ResolveSeed();
        UnityEngine.Random.InitState(lastUsedSeed);
        
        InitGrid();

        foreach(var gen in generators)
        {
            gen.Generate();
        }

        if (drawAreaTiles)
        {
            DrawAreaTiles();
        }

        UnityEngine.Random.state = previousRandomState;
    }

    [ContextMenu("Clear All")]
    void Clear()
    {
        foreach(var gen in generators)
        {
            gen.CleanUp();
        }
        InitGrid();
    }
    private int ResolveSeed()
    {
        if (useFixedSeed)
        {
            return fixedSeed;
        }

        return System.Environment.TickCount;
    }


    void InitGrid()
    {
        levelR = radiusCells;
        levelW = radiusCells * 2 + 1;
        levelCenter = new Vector2Int(radiusCells, radiusCells);
        solidCells = new bool[levelW, levelW];
        areaCells = new bool[levelW, levelW];

        for(int x = -10; x < 10; x++)
        {
            for(int y = -10; y < 10; y++)
            {  
                var p = levelCenter + new Vector2Int(x, y);
                solidCells[p.x, p.y] = true;
            }
        }
    }

    public bool IsInside(Vector2Int pos)
    {
        return Math.Max(pos.x, pos.y) < levelW && Math.Min(pos.x, pos.y) >= 0;
    }
    public void SetSolidTileValue(Vector2Int pos, bool val)
    {
        if(IsInside(pos)){
            solidCells[pos.x, pos.y] = val;
            if(val)
                areaCells[pos.x, pos.y] = false;
        }
    }
    public void SetAreaTileValue(Vector2Int pos, bool val)
    {
        if(IsInside(pos) && !solidCells[pos.x, pos.y]) areaCells[pos.x, pos.y] = val;
    }

    private void DrawAreaTiles()
    {
        for(int x = 0; x < levelW; x++)
        {
            for(int y = 0; y < levelW; y++)
            {
                if(!areaCells[x, y]) continue;

                Vector2Int pos = new Vector2Int(x, y) - levelCenter;
                Vector3Int pos_3 = new Vector3Int(pos.x, pos.y, 0);
                areaTilemap.SetTile(pos_3, areaTileBase);
            }   
        }
    }

}