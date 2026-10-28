using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZooWorld
{
    public sealed class TastyPresenter
    {
        private sealed class Toast
        {
            public Text Text;
            public AnimalAgent Target;
            public long LifeId;
            public float Remaining;
        }
        private readonly RectTransform layer;
        private readonly Camera camera;
        private readonly Font font;
        private readonly List<Toast> active = new List<Toast>();
        private readonly Stack<Toast> pool = new Stack<Toast>();
        public int VisibleCount => active.Count;

        public TastyPresenter(RectTransform layer, Camera camera, Font font) { this.layer = layer; this.camera = camera; this.font = font; }

        public void Show(AnimalAgent eater)
        {
            foreach (var existing in active)
                if (existing.Target == eater && existing.LifeId == eater.Life.Id) { existing.Remaining = 1.1f; return; }
            Toast toast = pool.Count > 0 ? pool.Pop() : Create();
            toast.Target = eater; toast.LifeId = eater.Life.Id; toast.Remaining = 1.1f;
            toast.Text.gameObject.SetActive(true);
            active.Add(toast);
            Position(toast);
        }

        public void Tick(float deltaTime)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var toast = active[i];
                toast.Remaining -= deltaTime;
                if (toast.Remaining <= 0 || !toast.Target.IsAlive || toast.Target.Life.Id != toast.LifeId)
                { Recycle(i); continue; }
                Position(toast);
            }
        }

        public void Clear() { for (int i = active.Count - 1; i >= 0; i--) Recycle(i); }

        private void Position(Toast toast)
        {
            Vector3 point = camera.WorldToScreenPoint(toast.Target.transform.position);
            var canvas = layer.GetComponentInParent<Canvas>();
            point.y -= 32 * canvas.scaleFactor;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, point, null, out Vector2 local);
            toast.Text.rectTransform.anchoredPosition = local;
            toast.Text.color = new Color(1, 0.91f, 0.6f, Mathf.Clamp01(toast.Remaining * 3));
        }

        private void Recycle(int index)
        {
            var toast = active[index];
            toast.Text.gameObject.SetActive(false); toast.Target = null;
            active.RemoveAt(index); pool.Push(toast);
        }

        private Toast Create()
        {
            var go = new GameObject("Tasty! (pooled)", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            go.transform.SetParent(layer, false);
            var text = go.GetComponent<Text>();
            text.font = font; text.fontSize = 23; text.fontStyle = FontStyle.Bold; text.text = "Tasty!";
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(125, 42);
            go.GetComponent<Outline>().effectColor = new Color(0.04f, 0.12f, 0.09f, 0.9f);
            return new Toast { Text = text };
        }
    }
}
