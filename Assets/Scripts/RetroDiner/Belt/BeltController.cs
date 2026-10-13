using System;
using System.Collections.Generic;
using UnityEngine;
using RetroDiner.Core;
using RetroDiner.Data;

namespace RetroDiner.Belt
{
    /// <summary>
    /// Owns the spline, the items on it, the spawn queue and belt-speed modifiers (freeze, rewind, spills).
    /// Movement is purely distance-along-spline; items never leave the loop unless picked.
    /// </summary>
    public class BeltController : MonoBehaviour
    {
        public TrackSpline Track { get; private set; }
        public bool Running;
        public float SpeedMultiplier = 1f;
        public bool IsFrozen => freezeTimer > 0f;
        public float FreezeRemaining => Mathf.Max(0f, freezeTimer);
        public int QueuedCount => spawnQueue.Count;
        public IReadOnlyList<BeltItem> Items => items;

        readonly List<BeltItem> items = new List<BeltItem>(32);
        readonly Queue<string> spawnQueue = new Queue<string>();
        readonly List<Vector2> spills = new List<Vector2>();     // (start, end) distances
        readonly List<GameObject> decor = new List<GameObject>();

        LevelDefinition level;
        GameConfig config;
        ItemFactory factory;
        Func<string, ItemDefinition> lookup;
        SeededRandom rng;
        float baseSpeed;
        float freezeTimer;
        float rewindRemaining;
        float rewindSpeed;
        float spacing;
        float spawnCooldown;
        Material beltMaterial;
        float scrollOffset;

        public event Action<BeltItem> ItemCharred;

        public void Setup(GameConfig cfg, LevelDefinition lv, List<string> spawnList, ItemFactory itemFactory, Func<string, ItemDefinition> defLookup)
        {
            ClearLevel();
            config = cfg;
            level = lv;
            factory = itemFactory;
            lookup = defLookup;
            rng = new SeededRandom(lv.seed + 17);
            baseSpeed = lv.beltSpeed;
            spacing = cfg.itemSpacing;
            SpeedMultiplier = 1f;
            freezeTimer = 0f;
            rewindRemaining = 0f;

            Track = TrackSpline.FromShape(lv.trackShape, cfg.trackRadius);
            BuildBeltMesh();
            BuildSpills();

            foreach (var id in spawnList) spawnQueue.Enqueue(id);

            // Initial fill: evenly spaced around the loop, as many as fit.
            int initial = Mathf.Min(lv.maxItemsOnBelt, spawnList.Count, Mathf.FloorToInt(Track.Length / spacing));
            float step = Track.Length / Mathf.Max(1, initial);
            BeltItem previous = null;
            for (int i = 0; i < initial; i++)
            {
                var it = SpawnFromQueue(i * step);
                if (it == null) break;
                // Napkin pairs: tie to the previous item (adjacent on the belt).
                if (previous != null && previous.Obstacle == ItemObstacle.None && it.Obstacle == ItemObstacle.None
                    && lv.obstacles.napkinPairChance > 0f && rng.Chance(lv.obstacles.napkinPairChance))
                {
                    it.Partner = previous; previous.Partner = it;
                    it.SetObstacle(ItemObstacle.Napkin); previous.SetObstacle(ItemObstacle.Napkin);
                }
                previous = it;
            }
            Running = true;
        }

        BeltItem SpawnFromQueue(float distance)
        {
            if (spawnQueue.Count == 0) return null;
            string id = spawnQueue.Dequeue();
            var def = lookup(id);
            if (def == null) { Debug.LogWarning("[Belt] Unknown item id " + id); return null; }
            var item = factory.Spawn(def);
            item.Distance = distance;
            item.transform.position = Track.Position(distance);
            if (level.obstacles.clocheChance > 0f && rng.Chance(level.obstacles.clocheChance)) item.SetObstacle(ItemObstacle.Cloche);
            items.Add(item);
            return item;
        }

        void Update()
        {
            if (!Running || Track == null) return;
            float dt = Time.deltaTime;

            float move;
            if (rewindRemaining > 0f)
            {
                move = -Mathf.Min(rewindSpeed * dt, rewindRemaining);
                rewindRemaining += move; // move is negative
            }
            else if (freezeTimer > 0f)
            {
                freezeTimer -= dt;
                move = 0f;
            }
            else move = baseSpeed * SpeedMultiplier * dt;

            float time = Time.time;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it.InFlight) continue;
                float m = move;
                if (m > 0f && InSpill(it.Distance)) m *= config.spillSpeedMultiplier;
                float before = it.Distance;
                it.Distance += m;
                if (it.Distance >= Track.Length)
                {
                    it.Distance -= Track.Length;
                    it.Laps++;
                    if (level.obstacles.burnEnabled && it.Definition.canBurn && !it.IsCharred && it.Laps >= level.obstacles.loopsToChar)
                    {
                        it.SetCharred(true);
                        ItemCharred?.Invoke(it);
                    }
                }
                else if (it.Distance < 0f) it.Distance += Track.Length;
                it.transform.position = Track.Position(it.Distance);
                it.Bob(time);
                it.UpdateNapkinLink();
            }

            if (beltMaterial != null && move != 0f)
            {
                scrollOffset += move * 0.5f;
                beltMaterial.mainTextureOffset = new Vector2(-scrollOffset, 0f);
            }

            TrySpawn(dt);
        }

        void TrySpawn(float dt)
        {
            if (spawnQueue.Count == 0 || items.Count >= level.maxItemsOnBelt) return;
            spawnCooldown -= dt;
            if (spawnCooldown > 0f) return;
            // Spawn point is distance 0; wait until nothing is within one spacing of it.
            float nearest = float.MaxValue;
            for (int i = 0; i < items.Count; i++)
            {
                float d = Track.Wrap(items[i].Distance);
                float dist = Mathf.Min(d, Track.Length - d);
                if (dist < nearest) nearest = dist;
            }
            if (nearest < spacing) return;
            var it = SpawnFromQueue(0f);
            if (it != null)
            {
                it.transform.localScale = Vector3.zero;
                Tween.Scale(it.transform, Vector3.one, 0.3f, Ease.OutBack);
            }
            spawnCooldown = 0.15f;
        }

        bool InSpill(float d)
        {
            for (int i = 0; i < spills.Count; i++) if (d >= spills[i].x && d <= spills[i].y) return true;
            return false;
        }

        // ---------- public actions ----------

        public void Remove(BeltItem item)
        {
            items.Remove(item);
            if (item.Obstacle == ItemObstacle.Napkin) item.Untie();
        }

        /// <summary>Put items back into the spawn queue (continue offer, stuck-belt recovery). Keeps the
        /// multiples-of-3 invariant intact: nothing ever leaves the level except through a triplet.</summary>
        public void Enqueue(IEnumerable<string> ids)
        {
            foreach (var id in ids) spawnQueue.Enqueue(id);
            spawnCooldown = 0f;
        }

        public void Freeze(float seconds) { freezeTimer = Mathf.Max(freezeTimer, seconds); }

        public void Rewind(float distance, float duration)
        {
            rewindRemaining = distance;
            rewindSpeed = distance / Mathf.Max(0.05f, duration);
        }

        public int CountPickable(string id)
        {
            int c = 0;
            for (int i = 0; i < items.Count; i++) if (items[i].Definition.id == id && items[i].IsPickable) c++;
            return c;
        }

        public List<BeltItem> TakeTriple(string id)
        {
            var list = new List<BeltItem>(3);
            for (int i = 0; i < items.Count && list.Count < 3; i++)
                if (items[i].Definition.id == id && items[i].IsPickable) list.Add(items[i]);
            if (list.Count < 3) return null;
            foreach (var it in list) items.Remove(it);
            return list;
        }

        public List<BeltItem> SnapshotItems() => new List<BeltItem>(items);

        public void ClearLevel()
        {
            Running = false;
            if (factory != null) foreach (var it in items) factory.Release(it);
            items.Clear();
            spawnQueue.Clear();
            spills.Clear();
            foreach (var d in decor) if (d != null) Destroy(d);
            decor.Clear();
        }

        // ---------- visuals ----------

        void BuildBeltMesh()
        {
            float width = config.itemWorldSize * 1.25f;
            int n = Track.SampleCount;
            var verts = new Vector3[n * 2];
            var uvs = new Vector2[n * 2];
            var tris = new int[(n - 1) * 6];
            float acc = 0f;
            Vector3 prev = Track.Sample(0);
            for (int i = 0; i < n; i++)
            {
                Vector3 p = Track.Sample(i);
                acc += Vector3.Distance(prev, p); prev = p;
                Vector3 tan = Track.Tangent(acc);
                Vector3 side = Vector3.Cross(Vector3.up, tan).normalized * (width * 0.5f);
                verts[i * 2] = p - side;
                verts[i * 2 + 1] = p + side;
                uvs[i * 2] = new Vector2(acc, 0f);
                uvs[i * 2 + 1] = new Vector2(acc, 1f);
            }
            for (int i = 0; i < n - 1; i++)
            {
                int b = i * 2, t = i * 6;
                tris[t] = b; tris[t + 1] = b + 2; tris[t + 2] = b + 1;
                tris[t + 3] = b + 1; tris[t + 4] = b + 2; tris[t + 5] = b + 3;
            }
            var mesh = new Mesh { name = "BeltRibbon" };
            mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();

            var go = new GameObject("BeltRibbon");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            beltMaterial = new Material(PlaceholderMaterials.FindShader());
            beltMaterial.mainTexture = RollerTexture();
            beltMaterial.mainTextureScale = new Vector2(1.6f, 1f);
            beltMaterial.color = Color.white;
            if (beltMaterial.HasProperty("_BaseMap")) beltMaterial.SetTexture("_BaseMap", beltMaterial.mainTexture);
            if (beltMaterial.HasProperty("_Metallic")) beltMaterial.SetFloat("_Metallic", 0.75f);
            if (beltMaterial.HasProperty("_Glossiness")) beltMaterial.SetFloat("_Glossiness", 0.55f);
            if (beltMaterial.HasProperty("_Smoothness")) beltMaterial.SetFloat("_Smoothness", 0.55f);
            mr.material = beltMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            decor.Add(go);

            // Chrome rails on each edge, with a dark contact line underneath for depth.
            MakeRail(width * 0.5f + 0.06f, new Color(0.86f, 0.88f, 0.92f), 0.09f, 0.03f);
            MakeRail(-(width * 0.5f + 0.06f), new Color(0.86f, 0.88f, 0.92f), 0.09f, 0.03f);
            MakeRail(width * 0.5f + 0.1f, new Color(0.08f, 0.06f, 0.07f), 0.12f, -0.03f);
            MakeRail(-(width * 0.5f + 0.1f), new Color(0.08f, 0.06f, 0.07f), 0.12f, -0.03f);

            // Red vinyl base + chrome trim + dark underside: smooth 64-segment pucks (primitives show facets).
            var tb = Track.GetBounds();
            float rx = (tb.size.x + width + 0.8f) * 0.5f, rz = (tb.size.z + width + 0.8f) * 0.5f;
            Vector3 c = new Vector3(tb.center.x, 0f, tb.center.z);
            decor.Add(MakePuck("VinylBase", c + Vector3.down * 0.25f, rx, rz, 0.2f, new Color(0.80f, 0.13f, 0.22f), 0.25f, 0.5f));
            decor.Add(MakePuck("ChromeTrim", c + Vector3.down * 0.33f, rx + 0.08f, rz + 0.08f, 0.06f, new Color(0.85f, 0.87f, 0.9f), 0.9f, 0.85f));
            decor.Add(MakePuck("Underside", c + Vector3.down * 0.62f, rx * 0.8f, rz * 0.8f, 0.5f, new Color(0.12f, 0.08f, 0.09f), 0.2f, 0.3f));
        }

        GameObject MakePuck(string name, Vector3 center, float rx, float rz, float height, Color color, float metallic, float smooth)
        {
            const int seg = 64;
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tris = new List<int>();
            // top cap
            verts.Add(new Vector3(0f, height * 0.5f, 0f)); norms.Add(Vector3.up);
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * rx, height * 0.5f, Mathf.Sin(a) * rz)); norms.Add(Vector3.up);
            }
            for (int i = 0; i < seg; i++) { tris.Add(0); tris.Add(1 + (i + 1) % seg); tris.Add(1 + i); }
            // side
            int sideStart = verts.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                Vector3 n = new Vector3(Mathf.Cos(a) / rx, 0f, Mathf.Sin(a) / rz).normalized;
                verts.Add(new Vector3(Mathf.Cos(a) * rx, height * 0.5f, Mathf.Sin(a) * rz)); norms.Add(n);
                verts.Add(new Vector3(Mathf.Cos(a) * rx, -height * 0.5f, Mathf.Sin(a) * rz)); norms.Add(n);
            }
            for (int i = 0; i < seg; i++)
            {
                int t = sideStart + i * 2;
                tris.Add(t); tris.Add(t + 1); tris.Add(t + 2);
                tris.Add(t + 1); tris.Add(t + 3); tris.Add(t + 2);
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = center;
            go.AddComponent<MeshFilter>().mesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            var m = new Material(PlaceholderMaterials.FindShader());
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            r.material = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        void MakeRail(float offset, Color color, float width = 0.07f, float lift = 0.02f)
        {
            int n = Track.SampleCount;
            var lr = new GameObject("Rail").AddComponent<LineRenderer>();
            lr.transform.SetParent(transform, false);
            lr.positionCount = n;
            lr.loop = false;
            lr.widthMultiplier = width;
            lr.material = PlaceholderMaterials.Get(color);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.useWorldSpace = false;
            float acc = 0f; Vector3 prev = Track.Sample(0);
            for (int i = 0; i < n; i++)
            {
                Vector3 p = Track.Sample(i);
                acc += Vector3.Distance(prev, p); prev = p;
                Vector3 side = Vector3.Cross(Vector3.up, Track.Tangent(acc)).normalized * offset;
                lr.SetPosition(i, p + side + Vector3.up * lift);
            }
            decor.Add(lr.gameObject);
        }

        static Texture2D rollerTex;
        /// <summary>Chrome roller strip: each roller is a shaded cylinder with a specular line and a dark gap.</summary>
        static Texture2D RollerTexture()
        {
            if (rollerTex != null) return rollerTex;
            const int w = 128, h = 32, roller = 16;
            rollerTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4 };
            var px = new Color[w * h];
            var dark = new Color(0.16f, 0.17f, 0.19f);
            var mid = new Color(0.55f, 0.57f, 0.62f);
            var light = new Color(0.93f, 0.95f, 1f);
            for (int x = 0; x < w; x++)
            {
                int i = x % roller;
                float u = (i + 0.5f) / roller;              // 0..1 across one roller
                float shade = Mathf.Sin(u * Mathf.PI);       // cylinder lambert
                float spec = Mathf.Exp(-Mathf.Pow((u - 0.38f) * 7f, 2f));
                Color c = Color.Lerp(dark, mid, shade);
                c = Color.Lerp(c, light, spec * 0.85f);
                if (i == 0 || i == roller - 1) c = dark * 0.8f;
                for (int y = 0; y < h; y++)
                {
                    float v = y / (float)(h - 1);
                    float edge = 1f - Mathf.Pow(Mathf.Abs(v - 0.5f) * 2f, 6f) * 0.35f;   // darker at the belt edges
                    px[y * w + x] = new Color(c.r * edge, c.g * edge, c.b * edge, 1f);
                }
            }
            rollerTex.SetPixels(px); rollerTex.Apply();
            return rollerTex;
        }

        void BuildSpills()
        {
            int count = level.obstacles.spillSegments;
            if (count <= 0) return;
            float segLen = Track.Length * 0.12f;
            for (int i = 0; i < count; i++)
            {
                float start = Track.Length * ((i + 0.5f) / count) + rng.Range(-0.5f, 0.5f);
                start = Track.Wrap(start);
                spills.Add(new Vector2(start, start + segLen));
                Vector3 mid = Track.Position(start + segLen * 0.5f);
                var sprite = i % 2 == 0 ? ArtCatalog.SyrupSpill : ArtCatalog.KetchupSpill;
                var go = new GameObject("Spill");
                go.transform.SetParent(transform, false);
                go.transform.position = mid + Vector3.up * 0.01f;
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite != null ? sprite : PlaceholderMaterials.WhiteSprite;
                sr.color = sprite != null ? Color.white : (i % 2 == 0 ? new Color(0.9f, 0.55f, 0.1f, 0.8f) : new Color(0.85f, 0.1f, 0.1f, 0.8f));
                BeltItem.FitSprite(sr, segLen * 0.9f);
                decor.Add(go);
            }
        }
    }
}
