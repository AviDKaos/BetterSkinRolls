using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BetterSkinRolls
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "avidkaos.howtofish.betterskinrolls";
        public const string PluginName = "BetterSkinRolls";
        // Generated from <Version> in BetterSkinRolls.csproj.
        public const string PluginVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> LocalFallback;
        internal static ConfigEntry<bool> StrikeCompletedPrizes;
        internal static ConfigEntry<bool> LobbyFilter;
        internal static ConfigEntry<bool> LogRolls;

        private float _nextSync;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("General", "Enabled", true,
                "Weapons (and the boat) whose obtainable skins you all own stop showing up on the slot machine when you " +
                "roll. Needs the host to have the mod for everyone to see the same wheel (see Local fallback).");
            LocalFallback = Config.Bind("General", "Local fallback", true,
                "When the host doesn't have the mod, change your own rolls on your side only: you still get a skin of a " +
                "weapon you haven't completed, but the other players see the original wheel and prize.");
            StrikeCompletedPrizes = Config.Bind("General", "Strike completed prizes", true,
                "Strike through your completed weapons in the machine's \"Prizes:\" list (only on your screen).");
            LobbyFilter = Config.Bind("Host", "Lobby filter", true,
                "When you host: skip the completed weapons of every player who has the mod enabled, so the whole lobby " +
                "sees the same wheel. Off: other players' rolls are vanilla and their local fallback is off too. Your " +
                "own rolls follow General > Enabled.");
            LogRolls = Config.Bind("Debug", "Log rolls", false, "Log every roll this mod changes.");

            Enabled.SettingChanged += (_, __) => { LobbySync.PublishNow(); Patches.RefreshPrizes(); };
            LobbyFilter.SettingChanged += (_, __) => LobbySync.PublishNow();
            StrikeCompletedPrizes.SettingChanged += (_, __) => Patches.RefreshPrizes();

            new Harmony(PluginGuid).PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void Update()
        {
            // Lobby data is cheap to compare but there's no need to check it every frame.
            if (Time.unscaledTime < _nextSync)
                return;
            _nextSync = Time.unscaledTime + 1f;
            LobbySync.Publish(false);
        }
    }
}
