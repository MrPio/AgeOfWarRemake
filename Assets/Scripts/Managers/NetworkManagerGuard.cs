using Unity.Netcode;
using UnityEngine;

namespace Managers
{
    public class NetworkManagerGuard : MonoBehaviour
    {
        private void Awake()
        {
            var s = NetworkManager.Singleton;

            if (s != null && s.gameObject != gameObject)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }
    }
}