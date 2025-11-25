using UnityEngine;
using UnityEngine.Tilemaps;

public enum BiomeType
{
    Water, Sand, Ground, Forest, Jungle, Snow, Mountain, Volcano, City
}

[System.Serializable]
public struct BiomeDefinition
{
    public string Name;
    public BiomeType Type;

    [Header("Visuals")]
    public TileBase HexTile;

    [Header("Generation Thresholds")]
    [Range(0, 1)] public float MinHeight;
    [Range(0, 1)] public float MinMoisture;

    // Optional: Add a "Cost" for pathfinding later (e.g., Mountains = 100)
    public int MovementCost;
}