using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FishNet;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace BetterSkinRolls
{
    internal static class Patches
    {
        internal static readonly FieldInfo MachineInstance = AccessTools.Field(typeof(SlotMachine), "_instance");
        private static readonly MethodInfo UpdatePrizesText = AccessTools.Method(typeof(SlotMachine), "UpdatePrizesText");

        /// <summary>Re-runs the game's prize list (and our postfix on it), e.g. after an unlock.</summary>
        public static void RefreshPrizes()
        {
            try
            {
                var machine = MachineInstance?.GetValue(null) as SlotMachine;
                if (machine && UpdatePrizesText != null)
                    UpdatePrizesText.Invoke(machine, null);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not refresh the prize list: {e.Message}");
            }
        }

        internal static bool IsLocal(Player player) =>
            player && player.Owner != null && player.Owner.IsLocalClient;
    }

    /// <summary>
    /// Host side (approaches B/C). <see cref="SlotMachineManager.RollRandom"/> only runs on the server and builds the
    /// wheel from <see cref="SlotMachine.AvailableItems"/> (+ the boat unless <see cref="SlotMachine.ExcludeBoat"/>).
    /// For the duration of the call, hand it the pool minus the roller's completed collections and let the vanilla
    /// code do the rest: same odds, same cheat path, and its own SendRoll shows the result to every client, modded
    /// or not. Restored in the finalizer even if the roll throws.
    /// </summary>
    [HarmonyPatch(typeof(SlotMachineManager), nameof(SlotMachineManager.RollRandom))]
    internal static class RollRandomPatch
    {
        private static readonly MethodInfo SetAvailableItems = AccessTools.PropertySetter(typeof(SlotMachine), nameof(SlotMachine.AvailableItems));
        private static readonly MethodInfo SetExcludeBoat = AccessTools.PropertySetter(typeof(SlotMachine), nameof(SlotMachine.ExcludeBoat));

        internal struct Saved
        {
            public bool Active;
            public Item[] Items;
            public bool ExcludeBoat;
        }

        private static HashSet<byte> CompletedFor(Player roller)
        {
            if (Patches.IsLocal(roller))
                return Plugin.Enabled.Value ? SkinProgress.LocalCompleted() : null;
            return LobbySync.GetMemberCompleted(roller.SteamID); // null when that player is vanilla or turned it off
        }

        private static void Prefix(Player roller, out Saved __state)
        {
            __state = default;
            try
            {
                if (!roller || SetAvailableItems == null || SetExcludeBoat == null)
                    return;
                var items = SlotMachine.AvailableItems;
                if (items == null)
                    return;
                var done = CompletedFor(roller);
                if (Plugin.LogRolls.Value)
                    Plugin.Log.LogInfo($"[host] {roller.SteamName} ({(Patches.IsLocal(roller) ? "you" : "other player")}) " +
                                       $"rolls; completed weapons: " +
                                       (done == null ? "unknown (no mod, or Skip completed weapons off)" : WheelLog.Names(done)));
                if (done == null || done.Count == 0)
                    return;

                var keep = new List<Item>();
                foreach (var item in items)
                    if (item && !done.Contains(item.ID))
                        keep.Add(item);
                bool excludeBoat = SlotMachine.ExcludeBoat || done.Contains(SkinProgress.BoatId);

                if (keep.Count == items.Length && excludeBoat == SlotMachine.ExcludeBoat)
                    return; // nothing completed in this pool
                if (keep.Count == 0 && excludeBoat)
                    return; // everything completed: nothing left to win, keep the vanilla roll

                __state = new Saved { Active = true, Items = items, ExcludeBoat = SlotMachine.ExcludeBoat };
                SetAvailableItems.Invoke(null, new object[] { keep.ToArray() });
                SetExcludeBoat.Invoke(null, new object[] { excludeBoat });

                if (Plugin.LogRolls.Value)
                    Plugin.Log.LogInfo($"[host] filtered the wheel for {roller.SteamName}: skipping {WheelLog.Names(done)}, " +
                                       $"{keep.Count} item(s) left, boat {(excludeBoat ? "excluded" : "kept")}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Could not filter the roll: {e}");
            }
        }

        private static Exception Finalizer(Exception __exception, Saved __state)
        {
            if (__state.Active)
            {
                try
                {
                    SetAvailableItems.Invoke(null, new object[] { __state.Items });
                    SetExcludeBoat.Invoke(null, new object[] { __state.ExcludeBoat });
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Could not restore the prize pool: {e}");
                }
            }
            return __exception;
        }
    }

    /// <summary>
    /// Vanilla host (approach A): <see cref="SlotMachine.Roll"/> runs on every client with the
    /// host's arrays, before any <c>MachineSlot.SetIndex</c>. When you are the roller, swap each slot of a completed
    /// collection for a random incomplete one from the pool, keeping the slot's rarity. The arrays are edited in place
    /// so every other patch (SlotMachineSkinNames' SetIndex postfix, the OnRollFinished unlock) sees the new prize.
    /// </summary>
    [HarmonyPatch(typeof(SlotMachine), nameof(SlotMachine.Roll))]
    internal static class RollPatch
    {
        private static void Prefix(Player roller, byte[] itemIDs, byte[] itemSkins)
        {
            try
            {
                if (!Patches.IsLocal(roller) || InstanceFinder.IsServerStarted)
                    return; // the host already filtered its own rolls in RollRandom
                if (!Plugin.Enabled.Value || itemIDs == null || itemSkins == null)
                    return;
                if (LobbySync.GetHostState() != LobbySync.HostState.Vanilla)
                    return; // a modded host already filtered this roll for us

                var done = SkinProgress.LocalCompleted();
                if (done == null || done.Count == 0)
                    return;

                var pool = new List<byte>();
                if (SlotMachine.AvailableItems != null)
                    foreach (var item in SlotMachine.AvailableItems)
                        if (item && !done.Contains(item.ID))
                            pool.Add(item.ID);
                if (!SlotMachine.ExcludeBoat && !done.Contains(SkinProgress.BoatId))
                    pool.Add(SkinProgress.BoatId);
                if (pool.Count == 0)
                    return;

                int changed = 0;
                int n = Math.Min(itemIDs.Length, itemSkins.Length);
                for (int i = 0; i < n; i++)
                {
                    if (!done.Contains(itemIDs[i]))
                        continue;
                    var from = SkinProgress.PresetFor(itemIDs[i]);
                    if (!from || itemSkins[i] >= from.Skins.Count)
                        continue;
                    var rarity = from.Skins[itemSkins[i]].Rarity;
                    if (rarity == Rarity.Default)
                        continue;
                    byte id = pool[UnityEngine.Random.Range(0, pool.Count)];
                    var to = SkinProgress.PresetFor(id);
                    if (!to)
                        continue;
                    itemIDs[i] = id;
                    itemSkins[i] = to.GetRandomSkinIndex(rarity);
                    changed++;
                }
                if (changed > 0 && Plugin.LogRolls.Value)
                    Plugin.Log.LogInfo($"Vanilla host, changed locally: {changed} slot(s) of completed collections " +
                                       $"{SkinProgress.Encode(done)}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Could not change the roll locally: {e}");
            }
        }

        /// <summary>
        /// Debug > Log rolls: every client logs each wheel it shows (after any local change), so one player's log is
        /// enough to check a multiplayer test: who rolled, whether the host has the mod, the 10 slots, the prize and
        /// whether any of your completed weapons is on the wheel.
        /// </summary>
        private static void Postfix(Player roller, byte[] itemIDs, byte[] itemSkins, byte rolled)
        {
            if (!Plugin.LogRolls.Value)
                return;
            try
            {
                bool host = InstanceFinder.IsServerStarted;
                string hostMod = host ? "you are the host" : $"host has the mod: {(LobbySync.GetHostState() == LobbySync.HostState.Modded ? "yes" : "no")}";
                var done = SkinProgress.LocalCompleted();
                var slots = new List<string>();
                var yoursOnWheel = new HashSet<byte>();
                int n = Math.Min(itemIDs.Length, itemSkins.Length);
                for (int i = 0; i < n; i++)
                {
                    slots.Add(WheelLog.Slot(itemIDs[i], itemSkins[i]));
                    if (done != null && done.Contains(itemIDs[i]))
                        yoursOnWheel.Add(itemIDs[i]);
                }
                Plugin.Log.LogInfo($"[wheel] {roller?.SteamName} rolled ({(Patches.IsLocal(roller) ? "you" : "other player")}, {hostMod}). " +
                                   $"Prize: {(rolled < n ? WheelLog.Slot(itemIDs[rolled], itemSkins[rolled]) : "?")}. " +
                                   $"Your completed weapons on this wheel: {(yoursOnWheel.Count == 0 ? "none" : WheelLog.Names(yoursOnWheel))}. " +
                                   $"Slots: {string.Join(" | ", slots)}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not log the wheel: {e.Message}");
            }
        }
    }

    internal static class WheelLog
    {
        public static string Name(byte id)
        {
            if (id == SkinProgress.BoatId)
                return "Boat";
            var item = GameInfo.IDToItem(id);
            return item ? item.name : id.ToString();
        }

        public static string Names(IEnumerable<byte> ids)
        {
            var list = new List<string>();
            foreach (var id in ids)
                list.Add(Name(id));
            return list.Count == 0 ? "none" : string.Join(", ", list);
        }

        public static string Slot(byte id, byte skin)
        {
            var preset = SkinProgress.PresetFor(id);
            if (!preset || skin >= preset.Skins.Count)
                return $"{Name(id)} #{skin}";
            var s = preset.Skins[skin];
            return $"{Name(id)} {s.Name} ({s.Rarity})";
        }
    }

    /// <summary>The roller's unlock happens inside OnRollFinished: publish the new progress and refresh the prize list.</summary>
    [HarmonyPatch(typeof(SlotMachine), "OnRollFinished")]
    internal static class OnRollFinishedPatch
    {
        private static readonly FieldInfo PlayerWhoRolled = AccessTools.Field(typeof(SlotMachine), "_playerWhoRolled");

        private static void Postfix()
        {
            try
            {
                if (!Patches.IsLocal(PlayerWhoRolled?.GetValue(null) as Player))
                    return;
                LobbySync.PublishNow();
                Patches.RefreshPrizes();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not update after the roll: {e.Message}");
            }
        }
    }

    /// <summary>Rebuilds the vanilla "Prizes: Boat, Knife, ..." line with your completed collections struck through.</summary>
    [HarmonyPatch(typeof(SlotMachine), "UpdatePrizesText")]
    internal static class UpdatePrizesTextPatch
    {
        private static readonly AccessTools.FieldRef<SlotMachine, TextMeshProUGUI> PrizesText =
            AccessTools.FieldRefAccess<SlotMachine, TextMeshProUGUI>("_prizesText");
        private static readonly AccessTools.FieldRef<SlotMachine, Item[]> Items =
            AccessTools.FieldRefAccess<SlotMachine, Item[]>("_availableItems");
        private static readonly AccessTools.FieldRef<SlotMachine, bool> ExcludeBoat =
            AccessTools.FieldRefAccess<SlotMachine, bool>("_excludeBoat");

        private static void Postfix(SlotMachine __instance)
        {
            try
            {
                if (!Plugin.Enabled.Value || !Plugin.StrikeCompletedPrizes.Value)
                    return;
                var done = SkinProgress.LocalCompleted();
                if (done == null || done.Count == 0)
                    return;
                var text = PrizesText(__instance);
                var items = Items(__instance);
                if (!text || items == null)
                    return;

                var names = new List<string>();
                if (!ExcludeBoat(__instance))
                    names.Add(Mark(LocalizationManager.BoatLocalized.GetLocalizedString(), done.Contains(SkinProgress.BoatId)));
                foreach (var item in items)
                    if (item)
                        names.Add(Mark(item.GetName(), done.Contains(item.ID)));

                var sb = new StringBuilder("<color=#FFEF4D><b>");
                sb.Append(LocalizationManager.PrizesLocalized.GetLocalizedString()).Append(":</color></b> ");
                sb.Append(string.Join(", ", names));
                text.text = sb.ToString();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not mark the completed prizes: {e.Message}");
            }
        }

        private static string Mark(string name, bool completed) =>
            completed ? "<s><alpha=#80>" + name + "<alpha=#FF></s>" : name;
    }
}
