using System;
using System.Collections.Generic;
using UnityEngine;
using RetroDiner.Core;
using RetroDiner.Data;
using RetroDiner.UI;

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
        float outSign = 1f;
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

            Track = TrackSpline.FromShape(lv.trackShape, cfg.trackRadius, cfg.trackLength);
            widthScale = lv.trackShape == TrackShape.FigureEight ? 0.78f : 1f;   // keep the two lobes readable at the crossing
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
                beltMaterial.mainTextureOffset = new Vector2(-scrollOffset * beltMaterial.mainTextureScale.x, 0f);
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

        float widthScale = 1f;

        void BuildBeltMesh()
        {
            // ---------------- the diner machine, layered ----------------
            // belt surface (recessed, moving)  ->  dark recess walls  ->  bevelled chrome rims  ->
            // cherry enamel sidewalls (outer + inner)  ->  teal counter inset in the centre  ->
            // chassis, chrome legs, floor contact shadow.
            // +side must mean "away from the track centre" whatever the loop's winding.
            {
                var tb0 = Track.GetBounds();
                Vector3 p0 = Track.Sample(0);
                Vector3 s0 = Vector3.Cross(Vector3.up, Track.Tangent(0f)).normalized;
                outSign = Vector3.Distance(p0 + s0, tb0.center) > Vector3.Distance(p0 - s0, tb0.center) ? 1f : -1f;
            }
            float W = config.itemWorldSize * config.beltWidthFactor * widthScale;   // belt surface width
            float half = W * 0.5f;
            float rimTop = 0.14f, wallBottom = -0.5f;

            // Belt surface.
            var surface = Ribbon("BeltSurface", -half, half, 0f, 0f, true);
            beltMaterial = new Material(PlaceholderMaterials.FindShader());
            var tile = ArtCatalog.LoadTexture("env/rd3d_belt_surface_tile_512_v001");
            beltMaterial.mainTexture = tile != null ? tile : SlatTexture();
            if (tile != null) tile.wrapMode = TextureWrapMode.Repeat;
            beltMaterial.mainTextureScale = new Vector2(tile != null ? 0.9f : 1.2f, 1f);
            beltMaterial.color = Color.white;
            if (beltMaterial.HasProperty("_BaseMap")) beltMaterial.SetTexture("_BaseMap", beltMaterial.mainTexture);
            SetPbr(beltMaterial, 0.15f, 0.35f);
            surface.GetComponent<MeshRenderer>().material = beltMaterial;

            // Recess walls: short dark verticals just outside the surface, so the belt reads as sunk in.
            var recessColor = new Color(0.12f, 0.1f, 0.11f);
            Ribbon("RecessOuter", half + 0.02f, half + 0.02f, rimTop - 0.02f, 0f, false).GetComponent<MeshRenderer>().material = Mat(recessColor, 0.2f, 0.3f);
            Ribbon("RecessInner", -(half + 0.02f), -(half + 0.02f), rimTop - 0.02f, 0f, false).GetComponent<MeshRenderer>().material = Mat(recessColor, 0.2f, 0.3f);

            // Chrome rims: a flat cap ring on top of each wall + light and dark bevel lines.
            float rimW = 0.2f;
            Ribbon("RimOuter", half + 0.02f, half + 0.02f + rimW, rimTop, rimTop, false).GetComponent<MeshRenderer>().material = Mat(Style.Chrome, 0.45f, 0.85f);
            Ribbon("RimInner", -(half + 0.02f + rimW), -(half + 0.02f), rimTop, rimTop, false).GetComponent<MeshRenderer>().material = Mat(Style.Chrome, 0.45f, 0.85f);
            MakeRail(half + 0.02f + rimW * 0.5f, Style.ChromeLight, 0.06f, rimTop + 0.012f);
            MakeRail(-(half + 0.02f + rimW * 0.5f), Style.ChromeLight, 0.06f, rimTop + 0.012f);
            MakeRail(half + 0.02f + rimW, new Color(0.25f, 0.28f, 0.32f), 0.05f, rimTop - 0.01f);
            MakeRail(-(half + 0.02f + rimW), new Color(0.25f, 0.28f, 0.32f), 0.05f, rimTop - 0.01f);

            // Enamel sidewalls: outer and inner, cherry red with a deep-red lower band.
            float wallOut = half + 0.02f + rimW, wallIn = -(half + 0.02f + rimW);
            Ribbon("WallOuter", wallOut, wallOut, rimTop, wallBottom, false).GetComponent<MeshRenderer>().material = Mat(Style.Cherry, 0.1f, 0.75f);
            Ribbon("WallInner", wallIn, wallIn, rimTop, wallBottom * 0.6f, false).GetComponent<MeshRenderer>().material = Mat(Style.Cherry, 0.1f, 0.75f);
            MakeRail(wallOut + 0.005f, Style.DeepRed, 0.08f, wallBottom + 0.08f);     // shadow band at the foot of the wall
            MakeRail(wallOut + 0.004f, new Color(1f, 0.6f, 0.62f, 1f), 0.035f, rimTop - 0.06f);  // enamel highlight just under the rim

            // Centre counter inset: deep-teal recessed disc with a chrome ring and the optional badge.
            var tb = Track.GetBounds();
            Vector3 c = new Vector3(tb.center.x, 0f, tb.center.z);
            float innerRx = Mathf.Max(0.3f, tb.extents.x - Mathf.Abs(wallIn) - 0.05f);
            float innerRz = Mathf.Max(0.3f, tb.extents.z - Mathf.Abs(wallIn) - 0.05f);
            decor.Add(MakePuck("CounterInset", c + Vector3.up * (wallBottom * 0.6f + 0.12f), innerRx * 0.98f, innerRz * 0.98f, 0.08f, Style.Teal, 0.1f, 0.5f));
            decor.Add(MakePuck("CounterRing", c + Vector3.up * (wallBottom * 0.6f + 0.17f), innerRx * 0.75f, innerRz * 0.75f, 0.03f, Style.Chrome, 0.45f, 0.85f));
            decor.Add(MakePuck("CounterTop", c + Vector3.up * (wallBottom * 0.6f + 0.18f), innerRx * 0.7f, innerRz * 0.7f, 0.03f, new Color(0.14f, 0.42f, 0.45f), 0.2f, 0.6f));
            var badge = ArtCatalog.Load("env/rd3d_belt_center_badge_v001");
            if (badge != null)
            {
                var bgo = new GameObject("CenterBadge");
                bgo.transform.SetParent(transform, false);
                bgo.transform.position = c + Vector3.up * (wallBottom * 0.6f + 0.2f);
                bgo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var sr = bgo.AddComponent<SpriteRenderer>(); sr.sprite = badge;
                BeltItem.FitSprite(sr, Mathf.Min(innerRx, innerRz) * 0.8f);
                decor.Add(bgo);
            }

            // Chassis + legs + floor shadow: the machine sits on something.
            float rx = tb.extents.x + wallOut + 0.1f, rz = tb.extents.z + wallOut + 0.1f;
            decor.Add(MakePuck("Chassis", c + Vector3.up * (wallBottom - 0.12f), rx * 0.92f, rz * 0.92f, 0.24f, Style.ChromeShadow * 0.75f, 0.5f, 0.4f));
            decor.Add(MakePuck("ChassisTrim", c + Vector3.up * (wallBottom - 0.01f), rx * 0.96f, rz * 0.96f, 0.05f, Style.Chrome, 0.45f, 0.85f));
            float floorY = wallBottom - 0.95f;
            for (int i = 0; i < 4; i++)
            {
                float a = (i + 0.5f) / 4f * Mathf.PI * 2f;
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(leg.GetComponent<Collider>());
                leg.transform.SetParent(transform, false);
                leg.transform.localPosition = c + new Vector3(Mathf.Cos(a) * rx * 0.62f, (wallBottom - 0.24f + floorY) * 0.5f, Mathf.Sin(a) * rz * 0.62f);
                leg.transform.localScale = new Vector3(0.26f, (wallBottom - 0.24f - floorY) * 0.5f, 0.26f);
                leg.GetComponent<Renderer>().material = Mat(Style.Chrome, 0.45f, 0.8f);
                decor.Add(leg);
            }
            var shadowGo = new GameObject("FloorShadow");
            shadowGo.transform.SetParent(transform, false);
            shadowGo.transform.position = c + Vector3.up * (floorY + 0.01f);
            shadowGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var shadow = shadowGo.AddComponent<SpriteRenderer>();
            shadow.sprite = UI.ProcSprites.Glow(128, 1.4f);
            shadow.color = new Color(0f, 0f, 0f, 0.5f);
            shadow.sortingOrder = -5;
            shadowGo.transform.localScale = new Vector3(rx * 2.4f / 1.28f, rz * 2.4f / 1.28f, 1f);
            decor.Add(shadowGo);
        }

        /// <summary>A ribbon along the track between two side offsets and two heights (flat when y0 == y1, vertical when offsets match).</summary>
        GameObject Ribbon(string name, float off0, float off1, float y0, float y1, bool scrollUv)
        {
            int n = Track.SampleCount;
            // Front and back faces get their own vertices so RecalculateNormals never averages opposite
            // normals to zero (which renders black).
            var verts = new Vector3[n * 4];
            var uvs = new Vector2[n * 4];
            var tris = new int[(n - 1) * 12];
            float acc = 0f;
            Vector3 prev = Track.Sample(0);
            for (int i = 0; i < n; i++)
            {
                Vector3 p = Track.Sample(i);
                acc += Vector3.Distance(prev, p); prev = p;
                Vector3 side = Vector3.Cross(Vector3.up, Track.Tangent(acc)).normalized * outSign;
                Vector3 a = p + side * off0 + Vector3.up * y0;
                Vector3 b = p + side * off1 + Vector3.up * y1;
                float u = scrollUv ? acc : i / (float)n;
                verts[i * 2] = a; verts[i * 2 + 1] = b;
                verts[n * 2 + i * 2] = a; verts[n * 2 + i * 2 + 1] = b;
                uvs[i * 2] = new Vector2(u, 0f); uvs[i * 2 + 1] = new Vector2(u, 1f);
                uvs[n * 2 + i * 2] = new Vector2(u, 0f); uvs[n * 2 + i * 2 + 1] = new Vector2(u, 1f);
            }
            for (int i = 0; i < n - 1; i++)
            {
                int b = i * 2, t = i * 12, k = n * 2 + i * 2;
                tris[t] = b; tris[t + 1] = b + 2; tris[t + 2] = b + 1;
                tris[t + 3] = b + 1; tris[t + 4] = b + 2; tris[t + 5] = b + 3;
                tris[t + 6] = k; tris[t + 7] = k + 1; tris[t + 8] = k + 2;
                tris[t + 9] = k + 1; tris[t + 10] = k + 3; tris[t + 11] = k + 2;
            }
            var mesh = new Mesh { name = name };
            mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            decor.Add(go);
            return go;
        }

        static Material Mat(Color c, float metallic, float smooth)
        {
            var m = new Material(PlaceholderMaterials.FindShader());
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            SetPbr(m, metallic, smooth);
            return m;
        }

        static void SetPbr(Material m, float metallic, float smooth)
        {
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
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
                Vector3 side = Vector3.Cross(Vector3.up, Track.Tangent(acc)).normalized * offset * outSign;
                lr.SetPosition(i, p + side + Vector3.up * lift);
            }
            decor.Add(lr.gameObject);
        }

        static Texture2D slatTex;
        /// <summary>Broad, readable belt slats (not thin roller lines): dark rubber with soft edge highlights.</summary>
        static Texture2D SlatTexture()
        {
            if (slatTex != null) return slatTex;
            const int w = 256, h = 64, slat = 64;
            slatTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
            var px = new Color[w * h];
            var baseC = new Color(0.17f, 0.17f, 0.19f);
            var hi = new Color(0.36f, 0.37f, 0.4f);
            var gap = new Color(0.07f, 0.06f, 0.07f);
            for (int x = 0; x < w; x++)
            {
                int i = x % slat;
                float u = (i + 0.5f) / slat;
                float edge = Mathf.Exp(-Mathf.Pow((u - 0.08f) * 9f, 2f)) * 0.9f;      // highlight on the leading edge
                float shade = 1f - 0.25f * Mathf.Pow(u, 2f);                         // falls off toward the trailing edge
                Color c = Color.Lerp(baseC * shade, hi, edge);
                if (i < 3 || i > slat - 4) c = gap;
                for (int y = 0; y < h; y++)
                {
                    float v = Mathf.Abs(y / (float)(h - 1) - 0.5f) * 2f;
                    float side = 1f - Mathf.Pow(v, 8f) * 0.5f;
                    px[y * w + x] = new Color(c.r * side, c.g * side, c.b * side, 1f);
                }
            }
            slatTex.SetPixels(px); slatTex.Apply();
            return slatTex;
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
