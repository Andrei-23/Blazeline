using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Obstacle : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap wallsTilemap;
    [SerializeField] private Tilemap preservedAreaTilemap;

    // [Header("Collision")]
    // [SerializeField] private CompositeCollider2D wallsCompositeCollider;
    // [SerializeField] private CompositeCollider2D preservedAreaCompositeCollider;

    [Serializable]
    public enum Modifier
    {
        None,
        FlipX,
        FlipY,
        FlipXY,
        Rotate90,
        All,
    }
    // [Header("Random Modifiers")]
    // [SerializeField] public bool rotate = false;
    // [SerializeField] public bool flipX = false;
    // [SerializeField] public bool flipY = false;

    [Header("Other")]
    [SerializeField] public Modifier randomModifier = Modifier.All;
    [SerializeField] public float weight = 1f;
    [SerializeField] public GameObject obstacleExtraObject;
    [SerializeField] public bool useRectArea;
    [SerializeField] private TilemapRenderer areaRenderer;

    public bool rotate => randomModifier == Modifier.Rotate90 || randomModifier == Modifier.All;
    public bool flipX => (
        randomModifier == Modifier.FlipX || 
        randomModifier == Modifier.FlipXY || 
        randomModifier == Modifier.All);
    public bool flipY => (
        randomModifier == Modifier.FlipY || 
        randomModifier == Modifier.FlipXY);

    public Tilemap WallsTilemap => wallsTilemap;
    public Tilemap PreservedAreaTilemap => preservedAreaTilemap;

    public CompositeCollider2D WallsCompositeCollider => null;
    public CompositeCollider2D PreservedAreaCompositeCollider => null;

    public enum AreaTileType
    {
        None,
        HardBlock,   // аналог стены (нельзя ставить вообще)
        SoftBlock    // зона вокруг (area)
    }

    private void Start(){
        areaRenderer.enabled = false;
    }

    public void ApplyInverseRotationToTiles()
    {
        float rootZ = transform.eulerAngles.z;

        ApplyCustomOrientation(wallsTilemap, -rootZ);
        ApplyCustomOrientation(preservedAreaTilemap, -rootZ);
    }

    private static void ApplyCustomOrientation(Tilemap tilemap, float inverseAngle)
    {
        if (tilemap == null)
        {
            return;
        }

        tilemap.orientation = Tilemap.Orientation.Custom;
        tilemap.orientationMatrix = Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, inverseAngle));
    }
    private bool IsWallTile(TileBase tile)
    {
        if (tile == null) return false;
        // TODO
        return true;
    }
    private AreaTileType GetAreaTileType(TileBase tile)
    {
        if (tile == null) return AreaTileType.None;

        // string name = tile.name.ToLower();

        // if (name.Contains("hard") || name.Contains("block"))
        //     return AreaTileType.HardBlock;

        // if (name.Contains("soft") || name.Contains("buffer") || name.Contains("preserve"))
        //     return AreaTileType.SoftBlock;

        // return AreaTileType.None;
        return AreaTileType.SoftBlock;
    }
    public (List<Vector2Int>, List<Vector2Int>, Vector2Int) CalculateShapeCells()
    {
        var solidCells = new List<Vector2Int>();
        var areaCells = new List<Vector2Int>();

        if (wallsTilemap != null)
        {
            BoundsInt bounds = wallsTilemap.cellBounds;
            // Vector3 centerPos3 = bounds.center;
            // centerPos = new Vector2Int(Mathf.FloorToInt(centerPos3.x), Mathf.FloorToInt(centerPos3.y));
            // int x_min = int.MaxValue, y_min = int.MaxValue;
            // int x_max = int.MinValue, y_max = int.MinValue;
            foreach (var pos in bounds.allPositionsWithin)
            {
                TileBase tile = wallsTilemap.GetTile(pos);
                if (tile == null) continue;

                if (!IsWallTile(tile)) continue;

                Vector2Int p = new Vector2Int(pos.x, pos.y);
                solidCells.Add(p); // center is close to (0, 0)
            }
        }

        if (preservedAreaTilemap != null)
        {
            BoundsInt bounds = preservedAreaTilemap.cellBounds;
            // Vector3 centerPos3 = bounds.center;
            // Vector2Int centerPos = new Vector2Int(Mathf.FloorToInt(centerPos3.x), Mathf.FloorToInt(centerPos3.y));

            foreach (var pos in bounds.allPositionsWithin)
            {
                TileBase tile = preservedAreaTilemap.GetTile(pos);
                if (tile == null) continue;

                var type = GetAreaTileType(tile);
                if (type == AreaTileType.None) continue;

                Vector2Int p = new Vector2Int(pos.x, pos.y);
                // p -= centerPos;

                if (type == AreaTileType.HardBlock)
                {
                    // считаем как стену
                    solidCells.Add(p);
                }
                else if (type == AreaTileType.SoftBlock)
                {
                    areaCells.Add(p);
                }
            }
        }

        Vector2Int bmin = new Vector2Int(int.MaxValue, int.MaxValue);
        Vector2Int bmax = new Vector2Int(int.MinValue, int.MinValue);

        foreach (var p in areaCells)
        {
            bmin = Vector2Int.Min(bmin, p);
            bmax = Vector2Int.Max(bmax, p);
        }

        if (bmin.x == int.MaxValue)
            bmin = bmax = Vector2Int.zero;
        
        Vector2Int center_pos = (bmin + bmax) / 2;

        for (int i = 0; i < solidCells.Count; i++)
            solidCells[i] -= center_pos;

        for (int i = 0; i < areaCells.Count; i++)
            areaCells[i] -= center_pos;

        var solidSet = new HashSet<Vector2Int>(solidCells);
        var areaSet = new HashSet<Vector2Int>(areaCells);

        // если клетка уже solid - не нужна в blocked
        areaSet.ExceptWith(solidSet);

        solidCells = new List<Vector2Int>(solidSet);
        areaCells = new List<Vector2Int>(areaSet);
        var result = (solidCells, areaCells, center_pos);
        return result;
    }
}
