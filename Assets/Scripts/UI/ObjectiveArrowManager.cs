using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ObjectiveArrowManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private ObjectiveManager objectiveManager;

    // [Header("Visual")]
    // [SerializeField] private float circleRadius = 250f;

    [Header("Portal Arrow Settings")]
    [SerializeField] private GameObject portalArrowPrefab;
    [SerializeField] private RectTransform portalContainer;
    [SerializeField] private float portalFadeMinDistance = 50f;
    [SerializeField] private float portalFadeMaxDistance = 200f;
    [SerializeField] private float portalFadeMinAplha = 0.5f;

    [Header("Obelisk Arrow Settings")]
    [SerializeField] private GameObject obeliskArrowPrefab;
    [SerializeField] private RectTransform obeliskContainer;
    [SerializeField] private float obeliskFadeMinDistance = 50f;
    [SerializeField] private float obeliskFadeMaxDistance = 100f;

    private readonly List<GameObject> activeIndicators = new();

    private void Update()
    {
        ClearIndicators();

        if (objectiveManager == null || targetCamera == null)
            return;

        DrawIndicators(objectiveManager.GetActivePortals(), true);
        DrawIndicators(objectiveManager.GetActiveObelisks(), false);
    }

    private void DrawIndicators<T>(
        List<T> targets,
        bool isPortal
    ) where T : MonoBehaviour
    {
        if (targets == null)
            return;

        foreach (var target in targets)
        {
            if (target == null)
                continue;

            Vector2 worldPos = target.transform.position;
            Vector2 cameraPos = targetCamera.transform.position;
            
            float distance = Vector2.Distance(cameraPos, worldPos);

            float alpha;
            if (isPortal)
            {
                float x = Mathf.InverseLerp(portalFadeMinDistance, portalFadeMaxDistance, distance);
                alpha = Mathf.Lerp(1f, 0.5f, x);
            }
            else
            {
                float x = Mathf.InverseLerp(obeliskFadeMinDistance, obeliskFadeMaxDistance, distance);
                if(x == 1f) continue;
                alpha = 1f - x;
            }

            Vector3 viewportPos = targetCamera.WorldToViewportPoint(worldPos);

            bool visible =
                // viewportPos.z > 0 &&
                viewportPos.x > 0 &&
                viewportPos.x < 1 &&
                viewportPos.y > 0 &&
                viewportPos.y < 1;

            if (visible)
                continue;

            CreateIndicator(worldPos, alpha, isPortal);
        }
    }

    private void CreateIndicator(
        Vector3 worldPos,
        float alpha,
        bool isPortal
    )
    {
        var obj = Instantiate(
            isPortal ? portalArrowPrefab : obeliskArrowPrefab,
            isPortal ? portalContainer : obeliskContainer
        );
        ObjectiveArrow arrow = obj?.GetComponent<ObjectiveArrow>();
        activeIndicators.Add(obj);

        Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

        // if (screenPos.z < 0)
        // {
        //     screenPos *= -1f;
        // }

        Vector2 screenCenter = new(
            Screen.width * 0.5f,
            Screen.height * 0.5f
        );

        Vector2 direction = ((Vector2)screenPos - screenCenter).normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrow.SetRotation(angle);
        arrow.SetAlpha(alpha);
    }

    private void ClearIndicators()
    {
        foreach (var img in activeIndicators)
        {
            if (img != null)
                Destroy(img);
        }

        activeIndicators.Clear();
    }
}