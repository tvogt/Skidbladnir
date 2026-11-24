using HarmonyLib;
using UnityEngine;

namespace Skidbladnir
{
    /// <summary>
    /// Placing the unfold piece (with the equipped folded ship) places a vanilla longship instead, marks it as
    /// Skíðblaðnir with the item's level, sets its health from the item's durability and removes the item.
    /// The game's own placement checks (water, space, wards, no-build zones) apply because the ghost is a
    /// longship clone.
    /// </summary>
    internal static class Unfold
    {
        internal static readonly int LevelKey = "Skidbladnir.Level".GetStableHashCode();

        private static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> RightItem =
            AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_rightItem");

        private static ItemDrop.ItemData s_placing;     // item being unfolded during PlacePiece
        private static ItemDrop.ItemData s_toRemove;    // removed after UpdatePlacement, which still uses the build tool

        internal static int GetLevel(ZNetView nview)
        {
            if (!nview || !nview.IsValid()) return 0;
            return nview.GetZDO().GetInt(LevelKey, 0);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
        private static class PlacePiecePatch
        {
            private static void Prefix(Player __instance, ref Piece piece)
            {
                if (!Prefabs.UnfoldPiece || piece != Prefabs.UnfoldPiece || !Prefabs.LongshipPiece) return;
                ItemDrop.ItemData item = RightItem(__instance);
                if (item == null || item.m_shared.m_name != Prefabs.DisplayName) return;
                s_placing = item;
                piece = Prefabs.LongshipPiece;
            }

            private static void Postfix()
            {
                if (s_placing == null) return;
                s_toRemove = s_placing;
                s_placing = null;
            }

            private static System.Exception Finalizer(System.Exception __exception)
            {
                s_placing = null;
                return __exception;
            }
        }

        // Piece.SetCreator is called on the freshly instantiated object inside PlacePiece.
        [HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
        private static class SetCreatorPatch
        {
            private static void Postfix(Piece __instance)
            {
                ItemDrop.ItemData item = s_placing;
                if (item == null || !__instance.GetComponent<Ship>()) return;
                ZNetView nview = __instance.GetComponent<ZNetView>();
                if (!nview || !nview.IsValid()) return;
                ZDO zdo = nview.GetZDO();
                zdo.Set(LevelKey, Mathf.Max(1, item.m_quality));
                WearNTear wnt = __instance.GetComponent<WearNTear>();
                if (wnt)
                {
                    float max = item.GetMaxDurability();
                    float fraction = max > 0f ? Mathf.Clamp(item.m_durability / max, 0.01f, 1f) : 1f;
                    zdo.Set(ZDOVars.s_health, wnt.m_health * fraction);
                }
                Plugin.Log.LogInfo($"Unfolded {Prefabs.DisplayName} (level {item.m_quality}, {item.m_durability:0}% health).");
            }
        }

        [HarmonyPatch(typeof(Player), "UpdatePlacement")]
        private static class UpdatePlacementPatch
        {
            private static void Postfix(Player __instance)
            {
                ItemDrop.ItemData item = s_toRemove;
                if (item == null) return;
                s_toRemove = null;
                __instance.UnequipItem(item, false);
                __instance.GetInventory().RemoveItem(item);
            }
        }
    }
}
