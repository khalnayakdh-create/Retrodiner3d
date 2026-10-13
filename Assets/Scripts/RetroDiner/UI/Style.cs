using UnityEngine;

namespace RetroDiner.UI
{
    /// <summary>
    /// Central art-direction values (palette, timings, sizes) from the polish brief, so tuning does
    /// not mean editing many scripts. Colors are starting swatches; depth comes from shading/materials.
    /// </summary>
    public static class Style
    {
        // Palette
        public static readonly Color Cherry = Hex("#DB3345");
        public static readonly Color DeepRed = Hex("#8F2232");
        public static readonly Color Mint = Hex("#76D0BE");
        public static readonly Color Teal = Hex("#20636A");
        public static readonly Color Cream = Hex("#FFF0CF");
        public static readonly Color Gold = Hex("#FFC957");
        public static readonly Color Ink = Hex("#38251D");
        public static readonly Color ChromeShadow = Hex("#647782");
        public static readonly Color ChromeLight = Hex("#E8EEF4");
        public static readonly Color Chrome = Hex("#B9C3CC");

        // Timings (seconds) — 13A match-to-order animation and general feel
        public static float PressSquash = 0.12f;
        public static float MatchConfirm = 0.15f;      // 120–180 ms
        public static float MatchStagger = 0.06f;      // 50–70 ms
        public static float MatchFlight = 0.42f;       // 350–500 ms
        public static float TrayLanding = 0.28f;       // 250–350 ms belt-to-tray
        public static float TicketArrive = 0.22f;
        public static float ModalIn = 0.2f;            // 180–250 ms, no long overshoot

        // Layout (reference canvas 1080x1920, measured from the bottom)
        public static float TopBarY = -80f;
        public static float TicketRailY = -330f;
        public static float TrayY = 405f;
        public static float BoosterY = 165f;

        public static bool ReducedMotion => RetroDiner.Core.GameServices.Profile != null && RetroDiner.Core.GameServices.Profile.reducedMotion;

        public static Color Hex(string hex)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.magenta;
        }
    }
}
