using System;
using System.Collections.Generic;
using Managers.Serializer;
using Prefabs;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using LogType = UI.LogType;

namespace Managers
{
    public class GameManager : NetworkBehaviour
    {
        private SceneManager _sm;
        private static GameManager _instance;
        private readonly ISerializer _serializer = BinarySerializer.Instance;
        private bool _isHost;
        [NonSerialized] public ulong HostId, ClientId;
        [NonSerialized] public readonly List<Unit> UnitsAlly = new(), UnitsEnemy = new();
        [NonSerialized] public Base BaseAlly, BaseEnemy;
        [NonSerialized] public ulong? Winner;
        [NonSerialized] public int Moneys = 175;
        [NonSerialized] public bool IsGameOver;

        #region NetVars

        private readonly NetworkVariable<bool> _gameStarted = new();

        #endregion

        #region Events

        private void Awake()
        {
            if (_instance is not null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            _sm = FindFirstObjectByType<SceneManager>();
        }

        private async void Start()
        {
            await UnityServices.InitializeAsync();
            if (AuthenticationService.Instance.IsSignedIn)
                AuthenticationService.Instance.SignOut();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            // Start Host or Client depending on isMultiplayer bool
            if (_sm.isMultiplayer)
            {
                _isHost = _serializer.Deserialize($"{ISerializer.DebugDir}/NeedHost", true);
                _serializer.Serialize(!_isHost, $"{ISerializer.DebugDir}", "NeedHost");
                _sm.logger.Log($"Starting as {(_isHost ? "Host" : "Client")}");

                if (_isHost)
                    NetworkManager.Singleton.StartHost();
                else
                    NetworkManager.Singleton.StartClient();
            }
            else
                NetworkManager.Singleton.StartHost();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) // Host runs this
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }

            _gameStarted.OnValueChanged += OnGameStartedChanged;
        }

        public override void OnDestroy()
        {
            if (IsServer)
            {
                _serializer.Serialize(true, $"{ISerializer.DebugDir}", "NeedHost");
                if (NetworkManager.Singleton)
                {
                    NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                    NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                }
            }

            if (_gameStarted != null)
                _gameStarted.OnValueChanged -= OnGameStartedChanged;
        }

        #endregion

        // Host only
        private void OnClientConnected(ulong clientId)
        {
            _sm.logger.Log($"Player {clientId} connected.", LogType.HostClientConnection);
            TryStartGame();
        }

        // Host only
        private void OnClientDisconnected(ulong clientId)
        {
            _sm.logger.Log($"Player {clientId} disconnected.", LogType.HostClientConnection);
            if (_gameStarted.Value)
                EndGame();
        }

        // Host only
        private void TryStartGame()
        {
            if (NetworkManager.Singleton.ConnectedClients.Count ==
                (_sm.isMultiplayer ? 2 : 1)) // This includes the host
            {
                _sm.logger.Log("Both players connected. Starting game.", LogType.HostClientConnection);
                _gameStarted.Value = true;
            }
            else
            {
                _sm.logger.Log($"Waiting for the {(IsServer ? "Client" : "Host")}...", LogType.WaitingFor);
            }
        }

        // Host only
        private void EndGame()
        {
            _sm.logger.Log("Ending game...", LogType.HostClientConnection);
            _gameStarted.Value = false;

            // Trigger a UI message, return to menu, etc.
        }

        // Host & Client
        private void OnGameStartedChanged(bool previousValue, bool newValue)
        {
            // Store IDs of the 2 players
            foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if ((_isHost && id == NetworkManager.Singleton.LocalClientId) ||
                    (!_isHost && id != NetworkManager.Singleton.LocalClientId))
                {
                    HostId = id;
                    _sm.logger.Log($"{id} is the Host!", LogType.ReadingStatus);
                }
                else
                {
                    ClientId = id;
                    _sm.logger.Log($"{id} is the Client!", LogType.ReadingStatus);
                }
            }

            if (!_sm.isMultiplayer)
                ClientId = HostId;

            if (newValue)
            {
                _sm.logger.Log("Game Started!", LogType.HostClientConnection);
                _sm.StartGame();
            }
            else
            {
                _sm.logger.Log("Game Ended!", LogType.HostClientConnection);
                // Show end screen
            }
        }
    }
}