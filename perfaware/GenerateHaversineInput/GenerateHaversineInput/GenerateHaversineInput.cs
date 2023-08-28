namespace GenerateHaversineInput
{
    using System;
    using System.IO;
    using System.Text;

    public class GenerateHaversineInput
    {
        public enum GenerationMode
        {
            Cluster,
            Uniform,
        }

        private const double EarthRadius = 6372.8;
        private const double MinLat = -90;
        private const double MaxLat = 90;
        private const double MinLong = -180;
        private const double MaxLong = 180;
        
        public static void Main(string[] args)
        {
            if (args.Length == 3)
            {
                if (Enum.TryParse(args[0], true, out GenerationMode generationMode) == false)
                {
                    generationMode = GenerationMode.Cluster;
                    Console.Error.WriteLine($"Failed to parse generation mode ({args[0]}) using {generationMode} as the generation mode");
                }
                if (int.TryParse(args[1], out var randomSeed) == false)
                {
                    randomSeed = 0;
                    Console.Error.WriteLine($"Failed to parse random seed ({args[1]}) using {randomSeed} as the seed");
                }
                if (long.TryParse(args[2], out var amountOfPairs) == false)
                {
                    amountOfPairs = 100;
                    Console.Error.WriteLine($"Failed to parse amount of pairs ({args[2]}) using {amountOfPairs} as the amount of pairs");
                }
                GeneratePairs(generationMode, randomSeed, amountOfPairs);
            }
            else
            {
                Console.Error.WriteLine("USAGE: GenerateHaversineInput [uniform/cluster] [random seed] [amount of pairs to generate]");
            }
        }

        private static void GeneratePairs(GenerationMode generationMode, int randomSeed, long amountOfPairs)
        {
            var random = new Random(randomSeed);

            double averageDistance = 0;

            var minLong = MinLong;
            var minLat = MinLat;
            var maxLong = MaxLong;
            var maxLat = MaxLat;

            var rawResults = new byte[amountOfPairs * 8];

            var indentation = 0;
            var indentationString = "";
            var jsonBuilder = new StringBuilder();
            jsonBuilder.AppendLine("{");
            indentation++;
            indentationString += "\t";
            jsonBuilder.AppendLine($"{indentationString}\"pairs\":[");
            indentation++;
            indentationString += "\t";

            var clusterSize = generationMode == GenerationMode.Uniform ? amountOfPairs : 50;
            if (generationMode == GenerationMode.Cluster)
            {
                PickNewCluster(out minLong, out maxLong, out minLat, out maxLat);
            }
            var remainingCount = amountOfPairs;
            var amountLeftInCluster = Math.Min(remainingCount, clusterSize);

            while (remainingCount > 0)
            {
                remainingCount -= amountLeftInCluster;
                while (amountLeftInCluster > 0)
                {
                    amountLeftInCluster--;
                    var index = remainingCount + amountLeftInCluster;
                    var x0 = random.NextDoubleRange(minLong, maxLong);
                    var y0 = random.NextDoubleRange(minLat, maxLat);
                    var x1 = random.NextDoubleRange(minLong, maxLong);
                    var y1 = random.NextDoubleRange(minLat, maxLat);

                    var haversineDist = HaversineFormula.ReferenceHaversine(x0, y0, x1, y1, EarthRadius);
                    var rawBytes = BitConverter.GetBytes(haversineDist);
                    rawBytes.CopyTo(rawResults, index * 8);
                    averageDistance += (haversineDist / amountOfPairs);
                    var isLastEntry = amountLeftInCluster == 0 && remainingCount == 0;
                    jsonBuilder.AppendLine($"{indentationString}{{\"x0\":{x0:F16}, \"y0\":{y0:F16}, \"x1\":{x1:F16}, \"y1\":{y1:F16} }}{(isLastEntry ? ' ' : ',')}");
                }
                amountLeftInCluster = Math.Min(remainingCount, clusterSize);
                PickNewCluster(out minLong, out maxLong, out minLat, out maxLat);
            }
            indentation--;
            indentationString = indentationString.Remove(indentation);
            jsonBuilder.AppendLine($"{indentationString}]");
            jsonBuilder.AppendLine("}");

            File.WriteAllText($"data_{amountOfPairs}_flex.json", jsonBuilder.ToString());
            File.WriteAllBytes($"data_{amountOfPairs}_haveranswer.f64", rawResults);
            Console.Out.WriteLine($"Generation Mode: {generationMode}");
            Console.Out.WriteLine($"Random Seed: {randomSeed}");
            Console.Out.WriteLine($"Pair Count: {amountOfPairs}");
            Console.Out.WriteLine($"Expected Sum: {averageDistance:F16}");

            void PickNewCluster(out double minLongitude, out double maxLongitude, out double minLatitude, out double maxLatitude)
            {
                minLongitude = random.NextDoubleRange(MinLong, MaxLong);
                maxLongitude = random.NextDoubleRange(minLongitude, MaxLong);
                minLatitude = random.NextDoubleRange(MinLat, MaxLat);
                maxLatitude = random.NextDoubleRange(minLatitude, MaxLong);
            }
        }
        
        
    }
}