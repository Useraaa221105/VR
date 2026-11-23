using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    public sealed class TavernShop : MonoBehaviour
    {
        [SerializeField] Pressable cleaver, hammer, shield;

        public static TavernShop Build(Transform parent)
        {
            var root = new GameObject("Shop");
            root.transform.SetParent(parent, false);
            var shop = root.AddComponent<TavernShop>();
            var wood = new Color(.45f, .3f, .18f);
            Prim.Cube("Board", root.transform, new Vector3(0, 1.5f, .03f), new Vector3(1.5f, 1.1f, .02f), wood * .6f, false);
            Prim.Text(root.transform, new Vector3(0, 1.9f, -.01f), "Лавка инструментов", .06f, new Color(1f,.9f,.6f));
            shop.cleaver = shop.Create(ToolType.Cleaver, 150, 1.65f);
            shop.hammer = shop.Create(ToolType.Hammer, 300, 1.45f);
            shop.shield = shop.Create(ToolType.Shield, 400, 1.25f);
            return shop;
        }

        Pressable Create(ToolType tool, int price, float y) =>
            Pressable.Create($"{Tools.Name(tool)} — {price}", transform, new Vector3(0,y,0),
                new Color(.55f,.45f,.3f), null, new Vector3(.9f,.13f,.03f), .03f);

        public void Bind(Dictionary<ToolType, Pressable> buttons, SavedDungeonAssets config)
        {
            BindButton(buttons, ToolType.Cleaver, cleaver, config?.CleaverPrice ?? 150);
            BindButton(buttons, ToolType.Hammer, hammer, config?.HammerPrice ?? 300);
            BindButton(buttons, ToolType.Shield, shield, config?.ShieldPrice ?? 400);
        }

        static void BindButton(Dictionary<ToolType, Pressable> buttons, ToolType tool, Pressable button, int price)
        {
            button.SetLabel($"{Tools.Name(tool)} — {price}");
            button.OnPress = () => GameFlow.I.Buy(tool, price);
            buttons[tool] = button;
        }
    }
}
