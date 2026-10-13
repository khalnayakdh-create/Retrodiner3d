# Asset request — visual polish pass (v2 pack)

Live copy with prompts: https://claude.ai/code/artifact/e84a6129-8368-4862-b0ad-d5f903045a28

Generate with ChatGPT image gen, drop into `Chat Gpt Images/<folder>/` with these exact names; `Retro Diner > Import Art` matches by name.
Global style: toy-like 3D render, cherry-red #DB3345 / deep red #8F2232 / mint #76D0BE / teal #20636A / cream #FFF0CF / gold #FFC957 / text #38251D / chrome shadow #647782; upper-left key light; alpha PNG; no baked text except the logo pieces; 9-slice pieces keep detail in the border band.

| # | File | Size |
|---|---|---|
| A1 | env/rd3d_env_diner_interior_equirect_2048x1024_v001.png | 2048x1024 opaque equirect (chrome reflections) |
| A2 | env/rd3d_belt_surface_tile_512_v001.png | 512x512 opaque, seamless horizontally |
| A3 | env/rd3d_belt_center_badge_v001.png | 1024x1024 alpha |
| A4 | backgrounds/rd3d_bg_gameplay_diner_interior_v002_1080x1920.png (+_1080x2340) | opaque, reframed: crisp top 25%, dim/blurred middle, chrome counter bottom |
| B1 | ui/rd3d_ui_ticket_frame_v002.png | 900x960 alpha 9-slice, compact ticket w/ clip + progress well |
| B2 | ui/rd3d_ui_chip_chrome_v001.png | 768x256 alpha 9-slice pill |
| B3 | ui/rd3d_ui_badge_round_red_v001.png | 256x256 alpha |
| B4 | ui/rd3d_ui_bar_track_v001.png, rd3d_ui_bar_fill_green_v001.png, rd3d_ui_bar_fill_gold_v001.png | 1024x128 / 1024x96, 9-slice |
| B5 | icons/rd3d_icon_spatula_v001.png, _bell_, _coaster_, _extra_plate_ | 512x512 alpha, tool only (no button) |
| B6 | ui/rd3d_ui_booster_chassis_blue_v001.png, _purple_, _orange_, _grey_ | 1024x1024 alpha, empty centre |
| B7 | ui/rd3d_ui_button_green_v001.png | 2172x724 alpha 9-slice |
| B8 | ui/rd3d_ui_button_{red,mint,gold,green}_pressed_v001.png | 2172x724 alpha 9-slice |
| B9 | ui/rd3d_ui_button_small_blue_v001.png, _small_orange_ | 1024x512 alpha 9-slice |
| B10 | icons/rd3d_icon_{gear,shop,map,jukebox,punchcard,heart_plus}_v001.png | 512x512 alpha |
| B11 | ui/rd3d_ui_check_stamp_v001.png | 512x512 alpha |
| B12 | ui/rd3d_ui_topbar_plate_v001.png | 2048x256 alpha 9-slice |
| B13 | ui/rd3d_ui_slot_well_glow_v001.png | 512x512 alpha |
| C1 | ui/rd3d_ui_modal_frame_v002.png | 1122x1122 alpha 9-slice, short gingham header |
| C2 | ui/rd3d_ui_tray_frame_v002.png (1536x512 9-slice) + ui/rd3d_ui_tray_well_v001.png (512x512) | alpha |
| C3 | ui/rd3d_illo_extra_plate_stack_v001.png | 1024x1024 alpha |
| C4 | ui/rd3d_logo_retro_diner_3d_v001.png | 2048x1024 alpha, text "RETRO DINER 3D" + ribbon "ORDER UP!" |
| C5 | ui/rd3d_logo_tagline_v001.png | 1536x384 alpha, "Good Food, Brighter Days" |
| C6 | fx/rd3d_fx_steam_puff_v001.png (512) + fx/rd3d_fx_sparkle_gold_v001.png (256) | alpha |
| D | Fonts: Fredoka Bold/SemiBold, Nunito SemiBold/Bold (Google Fonts, OFL) → Assets/Resources/RetroDiner/Fonts/ (loaded by name: Fredoka-Bold, Nunito-SemiBold) | ttf |

Kept as-is: all 7 foods (+charred), cloches, napkin, spills, currency, stars, pause/close/confirm, tip jars, confetti sheet, banner, menu + map backgrounds.
