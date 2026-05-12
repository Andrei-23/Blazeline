using System;
using System.Collections.Generic;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteInEditMode]
public class TileRotation : MonoBehaviour
{   
    [Serializable]
    public class TilePack
    {
        public List<TileBase> variations;
    }
    // tiles in a list: default, 90, 180, 270, flipX, X90, X180, X270
    [SerializeField] private List<TilePack> tileModificationPacks;
    private Dictionary<TileBase, int> tilePackDict;

    void OnValidate()
    {
        UpdateDict();
    }

    private void UpdateDict()
    {
        tilePackDict = new Dictionary<TileBase, int>();
        for(int i = 0; i < tileModificationPacks.Count; i++)
        {   
            if(tileModificationPacks[i].variations.Count > 8)
            {
                Debug.LogError("Too much tiles in a pack!");
                var vars = tileModificationPacks[i].variations;
                while(vars.Count > 8)
                {
                    vars.RemoveAt(vars.Count - 1);
                }
                tileModificationPacks[i].variations = vars;
            }
            if(tileModificationPacks[i].variations.Count > 8)
            {
                Debug.LogWarning("Not enough tiles in a pack");
            }

            for(int j = 0; j < Math.Max(8, tileModificationPacks[i].variations.Count); j++)
            {  
                var tile = tileModificationPacks[i].variations[j];
                if (tilePackDict.ContainsKey(tile))
                {
                    // Debug.LogWarning("Duplicate tile modification");
                    continue;
                }
                tilePackDict.Add(tile, i * 8 + j);
            }
        }
    }
    public bool CheckPack(int i)
    {
        var vars = tileModificationPacks[i].variations;
        int cnt = vars.Count;
        if(cnt == 8) return true;
        Debug.LogError("Incorrect variant amount");
        return false;
    }

    public TileBase GetTileModification(TileBase tile, int rot90, bool flipX)
    {
        if(rot90 == 0 && !flipX)
            return tile;
        if(!tilePackDict.ContainsKey(tile))
            return tile;
        
        if (tilePackDict == null)
        {
            Debug.LogError("Dictionary not initialized");
            return tile;
        }
        

        int val = tilePackDict[tile];
        int pack_id = val / 8;
        if (!CheckPack(pack_id))
        {
            return tile;
        }

        int x = val % 8;
        int new_rot90 = (rot90 + (x % 4) + ((flipX && ((x % 4) % 2 == 1)) ? 2 : 0)) % 4;
        bool new_flipX = flipX ^ (x >= 4);
        int x1 = new_rot90 + (new_flipX ? 4 : 0);
        
        return tileModificationPacks[pack_id].variations[x1];
    }

}
