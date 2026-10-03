using System;
using System.Drawing;
using System.IO;
using System.Linq;
using FaceONNX;

namespace SkyTech.Services
{
    // Wraps FaceONNX's detector + embedder. Registered as a Singleton in
    // Program.cs — the ONNX models load once in the constructor, not per
    // request, since that's the expensive part.
    public class FaceRecognitionService : IDisposable
    {
        private readonly FaceDetector _detector;
        private readonly FaceEmbedder _embedder;

        public FaceRecognitionService()
        {
            _detector = new FaceDetector();
            _embedder = new FaceEmbedder();
        }

        // Returns null if no face was found in the image at all.
        // If multiple faces are in frame, uses the largest one (most likely
        // the person holding the phone, not someone in the background).
        public float[] ExtractEmbedding(byte[] imageBytes)
        {
            using var ms = new MemoryStream(imageBytes);
            using var bitmap = new Bitmap(ms);

            var faces = _detector.Forward(bitmap);
            if (faces.Length == 0)
                return null;

            var best = faces.OrderByDescending(f => f.Rectangle.Width * f.Rectangle.Height).First();

            var rect = best.Rectangle;
            rect.Intersect(new Rectangle(0, 0, bitmap.Width, bitmap.Height)); // clamp to image bounds
            if (rect.Width <= 0 || rect.Height <= 0)
                return null;

            using var cropped = bitmap.Clone(rect, bitmap.PixelFormat);
            return _embedder.Forward(cropped);
        }

        public static double CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length) return 0;

            double dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }

            if (magA == 0 || magB == 0) return 0;
            return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }

        public static string SerializeEmbedding(float[] embedding) =>
            string.Join(",", embedding);

        public static float[] DeserializeEmbedding(string serialized) =>
            serialized.Split(',').Select(float.Parse).ToArray();

        public void Dispose()
        {
            _detector.Dispose();
            _embedder.Dispose();
        }
    }
}