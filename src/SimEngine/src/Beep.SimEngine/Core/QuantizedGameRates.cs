using System;
using System.Numerics;

namespace Beep.SimEngine
{
    /// <summary>One exact cumulative quantity/time calculator. No elapsed or remainder state.</summary>
    public static class QuantizedGameRates
    {
        public static BigInteger AccumulatedRaw(Fix quantity, long periodNumerator, long periodDenominator,
            DurationUnit periodUnit, ISimulationTime time, WorkProgress elapsed, long quantumRaw, bool ceiling)
        {
            if (!quantity.IsPositive || periodNumerator <= 0 || periodDenominator <= 0 || time == null ||
                time.QuantumNumerator <= 0 || time.QuantumDenominator <= 0 || quantumRaw <= 0)
                throw new ArgumentException("positive exact native quantity/rational period/session/quantum required");
            BigInteger ticks = ((BigInteger)elapsed.WholeTicks << Fix.FractionalBits) + elapsed.Fraction;
            BigInteger numerator = (BigInteger)quantity.Raw * ticks * time.QuantumNumerator * periodDenominator;
            BigInteger denominator = (BigInteger)time.QuantumDenominator * periodNumerator * GameDuration.SecondsPerUnit(periodUnit) * quantumRaw * Fix.One.Raw;
            return (ceiling ? (numerator + denominator - 1) / denominator : numerator / denominator) * quantumRaw;
        }
    }
}
