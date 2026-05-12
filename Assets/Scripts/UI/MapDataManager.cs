using UnityEngine;
using System;
using System.Collections.Generic;

public class MapDataManager : MonoBehaviour
{
    public enum MapObjectType
    {
        Question,
        Danger,
        Portal,
        Obelisk,
    }

    public abstract class MapObjectData
    {
        public string Id { get; protected set; }
        public string DisplayName { get; protected set; }
        public MapObjectType ObjectType { get; protected set; }
        public Vector3 WorldPosition { get; protected set; }
        public bool Discovered { get; protected set; }

        protected MapObjectData(string id, string displayName, MapObjectType objectType, Vector3 worldPosition, bool discovered)
        {
            Id = id;
            DisplayName = displayName;
            ObjectType = objectType;
            WorldPosition = worldPosition;
            Discovered = discovered;
        }

        public void SetWorldPosition(Vector3 worldPosition)
        {
            WorldPosition = worldPosition;
        }
    }

    public sealed class QuestionMapObjectData : MapObjectData
    {
        public QuestionMapObjectData(string id, string displayName, Vector3 worldPosition, bool discovered = true)
            : base(id, displayName, MapObjectType.Question, worldPosition, discovered)
        {
        }
    }

    public sealed class DangerMapObjectData : MapObjectData
    {
        public DangerMapObjectData(string id, string displayName, Vector3 worldPosition, bool discovered = true)
            : base(id, displayName, MapObjectType.Danger, worldPosition, discovered)
        {
        }
    }

    public sealed class PortalMapObjectData : MapObjectData
    {
        public DateTime TimeOfCreation { get; }

        public PortalMapObjectData(string id, string displayName, Vector3 worldPosition, DateTime timeOfCreation, bool discovered = true)
            : base(id, displayName, MapObjectType.Portal, worldPosition, discovered)
        {
            TimeOfCreation = timeOfCreation;
        }
    }

    public sealed class ObeliskMapObjectData : MapObjectData
    {
        public bool IsActive { get; }

        public ObeliskMapObjectData(string id, string displayName, Vector3 worldPosition, bool isActive, bool discovered = true)
            : base(id, displayName, MapObjectType.Obelisk, worldPosition, discovered)
        {
            IsActive = isActive;
        }
    }

    [Header("World Bounds (XZ)")]
    [SerializeField] private Vector2 worldMin = new Vector2(-100f, -100f);
    [SerializeField] private Vector2 worldMax = new Vector2(100f, 100f);

    [Header("Test Data")]
    [SerializeField] private bool initializeTestDataOnStart = true;

    private readonly Dictionary<string, MapObjectData> mapObjectsById = new Dictionary<string, MapObjectData>();

    public Vector2 WorldMin => worldMin;
    public Vector2 WorldMax => worldMax;
    public IReadOnlyCollection<MapObjectData> MapObjects => mapObjectsById.Values;

    private void Start()
    {
        if (initializeTestDataOnStart)
        {
            InitializeTestMapObjects();
        }
    }

    /// <summary>
    /// Fills the map with sample POIs (for testing when inspector serialization is not used).
    /// Safe to call again — replaces the list.
    /// </summary>
    [ContextMenu("Initialize Test Map Objects")]
    public void InitializeTestMapObjects()
    {
        mapObjectsById.Clear();

        DateTime now = DateTime.UtcNow;

        AddIcon(new QuestionMapObjectData("test_q1", "Question A", new Vector3(-40f, 25f, 0f)));
        AddIcon(new DangerMapObjectData("test_d1", "Danger Zone", new Vector3(15f, -55f, 5f)));
        AddIcon(new PortalMapObjectData("test_p1", "Portal Alpha", new Vector3(60f, 10f, 0f), now.AddMinutes(-30)));
        AddIcon(new QuestionMapObjectData("test_q2", "Question B", new Vector3(-10f, -20f, 0f)));
        AddIcon(new PortalMapObjectData("test_p2", "Portal Beta", new Vector3(5f, 70f, 0f), now.AddHours(-2)));
    }

    public Vector2 NormalizeWorldPosition(Vector3 worldPosition)
    {
        float width = Mathf.Max(0.0001f, worldMax.x - worldMin.x);
        float height = Mathf.Max(0.0001f, worldMax.y - worldMin.y);

        float u = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPosition.x);
        float v = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPosition.y);
        return new Vector2(u, v);
    }

    public bool IsInsideWorldBounds(Vector3 worldPosition)
    {
        return worldPosition.x >= worldMin.x &&
               worldPosition.x <= worldMax.x &&
               worldPosition.y >= worldMin.y &&
               worldPosition.y <= worldMax.y;
    }

    public bool IsVisibleAtCenter(Vector3 poiWorldPosition, Vector2 centerWorldXY, float zoom)
    {
        float clampedZoom = Mathf.Max(1f, zoom);
        float worldWidth = Mathf.Max(0.0001f, worldMax.x - worldMin.x);
        float worldHeight = Mathf.Max(0.0001f, worldMax.y - worldMin.y);

        // intentionally increased mult from 2 to 1.5 -> better visuals on borders
        float halfVisibleWidth = worldWidth / (1.5f * clampedZoom); 
        float halfVisibleHeight = worldHeight / (1.5f * clampedZoom);

        return poiWorldPosition.x >= centerWorldXY.x - halfVisibleWidth &&
               poiWorldPosition.x <= centerWorldXY.x + halfVisibleWidth &&
               poiWorldPosition.y >= centerWorldXY.y - halfVisibleHeight &&
               poiWorldPosition.y <= centerWorldXY.y + halfVisibleHeight;
    }

    public bool AddIcon(MapObjectData mapObjectData)
    {
        if (mapObjectData == null || string.IsNullOrWhiteSpace(mapObjectData.Id))
        {
            return false;
        }

        if (mapObjectsById.ContainsKey(mapObjectData.Id))
        {
            return false;
        }

        mapObjectsById.Add(mapObjectData.Id, mapObjectData);
        return true;
    }

    public bool UpdateIconPosition(string id, Vector3 worldPosition)
    {
        if (!TryGetMapObjectById(id, out MapObjectData mapObjectData))
        {
            return false;
        }

        mapObjectData.SetWorldPosition(worldPosition);
        return true;
    }
    public bool UpdateObeliskIconActive(string id, bool isActive)
    {
        if (!TryGetMapObjectById(id, out MapObjectData mapObjectData))
        {
            return false;
        }

        var newData = new ObeliskMapObjectData(
            id,
            mapObjectData.DisplayName,
            mapObjectData.WorldPosition,
            isActive,
            true
        );
        mapObjectsById.Remove(id);
        mapObjectsById.Add(id, newData);
        return true;
    }

    public bool RemoveIcon(string id)
    {
        return mapObjectsById.Remove(id);
    }

    private bool TryGetMapObjectById(string id, out MapObjectData mapObjectData)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            mapObjectData = null;
            return false;
        }

        return mapObjectsById.TryGetValue(id, out mapObjectData);
    }
}
