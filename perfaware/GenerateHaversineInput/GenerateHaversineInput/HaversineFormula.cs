namespace GenerateHaversineInput
{
    using System;

    public class HaversineFormula
    {
        private static double Square(double A)
        {
            double Result = (A*A);
            return Result;
        }

        private static double RadiansFromDegrees(double Degrees)
        {
            double Result = 0.01745329251994329577 * Degrees;
            return Result;
        }

        // NOTE(casey): EarthRadius is generally expected to be 6372.8
        public static double ReferenceHaversine(double X0, double Y0, double X1, double Y1, double EarthRadius)
        {
            /* NOTE(casey): This is not meant to be a "good" way to calculate the Haversine distance.
               Instead, it attempts to follow, as closely as possible, the formula used in the real-world
               question on which these homework exercises are loosely based.
            */
    
            double lat1 = Y0;
            double lat2 = Y1;
            double lon1 = X0;
            double lon2 = X1;
    
            double dLat = RadiansFromDegrees(lat2 - lat1);
            double dLon = RadiansFromDegrees(lon2 - lon1);
            lat1 = RadiansFromDegrees(lat1);
            lat2 = RadiansFromDegrees(lat2);
    
            double a = Square(Math.Sin(dLat/2.0)) + Math.Cos(lat1)*Math.Cos(lat2)*Square(Math.Sin(dLon/2));
            double c = 2.0*Math.Asin(Math.Sqrt(a));
    
            double Result = EarthRadius * c;
    
            return Result;
        }
    }
}