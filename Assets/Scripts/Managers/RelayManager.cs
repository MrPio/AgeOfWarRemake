using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Managers
{
    public class RelayManager : MonoBehaviour
    {
        [SerializeField] private string connectionType = "wss"; // udp, dtls, wss

        // Host-only
        /// <summary>
        /// Creates a relay and starts host connection.
        /// </summary>
        /// <returns>The relay code</returns>
        public async Task<string> CreateRelay(int maxConnections = 2)
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(allocation.ToRelayServerData(connectionType));
            if (connectionType == "wss")
                NetworkManager.Singleton.GetComponent<UnityTransport>().UseWebSockets = true;

            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return NetworkManager.Singleton.StartHost() ? joinCode : null;
        }

        // Client-only
        /// <summary>
        /// Join a given relay code and starts client connection.
        /// </summary>
        /// <param name="joinCode"></param>
        /// <returns></returns>
        public async Task<bool> JoinRelay(string joinCode)
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(allocation.ToRelayServerData(connectionType));
            if (connectionType == "wss")
                NetworkManager.Singleton.GetComponent<UnityTransport>().UseWebSockets = true;

            return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
        }
    }
}