using Steamworks;

public static class LocalPlayMode
{
    public static bool Enabled => true;
    public static CSteamID UserId => Enabled ? new CSteamID(1UL) : SteamUser.GetSteamID();
    public static string PlayerName => Enabled ? "Local Player" : SteamFriends.GetPersonaName();
    public static string NameFor(ulong id) => Enabled ? "Local Player" : SteamFriends.GetFriendPersonaName(new CSteamID(id));
}
