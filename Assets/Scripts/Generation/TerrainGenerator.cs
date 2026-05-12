using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Tilemaps;

[ExecuteInEditMode]
public class TerrainGenerator : BaseGenerator
{
    public enum GeneralTileType
    {
        Mountain,
        Corridor,
        OutsideCorridor,
        // Lake,
    }
    public enum CorridorType
    {
        None,
        Deadend,
        Straight,
        Turn,
        Triple,
        Cross,
    }
    public enum MountainType
    {
        None,
        Full,
        Corner,
        Bump,
    }
    public enum CombinedTileType
    {
        None,
        MFull,
        MCorner,
        MBump,
        CDeadEnd,
        CStraight,
        CTurn,
        CTriple,
        CCross,
        CBump,
        CCornerSingle,
        CCornerDouble,
    }

    [Serializable]
    public class TileInfo
    {
        public Sprite sprite;
        public MountainType mType = MountainType.None;
        public CorridorType cType = CorridorType.None;
    }

    [Serializable]
    public class TileStructureList
    {
        public CombinedTileType type;
        public List<ChunkStructure> structures;
    }


    [SerializeField] private int chunkW = 24;

    [SerializeField] private Tilemap mountainTilemap;
    [SerializeField] private Tilemap corridorTilemap;

    [SerializeField] private List<TileInfo> tileInfo;

    [SerializeField] private Tilemap finalWallTilemap;
    [SerializeField] private TileRotation tileRotation;

    [SerializeField] private string structureAssetLabel;

    private Dictionary<CombinedTileType, List<GameObject>> tileStructures;
    private Dictionary<Vector2Int, GeneralTileType> tiles;

    public override void CleanUp()
    {
        tiles = new();
        tileStructures = new();
        finalWallTilemap.ClearAllTiles();
    }


    private void GetStructureAssets()
    {
        AsyncOperationHandle<IList<GameObject>> handle =
            Addressables.LoadAssetsAsync<GameObject>(
                structureAssetLabel,
                null
            );

        // Generate() is synchronous, so block until Addressables are loaded
        // to ensure tileStructures is ready before processing tiles.
        var result = handle.WaitForCompletion();
        if (result == null)
        {
            Debug.LogError($"Failed to load structure assets by label: {structureAssetLabel}");
            return;
        }

        var strObjList = new List<GameObject>(result);
        foreach (var go in strObjList)
        {
            var chunkStr = go?.GetComponent<ChunkStructure>();
            if (chunkStr == null) continue;
            var key = chunkStr.tileType;
            if (!tileStructures.ContainsKey(key))
                tileStructures.Add(key, new());
            tileStructures[key].Add(go);
        }

        Addressables.Release(handle);
    }

    public override void Generate()
    {
        GetStructureAssets();

        BoundsInt mb = mountainTilemap.cellBounds;
        BoundsInt cb = corridorTilemap.cellBounds;

        foreach(var p in mb.allPositionsWithin)
        {
            if (mountainTilemap.GetTile(p) == null) continue;

            Vector2Int p2 = new Vector2Int(p.x, p.y);
            tiles.Add(p2, GeneralTileType.Mountain);
        }

        foreach(var p in cb.allPositionsWithin)
        {
            if (corridorTilemap.GetTile(p) == null) continue;

            Vector2Int p2 = new Vector2Int(p.x, p.y);
            if (tiles.ContainsKey(p2))
                tiles[p2] = GeneralTileType.Corridor;
            else
                tiles[p2] = GeneralTileType.OutsideCorridor;
        }

        foreach(var p in tiles.Keys)
        {
            ProcessTile(p);
        }
    }

    private void ProcessTile(Vector2Int p)
    {
        Vector3Int p3 = new Vector3Int(p.x, p.y, 0);
        var curType = tiles[p];
        
        if(curType == GeneralTileType.OutsideCorridor) return;
        
        else
        {
            TileData data = new();

            TileBase mTile = mountainTilemap.GetTile(p3);
            if(mTile is not RuleTile mRuleTile)
            {
                Debug.LogError("Not a RuleTile");
                return;
            }
            
            mRuleTile.GetTileData(p3, mountainTilemap, ref data);
            MountainType mType = FindMountainType(data.sprite);
            int mRot = Mathf.RoundToInt(data.transform.rotation.eulerAngles.z / 90f);
            
            TileBase cTile = corridorTilemap.GetTile(p3);
            CorridorType cType = CorridorType.None;
            int cRot = 0;
            if(cTile != null)
            {
                if(cTile is not RuleTile cRuleTile)
                {
                    Debug.LogError("Not a RuleTile");
                    return;
                }
                
                cRuleTile.GetTileData(p3, corridorTilemap, ref data);
                cType = FindCorridorType(data.sprite);
                cRot = Mathf.RoundToInt(data.transform.rotation.eulerAngles.z / 90f);
            }

            var combType = GetCombinedTileType(mType, cType);
            var combRot = GetCombinedRotation(combType, mRot, cRot);
            bool flipX = false;
            if(combType == CombinedTileType.CCornerSingle)
            {   
                bool flipDR = false;
                if(cType == CorridorType.Deadend && mRot != cRot) flipDR = true;
                if(cType == CorridorType.Straight && (mRot % 2) == (cRot % 2)) flipDR = true;
                if (flipDR)
                {
                    combRot = (combRot + 1) % 4;
                    flipX = true;
                }
            }
            PlaceTile(combType, p, combRot, flipX);
        }
    }

    private MountainType FindMountainType(Sprite sprite)
    {
        for(int i = 0; i < tileInfo.Count; i++)
        {
            if(sprite == tileInfo[i].sprite)
            {
                return tileInfo[i].mType;
            }
        }
        return MountainType.None;
    }
    
    private CorridorType FindCorridorType(Sprite sprite)
    {
        for(int i = 0; i < tileInfo.Count; i++)
        {
            if(sprite == tileInfo[i].sprite)
            {
                return tileInfo[i].cType;
            }
        }
        return CorridorType.None;
    }

    private CombinedTileType GetCombinedTileType(MountainType mType, CorridorType cType)
    {
        if(mType == MountainType.None) return CombinedTileType.None;

        switch (mType)
        {
            case MountainType.None: return CombinedTileType.None;

            case MountainType.Bump:
                if(cType == CorridorType.None) return CombinedTileType.MBump;
                else return CombinedTileType.CBump;
            
            case MountainType.Full:
                switch (cType)
                {
                    case CorridorType.None: return CombinedTileType.MFull;
                    case CorridorType.Deadend: return CombinedTileType.CDeadEnd;
                    case CorridorType.Straight: return CombinedTileType.CStraight;
                    case CorridorType.Turn: return CombinedTileType.CTurn;
                    case CorridorType.Triple: return CombinedTileType.CTriple;
                    case CorridorType.Cross: return CombinedTileType.CCross;
                    default: return CombinedTileType.None;
                }

            case MountainType.Corner:
                switch (cType)
                {
                    case CorridorType.None: return CombinedTileType.MCorner;
                    case CorridorType.Deadend:
                    case CorridorType.Straight:
                        return CombinedTileType.CCornerSingle;
                    default: return CombinedTileType.CCornerDouble;
                }
            
            default: return CombinedTileType.None;
        }   
    }

    private int GetCombinedRotation(CombinedTileType combType, int mRot, int cRot)
    {
        switch (combType)
        {
            case CombinedTileType.CCornerSingle:
                return mRot; // check later
            
            case CombinedTileType.MCorner:
            case CombinedTileType.MBump:
                return mRot;
            
            case CombinedTileType.None:
            case CombinedTileType.MFull:
            case CombinedTileType.CCross:
                return 0;
            
            default:
                return cRot;
        }
    }

    private (ChunkStructure, int, bool) ChooseRandomStructure(CombinedTileType type)
    {
        if(type == CombinedTileType.None) return(null, 0, false);

        if(!tileStructures.ContainsKey(type)){
            Debug.LogError("type not found");
            return(null, 0, false);
        }

        int id = UnityEngine.Random.Range(0, tileStructures[type].Count);
        GameObject go = tileStructures[type][id];
        ChunkStructure structure = go?.GetComponent<ChunkStructure>();
        var(a, b) = structure.ChooseRandomModification();
        return(structure, a, b);

    }
    private void PlaceTile(CombinedTileType type, Vector2Int pos, int rot, bool flipX)
    {
        var(a, b, c) = ChooseRandomStructure(type);
        PlaceStructure(a, pos, b, c, rot, flipX);
    }

    private Vector2Int GetModifiedPos(Vector2Int pos, int rot90, bool flipX)
    {
        int neg(int v)
        {
            return -v - 1;
        }

        if(flipX) pos = new Vector2Int(neg(pos.x), pos.y);
        
        switch (rot90)
        {
            case 0: break;
            case 1: pos = new Vector2Int(neg(pos.y), pos.x); break;
            case 2: pos = new Vector2Int(neg(pos.x), neg(pos.y)); break;
            case 3: pos = new Vector2Int(pos.y, neg(pos.x)); break;
        }
        return pos;
    }
    private void PlaceStructure(ChunkStructure structure, Vector2Int chunkPos, int initRot, bool initFlipX, int rot, bool flipX)
    {   
        BoundsInt wBounds = structure.wallTilemap.cellBounds;
        foreach(Vector3Int p in wBounds.allPositionsWithin)
        {
            Vector2Int pStruct = new Vector2Int(p.x, p.y);
            Vector3Int structTilePos = new Vector3Int(pStruct.x, pStruct.y, 0);
            var curTile = structure.wallTilemap.GetTile(structTilePos);
            if(curTile == null) continue;

            Vector2Int p1 = GetModifiedPos(pStruct, initRot, initFlipX);
            Vector2Int p2 = GetModifiedPos(p1, rot, flipX);

            Vector2Int pGrid = chunkPos * chunkW + p2;
            pGrid += Vector2Int.one * (chunkW / 2);

            Vector2Int pTM = pGrid;
            pGrid += GenerationController.Instance.levelCenter;

            if(!GenerationController.Instance.IsInside(pGrid)) continue;
            GenerationController.Instance.SetSolidTileValue(pGrid, true);

            curTile = tileRotation.GetTileModification(curTile, initRot, initFlipX);
            curTile = tileRotation.GetTileModification(curTile, rot, flipX);

            Vector3Int gridTilePos = new Vector3Int(pTM.x, pTM.y, 0);
            finalWallTilemap.SetTile(gridTilePos, curTile);
        }

        BoundsInt aBounds = structure.areaTilemap.cellBounds;
        foreach(Vector3Int p in aBounds.allPositionsWithin)
        {
            Vector2Int pStruct = new Vector2Int(p.x, p.y);
            Vector3Int structTilePos = new Vector3Int(pStruct.x, pStruct.y, 0);
            var curTile = structure.areaTilemap.GetTile(structTilePos);
            if(curTile == null) continue;

            Vector2Int p1 = GetModifiedPos(pStruct, initRot, initFlipX);
            Vector2Int p2 = GetModifiedPos(p1, rot, flipX); 

            Vector2Int pGrid = chunkPos * chunkW + p2;
            pGrid += Vector2Int.one * (chunkW / 2);
            pGrid += GenerationController.Instance.levelCenter;
            if(!GenerationController.Instance.IsInside(pGrid)) continue;

            if(!GenerationController.Instance.solidCells[pGrid.x, pGrid.y])
                GenerationController.Instance.SetAreaTileValue(pGrid, true);
        }
        
    }
}
