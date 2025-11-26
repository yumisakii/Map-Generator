using System;
using UnityEngine;

[System.Serializable]
public struct HexCoordinates : IEquatable<HexCoordinates>
{
    public int q;
    public int r;
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
        return HashCode.Combine(q, r);
    }

    public static bool operator ==(HexCoordinates a, HexCoordinates b) => a.Equals(b);
    public static bool operator !=(HexCoordinates a, HexCoordinates b) => !a.Equals(b);

    public Vector2 ToWorldPos(float size)
    {
        float x = size * (Mathf.Sqrt(3) * q + Mathf.Sqrt(3) / 2 * r);
        float y = size * (3.0f / 2 * r);
        return new Vector2(x, y);
    }

    public int DistanceTo(HexCoordinates other)
    {
        // Formula: (|dq| + |dr| + |ds|) / 2
        return (Mathf.Abs(q - other.q) + Mathf.Abs(r - other.r) + Mathf.Abs(s - other.s)) / 2;
    }

    public HexCoordinates GetNeighbor(int directionIndex)
    {
        // Directions: NE, E, SE, SW, W, NW
        int[] dQ = { 1, 1, 0, -1, -1, 0 };
        int[] dR = { -1, 0, 1, 1, 0, -1 };
        int dir = directionIndex % 6;
        if (dir < 0) dir += 6; // Safety for negative indices
        return new HexCoordinates(this.q + dQ[dir], this.r + dR[dir]);
    }

    public Vector3Int ToOffsetCoordinates()
    {
        int col = q + (r - (r & 1)) / 2;
        int row = r;
        return new Vector3Int(col, row, 0);
    }

    public override string ToString() => $"({q}, {r})";
}