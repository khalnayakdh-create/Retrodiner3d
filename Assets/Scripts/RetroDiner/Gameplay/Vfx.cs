using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RetroDiner.Core;
using RetroDiner.Data;
using RetroDiner.UI;

namespace RetroDiner.Gameplay
{
    /// <summary>World-space particle bursts (pooled) and a UI confetti shower for the win screen.</summary>
    public class Vfx : MonoBehaviour
    {
        readonly List<ParticleSystem> pool = new List<ParticleSystem>();
        Material particleMat;

        public void Setup()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            particleMat = new Material(shader);
            particleMat.mainTexture = Texture2D.whiteTexture;
            for (int i = 0; i < 6; i++) pool.Add(Create());
        }

        ParticleSystem Create()
        {
            var go = new GameObject("Burst");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            // A freshly added system is already playing; stop it before touching duration etc.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = particleMat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        public void Burst(Vector3 worldPos, Color color, int count = 18)
        {
            ParticleSystem ps = null;
            foreach (var p in pool) if (!p.isPlaying) { ps = p; break; }
            if (ps == null) { ps = Create(); pool.Add(ps); }
            ps.transform.position = worldPos;
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.5f));
            ps.Emit(count);
        }

        /// <summary>UI confetti over the whole screen (win celebration).</summary>
        public static void Confetti(RectTransform layer, int count = 50)
        {
            if (layer == null) return;
            Sprite[] sprites = null;
            var sheet = ArtCatalog.ConfettiSheet;
            if (sheet != null)
            {
                var all = Resources.LoadAll<Sprite>("RetroDiner/Art/fx/rd3d_fx_confetti_sparkles_sheet_4x4_v001");
                if (all != null && all.Length > 0) sprites = all;
            }
            Color[] colors = { UIBuilder.Red, UIBuilder.Mint, UIBuilder.Gold, UIBuilder.Cream, UIBuilder.Sky };
            for (int i = 0; i < count; i++)
            {
                var img = UIBuilder.Panel(layer, "Confetti", colors[i % colors.Length], null, false);
                if (sprites != null) { img.sprite = sprites[i % sprites.Length]; img.color = Color.white; img.preserveAspect = true; }
                float size = Random.Range(28f, 64f);
                img.rectTransform.sizeDelta = new Vector2(size, size * (sprites != null ? 1f : Random.Range(0.4f, 1f)));
                float x = Random.Range(-540f, 540f);
                float startY = Random.Range(1000f, 1500f);
                float endY = Random.Range(-1100f, -700f);
                float drift = Random.Range(-200f, 200f);
                float rot = Random.Range(-720f, 720f);
                float dur = Random.Range(1.6f, 2.8f);
                var rt = img.rectTransform;
                rt.anchoredPosition = new Vector2(x, startY);
                Tween.To(dur, t =>
                {
                    if (rt == null) return;
                    rt.anchoredPosition = new Vector2(x + Mathf.Sin(t * 6f) * 40f + drift * t, Mathf.Lerp(startY, endY, t));
                    rt.localRotation = Quaternion.Euler(0f, 0f, rot * t);
                }, Ease.InQuad, () => { if (rt != null) Destroy(rt.gameObject); }, true, Random.Range(0f, 0.6f));
            }
        }
    }
}
