using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HexMapGenerator : MonoBehaviour
{
    [Header("Nature Settings")]
    [SerializeField] private float forestClumpScale = 0.2f;
    [SerializeField, Range(0f, 1f)] private float forestDensity = 0.4f;
    
    [Header("Generation Settings")]
    [SerializeField, Range(0.1f, 1f)] private float cityAmount = 0.75f;
    [SerializeField] private int minCitySpacing = 15;
    [SerializeField, Range(1, 6)] private int citySize = 4;
    [SerializeField] private int seed = 0;

    [Header("Offsets Settings")]
    [SerializeField] private float elevationOffset = 0f;
    [SerializeField] private float moistureOffset = 1000f;
    [SerializeField] private float cityOffset = -1000f;



    [Header("Configuration")]
    [SerializeField] private MapConfiguration config;
    [SerializeField] private BiomeDefinition[] biomeTable;
    [SerializeField] private CityDefinition[] cityTable;
    [SerializeField] private Tilemap targetTilemap;

    [Header("Visuals")]
    [SerializeField] private TileBase roadTile;
    [SerializeField] private TileBase defaultTile;



    private Dictionary<HexCoordinates, HexCell> grid = new Dictionary<HexCoordinates, HexCell>();
    private List<HexCoordinates> cityCenters = new List<HexCoordinates>();

    private class PathNode { public HexCoordinates Location; public PathNode Parent; public int G; public int H; public int F => G + H; }

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        grid.Clear();
        cityCenters.Clear();
        targetTilemap.ClearAllTiles();
        Random.InitState(seed);

        // 1. Base Terrain
        GenerateTerrain();

        // 2. Nature Pass (THE FIX: Breaks up empty areas)
        GenerateNatureClusters();

        // 3. City Centers
        PickCityLocations();

        // 4. Expand Cities
        ExpandCityCores();

        // 5. Roads
        GenerateHighways_AStar();

        // 6. Render
        DrawMap();

        Debug.Log($"Generated {grid.Count} tiles.");
    }

    private void GenerateTerrain()
    {
        Random.InitState(seed);
        for (int q = -config.MapRadius; q <= config.MapRadius; q++)
        {
            int r1 = Mathf.Max(-config.MapRadius, -q - config.MapRadius);
            int r2 = Mathf.Min(config.MapRadius, -q + config.MapRadius);
            for (int r = r1; r <= r2; r++)
            {
                HexCoordinates coords = new HexCoordinates(q, r);
                Vector2 samplePos = coords.ToWorldPos(config.HexSize) * config.NoiseScale;

                float elevation = Mathf.PerlinNoise(samplePos.x + elevationOffset, samplePos.y + elevationOffset);
                float moisture = Mathf.PerlinNoise(samplePos.x + moistureOffset, samplePos.y + moistureOffset);
                float cityNoise = Mathf.PerlinNoise((samplePos.x + cityOffset) * 0.5f, (samplePos.y + cityOffset) * 0.5f);

                HexCell cell = new HexCell
                {
                    Coordinates = coords,
                    HeightValue = elevation,
                    MoistureValue = moisture,
                    CurrentBiome = EvaluateBiome(elevation, moisture),
                    IsRoad = false,
                    HasBuilding = false,
                    IsCityZone = cityNoise > cityAmount
                };
                grid.Add(coords, cell);
            }
        }
    }

    private void GenerateNatureClusters()
    {
        foreach (var kvp in grid)
        {
            HexCell cell = kvp.Value;
            Vector2 pos = cell.Coordinates.ToWorldPos(config.HexSize);

            // 1. Forest Clumps on Ground
            // We use a different noise scale (higher frequency) to create small patches
            float detailNoise = Mathf.PerlinNoise(pos.x * forestClumpScale + seed, pos.y * forestClumpScale + seed);

            if (cell.CurrentBiome == BiomeType.Ground)
            {
                // If noise is high, turn this boring Ground into a Forest
                if (detailNoise > (1f - forestDensity))
                {
                    cell.CurrentBiome = BiomeType.Forest;
                }
                // Rare chance for a small Lake in the plains
                else if (detailNoise < 0.15f)
                {
                    cell.CurrentBiome = BiomeType.Water;
                }
            }
            // 2. Rocks/Oasis in Sand
            else if (cell.CurrentBiome == BiomeType.Sand)
            {
                // Add some "Mountains" (Rocks) to break up the desert dunes
                if (detailNoise > 0.85f)
                {
                    cell.CurrentBiome = BiomeType.Mountain;
                }
            }
        }
    }

    private void PickCityLocations()
    {
        List<HexCoordinates> candidates = new List<HexCoordinates>();
        foreach (var kvp in grid)
        {
            if (kvp.Value.IsCityZone &&
                kvp.Value.CurrentBiome != BiomeType.Water &&
                kvp.Value.CurrentBiome != BiomeType.Mountain)
            {
                candidates.Add(kvp.Key);
            }
        }

        candidates = candidates.OrderBy(x => Random.value).ToList();

        foreach (HexCoordinates candidate in candidates)
        {
            bool tooClose = false;
            foreach (HexCoordinates existingCity in cityCenters)
            {
                if (candidate.DistanceTo(existingCity) < minCitySpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                cityCenters.Add(candidate);
                grid[candidate].HasBuilding = true;
                grid[candidate].IsRoad = true;

                // Ensure the city sits on firm ground, not a random forest patch we just added
                grid[candidate].CurrentBiome = BiomeType.Ground;
            }
        }
    }

    private void ExpandCityCores()
    {
        foreach (var center in cityCenters)
        {
            int expandedCount = 0;
            List<int> directions = new List<int> { 0, 1, 2, 3, 4, 5 };
            directions = directions.OrderBy(x => Random.value).ToList();

            foreach (int dir in directions)
            {
                if (expandedCount >= citySize) break;
                HexCoordinates neighbor = center.GetNeighbor(dir);
                if (grid.TryGetValue(neighbor, out HexCell cell))
                {
                    if (cell.CurrentBiome != BiomeType.Water &&
                        cell.CurrentBiome != BiomeType.Mountain &&
                        !cell.HasBuilding)
                    {
                        cell.HasBuilding = true;
                        cell.IsRoad = true;
                        cell.CurrentBiome = BiomeType.Ground; // Clear forests under buildings
                        expandedCount++;
                    }
                }
            }
        }
    }

    private void GenerateHighways_AStar()
    {
        if (cityCenters.Count < 2) return;

        foreach (var start in cityCenters)
        {
            var nearest = cityCenters
                .Where(c => !c.Equals(start))
                .OrderBy(c => c.DistanceTo(start))
                .FirstOrDefault();

            if (cityCenters.Count > 1)
            {
                var path = GetPathAStar(start, nearest);
                if (path != null)
                {
                    foreach (var coord in path)
                    {
                        if (grid.ContainsKey(coord))
                        {
                            if (grid[coord].CurrentBiome == BiomeType.Water)
                                grid[coord].CurrentBiome = BiomeType.Ground;

                            grid[coord].IsRoad = true;
                            // Optional: Clear trees from roads
                            if (grid[coord].CurrentBiome == BiomeType.Forest)
                                grid[coord].CurrentBiome = BiomeType.Ground;
                        }
                    }
                }
            }
        }
    }

    private List<HexCoordinates> GetPathAStar(HexCoordinates start, HexCoordinates end)
    {
        List<PathNode> openList = new List<PathNode>();
        HashSet<HexCoordinates> closedList = new HashSet<HexCoordinates>();

        openList.Add(new PathNode { Location = start, G = 0, H = start.DistanceTo(end), Parent = null });

        int safety = 0;
        while (openList.Count > 0 && safety++ < 50000)
        {
            openList.Sort((a, b) => a.F.CompareTo(b.F));
            PathNode current = openList[0];
            openList.RemoveAt(0);

            if (current.Location.Equals(end))
            {
                List<HexCoordinates> path = new List<HexCoordinates>();
                while (current != null) { path.Add(current.Location); current = current.Parent; }
                return path;
            }

            closedList.Add(current.Location);

            for (int i = 0; i < 6; i++)
            {
                HexCoordinates neighborCoords = current.Location.GetNeighbor(i);
                if (!grid.ContainsKey(neighborCoords) || closedList.Contains(neighborCoords)) continue;

                HexCell nCell = grid[neighborCoords];

                int moveCost = 10;
                if (nCell.IsRoad) moveCost = 1;
                else if (nCell.CurrentBiome == BiomeType.Water) moveCost = 80;
                else if (nCell.CurrentBiome == BiomeType.Mountain) moveCost = 500;

                int newG = current.G + moveCost;

                PathNode existingNode = openList.Find(n => n.Location.Equals(neighborCoords));
                if (existingNode == null)
                    openList.Add(new PathNode { Location = neighborCoords, G = newG, H = neighborCoords.DistanceTo(end), Parent = current });
                else if (newG < existingNode.G)
                {
                    existingNode.G = newG;
                    existingNode.Parent = current;
                }
            }
        }
        return null;
    }

    public void DrawMap()
    {
        targetTilemap.ClearAllTiles();

        foreach (var currentCell in grid)
        {
            HexCell cell = currentCell.Value;
            Vector3Int tilePos = cell.Coordinates.ToOffsetCoordinates();

            TileBase tileToRender = null;

            if (cell.HasBuilding)
            {
                if (cityTable != null && cityTable.Length > 0)
                {
                    float random = Random.value;
                    if (random <= 0.1f) // 10% chance of spawning a church
                        tileToRender = cityTable[1].HexTile;
                    else if (random <= 0.3f) //30% chance of spawning a farm
                        tileToRender = cityTable[2].HexTile;
                    else
                        tileToRender = cityTable[0].HexTile;
                }

                else tileToRender = roadTile;
            }
            else if (cell.IsRoad)
            {
                tileToRender = roadTile;
            }
            else
            {
                tileToRender = FindTileForBiome(cell.CurrentBiome);
            }

            if (tileToRender == null) tileToRender = defaultTile;
            targetTilemap.SetTile(tilePos, tileToRender);
        }
    }

    private BiomeType EvaluateBiome(float height, float moisture)
    {
        // TWEAKED: Less aggressive Ground, more diverse
        if (height > 0.85f) return BiomeType.Mountain;
        if (height < 0.35f) return BiomeType.Water;

        if (height < 0.6f)
        {
            // Lowlands
            if (moisture < 0.4f) return BiomeType.Sand;
            if (moisture < 0.7f) return BiomeType.Ground;
            return BiomeType.Forest; // Wet lowlands become forest now
        }
        else
        {
            // Highlands
            return (moisture < 0.6f) ? BiomeType.Forest : BiomeType.Snow;
        }
    }

    private TileBase FindTileForBiome(BiomeType type)
    {
        foreach (var def in biomeTable) if (def.Type == type) return def.HexTile;
        return null;
    }
}