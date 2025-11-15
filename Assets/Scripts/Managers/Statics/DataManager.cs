using Managers.Singletons;

namespace Managers.Statics
{
    public static class DataManager
    {
        public static bool IsMultiplayer { get; set; }
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