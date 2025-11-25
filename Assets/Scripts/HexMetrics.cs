public static class HexMetrics
{
    // The 6 directions in Axial Coordinates (Pointy Top)
    // NE, E, SE, SW, W, NW
    public static HexCoordinates[] directions = {
        new HexCoordinates(1, -1), new HexCoordinates(1, 0), new HexCoordinates(0, 1),
        new HexCoordinates(-1, 1), new HexCoordinates(-1, 0), new HexCoordinates(0, -1)
    };
}