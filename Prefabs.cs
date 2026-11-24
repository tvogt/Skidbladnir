using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace Skidbladnir
{
    /// <summary>
    /// Creates and registers the folded-ship item, its one-piece build table and the unfold piece.
    ///
    /// - Item "Skidbladnir": clone of the Hammer (a build tool), with its own piece table, mini-longship model,
    ///   quality 1-2 and durability = ship health in percent.
    /// - Unfold piece "Skidbladnir_Unfold": clone of the vanilla longship piece (same colliders and placement rules)
    ///   without cost and without workbench. It is only used for the placement ghost; the placed object is
    ///   the vanilla longship (see Unfold), so removing the mod leaves ordinary longships behind.
    /// </summary>
    internal static class Prefabs
    {
        internal const string ItemName = "Skidbladnir";
        internal const string UnfoldName = "Skidbladnir_Unfold";
        internal const string DisplayName = "Skíðblaðnir";
        internal const string LongshipName = "VikingShip";

        internal static GameObject Item;            // item prefab
        internal static Piece UnfoldPiece;          // placement piece in our table
        internal static Piece LongshipPiece;        // the vanilla longship piece
        internal static Recipe Recipe;

        private static GameObject s_root;           // inactive holder so clones never Awake
        private static GameObject s_hammer;
        private static bool s_failed;

        private static readonly AccessTools.FieldRef<ObjectDB, Dictionary<int, GameObject>> ItemByHash =
            AccessTools.FieldRefAccess<ObjectDB, Dictionary<int, GameObject>>("m_itemByHash");
        private static readonly AccessTools.FieldRef<ZNetScene, Dictionary<int, GameObject>> NamedPrefabs =
            AccessTools.FieldRefAccess<ZNetScene, Dictionary<int, GameObject>>("m_namedPrefabs");

        private static GameObject FindPrefab(string name)
        {
            ObjectDB db = ObjectDB.instance;
            if (db)
            {
                GameObject p = db.GetItemPrefab(name);
                if (p) return p;
            }
            ZNetScene zs = ZNetScene.instance;
            if (zs)
            {
                GameObject p = zs.GetPrefab(name);
                if (p) return p;
            }
            PieceTable table = s_hammer ? s_hammer.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces : null;
            if (table)
            {
                foreach (GameObject p in table.m_pieces)
                    if (p && p.name == name) return p;
            }
            return null;
        }

        /// <summary>Creates the prefabs once. Needs only the Hammer prefab (its piece table holds the longship).</summary>
        private static bool Ensure(ObjectDB db)
        {
            if (Item) return true;
            if (s_failed) return false;

            if (!s_hammer)
            {
                if (db) s_hammer = db.m_items.Find(i => i && i.name == "Hammer");
                if (!s_hammer) s_hammer = FindPrefab("Hammer");
            }
            if (!s_hammer) return false; // try again at the next registration hook

            LongshipPiece = FindPrefab(LongshipName)?.GetComponent<Piece>();
            if (!LongshipPiece)
            {
                Plugin.Log.LogError($"Longship prefab '{LongshipName}' not found; {DisplayName} is disabled.");
                s_failed = true;
                return false;
            }

            s_root = new GameObject("Skidbladnir_Prefabs");
            s_root.SetActive(false);
            Object.DontDestroyOnLoad(s_root);

            // Unfold piece: the longship without cost and workbench.
            GameObject unfold = Object.Instantiate(LongshipPiece.gameObject, s_root.transform);
            unfold.name = UnfoldName;
            UnfoldPiece = unfold.GetComponent<Piece>();
            UnfoldPiece.m_name = "Unfold " + DisplayName; // distinct from the item: known pieces and recipes share one name list
            UnfoldPiece.m_description = "Unfold the ship here.";
            UnfoldPiece.m_resources = new Piece.Requirement[0];
            UnfoldPiece.m_craftingStation = null;
            UnfoldPiece.m_category = Piece.PieceCategory.Misc;
            UnfoldPiece.m_enabled = true;

            var tableGo = new GameObject("Skidbladnir_PieceTable");
            tableGo.transform.SetParent(s_root.transform, false);
            PieceTable table = tableGo.AddComponent<PieceTable>();
            table.m_pieces = new List<GameObject> { unfold };
            table.m_categories = new List<Piece.PieceCategory>();
            table.m_categoryLabels = new List<string>();
            table.m_canRemovePieces = false;
            table.m_canRemoveFeasts = false;
            table.m_skill = Skills.SkillType.None;
            table.m_hideAdvancedMenu = true;

            // Item: a build tool like the hammer.
            Item = Object.Instantiate(s_hammer, s_root.transform);
            Item.name = ItemName;
            LogHierarchy(Item.transform);
            ItemDrop.ItemData.SharedData sd = Item.GetComponent<ItemDrop>().m_itemData.m_shared;
            sd.m_name = DisplayName;
            sd.m_description = "Freyr's ship, made by the sons of Ívaldi. When it is not needed, it folds like a cloth and fits in a pouch.\n\n" +
                               "Equip it at the shore and place it to unfold it. Fold it again with the hammer's remove action (hold empty, nobody aboard).\n\n" +
                               "Level 2: a fair wind whenever someone is aboard.";
            sd.m_buildPieces = table;
            sd.m_maxStackSize = 1;
            sd.m_maxQuality = 2;
            sd.m_useDurability = true;
            sd.m_maxDurability = 100f;     // = ship health in percent
            sd.m_durabilityPerLevel = 0f;
            sd.m_destroyBroken = false;
            sd.m_canBeReparied = true;
            sd.m_teleportable = true;
            sd.m_value = 0;
            if (LongshipPiece.m_icon) sd.m_icons = new[] { LongshipPiece.m_icon };
            Miniature.Apply(Item, LongshipPiece.gameObject);

            Recipe = ScriptableObject.CreateInstance<Recipe>();
            Recipe.name = "Recipe_" + ItemName;
            Recipe.m_item = Item.GetComponent<ItemDrop>();
            Recipe.m_amount = 1;
            Recipe.m_enabled = true;

            Plugin.Log.LogInfo($"{DisplayName} prefabs created (from Hammer and {LongshipName}).");
            return true;
        }

        private static void LogHierarchy(Transform t)
        {
            var sb = new StringBuilder("Hammer clone hierarchy:");
            void Walk(Transform x, int depth)
            {
                sb.Append('\n').Append(' ', depth * 2).Append(x.name);
                foreach (Component c in x.GetComponents<Component>())
                    if (c && !(c is Transform)) sb.Append(" [").Append(c.GetType().Name).Append(']');
                foreach (Transform child in x) Walk(child, depth + 1);
            }
            Walk(t, 0);
            Plugin.Log.LogInfo(sb.ToString());
        }

        /// <summary>Applies config to the recipe and item. Safe to call any time; parts that cannot be resolved yet are filled in later.</summary>
        internal static void UpdateRecipeAndItem()
        {
            if (!Item || !Recipe) return;
            Item.GetComponent<ItemDrop>().m_itemData.m_shared.m_weight = Plugin.Weight.Value;

            GameObject station = FindPrefab(Plugin.Station.Value.Trim());
            Recipe.m_craftingStation = station ? station.GetComponentInChildren<CraftingStation>() : null;
            if (!Recipe.m_craftingStation)
                Plugin.Log.LogWarning($"Crafting station '{Plugin.Station.Value}' not found (yet); the recipe needs no station until it is.");
            Recipe.m_minStationLevel = Plugin.StationLevel.Value;

            var reqs = new List<Piece.Requirement>();
            if (Plugin.LongshipCost.Value && LongshipPiece)
            {
                foreach (Piece.Requirement r in LongshipPiece.m_resources)
                {
                    if (r?.m_resItem == null || r.m_amount <= 0) continue;
                    reqs.Add(new Piece.Requirement { m_resItem = r.m_resItem, m_amount = r.m_amount, m_amountPerLevel = 0, m_recover = true });
                }
            }
            AddCost(reqs, Plugin.ExtraCost.Value, upgrade: false);
            AddCost(reqs, Plugin.UpgradeCost.Value, upgrade: true);
            Recipe.m_resources = reqs.ToArray();
        }

        private static void AddCost(List<Piece.Requirement> reqs, string text, bool upgrade)
        {
            foreach (KeyValuePair<string, int> kv in Plugin.ParseCost(text))
            {
                ItemDrop drop = FindPrefab(kv.Key)?.GetComponent<ItemDrop>();
                if (!drop)
                {
                    Plugin.Log.LogWarning($"Recipe item '{kv.Key}' not found (yet).");
                    continue;
                }
                reqs.Add(upgrade
                    ? new Piece.Requirement { m_resItem = drop, m_amount = 0, m_amountPerLevel = kv.Value, m_recover = false }
                    : new Piece.Requirement { m_resItem = drop, m_amount = kv.Value, m_amountPerLevel = 0, m_recover = true });
            }
        }

        private static void Register(ObjectDB db)
        {
            if (!db || !Ensure(db)) return;
            if (!db.m_items.Contains(Item)) db.m_items.Add(Item);
            ItemByHash(db)[ItemName.GetStableHashCode()] = Item;
            UpdateRecipeAndItem();
            if (!db.m_recipes.Contains(Recipe)) db.m_recipes.Add(Recipe);
        }

        // Vanilla: upgrade level = crafting level + (quality - 1). Ours has its own upgrade level.
        [HarmonyPatch(typeof(Recipe), nameof(Recipe.GetRequiredStationLevel))]
        private static class RequiredStationLevel
        {
            private static void Postfix(Recipe __instance, int quality, ref int __result)
            {
                if (__instance != Recipe || !Recipe || quality < 2) return;
                __result = Mathf.Max(Plugin.StationLevel.Value, Plugin.UpgradeStationLevel.Value);
            }
        }

        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        private static class ObjectDBAwake
        {
            [HarmonyPriority(Priority.Low)]
            private static void Postfix(ObjectDB __instance) => Register(__instance);
        }

        // Main menu: FejdStartup copies the game's ObjectDB. The item must be known there too, or the
        // character's inventory would drop it on load.
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        private static class ObjectDBCopy
        {
            [HarmonyPriority(Priority.Low)]
            private static void Postfix(ObjectDB __instance) => Register(__instance);
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        private static class ZNetSceneAwake
        {
            [HarmonyPriority(Priority.Low)]
            private static void Postfix(ZNetScene __instance)
            {
                if (!Ensure(ObjectDB.instance)) return;
                if (!__instance.m_prefabs.Contains(Item)) __instance.m_prefabs.Add(Item);
                NamedPrefabs(__instance)[ItemName.GetStableHashCode()] = Item;
                UpdateRecipeAndItem(); // the station may only be resolvable now
            }
        }
    }
}
