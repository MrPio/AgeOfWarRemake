namespace Managers
{
    public static class DataManager
    {
        public static bool IsMultiplayer { get; set; }
        public static bool IsHost { get; set; }
        public static string Username { get; set; } = "Player";
        public static string LobbyCode { get; set; }
    }
}