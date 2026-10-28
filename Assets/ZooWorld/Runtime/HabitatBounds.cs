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
            if (camera != null)
            {
                Vector3 corner = GroundPoint(new Vector2(1, 1));
                limit = Vector2.Min(limit, new Vector2(Mathf.Abs(corner.x), Mathf.Abs(corner.z)) - Vector2.one * 0.65f);
            }
            if (Mathf.Abs(position.x) <= limit.x && Mathf.Abs(position.z) <= limit.y) return direction;
            var inward = new Vector3(-position.x, 0, -position.z);
            return inward.sqrMagnitude > 0.001f ? inward.normalized : Vector3.forward;
        }

        public Vector3 RandomPoint(Domain.IRandomSource random) => new Vector3(
            random.Range(-halfSize.x + 1, halfSize.x - 1), 0, random.Range(-halfSize.y + 1, halfSize.y - 1));

        private Vector3 GroundPoint(Vector2 viewport)
        {
            var ray = camera.ViewportPointToRay(viewport);
            return ground.Raycast(ray, out float distance) ? ray.GetPoint(distance) : new Vector3(halfSize.x, 0, halfSize.y);
        }
    }
}
