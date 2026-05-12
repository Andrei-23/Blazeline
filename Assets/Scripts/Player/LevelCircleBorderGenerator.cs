using UnityEngine;

public class LevelCircleBorderGenerator : MonoBehaviour
{
    [Header("Collider")]
    [SerializeField, Min(0.01f)] private float circleArenaRadius = 1f;
    [SerializeField, Min(0.01f)] private float outerBoxRadius = 1f;
    [SerializeField, Min(3)] private int colliderEdgeCount = 32;
    [SerializeField] private PolygonCollider2D polygonCollider;

    [Header("Pillars")]
    [SerializeField] private bool geneartePillars;
    [SerializeField] private GameObject pillarPrefab;
    [SerializeField, Min(1)] private int pillarCount = 12;
    [SerializeField, Min(0.01f)] private float pillarRadius = 1.25f;

    private readonly System.Collections.Generic.List<GameObject> spawnedPillars = new System.Collections.Generic.List<GameObject>();

    private void Awake()
    {
        EnsureColliderReference();
    }

    [ContextMenu("Generate Circle Border")]
    public void Generate()
    {
        EnsureColliderReference();
        GenerateCollider();
        if(geneartePillars){
            GeneratePillars();
        }
    }

    private void GenerateCollider()
    {
        int pointCount = Mathf.Max(3, colliderEdgeCount);
        Vector2[] points = new Vector2[pointCount + 7];
        float step = -Mathf.PI * 2f / pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float angle = i * step;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * circleArenaRadius;
        }

        if(outerBoxRadius < circleArenaRadius){
            Debug.LogWarning("Box radius was changed because is was too small");
            outerBoxRadius = circleArenaRadius;
        }
        points[pointCount] = new Vector2(outerBoxRadius, 0f);
        points[pointCount + 1] = new Vector2(outerBoxRadius, outerBoxRadius);
        points[pointCount + 2] = new Vector2(-outerBoxRadius, outerBoxRadius);
        points[pointCount + 3] = new Vector2(-outerBoxRadius, -outerBoxRadius);
        points[pointCount + 4] = new Vector2(outerBoxRadius, -outerBoxRadius);
        points[pointCount + 5] = new Vector2(outerBoxRadius, 0f);
        points[pointCount + 6] = points[pointCount - 1];

        polygonCollider.points = points;
    }

    private void GeneratePillars()
    {
        ClearGeneratedPillars();
        if (pillarPrefab == null)
        {
            return;
        }

        int count = Mathf.Max(1, pillarCount);
        float step = Mathf.PI * 2f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * step;
            Vector3 localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * pillarRadius;
            Vector3 directionToCenter = (-localPosition).normalized;
            Quaternion localRotation = Quaternion.FromToRotation(Vector3.up, directionToCenter);

            GameObject pillarInstance = Instantiate(pillarPrefab, transform);
            pillarInstance.transform.localPosition = localPosition;
            pillarInstance.transform.localRotation = localRotation;
            spawnedPillars.Add(pillarInstance);
        }
    }

    private void EnsureColliderReference()
    {
        if (polygonCollider == null)
        {
            polygonCollider = GetComponent<PolygonCollider2D>();
        }
    }

    private void ClearGeneratedPillars()
    {
        for (int i = spawnedPillars.Count - 1; i >= 0; i--)
        {
            GameObject spawnedPillar = spawnedPillars[i];
            if (spawnedPillar == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(spawnedPillar);
            }
            else
            {
                DestroyImmediate(spawnedPillar);
            }
        }

        spawnedPillars.Clear();
    }
}
