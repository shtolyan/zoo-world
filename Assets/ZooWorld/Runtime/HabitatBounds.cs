using UnityEngine;

namespace ZooWorld
{
    /// <summary>Steers rather than teleports or walls. Also respects a camera whose viewport has changed.</summary>
    public sealed class HabitatBounds
    {
        private readonly Vector2 halfSize;
        private readonly Camera camera;
        private readonly Plane ground = new Plane(Vector3.up, Vector3.zero);
        public HabitatBounds(Vector2 size, Camera camera) { halfSize = size * 0.5f; this.camera = camera; }

        public Vector3 Redirect(Vector3 position, Vector3 direction)
        {
            Vector2 limit = halfSize - Vector2.one * 0.65f;
            bool outside = Mathf.Abs(position.x) > limit.x || Mathf.Abs(position.z) > limit.y;
            Vector3 target = Vector3.zero;
            if (camera != null)
            {
                // Test the actual viewport, including translated or rotated top-down cameras.
                // One absolute corner only describes a camera centered at the world origin.
                Vector3 viewport = camera.WorldToViewportPoint(position);
                outside |= viewport.z <= 0 || viewport.x < 0.03f || viewport.x > 0.97f
                    || viewport.y < 0.03f || viewport.y > 0.97f;
                target = GroundPoint(new Vector2(0.5f, 0.5f));
                target.x = Mathf.Clamp(target.x, -limit.x, limit.x);
                target.z = Mathf.Clamp(target.z, -limit.y, limit.y);
            }
            if (!outside) return direction;
            var inward = target - position; inward.y = 0;
            return inward.sqrMagnitude > 0.001f ? inward.normalized : Vector3.forward;
        }

        public Vector3 RandomPoint(Domain.IRandomSource random, Vector3 trailingOffset = default)
        {
            Vector2 minimum = -halfSize + Vector2.one;
            Vector2 maximum = halfSize - Vector2.one;
            var offset = new Vector2(trailingOffset.x, trailingOffset.z);
            // The initial head AND the seeded tail fit inside the habitat.
            minimum = Vector2.Max(minimum, minimum + offset);
            maximum = Vector2.Min(maximum, maximum + offset);
            if (minimum.x > maximum.x || minimum.y > maximum.y)
                throw new System.InvalidOperationException("The habitat is too small to fit this animal's full body.");
            return new Vector3(random.Range(minimum.x, maximum.x), 0, random.Range(minimum.y, maximum.y));
        }

        private Vector3 GroundPoint(Vector2 viewport)
        {
            var ray = camera.ViewportPointToRay(viewport);
            return ground.Raycast(ray, out float distance) ? ray.GetPoint(distance) : Vector3.zero;
        }
    }
}
