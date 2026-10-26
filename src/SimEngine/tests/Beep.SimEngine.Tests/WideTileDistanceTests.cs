using System;
using System.Numerics;

namespace Beep.SimEngine.Tests
{
    public static class WideTileDistanceTests
    {
        public const string CaseName = "Tile distance: exact circles and whole distances across numeric bounds";
        public static void Register() => TestRunner.Add(CaseName, Run);
        private static void Run(Check check)
        {
            var zero = new TileCoord(0, 0);
            check.Equal(5L, zero.DistanceFloor(new TileCoord(3, 4)), "exact ordinary distance");
            check.That(zero.WithinDistance(new TileCoord(3, 4), 5), "boundary lies within circle");
            check.That(!zero.WithinDistance(new TileCoord(3, 4), 4), "smaller circle refuses");
            check.Equal(1L, zero.DistanceFloor(new TileCoord(1, 1)), "whole distance floors a fractional diagonal");
            check.That(!zero.WithinDistance(new TileCoord(1, 1), 1), "circle membership does not use rounded distance");
            check.That(zero.WithinDistance(zero, 0), "zero radius contains its centre");
            var maximum = new TileCoord(int.MaxValue, int.MaxValue);
            check.Equal(3037000498L, zero.DistanceFloor(maximum), "distance may exceed Int32");
            check.That(!zero.WithinDistance(maximum, 3037000498L), "wide exact squared comparison excludes rounded edge");
            check.That(zero.WithinDistance(maximum, 3037000499L), "wide next integer covers diagonal");
            var minimum = new TileCoord(-int.MaxValue, -int.MaxValue);
            check.Equal(6074000997L, minimum.DistanceFloor(maximum), "signed coordinate span does not overflow squared arithmetic");
            check.That(minimum.WithinDistance(maximum, long.MaxValue), "explicit large radius is not platform-clamped");
            check.Throws<ArgumentException>(() => TileCoord.Invalid.DistanceFloor(zero), "invalid coordinate refuses");
            check.Throws<ArgumentException>(() => zero.WithinDistance(TileCoord.Invalid, 1), "invalid target refuses");
            check.Throws<ArgumentOutOfRangeException>(() => zero.WithinDistance(maximum, -1), "negative radius refuses");
            for (int i = 0; i < 32; i++)
            {
                var point = new TileCoord(i * 12345, i * 617 + 2);
                long distance = zero.DistanceFloor(point);
                BigInteger squared = new BigInteger(point.X) * point.X + new BigInteger(point.Y) * point.Y;
                check.That(new BigInteger(distance) * distance <= squared && new BigInteger(distance + 1) * (distance + 1) > squared, "integer result brackets exact squared geometry");
                check.Equal(distance, point.DistanceFloor(zero), "symmetric distance");
                check.That(zero.WithinDistance(point, distance + 1), "ceil radius covers point");
            }
        }
    }
}
