using UnityEngine;

namespace RetroDiner.UI
{
    /// <summary>Keeps a full-stretch RectTransform inside Screen.safeArea (notches, home indicators, system bars).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        Rect applied = new Rect(-1, -1, 0, 0);
        RectTransform rt;

        void Awake() { rt = GetComponent<RectTransform>(); Apply(); }
        void Update() { if (Screen.safeArea != applied) Apply(); }

        void Apply()
        {
            var sa = Screen.safeArea;
            applied = sa;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Vector2 min = sa.position, max = sa.position + sa.size;
            min.x /= Screen.width; min.y /= Screen.height; max.x /= Screen.width; max.y /= Screen.height;
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
