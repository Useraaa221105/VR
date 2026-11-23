using UnityEngine;

namespace Podz
{
    internal static class SavedUi
    {
        internal static GameObject CreateRoot(string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            return root;
        }
    }
}
