using System;
using System.Numerics;

namespace Beep.SimEngine
{
    /// <summary>A position in world space, in tiles, with fixed-point fractions.</summary>
    public readonly struct FixVec2 : IEquatable<FixVec2>
    {
        public readonly Fix X;
        public readonly Fix Y;

        public FixVec2(Fix x, Fix y) { X = x; Y = y; }

        public static readonly FixVec2 Zero = new FixVec2(Fix.Zero, Fix.Zero);

        /// <summary>The centre of a tile.</summary>
        public static FixVec2 Centre(TileCoord t) => new FixVec2(Fix.FromInt(t.X) + Fix.Half, Fix.FromInt(t.Y) + Fix.Half);

        public static FixVec2 operator +(FixVec2 a, FixVec2 b) => new FixVec2(a.X + b.X, a.Y + b.Y);
        public static FixVec2 operator -(FixVec2 a, FixVec2 b) => new FixVec2(a.X - b.X, a.Y - b.Y);
        public static FixVec2 operator *(FixVec2 a, Fix s) => new FixVec2(a.X * s, a.Y * s);
        public static bool operator ==(FixVec2 a, FixVec2 b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(FixVec2 a, FixVec2 b) => !(a == b);

        public Fix LengthSquared => X * X + Y * Y;
        public Fix Length => Fix.Sqrt(LengthSquared);
        /// <summary>Exact represented Euclidean distance rounded upward to the next raw unit.</summary>
        public Fix LengthCeiling => Fix.FromRaw(checked((long)RawLengthCeiling()));
        private BigInteger RawLengthCeiling()
        {
            BigInteger x = BigInteger.Abs(new BigInteger(X.Raw)), y = BigInteger.Abs(new BigInteger(Y.Raw));
            BigInteger squared = x * x + y * y;
            if (squared.IsZero) return BigInteger.Zero;
            BigInteger root = x + y;
            while (true)
            {
                BigInteger next = (root + squared / root) >> 1;
                if (next >= root) break;
                root = next;
            }
            return root * root == squared ? root : root + 1;
        }

        /// <summary>The tile this point lies in.</summary>
        public TileCoord Tile => new TileCoord(X.ToIntFloor(), Y.ToIntFloor());

        /// <summary>Moves toward <paramref name="target"/> by at most <paramref name="step"/>; lands exactly on it.</summary>
        public static FixVec2 MoveTowards(FixVec2 from, FixVec2 target, Fix step)
        {
            if (step < Fix.Zero) throw new ArgumentOutOfRangeException(nameof(step));
            if (step.IsZero) return from;
            FixVec2 delta = target - from;
            BigInteger length = delta.RawLengthCeiling();
            if (length <= step.Raw) return target;
            // Exact signed division truncates each displacement toward zero, never beyond its budget.
            return from + new FixVec2(Fix.FromRaw(checked((long)(new BigInteger(delta.X.Raw) * step.Raw / length))),
                Fix.FromRaw(checked((long)(new BigInteger(delta.Y.Raw) * step.Raw / length))));
        }

        public static FixVec2 Lerp(FixVec2 a, FixVec2 b, Fix t) => a + (b - a) * t;

        public bool Equals(FixVec2 other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is FixVec2 v && Equals(v);
        public override int GetHashCode() => X.Raw.GetHashCode() * 397 ^ Y.Raw.GetHashCode();
        public override string ToString() => "(" + X + ", " + Y + ")";
    }

    /// <summary>An integer tile coordinate.</summary>
    public readonly struct TileCoord : IEquatable<TileCoord>
    {
        public readonly int X;
        public readonly int Y;
        public TileCoord(int x, int y) { X = x; Y = y; }

        public static readonly TileCoord Invalid = new TileCoord(int.MinValue, int.MinValue);
        public bool IsValid => X != int.MinValue;

        /// <summary>Chebyshev distance: the number of 8-way steps between two tiles.</summary>
        public static int Steps(TileCoord a, TileCoord b) => checked((int)System.Math.Max(System.Math.Abs((long)a.X - b.X), System.Math.Abs((long)a.Y - b.Y)));
        /// <summary>Exact circular membership over represented coordinates, without overflow or floating point.</summary>
        public bool WithinDistance(TileCoord other, long radius)
        {
            if (!IsValid || !other.IsValid) throw new ArgumentException("distance requires valid coordinates");
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
            return SquaredDistance(other) <= new BigInteger(radius) * radius;
        }
        /// <summary>Whole Euclidean distance; the result can exceed Int32 without imposing a world-size cap.</summary>
        public long DistanceFloor(TileCoord other)
        {
            if (!IsValid || !other.IsValid) throw new ArgumentException("distance requires valid coordinates");
            BigInteger squared = SquaredDistance(other);
            long high = 2 * Math.Max(Math.Abs((long)X - other.X), Math.Abs((long)Y - other.Y)) + 1, low = 0;
            while (low + 1 < high)
            {
                long middle = low + (high - low) / 2;
                if (new BigInteger(middle) * middle <= squared) low = middle; else high = middle;
            }
            return low;
        }
        private BigInteger SquaredDistance(TileCoord other)
        {
            long dx = (long)X - other.X, dy = (long)Y - other.Y;
            return new BigInteger(dx) * dx + new BigInteger(dy) * dy;
        }

        /// <summary>Octile distance ×10 (straight 10, diagonal 14): the travel metric the whole engine uses.</summary>
        public static int Octile10(TileCoord a, TileCoord b)
        {
            long dx = System.Math.Abs((long)a.X - b.X), dy = System.Math.Abs((long)a.Y - b.Y);
            long lo = System.Math.Min(dx, dy), hi = System.Math.Max(dx, dy);
            return checked((int)(14 * lo + 10 * (hi - lo)));
        }

        public static bool operator ==(TileCoord a, TileCoord b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(TileCoord a, TileCoord b) => !(a == b);
        public bool Equals(TileCoord o) => X == o.X && Y == o.Y;
        public override bool Equals(object o) => o is TileCoord t && Equals(t);
        public override int GetHashCode() => X * 73856093 ^ Y * 19349663;
        public override string ToString() => "[" + X + "," + Y + "]";
    }

    /// <summary>An axis-aligned rectangle of tiles: origin plus width and height.</summary>
    public readonly struct TileRect : IEquatable<TileRect>
    {
        public readonly int X, Y, W, H;
        public TileRect(int x, int y, int w, int h) { X = x; Y = y; W = w; H = h; }
        public TileRect(TileCoord origin, int w, int h) : this(origin.X, origin.Y, w, h) { }

        public TileCoord Origin => new TileCoord(X, Y);
        public int Right => checked(X + W);
        public int Bottom => checked(Y + H);
        public int Area => checked(W * H);
        public bool Overlaps(TileRect o) => X < o.Right && o.X < Right && Y < o.Bottom && o.Y < Bottom;
        public bool Contains(TileCoord t) => t.X >= X && t.X < Right && t.Y >= Y && t.Y < Bottom;
        public TileRect Inflate(int by) => new TileRect(checked(X - by), checked(Y - by), checked(W + 2 * by), checked(H + 2 * by));

        /// <summary>Expand within explicit bounds using wide intermediates, never a platform-size limit.</summary>
        public TileRect InflateClipped(int by, TileRect bounds)
        {
            if (by < 0 || W <= 0 || H <= 0 || bounds.W <= 0 || bounds.H <= 0)
                throw new ArgumentException("clipped expansion needs nonnegative padding and positive rectangles");
            long rightBound = bounds.Right, bottomBound = bounds.Bottom;
            long left = Math.Min(rightBound, Math.Max(bounds.X, (long)X - by));
            long top = Math.Min(bottomBound, Math.Max(bounds.Y, (long)Y - by));
            long right = Math.Max(left, Math.Min(rightBound, (long)X + W + by));
            long bottom = Math.Max(top, Math.Min(bottomBound, (long)Y + H + by));
            return new TileRect((int)left, (int)top, (int)(right - left), (int)(bottom - top));
        }

        /// <summary>Tile steps from this rectangle to a tile; 0 inside it.</summary>
        public int StepsTo(TileCoord t)
        {
            long dx = t.X < X ? (long)X - t.X : t.X >= Right ? (long)t.X - Right + 1 : 0;
            long dy = t.Y < Y ? (long)Y - t.Y : t.Y >= Bottom ? (long)t.Y - Bottom + 1 : 0;
            return checked((int)System.Math.Max(dx, dy));
        }

        public static bool operator ==(TileRect a, TileRect b) => a.Equals(b);
        public static bool operator !=(TileRect a, TileRect b) => !a.Equals(b);
        public bool Equals(TileRect o) => X == o.X && Y == o.Y && W == o.W && H == o.H;
        public override bool Equals(object o) => o is TileRect r && Equals(r);
        public override int GetHashCode() => X * 31 + Y * 17 + W * 7 + H;
        public override string ToString() => "[" + X + "," + Y + " " + W + "x" + H + "]";
    }
}
