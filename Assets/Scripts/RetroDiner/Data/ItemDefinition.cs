using UnityEngine;

namespace RetroDiner.Data
{
    public enum HapticType { None, Light, Medium, Heavy }

    /// <summary>
    /// Data for one breakfast item. All art/audio references live here so real assets can be
    /// swapped in without code changes. Null references fall back to code-generated placeholders.
    /// </summary>
    [CreateAssetMenu(menuName = "Retro Diner/Item Definition", fileName = "Item_")]
    public class ItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [Tooltip("Used by the primitive placeholder when no sprite/prefab is assigned.")]
        public Color placeholderColor = Color.white;
        [Tooltip("Optional 3D prefab (FBX). If null, the icon sprite is billboarded on the belt.")]
        public GameObject prefab;
        public Sprite icon;
        public Sprite charredIcon;
        public bool canBurn;
        public AudioClip pickupSfx;
        [Tooltip("Key into AudioService's procedural clip bank when pickupSfx is null.")]
        public string pickupSfxKey = "pickup";
        public HapticType pickupHaptic = HapticType.Light;
        public Color particleColor = new Color(1f, 0.85f, 0.4f);
    }
}
