using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RetroDiner.Belt;
using RetroDiner.Core;
using RetroDiner.Data;

namespace RetroDiner.Gameplay
{
    /// <summary>
    /// Ring buffer of belt-item snapshots (30 Hz, last 6 s). Can play the buffer back in-engine with
    /// ghost sprites over the real belt, and while doing so dump a PNG sequence to persistentDataPath
    /// for sharing (MP4 encoding needs Unity Recorder / a native plugin — see docs/DECISIONS.md).
    /// Zero allocation per frame after warm-up: fixed arrays, structs only.
    /// </summary>
    public class ReplayRecorder : MonoBehaviour
    {
        public const int Hz = 30;
        public const float Seconds = 6f;
        const int MaxItems = 24;

        struct Snap { public string id; public Vector3 pos; public float scale; public bool charred; public bool cloche; }
        class Frame { public readonly Snap[] items = new Snap[MaxItems]; public int count; public float time; }

        Frame[] frames;
        int head, filled;
        float accumulator;
        BeltController belt;
        Camera cam;
        Func<string, ItemDefinition> lookup;
        Transform ghostRoot;
        readonly List<SpriteRenderer> ghosts = new List<SpriteRenderer>();
        float itemSize;
        Quaternion billboard;

        public bool IsPlaying { get; private set; }
        public bool HasFootage => filled > 10;
        public string LastSavedFolder { get; private set; }

        public void Setup(BeltController beltController, Camera camera, Func<string, ItemDefinition> defLookup, float worldItemSize, Quaternion billboardRotation)
        {
            belt = beltController; cam = camera; lookup = defLookup; itemSize = worldItemSize; billboard = billboardRotation;
            int n = Mathf.CeilToInt(Seconds * Hz);
            frames = new Frame[n];
            for (int i = 0; i < n; i++) frames[i] = new Frame();
            ghostRoot = new GameObject("ReplayGhosts").transform;
            ghostRoot.SetParent(transform, false);
            ghostRoot.gameObject.SetActive(false);
        }

        public void Clear() { head = 0; filled = 0; accumulator = 0f; }

        void LateUpdate()
        {
            if (IsPlaying || belt == null || !belt.Running || frames == null) return;
            accumulator += Time.deltaTime;
            float step = 1f / Hz;
            if (accumulator < step) return;
            accumulator -= step;
            var f = frames[head];
            f.count = 0;
            f.time = Time.time;
            var items = belt.Items;
            for (int i = 0; i < items.Count && f.count < MaxItems; i++)
            {
                var it = items[i];
                if (it == null || !it.gameObject.activeSelf) continue;
                f.items[f.count++] = new Snap
                {
                    id = it.Definition.id, pos = it.transform.position,
                    scale = it.transform.localScale.x, charred = it.IsCharred, cloche = it.Obstacle == ItemObstacle.Cloche
                };
            }
            head = (head + 1) % frames.Length;
            if (filled < frames.Length) filled++;
        }

        /// <summary>Plays the buffered footage over the belt. savePngs writes a frame sequence while playing.</summary>
        public void PlayLast(bool savePngs, Action onDone)
        {
            if (IsPlaying || !HasFootage) { onDone?.Invoke(); return; }
            StartCoroutine(Playback(savePngs, onDone));
        }

        IEnumerator Playback(bool savePngs, Action onDone)
        {
            IsPlaying = true;
            bool wasRunning = belt.Running;
            belt.Running = false;
            var liveRoot = belt.transform.Find("Items");
            if (liveRoot != null) liveRoot.gameObject.SetActive(false);
            ghostRoot.gameObject.SetActive(true);

            string folder = null;
            if (savePngs)
            {
                folder = Path.Combine(Application.persistentDataPath, "replays", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                try { Directory.CreateDirectory(folder); } catch { folder = null; }
            }

            int start = (head - filled + frames.Length) % frames.Length;
            float step = 1f / Hz;
            int saved = 0;
            for (int n = 0; n < filled; n++)
            {
                var f = frames[(start + n) % frames.Length];
                DrawFrame(f);
                yield return new WaitForEndOfFrame();
                if (folder != null && n % 3 == 0)
                {
                    var tex = ScreenCapture.CaptureScreenshotAsTexture();
                    try
                    {
                        var small = Downscale(tex, 0.5f);
                        File.WriteAllBytes(Path.Combine(folder, "frame_" + saved.ToString("000") + ".png"), small.EncodeToPNG());
                        Destroy(small);
                        saved++;
                    }
                    catch (Exception e) { Debug.LogWarning("[Replay] PNG write failed: " + e.Message); folder = null; }
                    Destroy(tex);
                }
                float wait = step;
                while (wait > 0f) { wait -= Time.unscaledDeltaTime; yield return null; }
            }

            foreach (var g in ghosts) g.gameObject.SetActive(false);
            ghostRoot.gameObject.SetActive(false);
            if (liveRoot != null) liveRoot.gameObject.SetActive(true);
            belt.Running = wasRunning;
            IsPlaying = false;
            LastSavedFolder = folder;
            if (folder != null) Debug.Log("[Replay] Saved " + saved + " PNG frames to " + folder);
            onDone?.Invoke();
        }

        void DrawFrame(Frame f)
        {
            while (ghosts.Count < f.count)
            {
                var go = new GameObject("Ghost");
                go.transform.SetParent(ghostRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 3;
                go.transform.rotation = billboard;
                ghosts.Add(sr);
            }
            for (int i = 0; i < ghosts.Count; i++)
            {
                var sr = ghosts[i];
                if (i >= f.count) { sr.gameObject.SetActive(false); continue; }
                var s = f.items[i];
                var def = lookup(s.id);
                Sprite sprite = s.cloche ? ArtCatalog.ClocheClosed : (def != null ? (s.charred && def.charredIcon != null ? def.charredIcon : def.icon) : null);
                sr.sprite = sprite != null ? sprite : PlaceholderMaterials.WhiteSprite;
                sr.color = sprite != null ? Color.white : (def != null ? def.placeholderColor : Color.magenta);
                BeltItem.FitSprite(sr, itemSize * s.scale);
                sr.transform.position = s.pos + Vector3.up * (itemSize * 0.42f);
                sr.gameObject.SetActive(true);
            }
        }

        static Texture2D Downscale(Texture2D src, float factor)
        {
            int w = Mathf.Max(1, (int)(src.width * factor)), h = Mathf.Max(1, (int)(src.height * factor));
            var rt = RenderTexture.GetTemporary(w, h);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var dst = new Texture2D(w, h, TextureFormat.RGB24, false);
            dst.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            dst.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return dst;
        }
    }
}
