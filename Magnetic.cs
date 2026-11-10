using System;
using System.Linq;
using vatsys;

namespace ATISPlugin
{
    // METAR winds are true, ATIS winds and runway headings are magnetic.
    public static class Magnetic
    {
        // Degrees to add to a true direction to make it magnetic. vatSys stores East variation as negative
        // (YSSY is -13), so this is the airport position's MagneticVariation. Airports without one have it
        // estimated from their runways, whose numbers are magnetic. Null when neither is available.
        public static double? Variation(string icao)
        {
            if (string.IsNullOrWhiteSpace(icao)) return null;

            var position = LogicalPositions.Positions?.FirstOrDefault(x => x.Name == icao);

            if (position != null && position.MagneticVariation != 0) return position.MagneticVariation;

            return FromRunways(icao) ?? position?.MagneticVariation;
        }

        private static double? FromRunways(string icao)
        {
            var airport = Airspace2.GetAirport(icao);

            if (airport == null) return null;

            var differences = airport.Runways
                .Select(runway =>
                {
                    if (!int.TryParse(runway.Name.TrimEnd('L', 'R', 'C'), out var number)) return (double?)null;

                    var opposite = EditorWindow.OppositeRunway(airport, runway.Name);

                    if (opposite == null) return null;

                    var track = Conversions.CalculateTrack(runway.LatLong, opposite.LatLong);

                    return Normalise(number * 10 - track);
                })
                .Where(x => x != null)
                .Select(x => x.Value)
                .ToList();

            // Runway numbers are rounded to 10 degrees, so the average of every runway is the best estimate.
            return differences.Any() ? Math.Round(differences.Average(), 1) : (double?)null;
        }

        // An angle between -180 and 180.
        private static double Normalise(double angle) => ((angle % 360) + 540) % 360 - 180;

        // A true direction as magnetic, to the nearest 10 degrees (360 rather than 000).
        public static int ToMagnetic(int trueDirection, double variation)
        {
            var magnetic = (int)(Math.Round((trueDirection + variation) / 10.0, MidpointRounding.AwayFromZero) * 10);

            magnetic = ((magnetic % 360) + 360) % 360;

            return magnetic == 0 ? 360 : magnetic;
        }
    }
}
