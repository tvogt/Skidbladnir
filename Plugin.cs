using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace Skidbladnir
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "Lemuria.Skidbladnir";
        public const string Name = "Skidbladnir";
        public const string Version = "0.1.1";

        internal static ManualLogSource Log;

        // The item prefab must exist on every client, so the mod is required and versions must match.
        internal static readonly ServerSync.ConfigSync ConfigSync = new ServerSync.ConfigSync(Guid)
        {
            DisplayName = Name,
            CurrentVersion = Version,
            MinimumRequiredVersion = Version,
            ModRequired = true,
        };

        internal static ConfigEntry<bool> LockConfig;
        internal static ConfigEntry<string> Station;
        internal static ConfigEntry<int> StationLevel;
        internal static ConfigEntry<int> UpgradeStationLevel;
        internal static ConfigEntry<bool> LongshipCost;
        internal static ConfigEntry<string> ExtraCost;
        internal static ConfigEntry<string> UpgradeCost;
        internal static ConfigEntry<float> Weight;
        internal static ConfigEntry<int> TailwindLevel;

        private void Awake()
        {
            Log = Logger;

            LockConfig = Bind("0 - General", "LockConfiguration", true,
                "If on, only server admins can change the synced settings on a server. In single player this has no effect.");
            ConfigSync.AddLockingConfigEntry(LockConfig);

            Station = Bind("1 - Recipe", "CraftingStation", "forge",
                "Prefab name of the crafting station where Skíðblaðnir is crafted, upgraded and repaired (forge, piece_workbench, blackforge, piece_magetable, ...).");
            StationLevel = Bind("1 - Recipe", "MinStationLevel", 3,
                new ConfigDescription("Station level needed to craft and repair it. Forge: 3 = the finished bronze-age forge (Forge Cooler + Anvils).", new AcceptableValueRange<int>(1, 10)));
            UpgradeStationLevel = Bind("1 - Recipe", "UpgradeStationLevel", 5,
                new ConfigDescription("Station level needed for the upgrade to level 2 (fair wind). Forge: 5 = four extensions, iron age. Never lower than MinStationLevel.", new AcceptableValueRange<int>(1, 10)));
            LongshipCost = Bind("1 - Recipe", "LongshipMaterials", true,
                "Include the materials of a longship (taken from the game's own longship piece, so mods that change it are followed).");
            ExtraCost = Bind("1 - Recipe", "ExtraMaterials", "TrophySerpent:2",
                "Further materials for crafting, as comma-separated PrefabName:Amount.");
            UpgradeCost = Bind("1 - Recipe", "UpgradeMaterials", "TrophyDragonQueen:1",
                "Materials for the upgrade to level 2 (fair wind), as comma-separated PrefabName:Amount. Only needed for the upgrade, not for crafting.");
            Weight = Bind("2 - Item", "Weight", 2f,
                new ConfigDescription("Weight of the folded ship. The myth says it fits in a pouch.", new AcceptableValueRange<float>(0f, 300f)));
            TailwindLevel = Bind("3 - Fair wind", "TailwindLevel", 2,
                new ConfigDescription("Item level from which the ship always has a fair wind while someone is aboard (like Moder's power, for this ship only). 3 = never (the item only goes up to level 2).", new AcceptableValueRange<int>(1, 3)));

            foreach (ConfigEntryBase e in new ConfigEntryBase[] { Station, StationLevel, LongshipCost, ExtraCost, UpgradeCost, Weight })
            {
                Config.SettingChanged += (_, args) => { if (args.ChangedSetting == e) Prefabs.UpdateRecipeAndItem(); };
            }

            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo($"{Name} {Version} loaded.");
        }

        /// <summary>Config.Bind plus ServerSync registration.</summary>
        private ConfigEntry<T> Bind<T>(string section, string key, T value, ConfigDescription description, bool synced = true)
        {
            ConfigEntry<T> entry = Config.Bind(section, key, value, description);
            ConfigSync.AddConfigEntry(entry).SynchronizedConfig = synced;
            return entry;
        }

        private ConfigEntry<T> Bind<T>(string section, string key, T value, string description, bool synced = true)
            => Bind(section, key, value, new ConfigDescription(description), synced);

        /// <summary>Parses "Name:Amount, Name:Amount" (amount defaults to 1).</summary>
        internal static List<KeyValuePair<string, int>> ParseCost(string text)
        {
            var list = new List<KeyValuePair<string, int>>();
            if (string.IsNullOrEmpty(text)) return list;
            foreach (string raw in text.Split(','))
            {
                string s = raw.Trim();
                if (s.Length == 0) continue;
                int colon = s.IndexOf(':');
                string name = colon < 0 ? s : s.Substring(0, colon).Trim();
                int amount = 1;
                if (colon >= 0 && !int.TryParse(s.Substring(colon + 1).Trim(), out amount))
                {
                    Log.LogWarning($"Ignoring '{s}': amount is not a number.");
                    continue;
                }
                if (name.Length > 0 && amount > 0) list.Add(new KeyValuePair<string, int>(name, amount));
            }
            return list;
        }
    }
}
