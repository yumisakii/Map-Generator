[System.Serializable]
public class HexCell
{
    public HexCoordinates Coordinates;

    // Biome Data
    public float HeightValue;   // From Noise (0.0 to 1.0)
    public float MoistureValue; // From Noise (0.0 to 1.0)
    public BiomeType CurrentBiome;

    // City/Civilization Data
    public bool IsRoad;
    public bool HasBuilding;
    public bool IsCityZone;
    public CityDefinition BuildingDetails;
}