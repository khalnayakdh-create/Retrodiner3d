using System.Collections.Generic;
using UnityEngine;
using RetroDiner.Data;

namespace RetroDiner.Belt
{
    /// <summary>
    /// Closed Catmull-Rom spline with an arc-length table, so items can be placed by distance.
    /// Self-contained (no Splines package dependency). Lies on the XZ plane, y = 0.
    /// </summary>
    public class TrackSpline
    {
        readonly Vector3[] samples;
        readonly float[] cumulative;
        public float Length { get; private set; }
        public Vector3[] ControlPoints { get; private set; }

        public TrackSpline(Vector3[] controlPoints, int samplesPerSegment = 32)
        {
            ControlPoints = controlPoints;
            int n = controlPoints.Length;
            var pts = new List<Vector3>(n * samplesPerSegment + 1);
            for (int i = 0; i < n; i++)
            {
                Vector3 p0 = controlPoints[(i - 1 + n) % n];
                Vector3 p1 = controlPoints[i];
                Vector3 p2 = controlPoints[(i + 1) % n];
                Vector3 p3 = controlPoints[(i + 2) % n];
                for (int s = 0; s < samplesPerSegment; s++)
                    pts.Add(CatmullRom(p0, p1, p2, p3, s / (float)samplesPerSegment));
            }
            pts.Add(controlPoints[0]);
            samples = pts.ToArray();
            cumulative = new float[samples.Length];
            float acc = 0f;
            for (int i = 1; i < samples.Length; i++)
            {
                acc += Vector3.Distance(samples[i - 1], samples[i]);
                cumulative[i] = acc;
            }
            Length = acc;
        }

        public static TrackSpline FromShape(TrackShape shape, float radius)
        {
            var pts = new List<Vector3>();
            switch (shape)
            {
                case TrackShape.Teardrop:
                {
                    float r = radius;
                    float[,] k = {
                        { 0f, 1.35f }, { 0.55f, 0.95f }, { 0.98f, 0.25f }, { 0.85f, -0.55f }, { 0.35f, -1.0f },
                        { -0.35f, -1.0f }, { -0.85f, -0.55f }, { -0.98f, 0.25f }, { -0.55f, 0.95f } };
                    for (int i = 0; i < k.GetLength(0); i++) pts.Add(new Vector3(k[i, 0] * r, 0f, k[i, 1] * r * 0.85f));
                    break;
                }
                case TrackShape.FigureEight:
                {
                    const int n = 24;
                    for (int i = 0; i < n; i++)
                    {
                        float t = i / (float)n * Mathf.PI * 2f;
                        pts.Add(new Vector3(radius * 0.62f * Mathf.Sin(2f * t), 0f, radius * 1.08f * Mathf.Cos(t)));
                    }
                    break;
                }
                default:
                {
                    const int n = 16;
                    for (int i = 0; i < n; i++)
                    {
                        float t = i / (float)n * Mathf.PI * 2f;
                        pts.Add(new Vector3(radius * Mathf.Sin(t), 0f, radius * 0.92f * Mathf.Cos(t)));
                    }
                    break;
                }
            }
            return new TrackSpline(pts.ToArray());
        }

        public float Wrap(float distance)
        {
            if (Length <= 0f) return 0f;
            distance %= Length;
            if (distance < 0f) distance += Length;
            return distance;
        }

        public Vector3 Position(float distance)
        {
            float d = Wrap(distance);
            int lo = 0, hi = cumulative.Length - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (cumulative[mid] <= d) lo = mid; else hi = mid;
            }
            float segLen = cumulative[hi] - cumulative[lo];
            float t = segLen <= 0.00001f ? 0f : (d - cumulative[lo]) / segLen;
            return Vector3.Lerp(samples[lo], samples[hi], t);
        }

        public Vector3 Tangent(float distance)
        {
            Vector3 a = Position(distance - 0.05f);
            Vector3 b = Position(distance + 0.05f);
            Vector3 t = b - a;
            return t.sqrMagnitude < 1e-6f ? Vector3.forward : t.normalized;
        }

        public Bounds GetBounds()
        {
            var b = new Bounds(samples[0], Vector3.zero);
            for (int i = 1; i < samples.Length; i++) b.Encapsulate(samples[i]);
            return b;
        }

        public int SampleCount => samples.Length;
        public Vector3 Sample(int i) => samples[i];

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}
