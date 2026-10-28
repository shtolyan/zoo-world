using UnityEngine;

namespace ZooWorld
{
    [RequireComponent(typeof(Camera))]
    public sealed class HabitatCamera : MonoBehaviour
    {
        public Vector2 HabitatSize = new Vector2(24, 13);
        private Camera view;
        private void Awake() { view = GetComponent<Camera>(); Fit(); }
        private void LateUpdate() => Fit();

        private void Fit()
        {
            // Reserve space for the uGUI header and footer, including on narrow windows.
            view.rect = new Rect(0.025f, 0.115f, 0.95f, 0.68f);
            float aspect = (float)Screen.width * view.rect.width / Mathf.Max(1, Screen.height * view.rect.height);
            view.orthographicSize = Mathf.Max(HabitatSize.y * 0.5f + 0.65f, (HabitatSize.x * 0.5f + 0.65f) / aspect);
        }
    }
}
