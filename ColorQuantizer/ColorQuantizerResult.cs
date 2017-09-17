using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace ColorQuantizer
{

    public sealed class ColorQuantizerResult
    {
        public ColorQuantizerResult(int size)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException("size");
            }
            this.Size = size;
            this.Stats = new Dictionary<Color, int>();
        }

        public Dictionary<Color, int> Stats { get; private set; }

        public int Size { get; private set; }

        public List<Color> Palette { get { return Stats.Keys.ToList(); } }

        internal void IncrementColor(Color color)
        {
            if (!Stats.ContainsKey(color))
            {
                Stats.Add(color, 1);
            }
            else
            {
                Stats[color] += 1;
            }
        }
    }
}
