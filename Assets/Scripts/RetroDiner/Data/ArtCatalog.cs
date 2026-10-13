using System.Collections.Generic;
using UnityEngine;

namespace RetroDiner.Data
{
    /// <summary>
    /// Maps logical asset names to the PNG pack in Resources/RetroDiner/Art (copied there from
    /// "Chat Gpt Images" by the Editor menu Retro Diner > Import Art). Every lookup is optional:
    /// a missing sprite returns null and the caller draws a placeholder instead.
    /// </summary>
    public static class ArtCatalog
    {
        const string Root = "RetroDiner/Art/";
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static readonly HashSet<string> missing = new HashSet<string>();

        public static void ClearCache() { cache.Clear(); missing.Clear(); }

        public static Sprite Load(string relativePathWithoutExtension)
        {
            if (string.IsNullOrEmpty(relativePathWithoutExtension)) return null;
            Sprite s;
            if (cache.TryGetValue(relativePathWithoutExtension, out s)) return s;
            if (missing.Contains(relativePathWithoutExtension)) return null;
            s = Resources.Load<Sprite>(Root + relativePathWithoutExtension);
            if (s == null) { missing.Add(relativePathWithoutExtension); return null; }
            cache[relativePathWithoutExtension] = s;
            return s;
        }

        public static Texture2D LoadTexture(string relativePathWithoutExtension)
        {
            return Resources.Load<Texture2D>(Root + relativePathWithoutExtension);
        }

        // ----- food -----
        public static Sprite Food(string itemId, bool charred)
        {
            string file;
            if (!FoodFiles.TryGetValue(itemId, out file)) return null;
            return Load("food/" + file + (charred ? "_charred_v001" : "_normal_v001"));
        }

        public static readonly Dictionary<string, string> FoodFiles = new Dictionary<string, string>
        {
            { "pancakes", "rd3d_food_pancake_stack" },
            { "skillet_eggs", "rd3d_food_skillet_eggs" },
            { "coffee_mug", "rd3d_food_red_coffee_mug" },
            { "milkshake", "rd3d_food_strawberry_milkshake_bottle" },
            { "bacon", "rd3d_food_wavy_bacon" },
            { "waffle", "rd3d_food_belgian_waffle_slice" },
            { "cinnamon_roll", "rd3d_food_cinnamon_roll" },
        };

        // ----- obstacles -----
        public static Sprite ClocheClosed => Load("obstacles/rd3d_obstacle_silver_cloche_closed_v001");
        public static Sprite ClocheOpen => Load("obstacles/rd3d_obstacle_silver_cloche_open_v001");
        public static Sprite Napkin => Load("obstacles/rd3d_obstacle_tied_gingham_napkin_v001");
        public static Sprite SyrupSpill => Load("obstacles/rd3d_obstacle_syrup_spill_v001");
        public static Sprite KetchupSpill => Load("obstacles/rd3d_obstacle_ketchup_spill_v001");

        // ----- boosters -----
        public static Sprite BoosterSpatula => Load("boosters/rd3d_booster_golden_spatula_v001");
        public static Sprite BoosterBell => Load("boosters/rd3d_booster_order_up_bell_v001");
        public static Sprite BoosterRewind => Load("boosters/rd3d_booster_rewind_coaster_v001");
        public static Sprite BoosterExtraPlate => Load("boosters/rd3d_booster_extra_plate_v001");

        // ----- ui -----
        public static Sprite TicketNormal => Load("ui/rd3d_ui_order_ticket_normal_v001");
        public static Sprite TicketActive => Load("ui/rd3d_ui_order_ticket_active_gold_v001");
        public static Sprite TicketComplete => Load("ui/rd3d_ui_order_ticket_complete_v001");
        public static Sprite Tray7 => Load("ui/rd3d_ui_holding_tray_07_slots_v001");
        public static Sprite Tray8 => Load("ui/rd3d_ui_holding_tray_08_slots_v001");
        public static Sprite TipJar10 => Load("ui/rd3d_ui_tip_jar_fill_010_v001");
        public static Sprite TipJar50 => Load("ui/rd3d_ui_tip_jar_fill_050_v001");
        public static Sprite TipJar100 => Load("ui/rd3d_ui_tip_jar_fill_100_v001");
        public static Sprite StarBar => Load("ui/rd3d_ui_star_progress_bar_v001");
        public static Sprite StarFilled => Load("ui/rd3d_ui_progress_star_filled_gold_v001");
        public static Sprite StarEmpty => Load("ui/rd3d_ui_progress_star_empty_grey_v001");
        public static Sprite PauseButton => Load("ui/rd3d_ui_pause_button_blue_v001");
        public static Sprite CloseButton => Load("ui/rd3d_ui_close_button_red_v001");
        public static Sprite ConfirmButton => Load("ui/rd3d_ui_confirm_button_green_v002");
        public static Sprite ButtonRed => Load("ui/rd3d_ui_primary_button_red_v001");
        public static Sprite ButtonMint => Load("ui/rd3d_ui_primary_button_mint_v001");
        public static Sprite ButtonGold => Load("ui/rd3d_ui_primary_button_gold_v001");
        public static Sprite PopupPanel => Load("ui/rd3d_ui_popup_panel_gingham_v001");

        // ----- currency -----
        public static Sprite Coin => Load("currency/rd3d_currency_diner_coin_v001");
        public static Sprite Token => Load("currency/rd3d_currency_jukebox_token_v001");
        public static Sprite Heart => Load("currency/rd3d_currency_energy_heart_v001");

        // ----- fx / backgrounds -----
        public static Sprite ShiftFinishedBanner => Load("fx/rd3d_fx_shift_finished_banner_blank_v001");
        public static Texture2D ConfettiSheet => LoadTexture("fx/rd3d_fx_confetti_sparkles_sheet_4x4_v001");
        public static Sprite GameplayBackground => Load("backgrounds/rd3d_bg_gameplay_diner_interior_v002_1080x2340") ?? Load("backgrounds/rd3d_bg_gameplay_diner_interior_v002_1080x1920") ?? Load("backgrounds/rd3d_bg_gameplay_diner_interior_1080x1920_v001");
        public static Sprite MainMenuBackground => Load("backgrounds/rd3d_bg_main_menu_diner_dusk_1080x1920_v001");
        public static Sprite LevelSelectBackground => Load("backgrounds/rd3d_bg_level_select_roadside_map_1080x1920_v001");

        /// <summary>Config override first, then Resources, then null.</summary>
        public static Sprite Pick(Sprite configOverride, Sprite fromResources) => configOverride != null ? configOverride : fromResources;
    }
}
