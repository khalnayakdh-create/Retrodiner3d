using UnityEngine;
using RetroDiner.Audio;
using RetroDiner.Data;
using RetroDiner.Meta;

namespace RetroDiner.Core
{
    /// <summary>
    /// The one service locator allowed by the architecture. Everything else is injected.
    /// </summary>
    public static class GameServices
    {
        public static GameConfig Config { get; private set; }
        public static PlayerProfile Profile { get; private set; }
        public static SaveService Save { get; private set; }
        public static AudioService Audio { get; private set; }
        public static IHapticService Haptics { get; private set; }
        public static IAdService Ads { get; private set; }
        public static IIAPService IAP { get; private set; }
        public static IShareService Share { get; private set; }
        public static EconomyService Economy { get; private set; }
        public static AdPolicyService AdPolicy { get; private set; }
        public static bool Initialized { get; private set; }

        public static void Initialize(GameConfig config, Transform audioParent)
        {
            Config = config;
            Save = new SaveService();
            Profile = Save.Load();
            Audio = new AudioService(audioParent);
            Haptics = new HandheldHapticService { Enabled = Profile.hapticsOn };
            Ads = new FakeAdService();
            IAP = new FakeIAPService();
            Share = new StubShareService();
            if (config.storeCatalog == null) { config.storeCatalog = ScriptableObject.CreateInstance<StoreCatalog>(); config.storeCatalog.offers = StoreCatalog.DefaultOffers(); }
            if (config.adPolicy == null) config.adPolicy = ScriptableObject.CreateInstance<AdPolicy>();
            Economy = new EconomyService(config, config.storeCatalog);
            AdPolicy = new AdPolicyService(config.adPolicy);
            Audio.SfxEnabled = Profile.soundOn;
            Audio.MusicEnabled = Profile.musicOn;
            Initialized = true;
        }

        /// <summary>Dev helper: wipe the save and reload a fresh profile without rebuilding services.</summary>
        public static void ResetProfile()
        {
            if (Save == null) Save = new SaveService();
            Save.Delete();
            Profile = Save.Load();
            if (Config != null) Economy = new EconomyService(Config, Config.storeCatalog);
            GameEvents.RaiseProfileChanged();
        }

        public static void SaveProfile()
        {
            if (Save != null && Profile != null) Save.Save(Profile);
            GameEvents.RaiseProfileChanged();
        }
    }
}
