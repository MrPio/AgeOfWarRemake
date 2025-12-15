using Managers.Singletons;

namespace Managers.Statics
{
    public enum GameMode
    {
        Singleplayer,
        Multiplayer
    }
    public static class DataManager
    {
        public static GameMode GameMode { get; set; }
        public static bool IsSingleplayer => GameMode is GameMode.Singleplayer;
        public static bool IsMultiplayer => GameMode is GameMode.Multiplayer;
        public static bool IsHost { get; set; }

        public static string Username
        {
            get => SettingsManager.Instance.Get<string>(SettingType.Username);
            set
            {
                if (value.Length >= 1)
                    SettingsManager.Instance.Set(SettingType.Username, value);
            }
        }

        public static string LobbyCode { get; set; }
    }
}