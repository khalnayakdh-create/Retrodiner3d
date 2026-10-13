using UnityEngine;
using UnityEngine.EventSystems;
using RetroDiner.Belt;
using RetroDiner.Core;

namespace RetroDiner.Gameplay
{
    /// <summary>
    /// Raycasts taps against belt items. Uses the legacy Input API (mouse + first touch) so the
    /// project runs with Unity's default "Input Manager" setting; see docs/DECISIONS.md.
    /// </summary>
    public class TapInput : MonoBehaviour
    {
        public Camera Cam;
        public bool Enabled = true;
        static readonly RaycastHit[] hits = new RaycastHit[16];

        void Update()
        {
            if (!Enabled || Cam == null) return;
            if (!Input.GetMouseButtonDown(0)) return;
            if (IsOverUI()) return;

            Vector3 screen = Input.mousePosition;
            // Ignore taps outside the belt camera's viewport band.
            Rect px = Cam.pixelRect;
            if (!px.Contains(new Vector2(screen.x, screen.y))) return;

            Ray ray = Cam.ScreenPointToRay(screen);
            int n = Physics.RaycastNonAlloc(ray, hits, 100f);
            BeltItem best = null; float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var item = hits[i].collider.GetComponentInParent<BeltItem>();
                if (item == null) continue;
                if (hits[i].distance < bestDist) { bestDist = hits[i].distance; best = item; }
            }
            if (best != null) GameEvents.RaiseItemTapped(best);
        }

        static bool IsOverUI()
        {
            var es = EventSystem.current;
            if (es == null) return false;
            if (Input.touchCount > 0 && es.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return true;
            return es.IsPointerOverGameObject();
        }
    }
}
