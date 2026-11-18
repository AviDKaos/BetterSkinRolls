using System;
using System.Collections.Generic;
using FishNet;
using Steamworks;

namespace BetterSkinRolls
{
    /// <summary>
    /// Shares state through Steam lobby data instead of FishNet messages, so vanilla players never receive anything
    /// they can't parse:
    /// - member data <see cref="Key"/> on every modded player: "on:54,57,255" (completed collections) or "off";
    /// - lobby data <see cref="Key"/>, set by a modded host (the lobby owner): "on". It means the host filters the
    ///   rolls of every modded player (each one's own Enabled decides); missing means a vanilla host, and modded
    ///   players then fall back to changing their own rolls locally.
    /// </summary>
    internal static class LobbySync
    {
        public const string Key = "BetterSkinRolls";

        public enum HostState { Vanilla, Modded }

        private static CSteamID _lastLobby = CSteamID.Nil;
        private static string _lastMember, _lastHost;

        public static void PublishNow() => Publish(true);

        public static void Publish(bool force)
        {
            try
            {
                var lobby = SteamManager.CurrentLobbyID;
                if (lobby == CSteamID.Nil)
                {
                    _lastLobby = CSteamID.Nil;
                    return;
                }
                if (lobby != _lastLobby)
                {
                    _lastLobby = lobby;
                    _lastMember = _lastHost = null;
                }

                string member = "off";
                if (Plugin.Enabled.Value)
                {
                    var done = SkinProgress.LocalCompleted();
                    member = done == null ? null : "on:" + SkinProgress.Encode(done);
                }
                if (member != null && (force || member != _lastMember))
                {
                    SteamMatchmaking.SetLobbyMemberData(lobby, Key, member);
                    _lastMember = member;
                }

                if (InstanceFinder.IsServerStarted)
                {
                    const string host = "on";
                    // Only the owner can set lobby data; the call just fails for anyone else.
                    if ((force || host != _lastHost) && SteamMatchmaking.SetLobbyData(lobby, Key, host))
                        _lastHost = host;
                }
            }
            catch (Exception e)
            {
                if (force)
                    Plugin.Log.LogWarning($"Could not update the lobby data: {e.Message}");
            }
        }

        public static HostState GetHostState()
        {
            try
            {
                var lobby = SteamManager.CurrentLobbyID;
                if (lobby == CSteamID.Nil)
                    return HostState.Vanilla;
                switch (SteamMatchmaking.GetLobbyData(lobby, Key))
                {
                    case "": case null: return HostState.Vanilla;
                    default: return HostState.Modded;
                }
            }
            catch
            {
                return HostState.Vanilla;
            }
        }

        /// <summary>Host side: the completed collections another player published, or null (vanilla, off, unknown).</summary>
        public static HashSet<byte> GetMemberCompleted(ulong steamId)
        {
            try
            {
                var lobby = SteamManager.CurrentLobbyID;
                if (lobby == CSteamID.Nil || steamId == 0)
                    return null;
                var value = SteamMatchmaking.GetLobbyMemberData(lobby, new CSteamID(steamId), Key);
                if (string.IsNullOrEmpty(value) || !value.StartsWith("on:"))
                    return null;
                return SkinProgress.Decode(value.Substring(3));
            }
            catch
            {
                return null;
            }
        }
    }
}
