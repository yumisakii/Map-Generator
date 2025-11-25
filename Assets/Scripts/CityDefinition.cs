using UnityEngine;
using UnityEngine.Tilemaps;

public enum CityType
{
    Castle, Church, School, Hospital
}

[System.Serializable]
public struct CityDefinition
{
    public string Name;
    public CityType Type;

    [Header("Visuals")]
    public TileBase HexTile;

    [Header("Generation Thresholds")]
    [Range(0, 1)] public float MinHeight;
    [Range(0, 1)] public float MinMoisture;

    // Optional: Add a "Cost" for pathfinding later (e.g., Mountains = 100)
    public int MovementCost;
}