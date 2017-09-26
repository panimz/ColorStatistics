using PixelParser.Converters;
using PixelParser.Converters.Formats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PixelParser.Palette
{
    public static class Generator
    {
        public static List<Color> GetPalette(int colorCount, PaletteOptions options = null)
        {
            if (colorCount < 1)
            {
                return new List<Color>();
            }
            if (options == null)
            {
                options = new PaletteOptions();
            }
            if (options.Samples < colorCount * 5)
            {
                options.Samples = colorCount * 5;
            };

            var samples = GenerateSamples(options);
            if (samples.Count < colorCount)
            {
                throw new Exception("Not enough samples to generate palette, increase sample count.");
            }

            var sliceSize = samples.Count / colorCount;
            var colors = new List<LabColor>();
            for (var i = 0; i < samples.Count; i += sliceSize)
            {
                colors.Add(samples[i]);
                if (colors.Count >= colorCount)
                {
                    break;
                }
            }

            for (var step = 1; step <= options.Quality; step++)
            {
                var zones = GenerateZones(samples, colors);
                var lastColors = colors.Select(x => x).ToList();
                for (var i = 0; i < zones.Count; i++)
                {
                    var zone = zones[i];
                    var total = zone.Count;
                    var lAvg = zone.Sum(x => x.L) / total;
                    var aAvg = zone.Sum(x => x.A) / total;
                    var bAvg = zone.Sum(x => x.B) / total;
                    colors[i] = new LabColor(lAvg, aAvg, bAvg);
                }

                if (!AreEqualPalettes(lastColors, colors))
                {
                    break;
                }
            }

            colors = SortByContrast(colors);
            return colors.Select((lab) => lab.ToRgb()).ToList();
        }

        private static bool CheckColor(LabColor color, PaletteOptions options)
        {
            var rgb = color.ToRgb();
            var hcl = color.ToHcl();
            var compLab = rgb.ToLab();
            var labTolerance = 7;

            return (
              hcl.H >= options.HueMin &&
              hcl.H <= options.HueMax &&
              hcl.C >= options.ChromaMin &&
              hcl.C <= options.ChromaMax &&
              hcl.L >= options.LightMin &&
              hcl.L <= options.LightMax &&
              compLab.L >= (color.L - labTolerance) &&
              compLab.L <= (color.L + labTolerance) &&
              compLab.A >= (color.A - labTolerance) &&
              compLab.A <= (color.A + labTolerance) &&
              compLab.B >= (color.B - labTolerance) &&
              compLab.B <= (color.B + labTolerance)
            );
        }

        private static List<LabColor> SortByContrast(List<LabColor> colorList)
        {
            var unsortedColors = colorList.Select(x => x).ToList();
            var sortedColors = new List<LabColor>() { unsortedColors[0] };
            unsortedColors.RemoveAt(0);
            while (unsortedColors.Count > 0)
            {
                var lastColor = sortedColors.Last();
                var nearestId = 0;
                var maxDist = double.MinValue;
                for (var i = 0; i < unsortedColors.Count; i++)
                {
                    var curr = unsortedColors[i];
                    var dist = Math.Pow((lastColor.L - curr.L), 2) +
                               Math.Pow((lastColor.A - curr.A), 2) +
                               Math.Pow((lastColor.B - curr.B), 2);
                    if (dist > maxDist)
                    {
                        maxDist = dist;
                        nearestId = i;
                    }
                }
                sortedColors.Add(unsortedColors[nearestId]);
                unsortedColors.RemoveAt(nearestId);
            }
            return sortedColors;
        }

        

        private static List<LabColor> GenerateSamples(PaletteOptions options)
        {
            var samples = new List<LabColor>();
            var rangeDivider = Math.Pow(options.Samples, 1.0 / 3.0) * 1.001;

            var hStep = (options.HueMax - options.HueMin) / rangeDivider;
            var cStep = (options.ChromaMax - options.ChromaMin) / rangeDivider;
            var lStep = (options.LightMax - options.LightMin) / rangeDivider;
            for (double h = options.HueMin; h <= options.HueMax; h += hStep)
            {
                for (double c = options.ChromaMin; c <= options.ChromaMax; c += cStep)
                {
                    for (double l = options.LightMin; l <= options.LightMax; l += lStep)
                    {
                        var color = new HclColor(h, c, l).ToLab();
                        if (CheckColor(color, options))
                        {
                            samples.Add(color);
                        }
                    }
                }
            }

            return samples.Distinct().ToList();
        }

        private static List<List<LabColor>> GenerateZones(List<LabColor> samples, List<LabColor> colors)
        {
            var zones = new List<List<LabColor>>();
            for (var c = 0; c < samples.Count; c++)
            {
                zones.Add(new List<LabColor>());
            }

            // Find closest color for each sample
            for (var i = 0; i < samples.Count; i++)
            {
                var minDist = double.MaxValue;
                var nearest = 0;
                var sample = samples[i];
                for (var j = 0; j < colors.Count; j++)
                {
                    var color = colors[j];
                    var dist = Math.Pow((sample.L - color.L), 2) +
                               Math.Pow((sample.A - color.A), 2) +
                               Math.Pow((sample.B - color.B), 2);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = j;
                    }
                }
                zones[nearest].Add(samples[i]);
            }

            return zones;
        }

        private static bool AreEqualPalettes(List<LabColor> lastColors, List<LabColor> colors)
        {
            if (lastColors == null || colors == null) { return false; }
            if (lastColors.Count != colors.Count) { return false; }
            foreach( var color in colors)
            {
                if (!lastColors.Any(x => x.Equals(color))) {
                    return false;
                }
            }
            return true;
        }
    }
}
