using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BetterSkinRolls
{
    /// <summary>Which skin collections (item id, 255 = boat) the local save has completed.</summary>
    internal static class SkinProgress
    {
        public const byte BoatId = byte.MaxValue;

        private static readonly FieldInfo CurLocalSave = AccessTools.Field(typeof(SaveManager), "_curLocalSave");
        private static SkinPreset _boatPreset;

        public static SkinPreset PresetFor(byte id)
        {
            if (id != BoatId)
            {
                var item = GameInfo.IDToItem(id);
                return item ? item.SkinPreset : null;
            }
            if (BoatManager.Boat)
                return BoatManager.Boat.SkinPreset;
            if (!_boatPreset)
            {
                // Before a boat spawns (main menu, loading): any loaded Boat has the same preset.
                foreach (var boat in Resources.FindObjectsOfTypeAll<Boat>())
                {
                    if (boat && boat.SkinPreset)
                    {
                        _boatPreset = boat.SkinPreset;
                        break;
                    }
                }
            }
            return _boatPreset;
        }

        /// <summary>
        /// A collection is complete when every skin that can drop (any rarity but Default) is unlocked. Unobtainable
        /// skins (Rarity.Default at index != 0) don't count. Null when there's no save loaded.
        /// </summary>
        public static HashSet<byte> LocalCompleted()
        {
            var save = CurLocalSave?.GetValue(null) as SaveManager.LocalSaveObject;
            if (save == null)
                return null;

            var done = new HashSet<byte>();
            var items = GameInfo.ItemWithSkinsforCommands;
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (!item)
                        continue;
                    List<byte> unlocked = null;
                    if (save.UnlockedItemSkins != null)
                        foreach (var s in save.UnlockedItemSkins)
                            if (s != null && s.ID == item.ID) { unlocked = s.SkinsUnlocked; break; }
                    if (IsComplete(item.SkinPreset, unlocked))
                        done.Add(item.ID);
                }
            }
            if (IsComplete(PresetFor(BoatId), save.UnlockedBoatSkins))
                done.Add(BoatId);
            return done;
        }

        private static bool IsComplete(SkinPreset preset, List<byte> unlocked)
        {
            if (!preset || unlocked == null)
                return false;
            var skins = preset.Skins;
            int obtainable = 0;
            for (int i = 0; i < skins.Count; i++)
            {
                if (skins[i].Rarity == Rarity.Default)
                    continue;
                obtainable++;
                if (!unlocked.Contains((byte)i))
                    return false;
            }
            return obtainable > 0;
        }

        public static string Encode(HashSet<byte> ids)
        {
            var list = new List<byte>(ids);
            list.Sort();
            return string.Join(",", list);
        }

        public static HashSet<byte> Decode(string csv)
        {
            var set = new HashSet<byte>();
            foreach (var part in csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                if (byte.TryParse(part, out var id))
                    set.Add(id);
            return set;
        }
    }
}
