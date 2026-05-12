using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MapUI : MonoBehaviour
{
    [System.Serializable]
    private struct MapObjectIconBinding
    {
        public MapDataManager.MapObjectType objectType;
        public Sprite iconSprite;
    }

    [SerializeField] private bool isMiniMap;
    
    [Header("References")]
    [SerializeField] private MapDataManager mapDataManager;
    [SerializeField] private Transform trackedWorldTarget;
    [SerializeField] private RectTransform mapViewportRect;
    [SerializeField] private RectTransform mapImageRect;
    [SerializeField] private MapIcon mapIconPrefab;

    [Header("View")]
    [SerializeField] private float zoom = 2f;
    [SerializeField] private float minZoom = 1f;
    [SerializeField] private float maxZoom = 8f;
    [SerializeField] private bool invertY = false;

    [Header("Icons")]
    [SerializeField] private List<MapObjectIconBinding> iconBindings = new List<MapObjectIconBinding>();
    [SerializeField] private Sprite defaultIconSprite;

    private readonly Dictionary<string, MapIcon> activeIcons = new Dictionary<string, MapIcon>();
    private readonly Dictionary<MapDataManager.MapObjectType, Sprite> iconByType =
        new Dictionary<MapDataManager.MapObjectType, Sprite>();

    private void Awake()
    {
        EnsureCenteredRectTransform(mapImageRect);
        CacheIconBindings();
    }

    private void OnValidate()
    {
        CacheIconBindings();
    }

    private void Update()
    {
        if (!ValidateReferences())
        {
            return;
        }

        zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        Vector2 centerWorldXY = GetCenterWorldXY();

        UpdateMapImageTransform(centerWorldXY, zoom);
        UpdateIcons(centerWorldXY, zoom);
    }

    public void SetZoom(float newZoom)
    {
        zoom = Mathf.Clamp(newZoom, minZoom, maxZoom);
    }

    public void AddZoom(float delta)
    {
        SetZoom(zoom + delta);
    }

    private void CacheIconBindings()
    {
        iconByType.Clear();
        for (int i = 0; i < iconBindings.Count; i++)
        {
            if (iconByType.ContainsKey(iconBindings[i].objectType))
            {
                continue;
            }

            iconByType.Add(iconBindings[i].objectType, iconBindings[i].iconSprite);
        }
    }

    private bool ValidateReferences()
    {
        return mapDataManager != null &&
               trackedWorldTarget != null &&
               mapViewportRect != null &&
               mapImageRect != null &&
               mapIconPrefab != null;
    }

    private Vector2 GetCenterWorldXY()
    {
        Vector2 worldMin = mapDataManager.WorldMin;
        Vector2 worldMax = mapDataManager.WorldMax;

        if (isMiniMap)
        {
            Vector2 target = trackedWorldTarget.position;
            float clampedX = Mathf.Clamp(target.x, worldMin.x, worldMax.x);
            float clampedY = Mathf.Clamp(target.y, worldMin.y, worldMax.y);
            return new Vector2(clampedX, clampedY);
        }
        else
        {
            return Vector2.zero;
        }
    }

    private void UpdateMapImageTransform(Vector2 centerWorldXY, float currentZoom)
    {
        Vector2 normalizedCenter = mapDataManager.NormalizeWorldPosition(new Vector3(centerWorldXY.x, centerWorldXY.y, 0f));
        normalizedCenter = ApplyYAxis(normalizedCenter);

        Vector2 viewportSize = mapViewportRect.rect.size;
        Vector2 scaledMapSize = viewportSize * currentZoom;

        mapImageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, scaledMapSize.x);
        mapImageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, scaledMapSize.y);

        Vector2 offset = new Vector2(
            (normalizedCenter.x - 0.5f) * scaledMapSize.x,
            (normalizedCenter.y - 0.5f) * scaledMapSize.y
        );

        mapImageRect.anchoredPosition = -offset;
    }

    private void UpdateIcons(Vector2 centerWorldXY, float currentZoom)
    {
        HashSet<string> visibleIds = new HashSet<string>();
        IReadOnlyCollection<MapDataManager.MapObjectData> mapObjects = mapDataManager.MapObjects;
        Vector2 mapSize = mapImageRect.rect.size;

        foreach (MapDataManager.MapObjectData data in mapObjects)
        {
            if (data == null || !data.Discovered)
            {
                continue;
            }

            if (!mapDataManager.IsVisibleAtCenter(data.WorldPosition, centerWorldXY, currentZoom))
            {
                continue;
            }

            if(data is MapDataManager.ObeliskMapObjectData obeliskData)
            {
                if(!obeliskData.IsActive) continue;
            }

            visibleIds.Add(data.Id);
            MapIcon icon = GetOrCreateIcon(data);

            Vector2 normalized = mapDataManager.NormalizeWorldPosition(data.WorldPosition);
            normalized = ApplyYAxis(normalized);

            Vector2 iconPosition = new Vector2(
                (normalized.x - 0.5f) * mapSize.x,
                (normalized.y - 0.5f) * mapSize.y
            );

            icon.SetAnchoredPosition(iconPosition);
            icon.SetZoom(currentZoom);
        }

        CleanupHiddenIcons(visibleIds);
    }

    private MapIcon GetOrCreateIcon(MapDataManager.MapObjectData data)
    {
        if (activeIcons.TryGetValue(data.Id, out MapIcon existingIcon) && existingIcon != null)
        {
            return existingIcon;
        }

        MapIcon spawnedIcon = Instantiate(mapIconPrefab, mapImageRect);
        spawnedIcon.Initialize(data, GetIconSprite(data.ObjectType));
        activeIcons[data.Id] = spawnedIcon;
        return spawnedIcon;
    }

    private Sprite GetIconSprite(MapDataManager.MapObjectType objectType)
    {
        if (iconByType.TryGetValue(objectType, out Sprite sprite) && sprite != null)
        {
            return sprite;
        }

        return defaultIconSprite;
    }

    private void CleanupHiddenIcons(HashSet<string> visibleIds)
    {
        if (activeIcons.Count == 0)
        {
            return;
        }

        List<string> toRemove = new List<string>();
        foreach (KeyValuePair<string, MapIcon> pair in activeIcons)
        {
            if (visibleIds.Contains(pair.Key))
            {
                continue;
            }

            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }

            toRemove.Add(pair.Key);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            activeIcons.Remove(toRemove[i]);
        }
    }

    private Vector2 ApplyYAxis(Vector2 normalized)
    {
        if (invertY)
        {
            normalized.y = 1f - normalized.y;
        }

        return normalized;
    }

    private void EnsureCenteredRectTransform(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }
}
