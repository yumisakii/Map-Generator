using System;
using UnityEngine;

[System.Serializable]
public struct HexCoordinates : IEquatable<HexCoordinates>
{
    public int q; // Column (Axial)
    public int r; // Row (Axial)

    // Computed 's' (Cube coordinate)
    public int s => -q - r;

    public HexCoordinates(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    public bool Equals(HexCoordinates other)
    {
        return this.q == other.q && this.r == other.r;
    }

    public override bool Equals(object obj)
    {
        return obj is HexCoordinates other && Equals(other);
    }

    public override int GetHashCode()
    {
        // HashCode.Combine is the modern, efficient way (C# 8+)
        // If you are on an older Unity .NET version, use: q.GetHashCode() ^ r.GetHashCode();
        return HashCode.Combine(q, r);
    }

    // 4. Operator Overloads (Optional but good for readable code later: if(a == b))
    public static bool operator ==(HexCoordinates a, HexCoordinates b) => a.Equals(b);
    public static bool operator !=(HexCoordinates a, HexCoordinates b) => !a.Equals(b);

    // Convert to Unity World Position (Pointy-topped)
    public Vector2 ToWorldPos(float size)
    {
        float x = size * (Mathf.Sqrt(3) * q + Mathf.Sqrt(3) / 2 * r);
        float y = size * (3.0f / 2 * r);
        return new Vector2(x, y);
    }

    public HexCoordinates GetNeighbor(int directionIndex)
    {
        // Direction offsets for Pointy-Top Hexes (odd/even row handling is automatic in Axial)
        // Directions: NE, E, SE, SW, W, NW
        int[] dQ = { 1, 1, 0, -1, -1, 0 };
        int[] dR = { -1, 0, 1, 1, 0, -1 };

        int dir = directionIndex % 6;
        return new HexCoordinates(this.q + dQ[dir], this.r + dR[dir]);
    }

    public override string ToString() => $"({q}, {r})";
}