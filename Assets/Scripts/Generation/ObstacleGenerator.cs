using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Tilemaps;

public class ObstacleGenerator : BaseGenerator
{

    public class ObstacleShape
    {
        public GameObject obstaclePrefab;

        public Vector2Int[] solidCells;    // стены (занимают)
        public Vector2Int[] areaCells;  // зона запрета (буфер)

        // public Vector2Int min;
        // public Vector2Int max;

        public Vector2Int centerOffset = Vector2Int.zero;

        public int rot90 = 0; // 0..3
        public bool flipX = false;

        public Vector2Int ApplyPosModifier(Vector2Int position)
        {
            Vector2Int pos = position;
            if(flipX) pos = new Vector2Int(-pos.x, pos.y);
            
            switch (rot90)
            {
                case 0: break;
                case 1: pos = new Vector2Int(-pos.y, pos.x); break;
                case 2: pos = new Vector2Int(-pos.x, -pos.y); break;
                case 3: pos = new Vector2Int(pos.y, -pos.x); break;
            }
            return pos;
        }
    }

    public class ObstacleShapeVariants
    {
        public List<ObstacleShape> variants;
        public float weight = 1f;
        
        public ObstacleShapeVariants(GameObject prefab)
        {
            GameObject tempObj = Instantiate(prefab);
            Obstacle obstacle = tempObj?.GetComponent<Obstacle>();
            if (obstacle == null)
            {
                Debug.LogError("Prefab is not an instance of Obstacle");
                // ComputeBounds();
                DestroySafe(tempObj);
                return;
            }
            
            bool m_flipX = obstacle.flipX;
            bool m_flipY = obstacle.flipY;
            bool m_rot90 = obstacle.rotate;
            if (m_flipX && m_flipY && m_rot90)
            {
                m_flipY = false;
            }

            List<bool> flipX_vals = m_flipX ? new List<bool>{false, true} : new List<bool>{false};
            List<bool> flipY_vals = m_flipY ? new List<bool>{false, true} : new List<bool>{false};
            List<int> rot_vals = m_rot90 ? new List<int>{0,1,2,3} : new List<int>{0};

            var(a, b, c) = obstacle.CalculateShapeCells();

            variants = new();
            weight = obstacle.weight;

            foreach(var flipX in flipX_vals)
            {
                foreach(var flipY in flipY_vals)
                {
                    foreach(var rot90 in rot_vals)
                    {
                        ObstacleShape shape = new ObstacleShape
                        {
                            obstaclePrefab = prefab,
                            solidCells = ApplyModifiers(a, rot90, flipX, flipY).ToArray(),
                            areaCells = ApplyModifiers(b, rot90, flipX, flipY).ToArray(),
                            centerOffset = c,
                            rot90 = rot90,
                            flipX = flipX,
                        };
                        if (flipY)
                        {
                            shape.flipX = !shape.flipX;
                            shape.rot90 = (shape.rot90 + 2) % 4;
                        }
                        variants.Add(shape);
                    }   
                }
            }
            DestroySafe(tempObj);
        }

        List<Vector2Int> ApplyModifiers(List<Vector2Int> cells, int rot90, bool flipX, bool flipY)
        {
            List<Vector2Int> res = new();

            foreach(var p in cells)
            {
                Vector2Int pos = p;
                if(flipX) pos = new Vector2Int(-pos.x, pos.y);
                if(flipY) pos = new Vector2Int(pos.x, -pos.y);
                
                switch (rot90)
                {
                    case 0: break;
                    case 1: pos = new Vector2Int(-pos.y, pos.x); break;
                    case 2: pos = new Vector2Int(-pos.x, -pos.y); break;
                    case 3: pos = new Vector2Int(pos.y, -pos.x); break;
                }
                res.Add(pos);
            }
            return res;
        }

        public ObstacleShape ChooseRandomVariation()
        {
            if(variants.Count == 0)
            {
                return null;
            }
            int i = UnityEngine.Random.Range(0, variants.Count);
            return variants[i];
        }
    }
    
    [Serializable]
    public class ObstacleGroupSerializable
    {
        public List<GameObject> obstaclePrefabs = new();
        
        /// <summary>
        /// If not zero, attempts are limited to this amount, if targetCount is not zero, use targetCount * 20 instead, otherwise use default value.
        /// </summary>
        public int maxAttempts = 0;
        
        /// <summary>
        /// If not zero, attempts are limited to this amount, if targetCount is not zero, use targetCount * 20 instead, otherwise use default value.
        /// </summary>
        public int targetObstacleCount = 50;

        // либо плотность (если > 0, приоритетнее)
        [Range(0,1)] public float targetDensity = 0f;

    }

    public class ObstacleGroup
    {
        public List<ObstacleShapeVariants> shapeVars = new();
        
        public int maxAttempts = 0;
        // либо фиксированное количество
        public int targetCount = 50;

        // либо плотность (если > 0, приоритетнее)
        [Range(0,1)] public float targetDensity = 0f;

        public float totalWeight = 0f;

        public ObstacleGroup(ObstacleGroupSerializable ogs)
        {
            maxAttempts = ogs.maxAttempts;
            targetCount = ogs.targetObstacleCount;
            targetDensity = ogs.targetDensity;
            totalWeight = 0f;
            shapeVars = new();

            foreach(var prefab in ogs.obstaclePrefabs)
            {
                shapeVars.Add(new ObstacleShapeVariants(prefab));
            }

            foreach(var shape in shapeVars)
            {
                totalWeight += shape.weight;
            }
        }

        public ObstacleShape PickWeighted()
        {
            float r = UnityEngine.Random.Range(0, totalWeight);

            ObstacleShapeVariants svar = shapeVars[^1];
            foreach (var s in shapeVars)
            {
                r -= s.weight;
                if (r <= 0)
                {
                    svar = s;
                    break;
                }
            }

            return svar.ChooseRandomVariation();
        }
    }

    [Header("Generation Setup")]
    [SerializeField] private List<ObstacleGroupSerializable> obstacleGroups;
    [SerializeField] private Transform obstacleObjectParent; // extra objects are here
    [SerializeField] private Tilemap obstacleFinalTilemap; // the one with walls

    [SerializeField] private int maxPlacementAttemptsPerObstacle = 200;
    [SerializeField] private int maxTotalFailedPlacementAttempts = 20000;
    [SerializeField] private int defaultMaxAttempt = 10000;
    // [SerializeField] private bool generateOnStart = true;

    [SerializeField] private bool invertTileRotation = false;


    [Header("Spawn Area (Box)")]
    [SerializeField] private float spawnDistance = 250f; // half of box width


    [Header("Other")]
    [SerializeField] private TileRotation tileRotation;
    [SerializeField] private Tilemap debugAreaTilemap;
    // [SerializeField] private TileBase areaTileBase;



    // [Header("Overlap Check")]
    // [SerializeField] private bool useBlockingLayers = false;
    // [SerializeField] private LayerMask areaBlockingLayers = ~0;
    // [SerializeField] private LayerMask wallBlockingLayers = ~0;
    // [SerializeField] private bool includeTriggerColliders = false;

    [Header("Seed")]
    [SerializeField] private bool useFixedSeed = true;
    [SerializeField] private int fixedSeed = 12345;
    [SerializeField] private int lastUsedSeed;


    private bool[,] solidCells => GenerationController.Instance.solidCells;
    private bool[,] areaCells => GenerationController.Instance.areaCells;

    
    private List<Vector2Int> candidates = new();
    private HashSet<Vector2Int> candidateSet = new();

    private int totalCells;
    private int levelR  => GenerationController.Instance.levelR;
    private int levelW  => GenerationController.Instance.levelW;
    private Vector2Int levelCenter => GenerationController.Instance.levelCenter;

    private int totalAreaTiles = 0;

    private void Start()
    {
        // if (generateOnStart)
        // {
        //     Generate();
        // }
    }

    private bool IsInsideLevel(Vector2Int pos)
    {
        return IsInsideLevel(pos.x, pos.y);
    }
    private bool IsInsideLevel(int x, int y)
    {
        x -= levelCenter.x;
        y -= levelCenter.y;
        return Math.Min(x,y) >= -spawnDistance && Math.Max(x,y) < spawnDistance;
    }

    private void InitCandidates()
    {
        candidates = new();
        candidateSet = new();
        totalCells = 0;

        for (int x = 0; x < levelW; x++)
        for (int y = 0; y < levelW; y++)
        {
            if (IsInsideLevel(x, y) && !solidCells[x,y] && !areaCells[x,y])
            {
                AddCandidate(new Vector2Int(x, y));
                totalCells += 1;
            }
        }
    }

    void AddCandidate(Vector2Int p)
    {
        if (!candidateSet.Contains(p))
        {
            candidateSet.Add(p);
            candidates.Add(p);
        }
    }

    void RemoveCandidate(Vector2Int p)
    {
        candidateSet.Remove(p);
    }


    [ContextMenu("Cleanup")]
    public override void CleanUp()
    {
        ClearObstaclesRoot();
        obstacleFinalTilemap.ClearAllTiles();
        debugAreaTilemap.ClearAllTiles();
    }


    // [ContextMenu("Clear All Obstacles In Root")]
    private void ClearObstaclesRoot()
    {
        if (obstacleObjectParent == null)
        {
            return;
        }

        for (int i = obstacleObjectParent.childCount - 1; i >= 0; i--)
        {
            Transform child = obstacleObjectParent.GetChild(i);
            if (child != null)
            {
                DestroySafe(child.gameObject);
            }
        }
    }


    public override void Generate()
    {
        // UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
        // if(useSeed){
        //     lastUsedSeed = ResolveSeed();
        //     UnityEngine.Random.InitState(lastUsedSeed);
        // }

        // CleanUp();

        totalAreaTiles = 0;
        
        InitCandidates();

        foreach (var groupSerializable in obstacleGroups)
        {
            var group = new ObstacleGroup(groupSerializable);
            GenerateGroup(group);
        }
        
        // if (useSeed)
            // UnityEngine.Random.state = previousRandomState;

    }

    private void GenerateGroup(ObstacleGroup group)
    {
        int targetCount = group.targetCount;
        int targetAreaTileCount = Mathf.RoundToInt(totalCells * group.targetDensity);

        int placed = 0;
        int attempts = 0;
        int maxAttempts;
        if(group.maxAttempts != 0)
        {
            maxAttempts = group.maxAttempts;
        }
        else if(targetCount != 0)
        {
            maxAttempts = targetCount * 20;
        }
        else
        {
            maxAttempts = defaultMaxAttempt;    
        }

        int failedPlaceAttempts = 0;

        while (attempts < maxAttempts)
        {
            if(targetCount != 0 && placed >= targetCount) break;
            if(targetAreaTileCount != 0 && totalAreaTiles >= targetAreaTileCount) break;

            attempts++;

            var shape = group.PickWeighted();

            // пробуем через кандидатов
            for (int i = 0; i < maxPlacementAttemptsPerObstacle; i++)
            {
                var pos = GetRandomCandidate();
                if (pos == null) break;
                // if (pos.Value.y >= 440)
                // {
                //     int kkk = 3;
                // }
                if (CanPlace(shape, pos.Value))
                {
                    PlaceObstacle(shape, pos.Value);
                    placed++;
                    break;
                }
                else
                {
                    failedPlaceAttempts += 1;
                    if(failedPlaceAttempts >= maxTotalFailedPlacementAttempts)
                    {
                        break;
                    }
                }
            }
        }
        Debug.Log("Obstacle count: " + placed);
        Debug.Log("Obstacle density: " + ((float)totalAreaTiles / totalCells));
    }

    Vector2Int? GetRandomCandidate()
    {
        if (candidates.Count == 0) return null;

        if (candidates.Count > candidateSet.Count * 2)
            CompactCandidates();
        for (int i = 0; i < 10; i++)
        {
            int idx = UnityEngine.Random.Range(0, candidates.Count);
            var p = candidates[idx];

            if (candidateSet.Contains(p))
                return p;
        }

        return null;
    }
    void CompactCandidates()
    {
        candidates.RemoveAll(p => !candidateSet.Contains(p));
    }

    bool CanPlace(ObstacleShape shape, Vector2Int anchor)
    {
        // проверка solid
        foreach (var c in shape.solidCells)
        {
            Vector2Int p = anchor + c;

            if (!IsInsideLevel(p)) return false;
            if (solidCells[p.x, p.y]) return false;
            if (areaCells[p.x, p.y]) return false;
        }

        // проверка зоны запрета
        foreach (var c in shape.areaCells)
        {
            var p = anchor + c;

            if(IsInsideLevel(p)){
                if (solidCells[p.x, p.y]) return false;
            }
        }

        return true;
    }

    void PlaceObstacle(ObstacleShape shape, Vector2Int anchor)
    {
        // занять solid
        foreach (var c in shape.solidCells)
        {
            var p = anchor + c;
            if(!areaCells[p.x, p.y] && !solidCells[p.x, p.y])
            {
                totalAreaTiles += 1;
            }
            solidCells[p.x, p.y] = true;
            areaCells[p.x, p.y] = false;
            RemoveCandidate(p);
        }

        // занять blocked
        foreach (var c in shape.areaCells)
        {
            var p = anchor + c;
            if (IsInsideLevel(p))
            {
                if(!areaCells[p.x, p.y])
                {
                    totalAreaTiles += 1;
                }
                areaCells[p.x, p.y] = true;
                RemoveCandidate(p);
            }
        }

        GameObject obstacleTempObj = Instantiate(shape.obstaclePrefab);
        Obstacle obstacle = obstacleTempObj?.GetComponent<Obstacle>();
        if (obstacle == null)
        {
            Debug.LogError("Prefab is not an instance of Obstacle");
            DestroySafe(obstacleTempObj);
            return;
        }

        BoundsInt bounds = obstacle.WallsTilemap.cellBounds;
        // Vector3 centerPos3 = bounds.center;
        // Vector2Int centerPos = new Vector2Int(Mathf.FloorToInt(centerPos3.x), Mathf.FloorToInt(centerPos3.y));

        foreach (var pos in bounds.allPositionsWithin)
        {
            TileBase tile = obstacle.WallsTilemap.GetTile(pos);
            if (tile == null) continue;

            Vector2Int pos_2 = new Vector2Int(pos.x, pos.y) - shape.centerOffset;
            Vector2Int pos_modified = shape.ApplyPosModifier(pos_2);
            Vector2Int pos_new = pos_modified + anchor - levelCenter;
            Vector3Int pos_new_3 = new Vector3Int(pos_new.x, pos_new.y, pos.z);
            TileBase tileModified = tileRotation.GetTileModification(tile, shape.rot90, shape.flipX);
            obstacleFinalTilemap.SetTile(pos_new_3, tileModified);
        }

        GameObject extraObject = obstacle.obstacleExtraObject;
        if (extraObject != null){
            Vector2Int objPos = anchor - levelCenter;
            Vector3 offset = new Vector3(objPos.x, objPos.y, 0);
            Instantiate(extraObject, extraObject.transform.localPosition + offset, extraObject.transform.rotation, obstacleObjectParent);
        }

        DestroySafe(obstacleTempObj);
    }

    private int ResolveSeed()
    {
        if (useFixedSeed)
        {
            return fixedSeed;
        }

        return System.Environment.TickCount;
    }

    private static void DestroySafe(GameObject target)
    {
        if(target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
            return;
        }

        DestroyImmediate(target);
    }

    public (bool[,], int, Vector2Int) GetSolidCellsData()
    {
        if(solidCells == null) return new(null, 0, Vector2Int.zero);
        return new (solidCells, levelW, levelCenter);
    }
}
