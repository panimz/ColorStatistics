using ColorQuantizer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PixelParser.Models
{
    [Serializable]
    class ImageStats
    {
        public string Name { get; set; }
        public List<ColorStats> Colors { get; set; }

        public ImageStats(string name, ColorQuantizerResult source)
        {
            Name = name;
            Colors = new List<ColorStats>();
            foreach (var val in source.Stats)
            {
                var factor = (val.Value * 100.0f) / source.Size;
                var color = new ColorStats(val.Key, factor);
                Colors.Add(color);
            }
            Colors = Colors
                .OrderByDescending(x => x.Factor)
                .ToList();
        }
    }
}
