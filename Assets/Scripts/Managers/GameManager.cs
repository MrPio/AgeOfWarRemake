using System;
using System.Collections.Generic;
using Managers.Serializer;
using Model.Bases;
using Model.Utils;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.VisualScripting;
using UnityEngine;
using Base = Prefabs.Base;
using LogType = UI.LogType;
using Unit = Prefabs.Unit;

namespace Managers
{
    public class GameManager : NetworkBehaviour
    {
        private SceneManager _sm;
        private static GameManager _instance;
        private readonly ISerializer _serializer = BinarySerializer.Instance;
        [NonSerialized] public bool PlayAsHost;
        [NonSerialized] public ulong HostId, ClientId;
        [NonSerialized] public readonly List<Unit> UnitsAlly = new(), UnitsEnemy = new();
        [NonSerialized] public Base BaseAlly = null, BaseEnemy = null;
        [NonSerialized] public ulong? Winner;
        [NonSerialized] public bool IsGameOver;
        private float _gameStart, _lastMoneyPerSecond;
        public readonly List<Action<Unit>> OnAllySpawn = new();
        public readonly List<Action<Unit>> OnEnemySpawn = new();

        #region NetVars

        private readonly NetworkVariable<bool> _gameStarted = new();
        public readonly NetworkVariable<NetString> Username = new(writePerm: NetworkVariableWritePermission.Owner);

        #region Listeners

        // Host & Client
        private void OnGameStartedChanged(bool previousValue, bool newValue)
        {
            // Store IDs of the 2 players
            foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if ((PlayAsHost && id == NetworkManager.Singleton.LocalClientId) ||
                    (!PlayAsHost && id != NetworkManager.Singleton.LocalClientId))
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

            if (!DataManager.IsMultiplayer)
                ClientId = HostId; // the bot is the same machine as the host

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

        #endregion

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
            // Initialize logger
            _sm.logger.LOGFileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") +
                                     $" {(DataManager.IsMultiplayer ? DataManager.IsHost ? "Host" : "Client" : "Singleplayer")}";
            _sm.logger.Log($"Starting a {(DataManager.IsMultiplayer ? "Multiplayer" : "Singleplayer")} game");

            // Login Unity services 
            await UnityServices.InitializeAsync();
            if (AuthenticationService.Instance.IsSignedIn)
                AuthenticationService.Instance.SignOut();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            // Start Host or Client depending on isMultiplayer bool
            if (DataManager.IsMultiplayer)
            {
                _sm.logger.Log($"Starting as {(DataManager.IsHost ? "Host" : "Client")}");
                /*PlayAsHost = _serializer.Deserialize(ISerializer.DebugDir, "NeedHost", true);
                _serializer.Serialize(!PlayAsHost, $"{ISerializer.DebugDir}", "NeedHost");

                if (PlayAsHost)
                    NetworkManager.Singleton.StartHost();
                else
                    NetworkManager.Singleton.StartClient();*/
                if (DataManager.IsHost)
                {
                    DataManager.LobbyCode = await _sm.RelayManager.CreateRelay();
                    _sm.loadingMenu.Initialize(DataManager.IsMultiplayer, DataManager.IsHost, DataManager.LobbyCode);
                }
                else
                    await _sm.RelayManager.JoinRelay(DataManager.LobbyCode);
            }
            else
            {
                // Change unity transport from Relay to default
                NetworkManager.Singleton.NetworkConfig.NetworkTransport =
                    NetworkManager.Singleton.AddComponent<UnityTransport>();

                // There's no client on singleplayer
                NetworkManager.Singleton.StartHost();
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) // Host runs this
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }

            _gameStarted.OnValueChanged += OnGameStartedChanged;
            Username.Value = DataManager.Username;
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

        private void Update()
        {
            // Fullscreen management
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (Input.GetKeyDown(KeyCode.Escape))
                Screen.fullScreen = false;

            var f11Pressed = Input.GetKeyDown(KeyCode.F11);
            var altEnterPressed = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) &&
                                  Input.GetKeyDown(KeyCode.Return);
            if (f11Pressed || altEnterPressed)
                Screen.fullScreen = true;
#endif


            if (!_gameStarted.Value) return;

            // Add money per second if multiplayer (Server-only)
            if (IsServer && BaseAlly is not null && BaseEnemy is not null && DataManager.IsMultiplayer)
                if (Time.time - _lastMoneyPerSecond > 1)
                {
                    _lastMoneyPerSecond = Time.time;
                    foreach (var basePrefab in new List<Base> { BaseAlly, BaseEnemy })
                    {
                        // Add money based on the age
                        var newModel = basePrefab.Model.Value;
                        newModel.Money += BaseFactory.MoneyPerSecond[newModel.Age];
                        basePrefab.Model.Value = newModel;
                    }
                }
        }

        #endregion

        // Host-only
        private void OnClientConnected(ulong clientId)
        {
            _sm.logger.Log($"Player {clientId} connected.", LogType.HostClientConnection);
            TryStartGame();
        }

        // Host-only
        private void OnClientDisconnected(ulong clientId)
        {
            _sm.logger.Log($"Player {clientId} disconnected.", LogType.HostClientConnection);
            if (_gameStarted.Value)
                EndGame();
        }

        // Host-only
        private void TryStartGame()
        {
            if (NetworkManager.Singleton.ConnectedClients.Count ==
                (DataManager.IsMultiplayer ? 2 : 1)) // This includes the host
            {
                _sm.logger.Log("Both players connected. Starting game.", LogType.HostClientConnection);
                _gameStarted.Value = true;
                _gameStart = Time.time;
                _lastMoneyPerSecond = Time.time + 5;
            }
            else
                _sm.logger.Log($"Waiting for the {(IsServer ? "Client" : "Host")}...", LogType.WaitingFor);
        }

        // Host-only
        private void EndGame()
        {
            _sm.logger.Log("Ending game...", LogType.HostClientConnection);
            _gameStarted.Value = false;

            // Trigger a UI message, return to menu, etc.
        }
    }
}