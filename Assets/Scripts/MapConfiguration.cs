using UnityEngine;

[CreateAssetMenu(menuName = "PCG/MapConfig")]
public class MapConfiguration : ScriptableObject
{
    [Header("Grid Settings")]
    public int MapRadius = 250;
    public float HexSize = 1f;

    [Header("Noise Settings")]
    public float NoiseScale = 0.1f;
    public int Octaves = 4;
    public Vector2 Offset;

    [Header("Biome Thresholds")]
    public BiomeDefinition[] Biomes; // Struct linking thresholds to Visual Assets
}