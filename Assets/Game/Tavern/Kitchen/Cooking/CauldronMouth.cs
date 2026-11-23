using UnityEngine;

namespace Podz
{
    public class CauldronMouth : MonoBehaviour
    {
        public Cauldron Owner;
        void OnTriggerEnter(Collider c)
        {
            var ing = c.GetComponentInParent<Ingredient>();
            if (ing != null && Owner != null) Owner.OnIngredient(ing);
        }
    }
}
