using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SkyTech.Services
{
    /// <summary>
    /// Computes a simple perceptual hash (average hash / aHash) for an
    /// uploaded image, used to detect near-duplicate collateral photos
    /// across loan applications. Not a security hash — this is designed
    /// to tolerate different lighting/angle/compression, which is exactly
    /// why it's suitable for "is this roughly the same photo" checks.
    /// </summary>
    public static class ImageHashHelper
    {
        public static string ComputeAverageHash(byte[] imageBytes)
        {
            using var image = Image.Load<L8>(imageBytes); // L8 = 8-bit grayscale

            // Shrink to 8x8 — this also naturally normalizes away most
            // lighting/angle/resolution differences between two photos of
            // the same physical item.
            image.Mutate(ctx => ctx.Resize(8, 8));

            var pixels = new byte[64];
            int index = 0;
            long total = 0;

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var gray = image[x, y].PackedValue;
                    pixels[index++] = gray;
                    total += gray;
                }
            }

            byte average = (byte)(total / 64);

            // Each pixel becomes 1 bit: 1 if brighter than the average, 0 if not.
            ulong hash = 0;
            for (int i = 0; i < 64; i++)
            {
                if (pixels[i] >= average)
                {
                    hash |= (1UL << i);
                }
            }

            return hash.ToString("x16"); // 16-char hex string = 64 bits
        }
    }
}