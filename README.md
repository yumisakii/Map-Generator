# Hexagonal Map Generator

A procedural generation tool capable of creating organic 2D maps on a hexagonal grid. This project explores terrain generation algorithms and pathfinding within a non-Cartesian coordinate system.

![Screenshot Placeholder: View of a generated map with roads]

## Core Features

### Procedural Terrain Generation
* **Perlin Noise:** Utilized Perlin Noise algorithms to generate heightmaps and biome distribution.
* **Biome Mapping:** The system maps noise values to specific terrain types (Water, Plains, Forests, Mountains) to create natural-looking environments.

### Hexagonal Grid Architecture
* **Custom Grid System:** Implemented a grid logic specifically for hexagonal tiles (handling axial or cubic coordinates).
* **Neighbor Logic:** Efficiently calculates distance and adjacency for every tile, forming the basis for the pathfinding system.

### Road Generation (A* Algorithm)
* **Pathfinding:** Implemented a custom version of the A* (A-Star) algorithm adapted for hexagonal movement.
* **Cost-Based Traversal:** The algorithm generates roads connecting points of interest. It weighs movement costs based on terrain type (e.g., roads avoid mountains or water unless necessary), resulting in organic-looking paths rather than straight lines.

## Technologies
* **Language:** C#
* **Algorithms:** Perlin Noise, A* Pathfinding
