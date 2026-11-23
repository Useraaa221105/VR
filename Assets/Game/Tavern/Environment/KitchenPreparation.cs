using UnityEngine;

namespace Podz
{
    public sealed class KitchenPreparation : MonoBehaviour
    {
        public Bounds Sink = new Bounds(new Vector3(-2.3f, 1.1f, 2.55f), new Vector3(.7f,.45f,.6f));
        public Bounds Board = new Bounds(new Vector3(-.9f, 1.05f, 2.55f), new Vector3(.7f,.35f,.55f));
        public Bounds Grill = new Bounds(new Vector3(.6f, 1.05f, 2.55f), new Vector3(.6f,.35f,.55f));

        public Bounds WorldBounds(Bounds box)
        {
            var result = new Bounds(transform.TransformPoint(box.center), Vector3.zero);
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        result.Encapsulate(transform.TransformPoint(box.center + Vector3.Scale(box.extents, new Vector3(x,y,z))));
            return result;
        }
    }
}
