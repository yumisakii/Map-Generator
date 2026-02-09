# Hexagonal Map Generator

A procedural generation tool capable of creating organic 2D maps on a hexagonal grid. This project explores terrain generation algorithms and pathfinding within a non-Cartesian coordinate system.

<img width="1581" height="876" alt="Capture d&#39;écran 2026-02-09 193613" src="https://github.com/user-attachments/assets/ac47a6d3-07eb-42f9-9999-54de728d6fbd" />

## Core Features

### Procedural Terrain Generation
* **Dual-Layer Perlin Noise:** Implemented a multi-pass noise system where one layer generates elevation and another generates humidity.
* **Biome Logic:** By overlaying these two maps, the system determines biomes based on realistic rules (e.g., *Low Elevation + High Humidity* results in Water/Swamp, while *High Elevation + Low Humidity* generates Mountain Peaks).

### Hexagonal Grid Architecture
* **Custom Grid System:** Implemented a grid logic specifically for hexagonal tiles (handling axial or cubic coordinates).
* **Neighbor Logic:** Efficiently calculates distance and adjacency for every tile, forming the basis for the pathfinding system.

### Road Generation (A* Algorithm)
* **Pathfinding:** Implemented a custom version of the A* (A-Star) algorithm adapted for hexagonal movement.
* **Cost-Based Traversal:** The algorithm generates roads connecting points of interest. It weighs movement costs based on terrain type (e.g., roads avoid mountains or water unless necessary), resulting in organic-looking paths rather than straight lines.

## Technologies
* **Language:** C#
* **Algorithms:** Perlin Noise, A* Pathfinding
