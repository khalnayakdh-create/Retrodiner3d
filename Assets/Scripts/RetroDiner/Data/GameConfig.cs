using UnityEngine;

namespace RetroDiner.Data
{
    /// <summary>
    /// Root config asset. Loaded from Resources/RetroDiner/GameConfig; if that asset does not exist
    /// (fresh checkout before running the Editor generator) a default is built in memory by ContentLibrary.
    /// </summary>
    [CreateAssetMenu(menuName = "Retro Diner/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Content")]
        public ItemDefinition[] items = new ItemDefinition[0];
        public LevelDefinition[] levels = new LevelDefinition[0];

        [Header("Rules")]
        public int trayCapacity = 7;
        public int ticketsVisible = 3;
        public int tokensPerTicket = 3;
        public float nearMissSeconds = 5f;
        public float nearMissTimeScale = 0.2f;
        public int continueCostCoins = 90;
        public int continueCostTokens = 10;
        public int continueClearItems = 3;
        public int coinsBase = 50;
        public int coinsPerStar = 25;

        [Header("Presentation")]
        public bool showBuildLabel = false;

        [Header("Meta")]
        public StoreCatalog storeCatalog;
        public AdPolicy adPolicy;
        public int energyMax = 5;
        public float energyRegenMinutes = 30f;
        public int energyRefillTokens = 10;
        public int tipJarCrackThreshold = 1200;

        [Header("Boosters")]
        public int spatulaUnlockLevel = 4;
        public int bellUnlockLevel = 5;
        public int rewindUnlockLevel = 7;
        public int boostersGrantedOnUnlock = 3;
        public float bellFreezeSeconds = 15f;
        public float rewindDistance = 5f;
        public float rewindDuration = 1f;

        [Header("Belt / Camera")]
        public float trackRadius = 2.6f;
        [Tooltip("All track shapes are scaled to this loop length (0 = leave each shape's natural length).")]
        public float trackLength = 16f;
        public float itemWorldSize = 1.1f;
        public float itemSpacing = 1.3f;
        [Tooltip("Belt surface width as a multiple of itemWorldSize.")]
        public float beltWidthFactor = 1.45f;
        public float spillSpeedMultiplier = 1.8f;
        [Range(10f, 80f)] public float cameraPitchDegrees = 38f;
        [Tooltip("Vertical FOV; lower = less perspective so rear items stay readable.")]
        [Range(15f, 70f)] public float cameraFov = 34f;
        public float cameraDistance = 10.5f;
        [Tooltip("Fraction of the viewport width the conveyor silhouette should fill.")]
        [Range(0.6f, 1f)] public float conveyorWidthFill = 0.92f;
        [Tooltip("Vertical screen band the belt camera renders into (viewport Y min/max).")]
        public Vector2 cameraViewportBand = new Vector2(0.27f, 0.75f);

        [Header("Optional art overrides (null = load from Resources/RetroDiner/Art or use placeholders)")]
        public Sprite gameplayBackground;
        public Sprite mainMenuBackground;
        public Sprite levelSelectBackground;
        public Sprite tray7;
        public Sprite tray8;
        public Sprite ticketNormal;
        public Sprite ticketActive;
        public Sprite ticketComplete;
        public Sprite buttonRed;
        public Sprite buttonMint;
        public Sprite buttonGold;
        public Sprite popupPanel;
        public Sprite pauseButton;
        public Sprite closeButton;
        public Sprite confirmButton;
        public Sprite starFilled;
        public Sprite starEmpty;
        public Sprite coinIcon;
        public Sprite tokenIcon;
        public Sprite heartIcon;
        public Sprite tipJar10;
        public Sprite tipJar50;
        public Sprite tipJar100;
        public Sprite boosterSpatula;
        public Sprite boosterBell;
        public Sprite boosterRewind;
        public Sprite boosterExtraPlate;
        public Sprite clocheClosed;
        public Sprite clocheOpen;
        public Sprite napkin;
        public Sprite syrupSpill;
        public Sprite ketchupSpill;
        public Sprite shiftFinishedBanner;
        public Texture2D confettiSheet;

        public ItemDefinition GetItem(string id)
        {
            if (items == null) return null;
            for (int i = 0; i < items.Length; i++) if (items[i] != null && items[i].id == id) return items[i];
            return null;
        }

        public LevelDefinition GetLevel(int index)
        {
            if (levels == null) return null;
            for (int i = 0; i < levels.Length; i++) if (levels[i] != null && levels[i].levelIndex == index) return levels[i];
            return null;
        }

        public int LevelCount => levels == null ? 0 : levels.Length;
    }
}
