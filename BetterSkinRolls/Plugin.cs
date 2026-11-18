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
        internal static ConfigEntry<bool> StrikeCompletedPrizes;
        internal static ConfigEntry<bool> LogRolls;

        private float _nextSync;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("General", "Enabled", true,
                "Skip completed weapons: when you roll the slot machine, weapons whose obtainable skins you've all " +
                "unlocked are left out of the wheel. Other weapons can still give repeats. If the host also has the " +
                "mod, everyone in the lobby sees the same wheel; if not, only your screen shows the changed wheel and " +
                "the others see the original roll. Also in Options > Skin Rolls.");
            StrikeCompletedPrizes = Config.Bind("General", "Strike completed prizes", true,
                "Cross out completed weapons in the prize list: the slot machine's \"Prizes:\" list crosses out the " +
                "weapons you've completed. Only you see the marks. Also in Options > Skin Rolls.");
            LogRolls = Config.Bind("Debug", "Log rolls", false, "Log every roll this mod changes.");

            Enabled.SettingChanged += (_, __) => { LobbySync.PublishNow(); Patches.RefreshPrizes(); GameMenus.Refresh(); };
            StrikeCompletedPrizes.SettingChanged += (_, __) => { Patches.RefreshPrizes(); GameMenus.Refresh(); };

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
