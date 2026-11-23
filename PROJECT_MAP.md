# Карта проекта «Подземелье вкусностей»

Рабочий проект `VR/VR`, Unity `6000.6.0f1`. Карта описывает файлы, которые лежат в проекте.

## Как открыть и найти объект

1. В Unity откройте **Window → General → Project**. Чтобы найти объект, введите имя без расширения, например `t:Prefab Slime` или `t:Scene Main`. Если результатов несколько, сверяйте папку с путём в этой карте.
2. Дважды щёлкните по [Main.unity](Assets/Game/Scenes/Main.unity). В **Hierarchy** до Play Mode доступны `GameBootstrap` и экземпляр `TavernArchitecture`. Раскройте архитектуру, чтобы увидеть пол, стены, перегородку и свет. Игрок, рабочий экземпляр кухни, монстры и панели HUD появляются при запуске.
3. Дважды щёлкните по префабу в **Project**, чтобы открыть **Prefab Mode**. Выберите корень или нужного ребёнка в **Hierarchy**. Редактируйте **Transform**, **Mesh Filter**, **Mesh Renderer**, **Collider** и поля компонента в **Inspector**. Ссылку на другой ассет можно открыть через кружок справа от поля или выделить щелчком по полю.
4. В `Main` выберите **GameBootstrap → Game Bootstrap**. `Tavern Architecture` указывает на экземпляр в сцене, `Saved World` — на [RuntimeAssets.asset](Assets/_Project/Configs/Original/RuntimeAssets.asset), `Saved Monsters` — на семь игровых префабов. Это удобная точка входа, если поиск показывает несколько версий одного ресурса.

| Тип | Где лежит | Когда редактировать |
|---|---|---|
| Объект сцены | В `Main.unity` или в обзорной сцене | В обычной Hierarchy до Play Mode. У экземпляра префаба можно сохранить изменения только для этой сцены или применить их ко всем экземплярам через **Overrides → Apply**. |
| Prefab | Файл `.prefab` в Project | В Prefab Mode. Изменения применяются к будущим экземплярам, в том числе созданным генератором. |
| ScriptableObject | Файл `.asset`, например `RuntimeAssets` | Один щелчок в Project. В Inspector доступны ссылки, цены и прочие данные, объявленные сериализованными. |
| Объект Play Mode | Экземпляр, созданный скриптом или клонированный из префаба | Подходит для проверки. **Изменения объектов во время Play Mode обычно исчезают после Stop.** Для постоянной правки меняйте исходный prefab, сцену, конфигурацию или код. Изменения самого ассета через Project могут сохраниться и во время Play Mode. |

## Реальное дерево Assets

Ниже показаны папки и важные файлы. Повторяющиеся `.meta`, меши, материалы и варианты с одинаковой структурой сокращены. Их GUID сохраняют связи между ассетами. Перемещайте ресурсы через Unity Project, чтобы не потерять `.meta`.

```text
└── Assets/
    ├── _Project/
    │   ├── Configs/
    │   │   └── Original/
    │   │       ├── Slime_Generated/
    │   │       │   └── Slime.prefab
    │   │       ├── AcidDrop_F1.asset
    │   │       ├── Basilisk.asset
    │   │       ├── Chest_F3.asset
    │   │       ├── DartPlate_F2.asset
    │   │       ├── DartPlate_F3.asset
    │   │       ├── DoorWithHandles_F1.asset
    │   │       ├── DoorWithHandles_F2.asset
    │   │       ├── DoorWithHandles_F3.asset
    │   │       ├── Dragon.asset
    │   │       ├── Elixir_F1.asset
    │   │       ├── Elixir_F2.asset
    │   │       ├── Elixir_F3.asset
    │   │       ├── FakeArmor_F3.asset
    │   │       ├── FireCover_F4.asset
    │   │       ├── FireGrate_F3.asset
    │   │       ├── Floor_1.asset
    │   │       ├── Floor_2.asset
    │   │       ├── Floor_3.asset
    │   │       ├── Floor_4.asset
    │   │       ├── LivingArmor.asset
    │   │       ├── Loot_BasiliskMeat.asset
    │   │       ├── Loot_DragonMeat.asset
    │   │       ├── Loot_MandrakeRoot.asset
    │   │       ├── Loot_MimicMeat.asset
    │   │       ├── Loot_MushroomLegs.asset
    │   │       ├── Loot_ShellfishMeat.asset
    │   │       ├── Loot_SlimeJelly.asset
    │   │       ├── Mandrake.asset
    │   │       ├── Mimic.asset
    │   │       ├── Mushroom.asset
    │   │       ├── Pendulum_F3.asset
    │   │       ├── PortalVisual_F1.asset
    │   │       ├── Puddle_F1.asset
    │   │       ├── ReturnPortalVisual_F1.asset
    │   │       ├── Rockfall_F2.asset
    │   │       ├── RuntimeAssets.asset
    │   │       ├── Slime.asset
    │   │       ├── Spores_F1.asset
    │   │       ├── Spores_F2.asset
    │   │       ├── Stalactite_F1.asset
    │   │       ├── StepsAndPlatform_F1.asset
    │   │       ├── Vines_F2.asset
    │   │       ├── Well_F1.asset
    │   │       └── Well_F2.asset
    │   ├── Editor/
    │   │   ├── OriginalAssetExport.cs
    │   │   └── OriginalRuntimeAssetExport.cs
    │   ├── Materials/
    │   │   ├── Tavern_Ceiling.mat
    │   │   ├── Tavern_Floor.mat
    │   │   ├── Tavern_Partitions.mat
    │   │   └── Tavern_Walls.mat
    │   ├── Prefabs/
    │   │   ├── Original/
    │   │   │   ├── BestiaryCanvas/
    │   │   │   │   └── BestiaryCanvas.prefab
    │   │   │   ├── Bowl_0/
    │   │   │   │   └── Bowl_0.prefab
    │   │   │   ├── DungeonSettings/
    │   │   │   │   └── DungeonSettings.prefab
    │   │   │   ├── FortressDecor_8x8_0/
    │   │   │   │   └── FortressDecor_8x8_0.prefab
    │   │   │   ├── FortressPassage_54dm/
    │   │   │   │   └── FortressPassage_54dm.prefab
    │   │   │   ├── FortressRoom_8x8_1/
    │   │   │   │   └── FortressRoom_8x8_1.prefab
    │   │   │   ├── Fridge/
    │   │   │   │   └── Fridge.prefab
    │   │   │   ├── FridgeScreen/
    │   │   │   │   └── FridgeScreen.prefab
    │   │   │   ├── Guest_Miner/
    │   │   │   │   └── Guest_Miner.prefab
    │   │   │   ├── GuideCanvas/
    │   │   │   │   └── GuideCanvas.prefab
    │   │   │   ├── Hotbar/
    │   │   │   │   └── Hotbar.prefab
    │   │   │   ├── HudPanel/
    │   │   │   │   └── HudPanel.prefab
    │   │   │   ├── HuntCoachCanvas/
    │   │   │   │   └── HuntCoachCanvas.prefab
    │   │   │   ├── HuntIntroPc/
    │   │   │   │   └── HuntIntroPc.prefab
    │   │   │   ├── HuntIntroVr/
    │   │   │   │   └── HuntIntroVr.prefab
    │   │   │   ├── Ingredient_SlimeJelly/
    │   │   │   │   └── Ingredient_SlimeJelly.prefab
    │   │   │   ├── KitchenCoachCanvas/
    │   │   │   │   └── KitchenCoachCanvas.prefab
    │   │   │   ├── KitchenPreparation/
    │   │   │   │   └── KitchenPreparation.prefab
    │   │   │   ├── LargeCauldron/
    │   │   │   │   └── LargeCauldron.prefab
    │   │   │   ├── OrderBoard/
    │   │   │   │   └── OrderBoard.prefab
    │   │   │   ├── RecipeScreen/
    │   │   │   │   └── RecipeScreen.prefab
    │   │   │   ├── SmallCauldron/
    │   │   │   │   └── SmallCauldron.prefab
    │   │   │   ├── TavernShop/
    │   │   │   │   └── TavernShop.prefab
    │   │   │   ├── Toby/
    │   │   │   │   └── Toby.prefab
    │   │   │   ├── Tool_Cleaver/
    │   │   │   │   └── Tool_Cleaver.prefab
    │   │   │   ├── Tool_Hammer/
    │   │   │   │   └── Tool_Hammer.prefab
    │   │   │   ├── Tool_Knife/
    │   │   │   │   └── Tool_Knife.prefab
    │   │   │   ├── Tool_Ladle/
    │   │   │   │   └── Tool_Ladle.prefab
    │   │   │   ├── Tool_Net/
    │   │   │   │   └── Tool_Net.prefab
    │   │   │   ├── Tool_Shield/
    │   │   │   │   └── Tool_Shield.prefab
    │   │   │   └── Tool_Spatula/
    │   │   │       └── Tool_Spatula.prefab
    │   │   └── TavernArchitecture.prefab
    │   └── Scenes/
    │       └── Original/
    │           ├── Floor_1_Review.unity
    │           ├── Floor_2_Review.unity
    │           ├── Floor_3_Review.unity
    │           └── Floor_4_Review.unity
    ├── Game/
    │   ├── Core/
    │   │   ├── Damage.cs
    │   │   ├── GameBootstrap.cs
    │   │   ├── GameEvents.cs
    │   │   ├── Inputs.cs
    │   │   ├── Interact.cs
    │   │   ├── Items.cs
    │   │   ├── Pressable.cs
    │   │   ├── SaveSystem.cs
    │   │   └── Wallet.cs
    │   ├── Developer/
    │   │   ├── DebugOverlay.cs
    │   │   ├── PerformanceRecorder.cs
    │   │   └── PrefabReviewStand.cs
    │   ├── Dungeon/
    │   │   ├── Common/
    │   │   │   ├── Generation/
    │   │   │   │   ├── ArchitectureMeshOwner.cs
    │   │   │   │   ├── Biomes.cs
    │   │   │   │   ├── CavePrototype.cs
    │   │   │   │   ├── DungeonArchitecture.cs
    │   │   │   │   ├── DungeonBlock.cs
    │   │   │   │   ├── DungeonManager.cs
    │   │   │   │   ├── DungeonObjectRecipe.cs
    │   │   │   │   ├── DungeonPrefabCatalog.cs
    │   │   │   │   ├── DungeonPrefabPreview.cs
    │   │   │   │   ├── DungeonProps.cs
    │   │   │   │   ├── DungeonSourcePlan.cs
    │   │   │   │   ├── FaceViewer.cs
    │   │   │   │   ├── Flicker.cs
    │   │   │   │   ├── FloorGenerator.cs
    │   │   │   │   ├── FortressGeometryBlock.cs
    │   │   │   │   ├── GlowRing.cs
    │   │   │   │   ├── Hazard.cs
    │   │   │   │   ├── HealPickup.cs
    │   │   │   │   ├── Portal.cs
    │   │   │   │   ├── Puddle.cs
    │   │   │   │   └── SavedDungeonAssets.cs
    │   │   │   ├── Healing/
    │   │   │   │   ├── Elixir.cs
    │   │   │   │   └── ElixirPickup.cs
    │   │   │   ├── Monsters/
    │   │   │   │   ├── AttackGate.cs
    │   │   │   │   ├── Bestiary.cs
    │   │   │   │   ├── FortressCreatures.cs
    │   │   │   │   ├── HitZone.cs
    │   │   │   │   ├── MimicReferencePose.cs
    │   │   │   │   ├── Monster.cs
    │   │   │   │   ├── MonsterAnim.cs
    │   │   │   │   ├── MonsterCache.cs
    │   │   │   │   ├── MonsterCard.cs
    │   │   │   │   ├── MonsterGrabPoint.cs
    │   │   │   │   ├── MonsterLooks.cs
    │   │   │   │   ├── MonsterModelRecipe.cs
    │   │   │   │   ├── MonsterRegistry.cs
    │   │   │   │   ├── ReferenceMesh.cs
    │   │   │   │   ├── RuinsCreatures.cs
    │   │   │   │   ├── SavedMonsterPrefabs.cs
    │   │   │   │   └── UpperCaveCreatures.cs
    │   │   │   ├── Objects/
    │   │   │   │   ├── DoorWithHandles_Floor1_Generated/
    │   │   │   │   │   └── DoorWithHandles.prefab
    │   │   │   │   ├── DoorWithHandles_Floor2_Generated/
    │   │   │   │   │   └── DoorWithHandles.prefab
    │   │   │   │   ├── DoorWithHandles_Floor3_Generated/
    │   │   │   │   │   └── DoorWithHandles.prefab
    │   │   │   │   ├── Elixir_Floor1_Generated/
    │   │   │   │   │   └── Elixir.prefab
    │   │   │   │   ├── Elixir_Floor2_Generated/
    │   │   │   │   │   └── Elixir.prefab
    │   │   │   │   ├── Elixir_Floor3_Generated/
    │   │   │   │   │   └── Elixir.prefab
    │   │   │   │   ├── PhysicalLoot_Floor1_Generated/
    │   │   │   │   │   └── PhysicalLoot.prefab
    │   │   │   │   ├── PhysicalLoot_Floor1_Generated 1/
    │   │   │   │   │   └── PhysicalLoot.prefab
    │   │   │   │   ├── PhysicalLoot_Floor1_Generated 2/
    │   │   │   │   │   └── PhysicalLoot.prefab
    │   │   │   │   ├── PhysicalLoot_Floor1_Generated 3/
    │   │   │   │   │   └── PhysicalLoot.prefab
    │   │   │   │   ├── PhysicalLoot_Floor1_Generated 4/
    │   │   │   │   │   └── PhysicalLoot.prefab
    │   │   │   │   ├── PhysicalLoot_Floor1_Generated 5/
    │   │   │   │   │   └── PhysicalLoot.prefab
    │   │   │   │   ├── PhysicalLoot_Floor1_Generated 6/
    │   │   │   │   │   └── PhysicalLoot.prefab
    │   │   │   │   ├── PortalVisual_Floor1_Generated/
    │   │   │   │   │   └── PortalVisual.prefab
    │   │   │   │   ├── Puddle_Floor1_Generated/
    │   │   │   │   │   └── Puddle.prefab
    │   │   │   │   ├── ReturnPortalVisual_Floor1_Generated/
    │   │   │   │   │   └── ReturnPortalVisual.prefab
    │   │   │   │   ├── StepsAndPlatform_Floor1_Generated/
    │   │   │   │   │   └── StepsAndPlatform.prefab
    │   │   │   │   ├── Well_Floor1_Generated/
    │   │   │   │   │   └── Well.prefab
    │   │   │   │   └── Well_Floor2_Generated/
    │   │   │   │       └── Well.prefab
    │   │   │   └── Transitions/
    │   │   │       └── TransitionDoor.cs
    │   │   ├── Floor01_UpperCaves/
    │   │   │   ├── Blocks/
    │   │   │   │   ├── F1_Passage_3.500_1_Generated/
    │   │   │   │   │   └── F1_Passage_3.500_1.prefab
    │   │   │   │   ├── F1_Passage_4.000_3_Generated/
    │   │   │   │   │   └── F1_Passage_4.000_3.prefab
    │   │   │   │   ├── F1_Passage_4.500_2_Generated/
    │   │   │   │   │   └── F1_Passage_4.500_2.prefab
    │   │   │   │   ├── F1_Passage_5.000_5_Generated/
    │   │   │   │   │   └── F1_Passage_5.000_5.prefab
    │   │   │   │   ├── F1_Passage_6.000_0_Generated/
    │   │   │   │   │   └── F1_Passage_6.000_0.prefab
    │   │   │   │   ├── F1_Passage_6.000_6_Generated/
    │   │   │   │   │   └── F1_Passage_6.000_6.prefab
    │   │   │   │   ├── F1_Passage_6.500_4_Generated/
    │   │   │   │   │   └── F1_Passage_6.500_4.prefab
    │   │   │   │   ├── F1_Room_0_0_11x9_Ports1_Generated/
    │   │   │   │   │   └── F1_Room_0_0_11x9_Ports1.prefab
    │   │   │   │   ├── F1_Room_1_1_12x11_Ports7_Generated/
    │   │   │   │   │   └── F1_Room_1_1_12x11_Ports7.prefab
    │   │   │   │   ├── F1_Room_2_3_13x12_Ports9_Generated/
    │   │   │   │   │   └── F1_Room_2_3_13x12_Ports9.prefab
    │   │   │   │   ├── F1_Room_3_2_13x11_Ports4_Generated/
    │   │   │   │   │   └── F1_Room_3_2_13x11_Ports4.prefab
    │   │   │   │   ├── F1_Room_4_5_8x13_Ports12_Generated/
    │   │   │   │   │   └── F1_Room_4_5_8x13_Ports12.prefab
    │   │   │   │   ├── F1_Room_5_4_11x12_Ports6_Generated/
    │   │   │   │   │   └── F1_Room_5_4_11x12_Ports6.prefab
    │   │   │   │   ├── F1_Room_6_6_12x10_Ports5_Generated/
    │   │   │   │   │   └── F1_Room_6_6_12x10_Ports5.prefab
    │   │   │   │   └── F1_Room_7_7_11x10_Ports1_Generated/
    │   │   │   │       └── F1_Room_7_7_11x10_Ports1.prefab
    │   │   │   ├── Environment/
    │   │   │   │   └── UpperCaves.cs
    │   │   │   ├── Monsters/
    │   │   │   │   ├── Mushroom/
    │   │   │   │   │   ├── Mushroom_Generated/
    │   │   │   │   │   │   └── Mushroom.prefab
    │   │   │   │   │   └── Mushroom.cs
    │   │   │   │   └── Slime/
    │   │   │   │       ├── Slime.cs
    │   │   │   │       └── SlimeModelRecipe.cs
    │   │   │   └── Traps/
    │   │   │       ├── AcidDrop_Floor1_Generated/
    │   │   │       │   └── AcidDrop.prefab
    │   │   │       ├── Spores_Floor1_Generated/
    │   │   │       │   └── Spores.prefab
    │   │   │       └── Stalactite_Floor1_Generated/
    │   │   │           └── Stalactite.prefab
    │   │   ├── Floor02_OvergrownRuins/
    │   │   │   ├── Blocks/
    │   │   │   │   ├── F2_Passage_6.000_1_Generated/
    │   │   │   │   │   └── F2_Passage_6.000_1.prefab
    │   │   │   │   ├── F2_Passage_6.000_2_Generated/
    │   │   │   │   │   └── F2_Passage_6.000_2.prefab
    │   │   │   │   ├── F2_Passage_7.000_4_Generated/
    │   │   │   │   │   └── F2_Passage_7.000_4.prefab
    │   │   │   │   ├── F2_Passage_8.000_3_Generated/
    │   │   │   │   │   └── F2_Passage_8.000_3.prefab
    │   │   │   │   ├── F2_Passage_8.500_5_Generated/
    │   │   │   │   │   └── F2_Passage_8.500_5.prefab
    │   │   │   │   ├── F2_Passage_8.500_6_Generated/
    │   │   │   │   │   └── F2_Passage_8.500_6.prefab
    │   │   │   │   ├── F2_Passage_8.500_7_Generated/
    │   │   │   │   │   └── F2_Passage_8.500_7.prefab
    │   │   │   │   ├── F2_Passage_9.500_0_Generated/
    │   │   │   │   │   └── F2_Passage_9.500_0.prefab
    │   │   │   │   ├── F2_Room_0_0_11x10_Ports1_Generated/
    │   │   │   │   │   └── F2_Room_0_0_11x10_Ports1.prefab
    │   │   │   │   ├── F2_Room_1_1_12x11_Ports7_Generated/
    │   │   │   │   │   └── F2_Room_1_1_12x11_Ports7.prefab
    │   │   │   │   ├── F2_Room_2_3_16x7_Ports10_Generated/
    │   │   │   │   │   └── F2_Room_2_3_16x7_Ports10.prefab
    │   │   │   │   ├── F2_Room_3_2_12x12_Ports9_Generated/
    │   │   │   │   │   └── F2_Room_3_2_12x12_Ports9.prefab
    │   │   │   │   ├── F2_Room_4_6_13x13_Ports6_Generated/
    │   │   │   │   │   └── F2_Room_4_6_13x13_Ports6.prefab
    │   │   │   │   ├── F2_Room_5_5_13x12_Ports10_Generated/
    │   │   │   │   │   └── F2_Room_5_5_13x12_Ports10.prefab
    │   │   │   │   ├── F2_Room_6_4_10x11_Ports13_Generated/
    │   │   │   │   │   └── F2_Room_6_4_10x11_Ports13.prefab
    │   │   │   │   └── F2_Room_7_7_11x12_Ports4_Generated/
    │   │   │   │       └── F2_Room_7_7_11x12_Ports4.prefab
    │   │   │   ├── Environment/
    │   │   │   │   └── OvergrownRuins.cs
    │   │   │   ├── Monsters/
    │   │   │   │   ├── Basilisk/
    │   │   │   │   │   ├── Basilisk_Generated/
    │   │   │   │   │   │   └── Basilisk.prefab
    │   │   │   │   │   ├── Basilisk.cs
    │   │   │   │   │   └── RuinsNeckLinks.cs
    │   │   │   │   └── Mandrake/
    │   │   │   │       ├── Mandrake_Generated/
    │   │   │   │       │   └── Mandrake.prefab
    │   │   │   │       ├── Mandrake.cs
    │   │   │   │       ├── MandrakeCry.cs
    │   │   │   │       ├── MandrakeLeafPose.cs
    │   │   │   │       └── MandrakeVoice.cs
    │   │   │   └── Traps/
    │   │   │       ├── DartPlate_Floor2_Generated/
    │   │   │       │   └── DartPlate.prefab
    │   │   │       ├── Rockfall_Floor2_Generated/
    │   │   │       │   └── Rockfall.prefab
    │   │   │       ├── Spores_Floor2_Generated/
    │   │   │       │   └── Spores.prefab
    │   │   │       └── Vines_Floor2_Generated/
    │   │   │           └── Vines.prefab
    │   │   ├── Floor03_Fortress/
    │   │   │   ├── Blocks/
    │   │   │   │   ├── F3_Passage_3.400_0_Generated/
    │   │   │   │   │   └── F3_Passage_3.400_0.prefab
    │   │   │   │   ├── F3_Passage_3.400_3_Generated/
    │   │   │   │   │   └── F3_Passage_3.400_3.prefab
    │   │   │   │   ├── F3_Passage_3.900_1_Generated/
    │   │   │   │   │   └── F3_Passage_3.900_1.prefab
    │   │   │   │   ├── F3_Passage_3.900_4_Generated/
    │   │   │   │   │   └── F3_Passage_3.900_4.prefab
    │   │   │   │   ├── F3_Passage_3.900_6_Generated/
    │   │   │   │   │   └── F3_Passage_3.900_6.prefab
    │   │   │   │   ├── F3_Passage_5.400_2_Generated/
    │   │   │   │   │   └── F3_Passage_5.400_2.prefab
    │   │   │   │   ├── F3_Passage_5.400_5_Generated/
    │   │   │   │   │   └── F3_Passage_5.400_5.prefab
    │   │   │   │   ├── F3_Room_0_0_8x8_Ports2_Generated/
    │   │   │   │   │   └── F3_Room_0_0_8x8_Ports2.prefab
    │   │   │   │   ├── F3_Room_1_1_12x8_Ports9_Generated/
    │   │   │   │   │   └── F3_Room_1_1_12x8_Ports9.prefab
    │   │   │   │   ├── F3_Room_2_2_8x11_Ports12_Generated/
    │   │   │   │   │   └── F3_Room_2_2_8x11_Ports12.prefab
    │   │   │   │   ├── F3_Room_3_3_8x8_Ports11_Generated/
    │   │   │   │   │   └── F3_Room_3_3_8x8_Ports11.prefab
    │   │   │   │   ├── F3_Room_4_4_12x8_Ports3_Generated/
    │   │   │   │   │   └── F3_Room_4_4_12x8_Ports3.prefab
    │   │   │   │   ├── F3_Room_5_5_8x11_Ports5_Generated/
    │   │   │   │   │   └── F3_Room_5_5_8x11_Ports5.prefab
    │   │   │   │   ├── F3_Room_6_7_8x8_Ports4_Generated/
    │   │   │   │   │   └── F3_Room_6_7_8x8_Ports4.prefab
    │   │   │   │   └── F3_Room_7_6_12x8_Ports4_Generated/
    │   │   │   │       └── F3_Room_7_6_12x8_Ports4.prefab
    │   │   │   ├── Environment/
    │   │   │   │   ├── Chest_Floor3_Generated/
    │   │   │   │   │   └── Chest.prefab
    │   │   │   │   ├── FakeArmor_Floor3_Generated/
    │   │   │   │   │   └── FakeArmor.prefab
    │   │   │   │   ├── Chest.cs
    │   │   │   │   ├── FakeArmor.cs
    │   │   │   │   └── FortressReference.cs
    │   │   │   ├── Monsters/
    │   │   │   │   ├── LivingArmor/
    │   │   │   │   │   ├── LivingArmor_Generated/
    │   │   │   │   │   │   └── LivingArmor.prefab
    │   │   │   │   │   └── LivingArmor.cs
    │   │   │   │   └── Mimic/
    │   │   │   │       ├── Mimic_Generated/
    │   │   │   │       │   └── Mimic.prefab
    │   │   │   │       └── Mimic.cs
    │   │   │   └── Traps/
    │   │   │       ├── DartPlate_Floor3_Generated/
    │   │   │       │   └── DartPlate.prefab
    │   │   │       ├── FireGrate_Floor3_Generated/
    │   │   │       │   └── FireGrate.prefab
    │   │   │       └── Pendulum_Floor3_Generated/
    │   │   │           └── Pendulum.prefab
    │   │   └── Floor04_DragonLair/
    │   │       ├── Blocks/
    │   │       │   └── F4_Room_0_0_36x36_Ports0_Generated/
    │   │       │       └── F4_Room_0_0_36x36_Ports0.prefab
    │   │       ├── Environment/
    │   │       │   ├── FireCover_Floor4_Generated/
    │   │       │   │   └── FireCover.prefab
    │   │       │   ├── DragonLair.cs
    │   │       │   └── FireCover.cs
    │   │       └── Monsters/
    │   │           └── Dragon/
    │   │               ├── Dragon_Generated/
    │   │               │   └── Dragon.prefab
    │   │               ├── Dragon.cs
    │   │               ├── DragonFinisher.cs
    │   │               ├── DragonReference.cs
    │   │               └── DragonReferenceMotion.cs
    │   ├── Editor/
    │   │   ├── DungeonObjectRecipeEditor.cs
    │   │   ├── DungeonPrefabCatalogEditor.cs
    │   │   ├── MonsterModelRecipeEditor.cs
    │   │   ├── PodzFirstOpen.cs
    │   │   ├── PodzSetup.cs
    │   │   ├── PodzXRSetup.cs
    │   │   ├── PrefabReviewStandEditor.cs
    │   │   ├── SavedCommonResources.cs
    │   │   ├── SavedModelResources.cs
    │   │   └── SlimeModelRecipeEditor.cs
    │   ├── Player/
    │   │   ├── Hands/
    │   │   │   └── HandController.cs
    │   │   ├── Health/
    │   │   │   └── PlayerHealth.cs
    │   │   ├── Inventory/
    │   │   │   ├── BackpackView.cs
    │   │   │   └── DungeonLoot.cs
    │   │   ├── Movement/
    │   │   │   └── PlayerRig.cs
    │   │   └── Tools/
    │   │       ├── Tool.cs
    │   │       └── ToolRules.cs
    │   ├── Scenes/
    │   │   └── Main.unity
    │   ├── Shared/
    │   │   ├── Materials/
    │   │   │   ├── PodzLit.mat
    │   │   │   └── PodzOverlay.mat
    │   │   ├── GameAssets.cs
    │   │   ├── InteractionFeedback.cs
    │   │   ├── Prim.cs
    │   │   ├── Sfx.cs
    │   │   └── WorldLighting.cs
    │   ├── Tavern/
    │   │   ├── DayCycle/
    │   │   │   └── GameFlow.cs
    │   │   ├── Environment/
    │   │   │   ├── Kitchen.cs
    │   │   │   ├── KitchenPreparation.cs
    │   │   │   └── TavernShop.cs
    │   │   ├── Guests/
    │   │   │   ├── FaceCam.cs
    │   │   │   ├── Guest.cs
    │   │   │   ├── Guests.cs
    │   │   │   └── Toby.cs
    │   │   ├── Kitchen/
    │   │   │   ├── Cooking/
    │   │   │   │   ├── Bowl.cs
    │   │   │   │   ├── Cauldron.cs
    │   │   │   │   ├── CauldronMouth.cs
    │   │   │   │   └── SoupVisual.cs
    │   │   │   ├── Ingredients/
    │   │   │   │   ├── FoodModels.cs
    │   │   │   │   ├── FoodWobble.cs
    │   │   │   │   ├── Fridge.cs
    │   │   │   │   ├── FridgeScreen.cs
    │   │   │   │   ├── FridgeSlot.cs
    │   │   │   │   ├── Ingredient.cs
    │   │   │   │   └── TrashBin.cs
    │   │   │   ├── Recipes/
    │   │   │   │   ├── RecipePicture.cs
    │   │   │   │   ├── Recipes.cs
    │   │   │   │   └── RecipeScreen.cs
    │   │   │   └── KitchenCoach.cs
    │   │   └── Orders/
    │   │       └── OrderBoard.cs
    │   ├── UI/
    │   │   ├── Banner.cs
    │   │   ├── ClickToPlay.cs
    │   │   ├── DungeonSettings.cs
    │   │   ├── Fader.cs
    │   │   ├── Guide.cs
    │   │   ├── Hints.cs
    │   │   ├── HintsToggle.cs
    │   │   ├── Hotbar.cs
    │   │   ├── HudPanel.cs
    │   │   ├── HuntCoach.cs
    │   │   ├── HuntIntro.cs
    │   │   └── SavedUi.cs
    │   └── XR/
    │       └── VrRig.cs
    ├── PodzGame/
    │   └── Resources/
    │       ├── PodzGameAssets.asset
    │       ├── PodzHudOverlay.shader
    │       └── PodzTextDepth.shader
    ├── Resources/
    │   ├── BillingMode.json
    │   ├── PodzMandrakeCry.wav
    │   └── UpperCaveSlime.mat
    ├── Samples/
    │   └── XR Interaction Toolkit/
    │       └── 3.0.11/
    │           └── XR Device Simulator/
    │               ├── UI/
    │               │   └── XR Device Simulator UI.prefab
    │               └── XR Device Simulator.prefab
    ├── Scenes/
    │   └── SampleScene.unity
    ├── Settings/
    │   ├── DefaultVolumeProfile.asset
    │   ├── Mobile_Renderer.asset
    │   ├── Mobile_RPAsset.asset
    │   ├── PC_Renderer.asset
    │   ├── PC_RPAsset.asset
    │   ├── SampleSceneProfile.asset
    │   └── UniversalRenderPipelineGlobalSettings.asset
    ├── XR/
    │   ├── Loaders/
    │   │   └── OpenXRLoader.asset
    │   ├── Settings/
    │   │   ├── OpenXR Editor Settings.asset
    │   │   ├── OpenXR Package Settings.asset
    │   │   └── OpenXRPackageSettings.asset
    │   └── XRGeneralSettingsPerBuildTarget.asset
    ├── XRI/
    │   └── Settings/
    │       ├── Resources/
    │       │   ├── InteractionLayerSettings.asset
    │       │   ├── XRDeviceSimulatorSettings.asset
    │       │   └── XRInteractionRuntimeSettings.asset
    │       └── XRInteractionEditorSettings.asset
    ├── Readme.asset
    └── TutorialInfo/
```

| Раздел | Назначение |
|---|---|
| `Assets/Game/Core`, `Shared` | Запуск, ввод, взаимодействия, данные предметов, деньги, сохранение, звук, свет и эффекты. |
| `Assets/Game/Dungeon/Common` | Генерация, общие компоненты уровня, бой и модели монстров, переходы, лечение. |
| `Assets/Game/Dungeon/Floor*` | Геометрия, префабы блоков и поведение монстров конкретной локации. |
| `Assets/Game/Tavern` | Зал, кухня, гости, Тоби, заказы, смены и магазин. |
| `Assets/Game/Player`, `UI`, `XR` | Игрок, инструменты, рюкзак и здоровье. Экранные и пространственные панели. Привязка контроллеров. |
| `Assets/Game/Editor`, `Developer` | Генераторы ассетов, обзорные стенды и диагностика. Игровые ссылки смотрите в Main и RuntimeAssets перед повторным экспортом. |
| `Assets/_Project/Configs/Original` | Игровой каталог и рецепты редакторского экспорта. У рецепта экспорта ресурс к просмотру лежит в `Last Prefab`. |
| `Assets/_Project/Prefabs/Original` | Сохранённые предметы, кухня, гости, UI и варианты крепости. |
| `Assets/_Project/Scenes/Original` | Обзорные сцены этажей. Их правки не меняют маршрут игрока в Main. |
| `Assets/Resources`, `Assets/PodzGame/Resources` | Ресурсы, загружаемые по имени, включая звук, материал слайма, шейдеры текста и HUD. |
| `Assets/Settings`, `Assets/XR`, `Assets/XRI` | Настройки URP, XR и XRI. `Assets/Samples` содержит образец XR Device Simulator. `Assets/TutorialInfo` относится к шаблону Unity. |
| `Assets/Scenes` | Сцена-заготовка Unity. Игровая сцена в `Assets/Game/Scenes`. |

## Сцены, генерация и переходы

[GameBootstrap.cs](Assets/Game/Core/GameBootstrap.cs) запускает таверну, игрока, системы и UI. [DungeonManager.cs](Assets/Game/Dungeon/Common/Generation/DungeonManager.cs) проверяет доступ через `CanEnter`, строит уровень, размещает игрока и очищает предыдущую локацию. [Portal.cs](Assets/Game/Dungeon/Common/Generation/Portal.cs) следит за нахождением игрока в зоне и вызывает назначенный обработчик. [TransitionDoor.cs](Assets/Game/Dungeon/Common/Transitions/TransitionDoor.cs) открывает физический проход перед порталом. Условия доступа и координаты переходов задаёт `DungeonManager`. Для сборки основная сцена первая в [EditorBuildSettings.asset](ProjectSettings/EditorBuildSettings.asset) (**File → Build Profiles → Scene List**).

| Локация | Редактируемые точки входа | Как собирается в Main |
|---|---|---|
| Верхние пещеры | [Floor_1_Review.unity](Assets/_Project/Scenes/Original/Floor_1_Review.unity), [Floor_1.asset](Assets/_Project/Configs/Original/Floor_1.asset), [UpperCaves.cs](Assets/Game/Dungeon/Floor01_UpperCaves/Environment/UpperCaves.cs) | Фиксированная планировка, каталог `Caves` в RuntimeAssets. Слайм, гриб, споры, сталактит, зоны луж. |
| Заросшие руины | [Floor_2_Review.unity](Assets/_Project/Scenes/Original/Floor_2_Review.unity), [Floor_2.asset](Assets/_Project/Configs/Original/Floor_2.asset), [OvergrownRuins.cs](Assets/Game/Dungeon/Floor02_OvergrownRuins/Environment/OvergrownRuins.cs) | Фиксированная планировка, каталог `Ruins`. Мандрагора, василиск, лозы, дротики, камнепад. |
| Крепость | [Floor_3_Review.unity](Assets/_Project/Scenes/Original/Floor_3_Review.unity), [Floor_3.asset](Assets/_Project/Configs/Original/Floor_3.asset), [FloorGenerator.cs](Assets/Game/Dungeon/Common/Generation/FloorGenerator.cs), [FortressReference.cs](Assets/Game/Dungeon/Floor03_Fortress/Environment/FortressReference.cs) | Случайный связный план из 6–9 комнат. Для игры используются `Fortress Rooms`, `Fortress Passages`, `Fortress Decor` из RuntimeAssets. `Floor_3.asset` относится к обзорному экспорту. |
| Логово дракона | [Floor_4_Review.unity](Assets/_Project/Scenes/Original/Floor_4_Review.unity), [Floor_4.asset](Assets/_Project/Configs/Original/Floor_4.asset), [DragonLair.cs](Assets/Game/Dungeon/Floor04_DragonLair/Environment/DragonLair.cs) | Сохранённая арена из `Lair`, дракон и укрытия. Возврат после победы задаётся в коде. |

У [DungeonBlock.cs](Assets/Game/Dungeon/Common/Generation/DungeonBlock.cs) ищите `Footprint`, `Connections` и ключ исходного блока. Каталоги хранят ссылки на префабы. Изменить форму стены можно в Prefab Mode. Чтобы сдвинуть **всю комнату**, изменить связи или условия спуска, изучите исходный план и проверки генератора. Геометрия прохода и игровой план должны совпадать.

## Монстры

У каждого в таблице указаны рецепт экспорта и **активный prefab**, найденный из ссылок `Saved Monsters` в Main. В Prefab Mode выбирайте корень с игровым компонентом. У дочерних объектов ищите `Collider`, [HitZone.cs](Assets/Game/Dungeon/Common/Monsters/HitZone.cs), [MonsterGrabPoint.cs](Assets/Game/Dungeon/Common/Monsters/MonsterGrabPoint.cs), `Mesh Renderer`, `Skinned Mesh Renderer` при наличии и `Audio Source` при наличии. [Monster.cs](Assets/Game/Dungeon/Common/Monsters/Monster.cs) задаёт общую смену состояний, урон игроку и выдачу добычи. [MonsterRegistry.cs](Assets/Game/Dungeon/Common/Monsters/MonsterRegistry.cs) ведёт список активных монстров. Конкретный алгоритм охоты и пороги удара задаются в скрипте вида.

| Монстр | Активный prefab и рецепт | Скрипт | Поля Inspector | Поведение |
| --- | --- | --- | --- | --- |
| Слайм, этаж 1 | [Assets/_Project/Configs/Original/Slime_Generated/Slime.prefab](Assets/_Project/Configs/Original/Slime_Generated/Slime.prefab) · [Slime.asset](Assets/_Project/Configs/Original/Slime.asset) | [Slime.cs](Assets/Game/Dungeon/Floor01_UpperCaves/Monsters/Slime/Slime.cs) | Stun Time, Saved Body, Saved Visual, Stun Audio, Stun Sound | Приближение и прыжок, захват лица, оглушение и добыча лопаткой. |
| Ходячий гриб, этаж 1 | [Assets/Game/Dungeon/Floor01_UpperCaves/Monsters/Mushroom/Mushroom_Generated/Mushroom.prefab](Assets/Game/Dungeon/Floor01_UpperCaves/Monsters/Mushroom/Mushroom_Generated/Mushroom.prefab) · [Mushroom.asset](Assets/_Project/Configs/Original/Mushroom.asset) | [Mushroom.cs](Assets/Game/Dungeon/Floor01_UpperCaves/Monsters/Mushroom/Mushroom.cs) | Tired Time, Cap | Подкрадывание, окно усталости, захват сачком. |
| Мандрагора, этаж 2 | [Assets/Game/Dungeon/Floor02_OvergrownRuins/Monsters/Mandrake/Mandrake_Generated/Mandrake.prefab](Assets/Game/Dungeon/Floor02_OvergrownRuins/Monsters/Mandrake/Mandrake_Generated/Mandrake.prefab) · [Mandrake.asset](Assets/_Project/Configs/Original/Mandrake.asset) | [Mandrake.cs](Assets/Game/Dungeon/Floor02_OvergrownRuins/Monsters/Mandrake/Mandrake.cs) | Hold Time, Wilt Time, ссылки Leaves и Root | Хват листьев, срезание, извлечение корня и крик. |
| Василиск, этаж 2 | [Assets/Game/Dungeon/Floor02_OvergrownRuins/Monsters/Basilisk/Basilisk_Generated/Basilisk.prefab](Assets/Game/Dungeon/Floor02_OvergrownRuins/Monsters/Basilisk/Basilisk_Generated/Basilisk.prefab) · [Basilisk.asset](Assets/_Project/Configs/Original/Basilisk.asset) | [Basilisk.cs](Assets/Game/Dungeon/Floor02_OvergrownRuins/Monsters/Basilisk/Basilisk.cs) | Stuck Time, Stun Time, After Hit Minimum, Neck, Head, Grab, Eyes | Взгляд, клевок, застревание клюва и удары по шее. |
| Живые доспехи, этаж 3 | [Assets/Game/Dungeon/Floor03_Fortress/Monsters/LivingArmor/LivingArmor_Generated/LivingArmor.prefab](Assets/Game/Dungeon/Floor03_Fortress/Monsters/LivingArmor/LivingArmor_Generated/LivingArmor.prefab) · [LivingArmor.asset](Assets/_Project/Configs/Original/LivingArmor.asset) | [LivingArmor.cs](Assets/Game/Dungeon/Floor03_Fortress/Monsters/LivingArmor/LivingArmor.cs) | Stuck Time, Kneel Time, Sword, Helmet, Torso, Helm | Окно застрявшего меча, удары молотком и снятие шлема. |
| Мимик, этаж 3 | [Assets/Game/Dungeon/Floor03_Fortress/Monsters/Mimic/Mimic_Generated/Mimic.prefab](Assets/Game/Dungeon/Floor03_Fortress/Monsters/Mimic/Mimic_Generated/Mimic.prefab) · [Mimic.asset](Assets/_Project/Configs/Original/Mimic.asset) | [Mimic.cs](Assets/Game/Dungeon/Floor03_Fortress/Monsters/Mimic/Mimic.cs) | Crack Time, Lid, Legs, Leg, Open | Маскировка, удары молотком и захват за ногу. |
| Красный дракон, этаж 4 | [Assets/Game/Dungeon/Floor04_DragonLair/Monsters/Dragon/Dragon_Generated/Dragon.prefab](Assets/Game/Dungeon/Floor04_DragonLair/Monsters/Dragon/Dragon_Generated/Dragon.prefab) · [Dragon.asset](Assets/_Project/Configs/Original/Dragon.asset) | [Dragon.cs](Assets/Game/Dungeon/Floor04_DragonLair/Monsters/Dragon/Dragon.cs) | Overheat Time, Tail Open Time, Landing Open Time, Bite Damage, Shielded Bite Damage, Landing Damage | Фазы боя, перегрев, уязвимое горло, щит, хвост и приземление. |

## Таверна, кухня, экономика

| Объект | Где открыть в Project | Inspector и связи |
|---|---|---|
| Архитектура таверны | [TavernArchitecture.prefab](Assets/_Project/Prefabs/TavernArchitecture.prefab) | В Main доступен одноимённый экземпляр. Дети `Floor`, `Ceiling`, `WallN`, `WallS`, `WallW`, `WallE`, `PartA`, `PartB`, `PartLow`, `PartTop` и светильники. Меняются Transform, Renderer, Collider и Light. [Kitchen.cs](Assets/Game/Tavern/Environment/Kitchen.cs) собирает остальное, включая координаты спавна, посадочных мест и зоны выдачи. |
| Рабочие поверхности | [KitchenPreparation.prefab](Assets/_Project/Prefabs/Original/KitchenPreparation/KitchenPreparation.prefab) | `KitchenPreparation` содержит `Sink`, `Board`, `Grill` типа Bounds. Они определяют рабочие зоны отдельно от дочерней геометрии. |
| Котлы | [LargeCauldron.prefab](Assets/_Project/Prefabs/Original/LargeCauldron/LargeCauldron.prefab), [SmallCauldron.prefab](Assets/_Project/Prefabs/Original/SmallCauldron/SmallCauldron.prefab) | [Cauldron.cs](Assets/Game/Tavern/Kitchen/Cooking/Cauldron.cs) содержит `Heat`, число порций `Large Portions` и `Small Portions`, ссылки на кнопки, огонь, пар и индикаторы. Ввод продукта проходит через [CauldronMouth.cs](Assets/Game/Tavern/Kitchen/Cooking/CauldronMouth.cs). Уровни нагрева, таймеры, ошибки готовки и выдача миски управляются кодом. |
| Ледник | [Fridge.prefab](Assets/_Project/Prefabs/Original/Fridge/Fridge.prefab), [FridgeScreen.prefab](Assets/_Project/Prefabs/Original/FridgeScreen/FridgeScreen.prefab) | [Fridge.cs](Assets/Game/Tavern/Kitchen/Ingredients/Fridge.cs) управляет дверями, местом выдачи и запасами. [FridgeSlot.cs](Assets/Game/Tavern/Kitchen/Ingredients/FridgeSlot.cs) отвечает за ячейку, [FridgeScreen.cs](Assets/Game/Tavern/Kitchen/Ingredients/FridgeScreen.cs) — за витрину. Овощи выдаются без расхода запаса, монстровая добыча расходуется. |
| Ингредиенты и миски | Каталог `Ingredients` и `Bowls` в [RuntimeAssets.asset](Assets/_Project/Configs/Original/RuntimeAssets.asset) | [Ingredient.cs](Assets/Game/Tavern/Kitchen/Ingredients/Ingredient.cs), [FoodModels.cs](Assets/Game/Tavern/Kitchen/Ingredients/FoodModels.cs), [Bowl.cs](Assets/Game/Tavern/Kitchen/Cooking/Bowl.cs). Геометрия правится в prefab. Способ обработки и число действий заданы в [Items.cs](Assets/Game/Core/Items.cs). |
| Книга рецептов и экраны | [RecipeScreen.prefab](Assets/_Project/Prefabs/Original/RecipeScreen/RecipeScreen.prefab), [Recipes.cs](Assets/Game/Tavern/Kitchen/Recipes/Recipes.cs) | Каталог `Recipe Parameters` задаёт имя записи, цену, секунды и нагрев. Состав, условие разблокировки, инструкции и формат шагов идут из кода. Ряды книги строит `Kitchen`. |
| Заказы и гости | [OrderBoard.prefab](Assets/_Project/Prefabs/Original/OrderBoard/OrderBoard.prefab), [Guest_Miner.prefab](Assets/_Project/Prefabs/Original/Guest_Miner/Guest_Miner.prefab), [Guest.cs](Assets/Game/Tavern/Guests/Guest.cs), [Guests.cs](Assets/Game/Tavern/Guests/Guests.cs) | У доски четыре карточки с текстами, иконкой и кольцом ожидания. `GuestManager` выбирает гостей и заказы. Остальные типы ищите в `RuntimeAssets → Guests`. |
| Тоби | [Toby.prefab](Assets/_Project/Prefabs/Original/Toby/Toby.prefab), [Toby.cs](Assets/Game/Tavern/Guests/Toby.cs) | `Speed`, `Delivery Time`, точка переноса `Carry`. Маршрут зависит от окна выдачи, столов и очереди заказов. |
| Магазин и деньги | [TavernShop.prefab](Assets/_Project/Prefabs/Original/TavernShop/TavernShop.prefab), [TavernShop.cs](Assets/Game/Tavern/Environment/TavernShop.cs), [GameFlow.cs](Assets/Game/Tavern/DayCycle/GameFlow.cs), [Wallet.cs](Assets/Game/Core/Wallet.cs) | Цены тесака, молотка и щита, длина дня и цели выручки в RuntimeAssets. `GameFlow` проверяет покупки, ведёт смену и вызывает сохранение. |

## Инструменты, ловушки и предметы подземелья

[Tool.cs](Assets/Game/Player/Tools/Tool.cs) определяет измерение скорости кончика, направление лезвия и поиск попаданий. В корне префаба инструмента можно проверить `Type`, `Tip`, `Normal Local`, `Cast R` и рендереры. [Interact.cs](Assets/Game/Core/Interact.cs) содержит `HitInfo` и таблицы действий. [ToolRules.cs](Assets/Game/Player/Tools/ToolRules.cs) ограничивает доступность инструмента. [HandController.cs](Assets/Game/Player/Hands/HandController.cs) отвечает за хват, [DungeonLoot.cs](Assets/Game/Player/Inventory/DungeonLoot.cs) — за физическую добычу и подбор. Правило удара смотрите в `OnHit` нужного монстра или `OnToolHit` ингредиента.

| Префаб | Назначение |
| --- | --- |
| [Assets/_Project/Prefabs/Original/Tool_Spatula/Tool_Spatula.prefab](Assets/_Project/Prefabs/Original/Tool_Spatula/Tool_Spatula.prefab) | Слайм |
| [Assets/_Project/Prefabs/Original/Tool_Net/Tool_Net.prefab](Assets/_Project/Prefabs/Original/Tool_Net/Tool_Net.prefab) | Ходячий гриб |
| [Assets/_Project/Prefabs/Original/Tool_Cleaver/Tool_Cleaver.prefab](Assets/_Project/Prefabs/Original/Tool_Cleaver/Tool_Cleaver.prefab) | Охота и рубка продуктов |
| [Assets/_Project/Prefabs/Original/Tool_Hammer/Tool_Hammer.prefab](Assets/_Project/Prefabs/Original/Tool_Hammer/Tool_Hammer.prefab) | Доспехи, мимик, обработка панциря |
| [Assets/_Project/Prefabs/Original/Tool_Shield/Tool_Shield.prefab](Assets/_Project/Prefabs/Original/Tool_Shield/Tool_Shield.prefab) | Защита |
| [Assets/_Project/Prefabs/Original/Tool_Knife/Tool_Knife.prefab](Assets/_Project/Prefabs/Original/Tool_Knife/Tool_Knife.prefab) | Подготовка продуктов |
| [Assets/_Project/Prefabs/Original/Tool_Ladle/Tool_Ladle.prefab](Assets/_Project/Prefabs/Original/Tool_Ladle/Tool_Ladle.prefab) | Работа у котла |

В следующей таблице путь префаба взят из `Last Prefab` соответствующего рецепта. В runtime используемые варианты связывает [RuntimeAssets.asset](Assets/_Project/Configs/Original/RuntimeAssets.asset). Откройте рецепт, щёлкните по ссылке **Last Prefab**, затем в Prefab Mode выберите дочерний объект с компонентом поведения.

| Объект | Рецепт → prefab | Компонент | Использование |
| --- | --- | --- | --- |
| Двери | [DoorWithHandles_F1.asset](Assets/_Project/Configs/Original/DoorWithHandles_F1.asset) → [Assets/Game/Dungeon/Common/Objects/DoorWithHandles_Floor1_Generated/DoorWithHandles.prefab](Assets/Game/Dungeon/Common/Objects/DoorWithHandles_Floor1_Generated/DoorWithHandles.prefab)<br>[DoorWithHandles_F2.asset](Assets/_Project/Configs/Original/DoorWithHandles_F2.asset) → [Assets/Game/Dungeon/Common/Objects/DoorWithHandles_Floor2_Generated/DoorWithHandles.prefab](Assets/Game/Dungeon/Common/Objects/DoorWithHandles_Floor2_Generated/DoorWithHandles.prefab)<br>[DoorWithHandles_F3.asset](Assets/_Project/Configs/Original/DoorWithHandles_F3.asset) → [Assets/Game/Dungeon/Common/Objects/DoorWithHandles_Floor3_Generated/DoorWithHandles.prefab](Assets/Game/Dungeon/Common/Objects/DoorWithHandles_Floor3_Generated/DoorWithHandles.prefab) | [TransitionDoor.cs](Assets/Game/Dungeon/Common/Transitions/TransitionDoor.cs) | Этажи 1–3. |
| Порталы | [PortalVisual_F1.asset](Assets/_Project/Configs/Original/PortalVisual_F1.asset) → [Assets/Game/Dungeon/Common/Objects/PortalVisual_Floor1_Generated/PortalVisual.prefab](Assets/Game/Dungeon/Common/Objects/PortalVisual_Floor1_Generated/PortalVisual.prefab)<br>[ReturnPortalVisual_F1.asset](Assets/_Project/Configs/Original/ReturnPortalVisual_F1.asset) → [Assets/Game/Dungeon/Common/Objects/ReturnPortalVisual_Floor1_Generated/ReturnPortalVisual.prefab](Assets/Game/Dungeon/Common/Objects/ReturnPortalVisual_Floor1_Generated/ReturnPortalVisual.prefab) | [Portal.cs](Assets/Game/Dungeon/Common/Generation/Portal.cs) | Оформление спуска и возврата, действие назначается при создании. |
| Лужа | [Puddle_F1.asset](Assets/_Project/Configs/Original/Puddle_F1.asset) → [Assets/Game/Dungeon/Common/Objects/Puddle_Floor1_Generated/Puddle.prefab](Assets/Game/Dungeon/Common/Objects/Puddle_Floor1_Generated/Puddle.prefab) | [Puddle.cs](Assets/Game/Dungeon/Common/Generation/Puddle.cs) | Зона замедления; в пещерах исходный диск скрывается. |
| Сундук | [Chest_F3.asset](Assets/_Project/Configs/Original/Chest_F3.asset) → [Assets/Game/Dungeon/Floor03_Fortress/Environment/Chest_Floor3_Generated/Chest.prefab](Assets/Game/Dungeon/Floor03_Fortress/Environment/Chest_Floor3_Generated/Chest.prefab) | [Chest.cs](Assets/Game/Dungeon/Floor03_Fortress/Environment/Chest.cs) | Награда выдаётся один раз; крышка и кнопка меняют состояние. |
| Эликсиры | [Elixir_F1.asset](Assets/_Project/Configs/Original/Elixir_F1.asset) → [Assets/Game/Dungeon/Common/Objects/Elixir_Floor1_Generated/Elixir.prefab](Assets/Game/Dungeon/Common/Objects/Elixir_Floor1_Generated/Elixir.prefab)<br>[Elixir_F2.asset](Assets/_Project/Configs/Original/Elixir_F2.asset) → [Assets/Game/Dungeon/Common/Objects/Elixir_Floor2_Generated/Elixir.prefab](Assets/Game/Dungeon/Common/Objects/Elixir_Floor2_Generated/Elixir.prefab)<br>[Elixir_F3.asset](Assets/_Project/Configs/Original/Elixir_F3.asset) → [Assets/Game/Dungeon/Common/Objects/Elixir_Floor3_Generated/Elixir.prefab](Assets/Game/Dungeon/Common/Objects/Elixir_Floor3_Generated/Elixir.prefab) | [ElixirPickup.cs](Assets/Game/Dungeon/Common/Healing/ElixirPickup.cs) | Третий вариант также используется для логова. |
| Сталактит и споры | [Stalactite_F1.asset](Assets/_Project/Configs/Original/Stalactite_F1.asset) → [Assets/Game/Dungeon/Floor01_UpperCaves/Traps/Stalactite_Floor1_Generated/Stalactite.prefab](Assets/Game/Dungeon/Floor01_UpperCaves/Traps/Stalactite_Floor1_Generated/Stalactite.prefab)<br>[Spores_F1.asset](Assets/_Project/Configs/Original/Spores_F1.asset) → [Assets/Game/Dungeon/Floor01_UpperCaves/Traps/Spores_Floor1_Generated/Spores.prefab](Assets/Game/Dungeon/Floor01_UpperCaves/Traps/Spores_Floor1_Generated/Spores.prefab)<br>[Spores_F2.asset](Assets/_Project/Configs/Original/Spores_F2.asset) → [Assets/Game/Dungeon/Floor02_OvergrownRuins/Traps/Spores_Floor2_Generated/Spores.prefab](Assets/Game/Dungeon/Floor02_OvergrownRuins/Traps/Spores_Floor2_Generated/Spores.prefab) | [Hazard.cs](Assets/Game/Dungeon/Common/Generation/Hazard.cs) | Пещеры и руины. |
| Лозы, камнепад и дротики | [Vines_F2.asset](Assets/_Project/Configs/Original/Vines_F2.asset) → [Assets/Game/Dungeon/Floor02_OvergrownRuins/Traps/Vines_Floor2_Generated/Vines.prefab](Assets/Game/Dungeon/Floor02_OvergrownRuins/Traps/Vines_Floor2_Generated/Vines.prefab)<br>[Rockfall_F2.asset](Assets/_Project/Configs/Original/Rockfall_F2.asset) → [Assets/Game/Dungeon/Floor02_OvergrownRuins/Traps/Rockfall_Floor2_Generated/Rockfall.prefab](Assets/Game/Dungeon/Floor02_OvergrownRuins/Traps/Rockfall_Floor2_Generated/Rockfall.prefab)<br>[DartPlate_F2.asset](Assets/_Project/Configs/Original/DartPlate_F2.asset) → [Assets/Game/Dungeon/Floor02_OvergrownRuins/Traps/DartPlate_Floor2_Generated/DartPlate.prefab](Assets/Game/Dungeon/Floor02_OvergrownRuins/Traps/DartPlate_Floor2_Generated/DartPlate.prefab)<br>[DartPlate_F3.asset](Assets/_Project/Configs/Original/DartPlate_F3.asset) → [Assets/Game/Dungeon/Floor03_Fortress/Traps/DartPlate_Floor3_Generated/DartPlate.prefab](Assets/Game/Dungeon/Floor03_Fortress/Traps/DartPlate_Floor3_Generated/DartPlate.prefab) | [Hazard.cs](Assets/Game/Dungeon/Common/Generation/Hazard.cs) | Руины и крепость. |
| Маятник и решётка | [Pendulum_F3.asset](Assets/_Project/Configs/Original/Pendulum_F3.asset) → [Assets/Game/Dungeon/Floor03_Fortress/Traps/Pendulum_Floor3_Generated/Pendulum.prefab](Assets/Game/Dungeon/Floor03_Fortress/Traps/Pendulum_Floor3_Generated/Pendulum.prefab)<br>[FireGrate_F3.asset](Assets/_Project/Configs/Original/FireGrate_F3.asset) → [Assets/Game/Dungeon/Floor03_Fortress/Traps/FireGrate_Floor3_Generated/FireGrate.prefab](Assets/Game/Dungeon/Floor03_Fortress/Traps/FireGrate_Floor3_Generated/FireGrate.prefab) | [Hazard.cs](Assets/Game/Dungeon/Common/Generation/Hazard.cs) | Крепость. |
| Кислотная капля | [AcidDrop_F1.asset](Assets/_Project/Configs/Original/AcidDrop_F1.asset) → [Assets/Game/Dungeon/Floor01_UpperCaves/Traps/AcidDrop_Floor1_Generated/AcidDrop.prefab](Assets/Game/Dungeon/Floor01_UpperCaves/Traps/AcidDrop_Floor1_Generated/AcidDrop.prefab) | [Hazard.cs](Assets/Game/Dungeon/Common/Generation/Hazard.cs) | Присутствует в каталоге; стартовый маршрут пещер её не размещает. |

- `TransitionDoor` — `Next Level`, `Leaf Collider`, петля и ручки. Координата и поворот всей двери назначаются при сборке локации.
- `Hazard` — поля `_radius`, `_dmg`, `_c`, `_vis`, `_t`, `_period`. При подключении сохранённой ловушки код сверяет радиус, урон и статус с ожидаемыми значениями. Изменение только prefab может привести к использованию программного варианта. Период и начальная фаза выбираются в коде. Камнепад всегда берёт четыре исходных случайных образца.
- `Puddle` — видимый диск и радиус зоны. Размер зоны задаёт вызывающий код. `ElixirPickup` — `Bottle`, свет и луч. Радиус и эффект лечения задают [ElixirPickup.cs](Assets/Game/Dungeon/Common/Healing/ElixirPickup.cs) и [Elixir.cs](Assets/Game/Dungeon/Common/Healing/Elixir.cs). `Chest` — кнопка и крышка, награда задаётся в [Chest.cs](Assets/Game/Dungeon/Floor03_Fortress/Environment/Chest.cs).
- Кислотная капля присутствует как ассет. Активный маршрут первого этажа не размещает её. Подключение через каталог само по себе не добавляет ловушку в комнату.

## Canvas, кнопки и обучение

Это пространственные Canvas с исходными `TextMesh`, мешами и `Pressable`. Для изменения текста выберите дочерний объект с `TextMesh`. Для изменения геометрии или кнопки выберите её объект и проверьте Collider. [Pressable.cs](Assets/Game/Core/Pressable.cs) содержит ссылку на надпись, цвет и состояние `Interactable`. Действие `OnPress` привязывается кодом при запуске.

| Префаб | Скрипт | Что открывается и меняется |
| --- | --- | --- |
| [Assets/_Project/Prefabs/Original/HudPanel/HudPanel.prefab](Assets/_Project/Prefabs/Original/HudPanel/HudPanel.prefab) | [HudPanel.cs](Assets/Game/UI/HudPanel.cs) | HP, монеты и место; позиции подстраиваются к камере. |
| [Assets/_Project/Prefabs/Original/Hotbar/Hotbar.prefab](Assets/_Project/Prefabs/Original/Hotbar/Hotbar.prefab) | [Hotbar.cs](Assets/Game/UI/Hotbar.cs) | Панель инструментов и эликсира. |
| [Assets/_Project/Prefabs/Original/OrderBoard/OrderBoard.prefab](Assets/_Project/Prefabs/Original/OrderBoard/OrderBoard.prefab) | [OrderBoard.cs](Assets/Game/Tavern/Orders/OrderBoard.cs) | Талоны, надписи и таймеры гостей. |
| [Assets/_Project/Prefabs/Original/RecipeScreen/RecipeScreen.prefab](Assets/_Project/Prefabs/Original/RecipeScreen/RecipeScreen.prefab) | [RecipeScreen.cs](Assets/Game/Tavern/Kitchen/Recipes/RecipeScreen.cs) | Название, текущий заказ и шаги рецепта. |
| [Assets/_Project/Prefabs/Original/FridgeScreen/FridgeScreen.prefab](Assets/_Project/Prefabs/Original/FridgeScreen/FridgeScreen.prefab) | [FridgeScreen.cs](Assets/Game/Tavern/Kitchen/Ingredients/FridgeScreen.cs) | Строки продуктов, числа и кнопки выдачи. |
| [Assets/_Project/Prefabs/Original/DungeonSettings/DungeonSettings.prefab](Assets/_Project/Prefabs/Original/DungeonSettings/DungeonSettings.prefab) | [DungeonSettings.cs](Assets/Game/UI/DungeonSettings.cs) | Панель настроек помощи и закрытие. |
| [Assets/_Project/Prefabs/Original/BestiaryCanvas/BestiaryCanvas.prefab](Assets/_Project/Prefabs/Original/BestiaryCanvas/BestiaryCanvas.prefab) | [Bestiary.cs](Assets/Game/Dungeon/Common/Monsters/Bestiary.cs) | Рамки и фон страниц Face и Details. |
| [Assets/_Project/Prefabs/Original/HuntIntroPc/HuntIntroPc.prefab](Assets/_Project/Prefabs/Original/HuntIntroPc/HuntIntroPc.prefab) | [HuntIntro.cs](Assets/Game/UI/HuntIntro.cs) | Экран F1 с клавишами ПК. |
| [Assets/_Project/Prefabs/Original/HuntIntroVr/HuntIntroVr.prefab](Assets/_Project/Prefabs/Original/HuntIntroVr/HuntIntroVr.prefab) | [HuntIntro.cs](Assets/Game/UI/HuntIntro.cs) | Версия вводного экрана для контроллеров. |
| [Assets/_Project/Prefabs/Original/GuideCanvas/GuideCanvas.prefab](Assets/_Project/Prefabs/Original/GuideCanvas/GuideCanvas.prefab) | [Guide.cs](Assets/Game/UI/Guide.cs) | Задача, карточка и стрелка. |
| [Assets/_Project/Prefabs/Original/HuntCoachCanvas/HuntCoachCanvas.prefab](Assets/_Project/Prefabs/Original/HuntCoachCanvas/HuntCoachCanvas.prefab) | [HuntCoach.cs](Assets/Game/UI/HuntCoach.cs) | Подсказка охоты и контур. |
| [Assets/_Project/Prefabs/Original/KitchenCoachCanvas/KitchenCoachCanvas.prefab](Assets/_Project/Prefabs/Original/KitchenCoachCanvas/KitchenCoachCanvas.prefab) | [KitchenCoach.cs](Assets/Game/Tavern/Kitchen/KitchenCoach.cs) | Контекстные подсказки кухни. |

[Hints.cs](Assets/Game/UI/Hints.cs), [Banner.cs](Assets/Game/UI/Banner.cs), [HintsToggle.cs](Assets/Game/UI/HintsToggle.cs), [ClickToPlay.cs](Assets/Game/UI/ClickToPlay.cs), [Fader.cs](Assets/Game/UI/Fader.cs) и [BackpackView.cs](Assets/Game/Player/Inventory/BackpackView.cs) отвечают за подсказки, уведомления, переключатели, затемнение и рюкзак. В этих системах часть объектов создаётся кодом. Контекстный экран рецепта с иллюстрациями реализован в [RecipePicture.cs](Assets/Game/Tavern/Kitchen/Recipes/RecipePicture.cs). Бестиарий использует сохранённые фон и рамки. Карточки наполняются по данным [Bestiary.cs](Assets/Game/Dungeon/Common/Monsters/Bestiary.cs) и [MonsterCard.cs](Assets/Game/Dungeon/Common/Monsters/MonsterCard.cs).

## Звук, материалы, свет, настройки и сохранение

- **Звук.** [PodzMandrakeCry.wav](Assets/Resources/PodzMandrakeCry.wav) используется [MandrakeVoice.cs](Assets/Game/Dungeon/Floor02_OvergrownRuins/Monsters/Mandrake/MandrakeVoice.cs). Откройте клип в Project для настроек импорта. В сохранённом prefab слайма есть ссылки `Stun Audio` и `Stun Sound`. Остальные сигналы, в том числе кнопки и ловушки, синтезирует [Sfx.cs](Assets/Game/Shared/Sfx.cs). Громкость отдельного сигнала можно изменить в аргументе `volume` места вызова.
- **Материалы.** В Prefab Mode выберите объект → **Mesh Renderer → Materials**. Откройте назначенный материал или назначьте копию для локальной правки. Ресурсы камня и общие сгенерированные материалы лежат в `Assets/Game/Dungeon/Common/GeneratedResources`. [UpperCaveSlime.mat](Assets/Resources/UpperCaveSlime.mat) загружается по имени. Игровой код может управлять цветом, прозрачностью и свечением в ходе боя. [PodzGameAssets.asset](Assets/PodzGame/Resources/PodzGameAssets.asset) связывает общие ресурсы.
- **Свет и эффекты.** У светильника меняйте `Color`, `Intensity`, `Range` в **Light**. Общий фон, освещение и туман назначают [WorldLighting.cs](Assets/Game/Shared/WorldLighting.cs) и [Biomes.cs](Assets/Game/Dungeon/Common/Generation/Biomes.cs) при входе. [GlowRing.cs](Assets/Game/Dungeon/Common/Generation/GlowRing.cs), [Prim.cs](Assets/Game/Shared/Prim.cs) и эффекты в [Bestiary.cs](Assets/Game/Dungeon/Common/Monsters/Bestiary.cs) управляют подсветкой, частицами и цветом. Параметры URP: [PC_RPAsset.asset](Assets/Settings/PC_RPAsset.asset), [Mobile_RPAsset.asset](Assets/Settings/Mobile_RPAsset.asset).
- **Сохранение.** [SaveSystem.cs](Assets/Game/Core/SaveSystem.cs) читает и пишет `podz_save.json` в `Application.persistentDataPath`. Это пользовательская папка вне Assets, её фактический путь выводится в Console при сохранении. `SaveData` содержит монеты, день, купленные инструменты, прогресс, увиденных монстров, запасы ледника и состояние обучения. [GameFlow.cs](Assets/Game/Tavern/DayCycle/GameFlow.cs) переносит данные между профилем и системами. Для проверки правок используйте копию профиля.
- **Настройки.** [manifest.json](Packages/manifest.json) перечисляет пакеты, [packages-lock.json](Packages/packages-lock.json) фиксирует разрешённые версии, [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) фиксирует Unity. [ProjectSettings.asset](ProjectSettings/ProjectSettings.asset) задаёт общие настройки игрока и сборки. [VrRig.cs](Assets/Game/XR/VrRig.cs) подключает XR-взаимодействия, [PodzXRSetup.cs](Assets/Game/Editor/PodzXRSetup.cs) содержит отдельные редакторские настройки. Для обычной правки моделей и механик менять XR не требуется.

## ЧТО И ГДЕ РЕДАКТИРОВАТЬ

1. **Скорость, здоровье и урон слайма.** В Project найдите `t:Prefab Slime` и откройте prefab из строки слайма выше. На корне измените `Stun Time`, если нужна другая длительность оглушения. Для скорости откройте [Slime.cs](Assets/Game/Dungeon/Floor01_UpperCaves/Monsters/Slime/Slime.cs) двойным щелчком из Project и найдите `Move(ToPlayerFlat, 1.1f, dt)`. Это скорость приближения в соответствующем состоянии. У слайма нет обычного поля HP. Его устойчивость задаёт `OnHit` с переходом «оглушение → следующий удар → добыча». Урон от слайма задаёт `Dmg.SlimePerSec` в [Damage.cs](Assets/Game/Core/Damage.cs). Максимум HP игрока см. в [PlayerHealth.cs](Assets/Game/Player/Health/PlayerHealth.cs).
2. **Внешний вид монстра.** `Main → GameBootstrap → Saved Monsters` → откройте назначенный prefab. В Prefab Mode выберите визуального ребёнка, измените Mesh Filter или материалы Renderer. Проверьте отдельно HitZone, Collider и точки хвата. Если используете рецепт экспорта, откройте его из таблицы и сверяйте `Last Prefab`. Новая генерация может создать отдельный результат, который нужно явно назначить.
3. **Размер, цвет и расположение кухни.** Откройте [KitchenPreparation.prefab](Assets/_Project/Prefabs/Original/KitchenPreparation/KitchenPreparation.prefab) → `PrepCounter`, `Sink`, `Board` или `Grill` → **Transform** и **Mesh Renderer → Materials**. Перенесите также соответствующий `Bounds` на корне и проверьте рабочие коллайдеры. Чтобы передвинуть котёл или ледник в готовой таверне, откройте [Kitchen.cs](Assets/Game/Tavern/Environment/Kitchen.cs), найдите вызов их фабрики в `Build` и измените передаваемую позицию. Проверьте проход, экран и окно выдачи. Стены таверны можно менять в Main или в её архитектурном prefab.
4. **Расположение дверей.** Откройте один из префабов двери из таблицы → выберите петлю, ручку или створку → измените Transform и проверьте `Leaf Collider`. Для переноса дверного проёма откройте [DungeonManager.cs](Assets/Game/Dungeon/Common/Generation/DungeonManager.cs), найдите `TransitionDoor.Create` и проследите `TransitionEntrance` и `TransitionForward` до соответствующего плана. Согласуйте координаты двери, ступеней и зоны портала.
5. **Урон и поведение инструментов.** Откройте, например, [Tool_Hammer.prefab](Assets/_Project/Prefabs/Original/Tool_Hammer/Tool_Hammer.prefab) → корневой `Tool` → проверьте `Tip`, `Normal Local` и `Cast R`. Для порогов удара откройте [Tool.cs](Assets/Game/Player/Tools/Tool.cs) и `OnHit` нужного монстра, например [LivingArmor.cs](Assets/Game/Dungeon/Floor03_Fortress/Monsters/LivingArmor/LivingArmor.cs). У каждого вида собственные условия. Универсального поля `Damage` у инструмента нет.
6. **Цены ингредиентов и блюд.** Выберите [RuntimeAssets.asset](Assets/_Project/Configs/Original/RuntimeAssets.asset) → **Recipe Parameters** → строка блюда → `Price`. Там же находятся цены трёх покупаемых инструментов. У ингредиентов нет отдельных цен в текущей модели [Items.cs](Assets/Game/Core/Items.cs). Чтобы ввести продажу ингредиентов, потребуется изменение механики магазина и данных, отдельное от изменения цены блюда.
7. **Изменить или добавить рецепт.** Для цены, времени и огня выберите RuntimeAssets → **Recipe Parameters** → `Price`, `Cook Seconds`, `Heat`. Для состава и последовательности откройте [Recipes.cs](Assets/Game/Tavern/Kitchen/Recipes/Recipes.cs) из Project и измените соответствующую запись. Для нового рецепта проверьте уникальный индекс, доступность для гостей, тексты и картинки, ссылки `Bowls` и настройку нового рецепта. После сохранения кода проверьте полный цикл от котла до оплаты.
8. **Кнопки.** Откройте [LargeCauldron.prefab](Assets/_Project/Prefabs/Original/LargeCauldron/LargeCauldron.prefab) или [DungeonSettings.prefab](Assets/_Project/Prefabs/Original/DungeonSettings/DungeonSettings.prefab) → выберите объект кнопки → **Mesh Renderer**, **Collider**, **Pressable**. По ссылке `Label` найдите текст. Надписи, задаваемые заново через `SetLabel`, меняйте в соответствующем скрипте. Например, у настроек текст переключателя собирается в [DungeonSettings.cs](Assets/Game/UI/DungeonSettings.cs).
9. **Комнаты и декор крепости.** Выберите RuntimeAssets → `Fortress Rooms` или `Fortress Decor` → откройте назначенный шаблон. `FortressRoom_8x8_1` — пример шаблона с размерами основания и маской проёмов, закодированной последней цифрой. Измените геометрию и коллайдеры, сохраните проходимость. Декор выбирается по размеру и типу комнаты. Часть мелких деталей уже объединена в Mesh, поэтому их можно менять как группу или через исходный [FortressReference.cs](Assets/Game/Dungeon/Floor03_Fortress/Environment/FortressReference.cs). Для новых размеров и проёмов сверяйте [FloorGenerator.cs](Assets/Game/Dungeon/Common/Generation/FloorGenerator.cs).
10. **Звук, громкость и освещение.** Найдите [PodzMandrakeCry.wav](Assets/Resources/PodzMandrakeCry.wav) → **Inspector** для импорта, либо выберите `Audio Source` у сохранённого объекта и измените `Volume`. Для синтезированных сигналов откройте [Sfx.cs](Assets/Game/Shared/Sfx.cs) и найдите вызов нужного сигнала. Для света откройте [TavernArchitecture.prefab](Assets/_Project/Prefabs/TavernArchitecture.prefab) или комнату крепости → дочерний Light → `Intensity`, `Color`, `Range`. Для общего тумана используйте [WorldLighting.cs](Assets/Game/Shared/WorldLighting.cs) и биом.
11. **Ловушки.** Откройте, например, [Rockfall_F2.asset](Assets/_Project/Configs/Original/Rockfall_F2.asset) → `Last Prefab` → объект с `Hazard`. Форму и материал можно менять в Prefab Mode. Сверьте радиус, урон, статус и период с [Hazard.cs](Assets/Game/Dungeon/Common/Generation/Hazard.cs), [Damage.cs](Assets/Game/Core/Damage.cs) и вызовом в [DungeonManager.cs](Assets/Game/Dungeon/Common/Generation/DungeonManager.cs). Для перемещения всей ловушки меняйте точку её установки в менеджере этажа. Для камнепада сохраняйте четыре исходных случайных образца.
12. **Монеты и сохранение.** Для цен и целей выберите RuntimeAssets → `Cleaver Price`, `Hammer Price`, `Shield Price`, `First Goal`, `Second Goal`, `Third Goal`. Текущие монеты принадлежат профилю. Начальное число для нового профиля задаёт `SaveData.coins` в [SaveSystem.cs](Assets/Game/Core/SaveSystem.cs), начисления проходят через [Wallet.cs](Assets/Game/Core/Wallet.cs). Чтобы менять формат сохранения, изучите `Load`, `Save` и `GameFlow.SaveNow`. Проверьте загрузку старого профиля, сохранение нового и повторную загрузку.

## БЫСТРЫЙ ПОИСК

| Хочу изменить | Где искать | Что открыть | Что менять |
|---|---|---|---|
| Поменять стартовую сцену сборки | File → Build Profiles | [EditorBuildSettings.asset](ProjectSettings/EditorBuildSettings.asset) | Scene List |
| Перекрасить слайма | Project → Resources | [UpperCaveSlime.mat](Assets/Resources/UpperCaveSlime.mat) | Материал, затем проверить игровые состояния |
| Замедлить слайма | Project → Slime | [Slime.cs](Assets/Game/Dungeon/Floor01_UpperCaves/Monsters/Slime/Slime.cs) | Скорость в вызове Move |
| Длительность оглушения | Main → Saved Monsters | Префаб слайма из таблицы | Stun Time |
| Передвинуть раковину | Project → KitchenPreparation | [KitchenPreparation.prefab](Assets/_Project/Prefabs/Original/KitchenPreparation/KitchenPreparation.prefab) | Transform и Sink Bounds |
| Цену и время блюда | Project → RuntimeAssets | [RuntimeAssets.asset](Assets/_Project/Configs/Original/RuntimeAssets.asset) | Recipe Parameters |
| Состав блюда | Project → Recipes | [Recipes.cs](Assets/Game/Tavern/Kitchen/Recipes/Recipes.cs) | Запись рецепта |
| Скорость Тоби | Project → Toby | [Toby.prefab](Assets/_Project/Prefabs/Original/Toby/Toby.prefab) | Speed, Delivery Time |
| Цвет доски заказов | Project → OrderBoard | [OrderBoard.prefab](Assets/_Project/Prefabs/Original/OrderBoard/OrderBoard.prefab) | Материалы, TextMesh карточек |
| Положение перехода | Game → Dungeon → Common → Generation | [DungeonManager.cs](Assets/Game/Dungeon/Common/Generation/DungeonManager.cs) | TransitionEntrance, портал и направления |
| Вид комнаты крепости | Project → RuntimeAssets | [RuntimeAssets.asset](Assets/_Project/Configs/Original/RuntimeAssets.asset) | Fortress Rooms и Fortress Decor |
| Подсказку обучения | Project → HuntIntroPc | [HuntIntroPc.prefab](Assets/_Project/Prefabs/Original/HuntIntroPc/HuntIntroPc.prefab) | Дочерний TextMesh. Динамические подсказки — Guide и Coach |
| Сохранение монет | Game → Core | [SaveSystem.cs](Assets/Game/Core/SaveSystem.cs) | SaveData, Load, Save |
