using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HexMapGenerator : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private MapConfiguration config;
    [SerializeField] private BiomeDefinition[] biomeTable;
    [SerializeField] private CityDefinition[] cityTable;

    [SerializeField] private Tilemap targetTilemap;

    [SerializeField, Range(0f, 1f)] private float cityAmount = 0.7f;

    [SerializeField] private int seed = 0;

    private Dictionary<HexCoordinates, HexCell> grid = new Dictionary<HexCoordinates, HexCell>();
    private List<HexCoordinates> cityCenters = new List<HexCoordinates>();

    [ExecuteAlways]
    private void Update()
    {
        Random.InitState(seed);
    }

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        grid.Clear();
        Debug.Log($"Starting Generation: {config.MapRadius * 2}x{config.MapRadius * 2} Grid");

        // Loop through axial coordinates
        // q = x-axis (diagonal), r = z-axis (row)
        for (int q = -config.MapRadius; q <= config.MapRadius; q++)
        {
            int r1 = Mathf.Max(-config.MapRadius, -q - config.MapRadius);
            int r2 = Mathf.Min(config.MapRadius, -q + config.MapRadius);

            for (int r = r1; r <= r2; r++)
            {
                CreateCell(new HexCoordinates(q, r));
            }
        }

        GenerateCityRoads();
        PlaceBuildings();

        Debug.Log($"Generated {grid.Count} tiles.");
    }

    private void CreateCell(HexCoordinates coords)
    {
        // 1. Calculate Noise
        // We offset by a large number to avoid symmetry at (0,0)
        Vector2 samplePos = coords.ToWorldPos(config.HexSize) * config.NoiseScale;

        // Elevation: Defines Shape (Water vs Land vs Mountain)
        float elevation = Mathf.PerlinNoise(samplePos.x + config.Offset.x, samplePos.y + config.Offset.y);

        // Moisture: Defines Type (Desert vs Forest)
        // We use a different offset/scale for moisture to ensure deserts can be hilly or flat
        float moisture = Mathf.PerlinNoise((samplePos.x + 5000) * 0.8f, (samplePos.y + 5000) * 0.8f);

        float cityNoise = Mathf.PerlinNoise(samplePos.x + -5000, samplePos.y + -5000);

        // 2. Determine Biome
        BiomeType selectedBiome = EvaluateBiome(elevation, moisture);

        // 3. Create Data Object
        HexCell cell = new HexCell
        {
            Coordinates = coords,
            HeightValue = elevation,
            MoistureValue = moisture,
            CurrentBiome = selectedBiome,
            IsRoad = false,
            HasBuilding = false,
            IsCityZone = cityNoise > cityAmount
        };

        if (cityNoise > 0.85f && cell.CurrentBiome != BiomeType.Water && cell.CurrentBiome != BiomeType.Mountain)
        {
            cityCenters.Add(coords);
        }

        grid.Add(coords, cell);
    }

    private BiomeType EvaluateBiome(float height, float moisture)
    {
        // Special Case: Volcanoes (Rare, High, Dry-ish or Random)
        // Let's say Volcano is very high altitude
        if (height > 0.90f) return BiomeType.Volcano;

        // Layer 1: Water
        if (height < 0.35f) return BiomeType.Water;

        // Layer 2: Land Logic
        if (height < 0.6f) // Lowlands
        {
            if (moisture < 0.5f) return BiomeType.Sand;
            return BiomeType.Jungle; // High moisture lowlands
        }
        else if (height < 0.80f) // Highlands
        {
            if (moisture < 0.6f) return BiomeType.Forest;
            return BiomeType.Snow; // Dense forest
        }

        // Layer 3: Peaks
        return BiomeType.Mountain; // Snowy peaks
    }

    [ContextMenu("Draw Map")]
    public void DrawMap()
    {
        targetTilemap.ClearAllTiles();

        foreach (var currentCell in grid)
        {
            HexCell cell = currentCell.Value;

            // Convert Axial q,r to Unity Tilemap coordinates
            // Note: Unity's Hex Tilemap uses (col, row) which maps slightly differently depending on layout
            // Usually for Pointy Top: Vector3Int(q, r, 0) works directly if configured right.
            Vector3Int tilePos = new Vector3Int(cell.Coordinates.q, cell.Coordinates.r, 0);

            // Find the visual asset
            TileBase tileToRender = FindTileForBiome(cell.CurrentBiome);
            
            if (cell.HasBuilding)
                tileToRender = FindTileForCity();

            targetTilemap.SetTile(tilePos, tileToRender);
        }
    }

    private TileBase FindTileForBiome(BiomeType type)
    {
        // Simple lookup - optimization: use a Dictionary<BiomeType, TileBase> cache in Start()
        foreach (var def in biomeTable)
        {
            if (def.Type == type) return def.HexTile;
        }
        return null;
    }

    private TileBase FindTileForCity()
    {
        int random = Random.Range(0, 10);

        if (random == 0) return cityTable[0].HexTile; // 1/11
        if (random <= 2) return cityTable[1].HexTile; // 2/11
        if (random <= 5) return cityTable[2].HexTile; // 3/11
        if (random <= 10) return cityTable[3].HexTile; // 5/11

        return null;
    }

    private void GenerateCityRoads()
    {
        // 1. Pick a few random centers to start cities from
        // (In a real game, you'd filter this list closer to prevent overlapping cities)
        foreach (var center in cityCenters)
        {
            // Only start if not already built
            if (grid[center].IsRoad) continue;

            // Start the Crawler
            Queue<HexCoordinates> frontier = new Queue<HexCoordinates>();
            frontier.Enqueue(center);
            grid[center].IsRoad = true;

            int roadBudget = 50; // Max roads per city (prevents infinite loops)
            int currentRoads = 0;

            while (frontier.Count > 0 && currentRoads < roadBudget)
            {
                HexCoordinates current = frontier.Dequeue();

                // Try to grow in random directions
                for (int i = 0; i < 6; i++)
                {
                    // 30% Chance to build a road in this direction (Organic Randomness)
                    if (UnityEngine.Random.value > 0.15f) continue;

                    HexCoordinates neighborCoords = current.GetNeighbor(i);

                    // Validation Checks (The "Constraints")
                    if (!grid.ContainsKey(neighborCoords)) continue; // Edge of map
                    HexCell neighbor = grid[neighborCoords];

                    if (neighbor.IsRoad) continue; // Already a road
                    if (!neighbor.IsCityZone) continue; // Don't build outside the noise blob
                    if (neighbor.CurrentBiome == BiomeType.Water || neighbor.CurrentBiome == BiomeType.Mountain) continue;

                    // Build Road
                    neighbor.IsRoad = true;
                    currentRoads++;
                    neighbor.CurrentBiome = BiomeType.Ground;

                    // Add to queue to grow from here (Branching)
                    frontier.Enqueue(neighborCoords);
                }
            }
        }
    }

    private void PlaceBuildings()
    {
        foreach (var kvp in grid)
        {
            HexCell cell = kvp.Value;

            // Skip if it's water, mountain, or already a road
            if (cell.CurrentBiome == BiomeType.Water || cell.CurrentBiome == BiomeType.Mountain) continue;
            if (cell.IsRoad) continue;

            // Only build in the City Zone
            if (!cell.IsCityZone) continue;

            // Check Neighbors: Am I next to a Road?
            bool isNextToRoad = false;
            for (int i = 0; i < 6; i++)
            {
                if (grid.TryGetValue(cell.Coordinates.GetNeighbor(i), out HexCell neighbor))
                {
                    if (neighbor.IsRoad)
                    {
                        isNextToRoad = true;
                        break;
                    }
                }
            }

            // Spawn Logic
            if (isNextToRoad)
            {
                // 70% chance to build a house, 30% chance to leave an empty yard
                if (UnityEngine.Random.value < 0.7f)
                {
                    cell.HasBuilding = true;
                    cell.CurrentBiome = BiomeType.City; // This sets the visual to your Castle/Church tile
                }
            }
        }
    }
}