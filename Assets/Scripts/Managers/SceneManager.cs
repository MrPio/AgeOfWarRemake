using System;
using System.Collections;
using System.Threading.Tasks;
using Managers.Singletons;
using Managers.Statics;
using Partials.Camera;
using Prefabs;
using TMPro;
using UI;
using UI.Menu;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = UI.Logger;

namespace Managers
{
    public class SceneManager : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] public float fieldLenght = 23f;

        [Header("Prefabs")] public GameObject hpBarHorizontal;
        public GameObject hpBarVertical;
        public GameObject floatingText;

        [SerializeField]
        private GameObject gameManagerPrefab, specialAttackManagerPrefab, powerupManagerPrefab, basePrefab;

        [Header("Managers")] [NonSerialized] public PowerupManager PowerupManager;
        [NonSerialized] public GameManager GameManager;
        [NonSerialized] public RelayManager RelayManager;
        [NonSerialized] public SpecialAttackManager SpecialAttackManager;
        [NonSerialized] public MusicManager MusicManager;


        [Header("References")] public Camera cam;
        public Canvas canvas;
        public Logger logger;
        [SerializeField] public GameObject statisticsScreen, pauseMenu;
        [SerializeField] public StatsMenu statsMenu;
        [SerializeField] public UnitLoadingMenu unitLoadingMenu;
        [SerializeField] public LoadingMenu loadingMenu;
        [SerializeField] public RechargeBar specialAttackRechargeBar;
        [SerializeField] public ActionMenu actionMenu;
        [SerializeField] public TextMeshProUGUI HostClientBanner;

        private void Awake()
        {
            MusicManager = GameObject.FindWithTag("MusicManager").GetComponent<MusicManager>();
        }

        private void Start()
        {
            GameManager = Instantiate(gameManagerPrefab).GetComponent<GameManager>();
            RelayManager = GameManager.GetComponent<RelayManager>();
            GameManager.gameObject.name = "GameManager";
            SpecialAttackManager = Instantiate(specialAttackManagerPrefab).GetComponent<SpecialAttackManager>();
            SpecialAttackManager.gameObject.name = "SpecialAttackManager";
            PowerupManager = Instantiate(powerupManagerPrefab).GetComponent<PowerupManager>();
            PowerupManager.gameObject.name = "PowerupManager";
            loadingMenu.gameObject.SetActive(true);
            loadingMenu.Initialize(DataManager.IsMultiplayer, DataManager.IsHost);
            actionMenu.transform.parent.gameObject.SetActive(false);
            cam.GetComponent<CameraZoom>().enabled = false;
            cam.GetComponent<CameraEdgePan>().enabled = false;
        }

        public void StartGame()
        {
            loadingMenu.gameObject.SetActive(false);
            actionMenu.transform.parent.gameObject.SetActive(true);
            cam.GetComponent<CameraZoom>().enabled = true;
            cam.GetComponent<CameraEdgePan>().enabled = true;
            HostClientBanner.gameObject.SetActive(DataManager.IsMultiplayer);
            if (DataManager.IsMultiplayer)
                HostClientBanner.text = DataManager.IsHost ? "Host" : "Client";


            MusicManager.StartLevel();
            cam.GetComponent<CameraZoom>().Initialize();


            // Host only
            if (NetworkManager.Singleton.IsServer)
            {
                // Spawn ally base
                var allyBase = Instantiate(basePrefab);
                allyBase.name = "Base (Ally)";
                allyBase.GetComponent<NetworkObject>().SpawnWithOwnership(GameManager.HostId);

                // Spawn enemy base
                var enemyBase = Instantiate(basePrefab).GetComponent<NetworkObject>();
                enemyBase.name = "Base (Enemy)";
                enemyBase.GetComponent<Base>().IsBot.Value = DataManager.IsSingleplayer;
                enemyBase.SpawnWithOwnership(GameManager.ClientId);
            }
        }

        public void EndGame()
        {
            GameManager.IsGameOver = true;
            StartCoroutine(ShowStatisticsScreen());
            return;

            IEnumerator ShowStatisticsScreen()
            {
                yield return new WaitForSeconds(1f);

                if (DataManager.IsMultiplayer)
                    GameManager.Winner = GameManager.BaseAlly.Model.Value.Hp <= 0.01
                        ? GameManager.BaseEnemy.OwnerClientId
                        : GameManager.BaseAlly.OwnerClientId;
                else
                    GameManager.Winner = GameManager.BaseAlly.Model.Value.Hp <= 0.01
                        ? 2
                        : GameManager.BaseAlly.OwnerClientId;
                logger.Log(
                    $"Winner is {GameManager.Winner}! {GameManager.BaseEnemy.OwnerClientId}-{GameManager.BaseAlly.OwnerClientId} {GameManager.BaseAlly.Model.Value.Hp} {GameManager.BaseEnemy.Model.Value.Hp}");
                statisticsScreen.SetActive(true);
            }
        }

        public async Task QuitLobby()
        {
            MusicManager.EndLevel();
            GameManager.IsGameOver = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);

            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
            var nmPrefab = Resources.Load<NetworkManager>("Prefabs/Network/NetworkManager");
            Instantiate(nmPrefab);

            // Application.Quit();
        }
    }
}