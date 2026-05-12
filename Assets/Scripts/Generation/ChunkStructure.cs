using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkStructure : MonoBehaviour
{
    public enum InitialModification
    {
        None,
        flipX,
        flipY,
        flipXY,
        flipDR,
        rotate,
        rotateFlipX
    }
    [SerializeField] public TerrainGenerator.CombinedTileType tileType;
    [SerializeField] public InitialModification initialModification;
    [SerializeField] public Tilemap wallTilemap;
    [SerializeField] public Tilemap areaTilemap;

// public bool rotate => randomModifier == Modifier.Rotate90 || randomModifier == Modifier.All;
//     public bool flipX => (
//         randomModifier == Modifier.FlipX || 
//         randomModifier == Modifier.FlipXY || 
//         randomModifier == Modifier.All);
//     public bool flipY => (
//         randomModifier == Modifier.FlipY || 
//         randomModifier == Modifier.FlipXY);
    public (int, bool) ChooseRandomModification()
    {
        switch (initialModification)
        {
            case InitialModification.None: return (0, false);
            case InitialModification.flipDR:
                if (UnityEngine.Random.Range(0, 2) == 0) return (0, false);
                else return(1, true); // flip around DR corner
            case InitialModification.flipY:
                if (UnityEngine.Random.Range(0, 2) == 0) return (0, false);
                else return(2, true); // flip Y
            case InitialModification.flipXY:
                int r1 = UnityEngine.Random.Range(0, 2) * 2;
                bool f1 = UnityEngine.Random.Range(0, 2) == 0;
                return (r1, f1);
        }
        
        bool genRotate = (
            initialModification == InitialModification.rotate ||
            initialModification == InitialModification.rotateFlipX
        );
        bool genFlipX = (
            initialModification == InitialModification.flipX ||
            initialModification == InitialModification.rotateFlipX
        );

        int rot = genRotate ? UnityEngine.Random.Range(0, 4) : 0;
        bool flipX = genFlipX ? (UnityEngine.Random.Range(0, 2) == 0) : false;
        return new(rot, flipX);
    }
}
