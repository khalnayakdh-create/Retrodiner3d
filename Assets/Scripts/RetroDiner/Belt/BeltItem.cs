using UnityEngine;
using RetroDiner.Data;

namespace RetroDiner.Belt
{
    public enum ItemObstacle { None, Cloche, Napkin }

    /// <summary>
    /// View + state for one dish on the belt. Movement is spline-driven (kinematic rigidbody so VFX
    /// can bump it, but it never falls off). Visual = assigned prefab, else a billboarded sprite,
    /// else a colored primitive with a label.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class BeltItem : MonoBehaviour
    {
        public ItemDefinition Definition { get; private set; }
        public float Distance;
        public int Laps;
        public bool IsCharred { get; private set; }
        public ItemObstacle Obstacle { get; private set; }
        public BeltItem Partner;          // napkin pair
        public bool InFlight;

        public bool IsPickable => Obstacle == ItemObstacle.None && !InFlight && gameObject.activeSelf;

        Transform visualRoot;
        SpriteRenderer sprite;
        Renderer primitive;
        TextMesh label;
        GameObject overlayObj;
        SpriteRenderer overlaySprite;
        Transform napkinLink;
        float worldSize = 0.95f;
        Quaternion billboardRotation = Quaternion.identity;
        float bobSeed;

        public void Init(ItemDefinition def, float size, Quaternion billboard)
        {
            Definition = def;
            worldSize = size;
            billboardRotation = billboard;
            IsCharred = false;
            Obstacle = ItemObstacle.None;
            Partner = null;
            InFlight = false;
            Laps = 0;
            bobSeed = Random.value * 10f;
            name = "Item_" + def.id;

            var col = GetComponent<SphereCollider>();
            col.radius = size * 0.5f;
            col.center = new Vector3(0f, size * 0.35f, 0f);

            var rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            BuildVisual();
            RefreshVisual();
        }

        void BuildVisual()
        {
            if (visualRoot != null) Destroy(visualRoot.gameObject);
            visualRoot = new GameObject("Visual").transform;
            visualRoot.SetParent(transform, false);
            sprite = null; primitive = null; label = null;

            if (Definition.prefab != null)
            {
                var inst = Instantiate(Definition.prefab, visualRoot);
                inst.transform.localPosition = Vector3.zero;
                primitive = inst.GetComponentInChildren<Renderer>();
            }
            else if (Definition.icon != null)
            {
                var go = new GameObject("Sprite");
                go.transform.SetParent(visualRoot, false);
                sprite = go.AddComponent<SpriteRenderer>();
                sprite.sprite = Definition.icon;
                FitSprite(sprite, worldSize);
                go.transform.localRotation = billboardRotation;
                go.transform.localPosition = new Vector3(0f, worldSize * 0.42f, 0f);
            }
            else
            {
                var prim = GameObject.CreatePrimitive(Definition.id == "coffee_mug" || Definition.id == "milkshake" ? PrimitiveType.Cylinder : PrimitiveType.Cube);
                Destroy(prim.GetComponent<Collider>());
                prim.transform.SetParent(visualRoot, false);
                prim.transform.localScale = new Vector3(worldSize * 0.8f, worldSize * 0.35f, worldSize * 0.8f);
                prim.transform.localPosition = new Vector3(0f, worldSize * 0.18f, 0f);
                primitive = prim.GetComponent<Renderer>();
                primitive.material = PlaceholderMaterials.Get(Definition.placeholderColor);

                var lgo = new GameObject("Label");
                lgo.transform.SetParent(visualRoot, false);
                label = lgo.AddComponent<TextMesh>();
                try
                {
                    var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (f != null) { label.font = f; lgo.GetComponent<MeshRenderer>().material = f.material; }
                }
                catch { }
                label.text = Definition.displayName;
                label.characterSize = 0.08f;
                label.fontSize = 48;
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.color = Color.white;
                lgo.transform.localRotation = billboardRotation;
                lgo.transform.localPosition = new Vector3(0f, worldSize * 0.55f, 0f);
            }

            // Soft blob shadow on the belt surface.
            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(visualRoot, false);
            var shadow = shadowGo.AddComponent<SpriteRenderer>();
            shadow.sprite = UI.ProcSprites.Glow(64, 2.2f);
            shadow.color = new Color(0f, 0f, 0f, 0.42f);
            shadow.sortingOrder = -2;
            shadowGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadowGo.transform.localPosition = new Vector3(0f, 0.005f, 0f);
            float sw = worldSize * 0.95f / 0.64f;
            shadowGo.transform.localScale = new Vector3(sw, sw * 0.55f, 1f);

            // Overlay sprite (cloche / napkin) drawn on top of the item.
            overlayObj = new GameObject("Overlay");
            overlayObj.transform.SetParent(visualRoot, false);
            overlaySprite = overlayObj.AddComponent<SpriteRenderer>();
            overlaySprite.sortingOrder = 5;
            overlayObj.transform.localRotation = billboardRotation;
            overlayObj.transform.localPosition = new Vector3(0f, worldSize * 0.48f, -0.02f);
            overlayObj.SetActive(false);
        }

        public static void FitSprite(SpriteRenderer sr, float targetSize)
        {
            if (sr == null || sr.sprite == null) return;
            var b = sr.sprite.bounds.size;
            float m = Mathf.Max(b.x, b.y, 0.0001f);
            sr.transform.localScale = Vector3.one * (targetSize / m);
        }

        public void SetObstacle(ItemObstacle obstacle)
        {
            Obstacle = obstacle;
            RefreshVisual();
        }

        public void OpenCloche()
        {
            if (Obstacle != ItemObstacle.Cloche) return;
            Obstacle = ItemObstacle.None;
            RefreshVisual();
            // Brief reveal pop.
            visualRoot.localScale = Vector3.one * 0.6f;
            Core.Tween.Scale(visualRoot, Vector3.one, 0.25f, Core.Ease.OutBack);
        }

        public void Untie()
        {
            if (Obstacle != ItemObstacle.Napkin) return;
            var other = Partner;
            Obstacle = ItemObstacle.None;
            Partner = null;
            if (napkinLink != null) { Destroy(napkinLink.gameObject); napkinLink = null; }
            RefreshVisual();
            if (other != null && other.Obstacle == ItemObstacle.Napkin) other.Untie();
        }

        public void SetCharred(bool charred)
        {
            if (IsCharred == charred) return;
            IsCharred = charred;
            RefreshVisual();
        }

        void RefreshVisual()
        {
            if (sprite != null)
            {
                var s = IsCharred && Definition.charredIcon != null ? Definition.charredIcon : Definition.icon;
                sprite.sprite = s;
                FitSprite(sprite, worldSize);
                sprite.color = IsCharred && Definition.charredIcon == null ? new Color(0.35f, 0.3f, 0.28f) : Color.white;
            }
            if (primitive != null && Definition.prefab == null)
            {
                primitive.material = PlaceholderMaterials.Get(IsCharred ? Color.Lerp(Definition.placeholderColor, Color.black, 0.7f) : Definition.placeholderColor);
            }
            if (label != null) label.text = IsCharred ? Definition.displayName + "\n(charred)" : Definition.displayName;

            // Cloche hides the item completely (mystery dish); napkin shows a small badge.
            bool cloched = Obstacle == ItemObstacle.Cloche;
            if (sprite != null) sprite.enabled = !cloched;
            if (primitive != null) primitive.enabled = !cloched;
            if (label != null) label.gameObject.SetActive(!cloched);

            if (overlaySprite != null)
            {
                Sprite ov = null;
                if (cloched) ov = ArtCatalog.ClocheClosed;
                else if (Obstacle == ItemObstacle.Napkin) ov = ArtCatalog.Napkin;
                if (ov != null)
                {
                    overlaySprite.sprite = ov;
                    overlaySprite.color = Color.white;
                    FitSprite(overlaySprite, cloched ? worldSize * 1.05f : worldSize * 0.55f);
                    overlayObj.transform.localPosition = cloched ? new Vector3(0f, worldSize * 0.45f, -0.02f) : new Vector3(worldSize * 0.3f, worldSize * 0.75f, -0.05f);
                    overlayObj.SetActive(true);
                }
                else if (cloched || Obstacle == ItemObstacle.Napkin)
                {
                    // Placeholder overlay: a flat colored square.
                    overlaySprite.sprite = PlaceholderMaterials.WhiteSprite;
                    overlaySprite.color = cloched ? new Color(0.78f, 0.8f, 0.85f) : new Color(0.85f, 0.15f, 0.2f, 0.9f);
                    overlayObj.transform.localScale = Vector3.one * (cloched ? worldSize : worldSize * 0.35f);
                    overlayObj.transform.localPosition = cloched ? new Vector3(0f, worldSize * 0.45f, -0.02f) : new Vector3(worldSize * 0.3f, worldSize * 0.75f, -0.05f);
                    overlayObj.SetActive(true);
                }
                else overlayObj.SetActive(false);
            }
        }

        /// <summary>Called by the belt every frame; draws the napkin link to the partner.</summary>
        public void UpdateNapkinLink()
        {
            if (Obstacle != ItemObstacle.Napkin || Partner == null) return;
            if (napkinLink == null)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(cube.GetComponent<Collider>());
                cube.name = "NapkinLink";
                cube.GetComponent<Renderer>().material = PlaceholderMaterials.Get(new Color(0.85f, 0.15f, 0.2f));
                napkinLink = cube.transform;
                napkinLink.SetParent(transform.parent, true);
            }
            Vector3 a = transform.position + Vector3.up * 0.1f;
            Vector3 b = Partner.transform.position + Vector3.up * 0.1f;
            napkinLink.position = (a + b) * 0.5f;
            Vector3 d = b - a;
            if (d.sqrMagnitude > 1e-4f) napkinLink.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
            napkinLink.localScale = new Vector3(0.08f, 0.08f, d.magnitude);
        }

        public void Bob(float time)
        {
            if (visualRoot == null) return;
            float y = Mathf.Sin((time + bobSeed) * 3f) * 0.015f;
            var p = visualRoot.localPosition; p.y = y; visualRoot.localPosition = p;
            if (sprite != null) sprite.transform.localRotation = billboardRotation * Quaternion.Euler(0f, 0f, Mathf.Sin((time + bobSeed) * 2.1f) * 2.5f);
        }

        public void ResetForPool()
        {
            if (napkinLink != null) { Destroy(napkinLink.gameObject); napkinLink = null; }
            Partner = null;
            InFlight = false;
            Obstacle = ItemObstacle.None;
            IsCharred = false;
            Laps = 0;
            if (visualRoot != null) visualRoot.localScale = Vector3.one;
            transform.localScale = Vector3.one;
        }
    }

    /// <summary>Shared unlit-ish materials for placeholders so we don't allocate one per item.</summary>
    public static class PlaceholderMaterials
    {
        static readonly System.Collections.Generic.Dictionary<Color, Material> cache = new System.Collections.Generic.Dictionary<Color, Material>();
        static Sprite whiteSprite;

        public static Shader FindShader()
        {
            string[] names = { "Universal Render Pipeline/Lit", "Standard", "Legacy Shaders/Diffuse", "Unlit/Color" };
            foreach (var n in names) { var s = Shader.Find(n); if (s != null) return s; }
            return Shader.Find("Sprites/Default");
        }

        public static Material Get(Color c)
        {
            Material m;
            if (cache.TryGetValue(c, out m) && m != null) return m;
            m = new Material(FindShader());
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.6f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.6f);
            cache[c] = m;
            return m;
        }

        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite == null)
                {
                    var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    var px = new Color[16];
                    for (int i = 0; i < 16; i++) px[i] = Color.white;
                    tex.SetPixels(px); tex.Apply();
                    whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                }
                return whiteSprite;
            }
        }
    }
}
