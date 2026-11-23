using System;
using UnityEngine;

namespace Podz
{
    [Serializable]
    public sealed class SavedMonsterPrefabs
    {
        [SerializeField] Slime slime;
        [SerializeField] Mushroom mushroom;
        [SerializeField] Mandrake mandrake;
        [SerializeField] Basilisk basilisk;
        [SerializeField] LivingArmor armor;
        [SerializeField] Mimic mimic;
        [SerializeField] Dragon dragon;

        public Monster Get(MonsterKind kind) => kind switch
        {
            MonsterKind.Slime => slime != null && slime.HasSavedModel ? slime : null,
            MonsterKind.Mushroom => Saved(mushroom),
            MonsterKind.Mandrake => Saved(mandrake),
            MonsterKind.Basilisk => Saved(basilisk),
            MonsterKind.Armor => Saved(armor),
            MonsterKind.Mimic => Saved(mimic),
            MonsterKind.Dragon => Saved(dragon),
            _ => null
        };

        static Monster Saved(Monster monster) => monster != null && monster.HasSavedGeometry ? monster : null;
    }
}