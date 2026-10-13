using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RetroDiner.Belt;
using RetroDiner.Core;
using RetroDiner.Data;
using RetroDiner.Gameplay;
using RetroDiner.UI;

namespace RetroDiner.Boot
{
    /// <summary>
    /// The only component that lives in the scene file. Builds cameras, lights, canvases and all
    /// gameplay objects from code, so the project has no hand-authored prefabs or scene hierarchy.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public const int BackgroundLayer = 30;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#if UNITY_ANDROID || UNITY_IOS
            Screen.orientation = ScreenOrientation.Portrait;
#endif
            var cfg = Resources.Load<GameConfig>("RetroDiner/GameConfig");
            if (cfg == null || cfg.items == null || cfg.items.Length == 0 || cfg.levels == null || cfg.levels.Length == 0)
            {
                Debug.Log("[Boot] No generated GameConfig found in Resources/RetroDiner — using the in-code default content. Run 'Retro Diner > Setup Project' in the Editor to create assets.");
                cfg = ContentLibrary.CreateDefaultConfig();
            }
            GameServices.Initialize(cfg, transform);
            GameEvents.ClearAll();

            // ---- EventSystem ----
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // ---- Background camera + canvas ----
            var bgCamGo = new GameObject("BackgroundCamera");
            var bgCam = bgCamGo.AddComponent<Camera>();
            bgCam.orthographic = true;
            bgCam.orthographicSize = 5f;
            bgCam.depth = -10;
            bgCam.clearFlags = CameraClearFlags.SolidColor;
            bgCam.backgroundColor = new Color(0.12f, 0.08f, 0.1f);
            bgCam.cullingMask = 1 << BackgroundLayer;
            bgCam.transform.position = new Vector3(0f, -500f, 0f);
            bgCam.nearClipPlane = 0.1f; bgCam.farClipPlane = 50f;

            var bgCanvas = UIBuilder.CreateCanvas("BackgroundCanvas", 0, RenderMode.ScreenSpaceCamera, bgCam, BackgroundLayer);
            var bgSprite = ArtCatalog.Pick(cfg.gameplayBackground, ArtCatalog.GameplayBackground);
            var bg = UIBuilder.Panel(bgCanvas.transform, "GameplayBackground", bgSprite != null ? Color.white : new Color(0.55f, 0.16f, 0.2f), bgSprite, false);
            UIBuilder.Stretch(bg.rectTransform);
            if (bgSprite != null)
            {
                var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = 1080f / 1920f;
            }
            else
            {
                // Placeholder: checkerboard floor band + counter band.
                var floor = UIBuilder.Panel(bgCanvas.transform, "Floor", new Color(0.9f, 0.88f, 0.82f), null, false);
                floor.rectTransform.anchorMin = new Vector2(0f, 0f); floor.rectTransform.anchorMax = new Vector2(1f, 0.45f);
                floor.rectTransform.offsetMin = Vector2.zero; floor.rectTransform.offsetMax = Vector2.zero;
                var wall = UIBuilder.Panel(bgCanvas.transform, "Wall", new Color(0.5f, 0.78f, 0.74f), null, false);
                wall.rectTransform.anchorMin = new Vector2(0f, 0.45f); wall.rectTransform.anchorMax = new Vector2(1f, 1f);
                wall.rectTransform.offsetMin = Vector2.zero; wall.rectTransform.offsetMax = Vector2.zero;
            }

            // ---- Belt camera ----
            var camGo = new GameObject("BeltCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.depth = 0;
            cam.clearFlags = CameraClearFlags.Depth;
            cam.cullingMask = ~(1 << BackgroundLayer);
            cam.nearClipPlane = 0.3f; cam.farClipPlane = 60f;
            camGo.AddComponent<AudioListener>();
            var rig = camGo.AddComponent<CameraRig>();
            rig.Setup(cam, cfg);

            // ---- Light ----
            var lightGo = new GameObject("KeyLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.93f, 0.82f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.5f;
            lightGo.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
            var fillGo = new GameObject("FillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.6f, 0.75f, 0.9f);
            fill.intensity = 0.35f;
            fillGo.transform.rotation = Quaternion.Euler(30f, 150f, 0f);

            // ---- Belt + factory + VFX ----
            var beltGo = new GameObject("Belt");
            var belt = beltGo.AddComponent<BeltController>();
            var itemsRoot = new GameObject("Items").transform;
            itemsRoot.SetParent(beltGo.transform, false);
            var factory = new ItemFactory(itemsRoot, cfg.itemWorldSize, rig.BillboardRotation);
            var vfxGo = new GameObject("VFX");
            var vfx = vfxGo.AddComponent<Vfx>();
            vfx.Setup();

            // ---- Canvases ----
            var hudCanvas = UIBuilder.CreateCanvas("HUDCanvas", 10);
            var menuCanvas = UIBuilder.CreateCanvas("MenuCanvas", 20);

            var controllerGo = new GameObject("GameplayController");
            var controller = controllerGo.AddComponent<GameplayController>();
            var input = controllerGo.AddComponent<TapInput>();
            input.Cam = cam;

            var hud = hudCanvas.gameObject.AddComponent<HudView>();
            hud.Build(hudCanvas, cfg, controller.Lookup, controller.TogglePause, controller.UseBooster);

            var menu = menuCanvas.gameObject.AddComponent<MainMenuView>();
            menu.Build(menuCanvas, cfg, controller.StartLevel);

            controller.Setup(cfg, cam, rig, hud, belt, factory, menu, vfx, input);

            var fakeAds = GameServices.Ads as FakeAdService;
            if (fakeAds != null) fakeAds.ShowOverlay = hud.ShowFakeAd;

            hud.Root.gameObject.SetActive(false);
            menu.ShowMenu();
            Debug.Log("[Boot] Retro Diner 3D ready. Items: " + cfg.items.Length + ", levels: " + cfg.levels.Length);
        }
    }
}
