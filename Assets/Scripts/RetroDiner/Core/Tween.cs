using System;
using System.Collections.Generic;
using UnityEngine;

namespace RetroDiner.Core
{
    public enum Ease { Linear, InQuad, OutQuad, InOutQuad, OutCubic, OutBack, OutElastic }

    public class TweenHandle
    {
        internal bool killed;
        public bool IsDone { get; internal set; }
        public void Kill() { killed = true; }
    }

    /// <summary>
    /// Tiny in-house tween utility (no third-party package). Drives a normalized 0..1 value
    /// through an easing curve and hands it to a callback every frame.
    /// </summary>
    public static class Tween
    {
        class Entry
        {
            public TweenHandle handle;
            public float elapsed, duration, delay;
            public Ease ease;
            public bool unscaled;
            public Action<float> onUpdate;
            public Action onComplete;
        }

        static readonly List<Entry> entries = new List<Entry>(128);
        static TweenRunner runner;

        static void EnsureRunner()
        {
            if (runner != null) return;
            var go = new GameObject("[TweenRunner]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<TweenRunner>();
        }

        public static TweenHandle To(float duration, Action<float> onUpdate, Ease ease = Ease.OutQuad,
            Action onComplete = null, bool unscaled = false, float delay = 0f)
        {
            EnsureRunner();
            var e = new Entry
            {
                handle = new TweenHandle(),
                duration = Mathf.Max(0.0001f, duration),
                ease = ease,
                unscaled = unscaled,
                onUpdate = onUpdate,
                onComplete = onComplete,
                delay = delay
            };
            entries.Add(e);
            return e.handle;
        }

        public static TweenHandle Delay(float seconds, Action onComplete, bool unscaled = false)
        {
            return To(0.0001f, null, Ease.Linear, onComplete, unscaled, seconds);
        }

        /// <summary>Tween a float from a to b.</summary>
        public static TweenHandle Float(float a, float b, float duration, Action<float> setter, Ease ease = Ease.OutQuad,
            Action onComplete = null, bool unscaled = false, float delay = 0f)
        {
            return To(duration, t => setter(Mathf.LerpUnclamped(a, b, t)), ease, onComplete, unscaled, delay);
        }

        public static TweenHandle Move(Transform tr, Vector3 to, float duration, Ease ease = Ease.OutQuad, Action onComplete = null, bool unscaled = false)
        {
            if (tr == null) return new TweenHandle();
            Vector3 from = tr.position;
            return To(duration, t => { if (tr != null) tr.position = Vector3.LerpUnclamped(from, to, t); }, ease, onComplete, unscaled);
        }

        public static TweenHandle Scale(Transform tr, Vector3 to, float duration, Ease ease = Ease.OutBack, Action onComplete = null, bool unscaled = false)
        {
            if (tr == null) return new TweenHandle();
            Vector3 from = tr.localScale;
            return To(duration, t => { if (tr != null) tr.localScale = Vector3.LerpUnclamped(from, to, t); }, ease, onComplete, unscaled);
        }

        public static TweenHandle AnchoredMove(RectTransform rt, Vector2 to, float duration, Ease ease = Ease.OutQuad, Action onComplete = null, bool unscaled = false)
        {
            if (rt == null) return new TweenHandle();
            Vector2 from = rt.anchoredPosition;
            return To(duration, t => { if (rt != null) rt.anchoredPosition = Vector2.LerpUnclamped(from, to, t); }, ease, onComplete, unscaled);
        }

        public static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.OutBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    float u = t - 1f;
                    return 1f + c3 * u * u * u + c1 * u * u;
                }
                case Ease.OutElastic:
                {
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    const float c4 = (2f * Mathf.PI) / 3f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                }
                default: return t;
            }
        }

        internal static void Tick(float dt, float udt)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                if (e.handle.killed) { entries.RemoveAt(i); continue; }
                float step = e.unscaled ? udt : dt;
                if (e.delay > 0f)
                {
                    e.delay -= step;
                    if (e.delay > 0f) continue;
                    step = -e.delay;
                    e.delay = 0f;
                }
                e.elapsed += step;
                float t = Mathf.Clamp01(e.elapsed / e.duration);
                if (e.onUpdate != null) e.onUpdate(Evaluate(e.ease, t));
                if (t >= 1f)
                {
                    e.handle.IsDone = true;
                    entries.RemoveAt(i);
                    if (e.onComplete != null) e.onComplete();
                }
            }
        }

        public static void KillAll() { entries.Clear(); }
    }

    public class TweenRunner : MonoBehaviour
    {
        void Update() { Tween.Tick(Time.deltaTime, Time.unscaledDeltaTime); }
    }
}
