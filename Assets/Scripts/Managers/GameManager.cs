using System;
using System.Collections.Generic;
using Managers.Serializer;
using Managers.Singletons;
using Managers.Statics;
using Model.Bases;
using Model.Utils;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Base = Prefabs.Base;
using LogType = UI.LogType;
using Unit = Prefabs.Unit;

namespace Managers
{
    public class GameManager : NetworkBehaviour
    {
        private SceneManager _sm;
        private ToastManager _tm;
        private static GameManager _instance;
        private readonly ISerializer _serializer = BinarySerializer.Instance;
        [NonSerialized] public ulong HostId, ClientId;
        [NonSerialized] public readonly List<Unit> UnitsAlly = new(), UnitsEnemy = new();
        [NonSerialized] public Base BaseAlly = null, BaseEnemy = null;
        [NonSerialized] public ulong? Winner;
        [NonSerialized] public bool IsGameOver;
        [NonSerialized] public float GameTime;
        private bool _isGamePaused;

        public bool IsGamePaused
        {
            get => _isGamePaused;
            set => _isGamePaused = !DataManager.IsMultiplayer
                ? value
                : throw new InvalidOperationException("Cannot pause in multiplayer mode.");
        }

        private float _lastMoneyPerSecond;
        public readonly List<Action<Unit>> OnAllySpawn = new();
        public readonly List<Action<Unit>> OnEnemySpawn = new();

        #region NetVars

        private readonly NetworkVariable<bool> _gameStarted = new();
        public readonly NetworkVariable<NetString> UsernameHost = new(), UsernameClient = new();

        #region Listeners

        // Host & Client
        private void OnGameStartedChanged(bool previousValue, bool newValue)
        {
            // Store IDs of the 2 players
            foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if ((DataManager.IsHost && id == NetworkManager.Singleton.LocalClientId) ||
                    (!DataManager.IsHost && id != NetworkManager.Singleton.LocalClientId))
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
                ClientId = HostId; // the bot is the same machine as the host TODO: client is not 2, check ownerships in bullets collisions

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

        // Server
        [ServerRpc(RequireOwnership = false)]
        private void SetUsernameServerRpc(NetString username, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId == HostId)
                UsernameHost.Value = username;
            else
                UsernameClient.Value = username;
        }

        #endregion

        #endregion

        #region Events

        private void Awake()
        {
            // if (_instance is not null && _instance != this)
            // {
            //     Destroy(this);
            //     return;
            // }
            //
            // _instance = this;
            _sm = FindFirstObjectByType<SceneManager>();
            _tm = GameObject.FindWithTag("ToastManager").GetComponent<ToastManager>();
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
                    _tm.MakeToast("Creating lobby...", ToastColor.Cyan);
                    DataManager.LobbyCode = await _sm.RelayManager.CreateRelay();
                    _sm.loadingMenu.Initialize(DataManager.IsMultiplayer, DataManager.IsHost, DataManager.LobbyCode);
                }
                else
                    try
                    {
                        await _sm.RelayManager.JoinRelay(DataManager.LobbyCode);
                    }
                    catch
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
                    }
            }
            else
            {
#if UNITY_WEBGL
                // There's no client on singleplayer
                await _sm.RelayManager.CreateRelay();
#else
                // Change unity transport from Relay to default
                var transport = NetworkManager.Singleton.AddComponent<UnityTransport>();
                transport.UseWebSockets = true;
                NetworkManager.Singleton.NetworkConfig.NetworkTransport = transport;
                NetworkManager.Singleton.StartHost();
#endif
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += EndGameHost;
            }
            else if (!IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += EndGameClient;
                if (NetworkManager.Singleton.ConnectedClients.Count !=
                    (DataManager.IsMultiplayer ? 2 : 1))
                    EndGameClient(0);
            }

            _gameStarted.OnValueChanged += OnGameStartedChanged;
            SetUsernameServerRpc(DataManager.Username);
        }

        public override void OnDestroy()
        {
            if (IsServer)
            {
                // _serializer.Serialize(true, $"{ISerializer.DebugDir}", "NeedHost");
                if (NetworkManager.Singleton)
                {
                    NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                    NetworkManager.Singleton.OnClientDisconnectCallback -= EndGameHost;
                }
            }
            else if (!IsServer)
            {
                if (NetworkManager.Singleton)
                    NetworkManager.Singleton.OnClientDisconnectCallback -= EndGameClient;
            }

            if (_gameStarted != null)
                _gameStarted.OnValueChanged -= OnGameStartedChanged;
        }

        private void Update()
        {
            if (!_gameStarted.Value) return;

            // [P] Pause menu (singleplayer-only)
            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
                _sm.pauseMenu.SetActive(!_sm.pauseMenu.activeSelf);

            // [Space] Special attack
            if (Input.GetKeyDown(KeyCode.Space))
                _sm.SpecialAttackManager.RunSpecialServerRpc();

            // Add money per second if multiplayer (Server-only)
            if (IsServer && BaseAlly is not null && BaseEnemy is not null && DataManager.IsMultiplayer)
                if (Time.time - _lastMoneyPerSecond > 1)
                {
                    _lastMoneyPerSecond = Time.time;
                    foreach (var basePrefab in new List<Base> { BaseAlly, BaseEnemy })
                    {
                        // Add money based on the age
                        var newModel = basePrefab.Model.Value;
                        newModel.Money += BaseFactory.MoneyPerSecond[newModel.Age - 1];
                        basePrefab.Model.Value = newModel;
                    }
                }
        }

        private void FixedUpdate()
        {
            if (_gameStarted.Value && !_isGamePaused)
                GameTime += Time.fixedDeltaTime;
        }

        // Host-only
        private void OnClientConnected(ulong clientId)
        {
            _sm.logger.Log($"Player {clientId} connected.", LogType.HostClientConnection);
            TryStartGame();
        }

        #endregion

        // Host-only
        private void TryStartGame()
        {
            if (NetworkManager.Singleton.ConnectedClients.Count ==
                (DataManager.IsMultiplayer ? 2 : 1)) // This includes the host
            {
                _sm.logger.Log("Both players connected. Starting game.", LogType.HostClientConnection);
                _gameStarted.Value = true;
                _lastMoneyPerSecond = Time.time + 5;
            }
            else
                _sm.logger.Log($"Waiting for the {(IsServer ? "Client" : "Host")}...", LogType.WaitingFor);
        }

        // Host-only
        private void EndGameHost(ulong clientId)
        {
            _sm.logger.Log($"Player {clientId} disconnected.", LogType.HostClientConnection);
            _sm.logger.Log("Ending game...", LogType.HostClientConnection);
            _gameStarted.Value = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }

        // Client-only
        private void EndGameClient(ulong clientId)
        {
            _sm.logger.Log($"Player {clientId} disconnected.", LogType.HostClientConnection);
            _sm.logger.Log("Ending game...", LogType.HostClientConnection);
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }
    }
}