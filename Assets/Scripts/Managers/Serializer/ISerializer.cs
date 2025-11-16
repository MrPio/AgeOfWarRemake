namespace Managers.Serializer
{
    public interface ISerializer
    {
        public const string SettingsDir = "settings/";
        public const string DebugDir = "debug/";
        public const string LogsDir = "logs/";

        public void Serialize(object obj, string dir, string filename);

        public T Deserialize<T>(string dir, string filename, T ifNotExist);
    }
}