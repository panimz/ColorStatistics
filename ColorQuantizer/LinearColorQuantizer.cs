using PixelParser.Palette;
using System;
using System.Drawing;
using System.Collections.Generic;

namespace ColorQuantizer
{
    /// <summary>
    /// Linear color quantizer based on predefined color palette
    /// </summary>
 
    public sealed class LinearColorQuantizer : IColorQuantizer
    {
        private const int IndexBits = 7;

        /// <summary>
        /// Quantizes an image.
        /// </summary>
        /// <param name="image">The image (XRGB).</param>
        /// <returns>The result.</returns>
        public ColorQuantizerResult Quantize(byte[] image)
        {
            return Quantize(image, 256);
        }

        /// <summary>
        /// Quantizes an image.
        /// </summary>
        /// <param name="image">The image (XRGB).</param>
        /// <param name="colorCount">The color count.</param>
        /// <returns>The result.</returns>
        public ColorQuantizerResult Quantize(byte[] image, int colorCount)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            if (colorCount < 1 || colorCount > 256)
            {
                throw new ArgumentOutOfRangeException("colorCount");
            }

            var palette = Generator.GetPalette(colorCount);
            return CalcStatistics(image, palette);
        }

        private ColorQuantizerResult CalcStatistics(byte[] image, List<Color> palette)
        {
            var quantizedImage = new ColorQuantizerResult(image.Length / 4);

            for (int i = 0; i < image.Length / 4; i++)
            {
                Color? nearestColor = null;
                double minDistance = double.MaxValue;
                foreach (var color in palette)
                {
                    int r = image[(i * 4) + 2] >> (8 - IndexBits);
                    int g = image[(i * 4) + 1] >> (8 - IndexBits);
                    int b = image[i * 4] >> (8 - IndexBits);
                    var currDistance = 0;
                    if (currDistance < minDistance)
                    {
                        nearestColor = color;
                        minDistance = currDistance;
                    }
                }

                quantizedImage.IncrementColor(nearestColor.GetValueOrDefault());
            }

            return quantizedImage;
        }
    }
}
