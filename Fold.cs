using HarmonyLib;
using UnityEngine;

namespace Skidbladnir
{
    /// <summary>
    /// The hammer's remove action on a Skíðblaðnir folds it back into the item (level and health kept)
    /// instead of tearing it down for materials. Same access rules as removing a piece; additionally the hold
    /// must be empty and closed and nobody may be aboard. Ordinary longships are not affected.
    /// </summary>
    internal static class Fold
    {
        private static readonly AccessTools.FieldRef<Player, int> RemoveRayMask = AccessTools.FieldRefAccess<Player, int>("m_removeRayMask");

        [HarmonyPatch(typeof(Player), "RemovePiece")]
        private static class RemovePiecePatch
        {
            private static bool Prefix(Player __instance, ref bool __result)
            {
                if (!Prefabs.Item || !GameCamera.instance) return true;
                Transform cam = GameCamera.instance.transform;
                if (!Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 50f, RemoveRayMask(__instance))) return true;
                if (Vector3.Distance(hit.point, __instance.m_eye.position) >= __instance.m_maxPlaceDistance) return true;
                Piece piece = hit.collider.GetComponentInParent<Piece>();
                if (!piece) return true;
                ZNetView nview = piece.GetComponent<ZNetView>();
                int level = Unfold.GetLevel(nview);
                if (level <= 0) return true; // not ours: vanilla removal

                __result = TryFold(__instance, piece, nview, level);
                return false;
            }
        }

        private static bool TryFold(Player player, Piece piece, ZNetView nview, int level)
        {
            if (Location.IsInsideNoBuildLocation(piece.transform.position))
            {
                player.Message(MessageHud.MessageType.Center, "$msg_nobuildzone");
                return false;
            }
            if (!PrivateArea.CheckAccess(piece.transform.position))
            {
                player.Message(MessageHud.MessageType.Center, "$msg_privatezone");
                return false;
            }
            Ship ship = piece.GetComponent<Ship>();
            if (ship && !ship.CanBeRemoved())
            {
                player.Message(MessageHud.MessageType.Center, "Nobody may be aboard to fold the ship.");
                return false;
            }
            ZDO zdo = nview.GetZDO();
            Container hold = piece.GetComponentInChildren<Container>();
            if (hold)
            {
                if (zdo.GetInt(ZDOVars.s_inUse) == 1 || hold.IsInUse())
                {
                    player.Message(MessageHud.MessageType.Center, "$msg_inuse");
                    return false;
                }
                if (hold.GetInventory() != null && hold.GetInventory().NrOfItems() > 0)
                {
                    player.Message(MessageHud.MessageType.Center, "Empty the hold first.");
                    return false;
                }
            }

            ItemDrop prefab = Prefabs.Item.GetComponent<ItemDrop>();
            ItemDrop.ItemData item = prefab.m_itemData.Clone();
            item.m_dropPrefab = Prefabs.Item;
            item.m_stack = 1;
            item.m_quality = Mathf.Clamp(level, 1, item.m_shared.m_maxQuality);
            item.m_worldLevel = Game.m_worldLevel;
            WearNTear wnt = piece.GetComponent<WearNTear>();
            float fraction = 1f;
            if (wnt && wnt.m_health > 0f) fraction = Mathf.Clamp01(zdo.GetFloat(ZDOVars.s_health, wnt.m_health) / wnt.m_health);
            item.m_durability = Mathf.Max(1f, fraction * item.GetMaxDurability());

            nview.ClaimOwnership();
            piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation, null, 1f, -1, player.GetZDOID());
            ZNetScene.instance.Destroy(piece.gameObject);

            Inventory inv = player.GetInventory();
            if (inv.CanAddItem(item) && inv.AddItem(item))
                player.Message(MessageHud.MessageType.Center, $"{Prefabs.DisplayName} folded.");
            else
            {
                ItemDrop.DropItem(item, 1, player.transform.position + player.transform.forward + Vector3.up, Quaternion.identity);
                player.Message(MessageHud.MessageType.Center, $"{Prefabs.DisplayName} folded; your inventory is full, it lies at your feet.");
            }
            Plugin.Log.LogInfo($"Folded {Prefabs.DisplayName} (level {item.m_quality}, {item.m_durability:0}% health).");
            return true;
        }
    }
}
