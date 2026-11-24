using HarmonyLib;

namespace Skidbladnir
{
    /// <summary>
    /// "As soon as the sail is raised it has a fair wind, wherever it is bound" (Gylfaginning 43).
    /// From TailwindLevel on, the ship counts as wind-controlled like under Moder's power: the game then turns
    /// the wind to the ship's heading for the player aboard.
    /// </summary>
    [HarmonyPatch(typeof(Ship), nameof(Ship.IsWindControllActive))]
    internal static class Tailwind
    {
        private static readonly AccessTools.FieldRef<Ship, ZNetView> NView = AccessTools.FieldRefAccess<Ship, ZNetView>("m_nview");

        private static void Postfix(Ship __instance, ref bool __result)
        {
            if (__result || !__instance.HasPlayerOnboard()) return;
            int level = Unfold.GetLevel(NView(__instance));
            if (level > 0 && level >= Plugin.TailwindLevel.Value) __result = true;
        }
    }
}
