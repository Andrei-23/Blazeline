using UnityEngine;
using System.Collections.Generic;

public class ObstacleGeneratorOld : MonoBehaviour
{
    [Header("Generation Setup")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private List<float> obstacleWeights = new List<float>();
    [SerializeField] private Transform generatedParent;
    [SerializeField] private int obstacleCount = 20;
    [SerializeField] private int maxPlacementAttemptsPerObstacle = 200;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool rotate45Degree = false;

    [SerializeField] private bool invertTileRotation = false;


    [Header("Spawn Area (World XY)")]
    [SerializeField] private Vector2 spawnMin = new Vector2(-30f, -30f);
    [SerializeField] private Vector2 spawnMax = new Vector2(30f, 30f);

    [Header("Overlap Check")]
    [SerializeField] private bool useBlockingLayers = false;
    [SerializeField] private LayerMask areaBlockingLayers = ~0;
    [SerializeField] private LayerMask wallBlockingLayers = ~0;
    [SerializeField] private bool includeTriggerColliders = false;

    [Header("Seed")]
    [SerializeField] private bool useFixedSeed = true;
    [SerializeField] private int fixedSeed = 12345;
    [SerializeField] private int lastUsedSeed;

    private readonly List<GameObject> spawnedObstacles = new List<GameObject>();
    private readonly List<CompositeCollider2D> spawnedAreaColliders = new List<CompositeCollider2D>();
    private readonly List<CompositeCollider2D> spawnedWallColliders = new List<CompositeCollider2D>();
    private readonly List<Collider2D> overlapBuffer = new List<Collider2D>(32);
    private float totalWeight = 1f;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateLevel();
        }
    }

    [ContextMenu("Generate Level")]
    public void GenerateLevel()
    {
        if (!ValidateSetup())
        {
            return;
        }

        UpdateTotalWeight();

        Random.State previousRandomState = Random.state;
        lastUsedSeed = ResolveSeed();
        Random.InitState(lastUsedSeed);

        ClearGeneratedObstacles();

        for (int i = 0; i < obstacleCount; i++)
        {
            TryPlaceSingleObstacle();
        }

        Random.state = previousRandomState;
    }

    private void UpdateTotalWeight(){
        totalWeight = 0f;
        
        for (int i = 0; i < obstaclePrefabs.Length; i++)
        {
            float weight = GetWeightForIndex(i);
            if (weight > 0f)
            {
                totalWeight += weight;
            }
        }
    }

    [ContextMenu("Clear Generated Obstacles")]
    public void ClearGeneratedObstacles()
    {
        ClearObstaclesRoot();

        spawnedObstacles.Clear();
        spawnedWallColliders.Clear();
        spawnedAreaColliders.Clear();
    }

    // [ContextMenu("Clear All Obstacles In Root")]
    private void ClearObstaclesRoot()
    {
        if (generatedParent == null)
        {
            return;
        }

        for (int i = generatedParent.childCount - 1; i >= 0; i--)
        {
            Transform child = generatedParent.GetChild(i);
            if (child != null)
            {
                DestroyObject(child.gameObject);
            }
        }
    }

    private bool TryPlaceSingleObstacle()
    {
        for (int attempt = 0; attempt < maxPlacementAttemptsPerObstacle; attempt++)
        {
            GameObject prefab = GetWeightedRandomObstaclePrefab();
            Vector3 position = GetRandomPosition();
            Quaternion rotation;
            if(rotate45Degree){
                rotation = Quaternion.Euler(0f, 0f, Random.Range(0, 8) * 45f);
            }
            else{
                rotation = Quaternion.Euler(0f, 0f, Random.Range(0, 4) * 90f);
            }

            GameObject candidate = Instantiate(prefab, position, rotation, generatedParent);
            Obstacle obstacleInitializer = candidate.GetComponent<Obstacle>();

            if (obstacleInitializer == null)
            {
                Debug.LogWarning($"Obstacle '{prefab.name}' is missing ObstacleInitializer.");
                DestroyObject(candidate);
                return false;
            }

            CompositeCollider2D areaCollider = obstacleInitializer.PreservedAreaCompositeCollider;
            CompositeCollider2D wallCollider = obstacleInitializer.WallsCompositeCollider;
            if (areaCollider == null)
            {
                Debug.LogWarning($"Obstacle '{prefab.name}' is missing preserved-area CompositeCollider2D reference.");
                DestroyObject(candidate);
                return false;
            }

            bool touchesAnything = DoesObstacleTouchAnything(areaCollider, wallCollider, candidate.transform);

            if (!touchesAnything)
            {
                if(invertTileRotation){
                    obstacleInitializer.ApplyInverseRotationToTiles();
                }
                spawnedObstacles.Add(candidate);
                spawnedAreaColliders.Add(areaCollider);
                spawnedWallColliders.Add(wallCollider);
                return true;
            }

            DestroyObject(candidate);
        }

        Debug.LogWarning("Failed to place obstacle after max attempts.");
        return false;
    }

    private GameObject GetWeightedRandomObstaclePrefab()
    {
        float totalWeight = 0f;

        for (int i = 0; i < obstaclePrefabs.Length; i++)
        {
            float weight = GetWeightForIndex(i);
            if (weight > 0f)
            {
                totalWeight += weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        }

        float roll = Random.value * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < obstaclePrefabs.Length; i++)
        {
            float weight = GetWeightForIndex(i);
            if (weight <= 0f)
            {
                continue;
            }

            cumulative += weight;
            if (roll <= cumulative)
            {
                return obstaclePrefabs[i];
            }
        }

        return obstaclePrefabs[obstaclePrefabs.Length - 1];
    }

    private float GetWeightForIndex(int index)
    {
        if (index < 0 || index >= obstacleWeights.Count)
        {
            return 1f;
        }

        return Mathf.Max(0f, obstacleWeights[index]);
    }

    private bool DoesObstacleTouchAnything(CompositeCollider2D areaCollider, CompositeCollider2D wallCollider, Transform candidateTransform)
    {
        Physics2D.SyncTransforms();

        ContactFilter2D areaFilter = new ContactFilter2D
        {
            useLayerMask = useBlockingLayers,
            layerMask = areaBlockingLayers,
            useTriggers = includeTriggerColliders
        };

        overlapBuffer.Clear();
        areaCollider.Overlap(areaFilter, overlapBuffer);

        for (int i = 0; i < overlapBuffer.Count; i++)
        {
            Collider2D hit = overlapBuffer[i];
            if (hit == null)
            {
                continue;
            }

            if (hit.transform.parent == candidateTransform)
            {
                continue;
            }

            return true;
        }

        // ContactFilter2D wallFilter = new ContactFilter2D
        // {
        //     useLayerMask = useBlockingLayers,
        //     layerMask = wallBlockingLayers,
        //     useTriggers = includeTriggerColliders
        // };

        // overlapBuffer.Clear();
        // wallCollider.Overlap(wallFilter, overlapBuffer);

        // for (int i = 0; i < overlapBuffer.Count; i++)
        // {
        //     Collider2D hit = overlapBuffer[i];
        //     if (hit == null)
        //     {
        //         continue;
        //     }

        //     if (hit.transform.parent == candidateTransform)
        //     {
        //         continue;
        //     }

        //     return true;
        // }

        for (int i = 0; i < spawnedWallColliders.Count; i++)
        {
            CompositeCollider2D otherWall = spawnedWallColliders[i];
            if (otherWall != null && CollidersTouchOrContain(areaCollider, otherWall))
            {
                return true;
            }

            CompositeCollider2D otherArea = spawnedAreaColliders[i];
            if (otherArea != null && CollidersTouchOrContain(wallCollider, otherArea))
            {
                return true;
            }
        }

        return false;
    }

    private static bool CollidersTouchOrContain(CompositeCollider2D a, CompositeCollider2D b)
    {
        ColliderDistance2D distance = a.Distance(b);
        if (distance.isOverlapped || distance.distance <= 0f)
        {
            return true;
        }
        return false;
        // if (AnyPathPointInside(a, b))
        // {
        //     return true;
        // }

        // return AnyPathPointInside(b, a);
    }

    private static bool AnyPathPointInside(CompositeCollider2D source, CompositeCollider2D target)
    {
        for (int pathIndex = 0; pathIndex < source.pathCount; pathIndex++)
        {
            int pointCount = source.GetPathPointCount(pathIndex);
            if (pointCount <= 0)
            {
                continue;
            }

            Vector2[] localPoints = new Vector2[pointCount];
            source.GetPath(pathIndex, localPoints);

            for (int i = 0; i < localPoints.Length; i++)
            {
                Vector2 worldPoint = source.transform.TransformPoint(localPoints[i]);
                if (target.OverlapPoint(worldPoint))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private Vector3 GetRandomPosition()
    {
        float x = Random.Range(spawnMin.x, spawnMax.x);
        float y = Random.Range(spawnMin.y, spawnMax.y);

        // snap to geid with size 2
        x = Mathf.Round(x / 2f) * 2f;
        y = Mathf.Round(y / 2f) * 2f;

        return new Vector3(x, y, 0f);
    }

    private bool ValidateSetup()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            Debug.LogError("LevelGeneration: Add at least one obstacle prefab.");
            return false;
        }

        if (obstacleWeights == null)
        {
            obstacleWeights = new List<float>();
        }

        while (obstacleWeights.Count < obstaclePrefabs.Length)
        {
            obstacleWeights.Add(1f);
        }

        if (obstacleWeights.Count > obstaclePrefabs.Length)
        {
            obstacleWeights.RemoveRange(obstaclePrefabs.Length, obstacleWeights.Count - obstaclePrefabs.Length);
        }

        return true;
    }

    private int ResolveSeed()
    {
        if (useFixedSeed)
        {
            return fixedSeed;
        }

        return System.Environment.TickCount;
    }

    private static void DestroyObject(GameObject target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
            return;
        }

        DestroyImmediate(target);
    }
}
