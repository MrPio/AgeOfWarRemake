using Interfaces;

namespace Managers.Singletons
{
    /// <summary>
    /// Used to prevent multiple NetworkManagers from spawning in the scene.
    /// </summary>
    public class NetworkManagerGuard : SingletonMonoBehaviour<NetworkManagerGuard>
    {
    }
}