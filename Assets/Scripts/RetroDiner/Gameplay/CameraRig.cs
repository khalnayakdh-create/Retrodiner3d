using UnityEngine;
using RetroDiner.Data;

namespace RetroDiner.Gameplay
{
    /// <summary>Positions the belt camera at the configured pitch so the whole track fits its viewport band.</summary>
    public class CameraRig : MonoBehaviour
    {
        public Camera Cam;
        GameConfig cfg;

        public void Setup(Camera cam, GameConfig config)
        {
            Cam = cam;
            cfg = config;
            Cam.rect = new Rect(0f, cfg.cameraViewportBand.x, 1f, cfg.cameraViewportBand.y - cfg.cameraViewportBand.x);
            Cam.transform.rotation = Quaternion.Euler(cfg.cameraPitchDegrees, 0f, 0f);
            Cam.fieldOfView = cfg.cameraFov;
            baseFov = Cam.fieldOfView;
        }

        public Quaternion BillboardRotation => Quaternion.Euler(cfg.cameraPitchDegrees, 0f, 0f);

        float baseFov = 50f;
        /// <summary>Quick FOV punch-in on satisfying moments (triplet clears).</summary>
        public void Punch(float amount = 2f, float duration = 0.22f)
        {
            if (Cam == null) return;
            Cam.fieldOfView = baseFov - amount;
            RetroDiner.Core.Tween.Float(baseFov - amount, baseFov, duration, v => { if (Cam != null) Cam.fieldOfView = v; }, RetroDiner.Core.Ease.OutQuad, null, true);
        }

        public void Frame(Bounds bounds)
        {
            float pitch = cfg.cameraPitchDegrees * Mathf.Deg2Rad;
            float vfov = baseFov * Mathf.Deg2Rad;
            float aspect = Cam.aspect;
            float tanV = Mathf.Tan(vfov * 0.5f);
            float tanH = tanV * aspect;

            // Conveyor silhouette = track + belt + walls (~0.45 world units per side beyond the belt edge).
            float wallOut = cfg.itemWorldSize * cfg.beltWidthFactor * 0.5f + 0.45f;
            float halfW = (bounds.extents.x + wallOut) / Mathf.Clamp(cfg.conveyorWidthFill, 0.6f, 1f);
            float halfD = bounds.extents.z + wallOut;
            // Depth foreshortens by sin(pitch); items stand up by ~cos(pitch) * itemSize.
            float halfProjected = halfD * Mathf.Sin(pitch) + cfg.itemWorldSize * Mathf.Cos(pitch) + 0.3f;

            float distW = halfW / tanH;
            float distH = halfProjected / tanV;
            float distance = Mathf.Max(distW, distH, cfg.cameraDistance * 0.5f);

            Vector3 center = new Vector3(bounds.center.x, 0f, bounds.center.z);
            Vector3 dir = Quaternion.Euler(cfg.cameraPitchDegrees, 0f, 0f) * Vector3.forward;
            Cam.transform.position = center - dir * distance;
            Cam.transform.rotation = Quaternion.Euler(cfg.cameraPitchDegrees, 0f, 0f);
        }
    }
}
