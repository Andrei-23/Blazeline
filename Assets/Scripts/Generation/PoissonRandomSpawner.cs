using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PoissonRandomSpawner : BaseGenerator
{

    [Header("Object")]
    [SerializeField] private GameObject prefab;
    [SerializeField][Min(1)] private int boxWidth = 1;
    [SerializeField][Min(1)] private int boxHeight = 1;

    [Header("Generation")]
    [SerializeField] private Transform objectParentTransform;
    [SerializeField] private int requiredAmount = 10;
    [SerializeField] private float pointDistance = 75f;
    [SerializeField] private float pointMaxExtraDistance = 50f;
    [SerializeField] private float spawnRadiusCells = 220f;
    [SerializeField] private float clearCenterRadiusCells = 75f;
    [SerializeField] private int firstPositionAttempts = 1000;
    [SerializeField] private int attemptsPerPoint = 20;

    
    [Header("Seed")]
    [SerializeField] private bool useFixedSeed = true;
    [SerializeField] private int fixedSeed = 12345;
    [SerializeField] private int lastUsedSeed;

    // private List<GameObject> generatedObjects;

    private int levelW => GenerationController.Instance.levelW;
    private Vector2Int levelCenter => GenerationController.Instance.levelCenter;
    private bool [,] solidCells => GenerationController.Instance.solidCells;

    [ContextMenu("Clean Up")]
    public override void CleanUp()
    {
        // foreach(var go in generatedObjects)
        // {
        //     if(go != null)
        //     {
        //         DestroySafe(go);
        //     }
        // }
        ClearObjectRoot();
    }
    private void ClearObjectRoot()
    {
        if (objectParentTransform == null)
        {
            return;
        }

        for (int i = objectParentTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = objectParentTransform.GetChild(i);
            if (child != null)
            {
                DestroySafe(child.gameObject);
            }
        }
    }
    public override void Generate()
    {
        GenerateList();
    }

    public List<GameObject> GenerateList()
    {
        return GenerateList(true);
    }
    public List<GameObject> GenerateList(bool useSeed)
    {
        UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
        if(useSeed){
            lastUsedSeed = ResolveSeed();
            UnityEngine.Random.InitState(lastUsedSeed);
        }

        List<Vector2> active = new();
        List<Vector2> points = new();
        var result = new List<GameObject>();

        Vector2Int first = ChooseFirstPos();
        active.Add(first);
        points.Add(first);

        while(active.Count > 0)
        {
            int pointId = UnityEngine.Random.Range(0, active.Count);
            Vector2 cur = active[pointId];

            bool found = false;
            for (int i = 0; i < attemptsPerPoint; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2);
                float dist = pointDistance + Random.Range(0f, pointMaxExtraDistance);

                Vector2 candidate = cur + new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) * dist;

                Vector2Int gridPos = new Vector2Int(
                    Mathf.RoundToInt(candidate.x),
                    Mathf.RoundToInt(candidate.y)
                );

                if (!IsInside(gridPos)) continue;
                if (!CanPlace(gridPos)) continue;

                bool tooClose = false;
                foreach(var p1 in points)
                {
                    if((p1 - candidate).magnitude < pointDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                // успех
                active.Add(candidate);
                points.Add(candidate);

                Place(gridPos);

                found = true;
                break;
            }

            if (!found)
            {
                active.RemoveAt(pointId);
            }
        }

        if(requiredAmount != 0)
        {
            if(points.Count < requiredAmount)
            {
                Debug.LogError("Not enough objects!");
                return new();
            }
            
            ListExtensions.Shuffle(points);
            while(points.Count > requiredAmount)
            {
                points.RemoveAt(points.Count - 1);
            }
        }
        else
        {
            ListExtensions.Shuffle(points);
        }

        foreach(var p in points)
        {
            Vector2Int gridPos = new Vector2Int(
                Mathf.RoundToInt(p.x),
                Mathf.RoundToInt(p.y)
            );
            var go = InstantiateObject(gridPos);
            result.Add(go);
        }
        
        if (useSeed)
            UnityEngine.Random.state = previousRandomState;
        return result;
    }

    private Vector2Int ChooseFirstPos()
    {
        for(int i = 0; i < firstPositionAttempts; i++)
        {
            int x = Random.Range(0, levelW);
            int y = Random.Range(0, levelW);
            Vector2Int p = new Vector2Int(x, y);
            if(CanPlace(p)) return p;
        }
        Debug.LogError("Failed to find first position for Poisson Algorithm");
        return Vector2Int.one * 100;
    }

    // p is bottom left corner
    private bool CanPlace(Vector2Int p)
    {
        for(int x = 0; x < boxWidth; x++)
        {
            for(int y = 0; y < boxHeight; y++)
            {
                if(!IsInside(p)) return false;
                if(solidCells[p.x + x, p.y + y]) return false;
            }
        }
        return true;
    }
    private bool IsInside(Vector2Int p)
    {
        p -= levelCenter;
        float r = p.magnitude;
        return r >= clearCenterRadiusCells && r <= spawnRadiusCells;
    }

    // p is bottom left corner
    private void Place(Vector2Int p)
    {
        for(int x = 0; x < boxWidth; x++)
        {
            for(int y = 0; y < boxHeight; y++)
            {
                if(!IsInside(p)) Debug.LogError("box outside the level");
                solidCells[p.x + x, p.y + y] = true;
            }
        }
    }

    private GameObject InstantiateObject(Vector2Int p)
    {
        Vector2 center = new Vector2(p.x + boxWidth / 2f, p.y + boxHeight / 2f);
        center -= levelCenter;
        center *= GenerationController.Instance.cellSize;
        return Instantiate(prefab, center, Quaternion.identity, objectParentTransform);
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

}
