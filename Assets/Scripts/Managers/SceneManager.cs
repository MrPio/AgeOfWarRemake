using System;
using System.Collections;
using System.Threading.Tasks;
using Partials;
using Partials.Camera;
using Prefabs;
using UI;
using UI.Menu;
using Unity.Netcode;
using UnityEngine;
using Logger = UI.Logger;

namespace Managers
{
    public class SceneManager : MonoBehaviour
    {
        public bool isMultiplayer;
        [Header("Settings")] [SerializeField] public float fieldLenght = 23f;

        [Header("Prefabs")] public GameObject hpBarHorizontal, hpBarVertical, floatingText;
        [SerializeField] private GameObject gameManagerPrefab, specialAttackManager, basePrefab;

        [Header("References")] public Camera cam;
        public Canvas canvas;
        public Logger logger;
        [NonSerialized] public GameManager GameManager;
        [NonSerialized] public SpecialAttackManager SpecialAttackManager;
        public MusicManager musicManager;
        [SerializeField] private GameObject statisticsScreen, waitForClientScreen;
        [SerializeField] public StatsMenu statsMenu;
        [SerializeField] public UnitLoadingMenu unitLoadingMenu;
        [SerializeField] public RechargeBar specialAttackRechargeBar;
        [SerializeField] public ActionMenu actionMenu;

        private void Start()
        {
            GameManager = Instantiate(gameManagerPrefab).GetComponent<GameManager>();
            GameManager.gameObject.name = "GameManager";
            SpecialAttackManager = Instantiate(specialAttackManager).GetComponent<SpecialAttackManager>();
            SpecialAttackManager.gameObject.name = "SpecialAttackManager";
            waitForClientScreen.SetActive(true);
        }

        public void StartGame()
        {
            waitForClientScreen.SetActive(false);
            musicManager.StartLevel();
            cam.GetComponent<CameraZoom>().Initialize();


            // Host only
            if (NetworkManager.Singleton.IsServer)
            {
                // Spawn ally base
                var allybase=Instantiate(basePrefab);
                allybase.name = "Base (Ally)";
                allybase.GetComponent<NetworkObject>().SpawnWithOwnership(GameManager.HostId);

                // Spawn enemy base
                var enemyBase = Instantiate(basePrefab).GetComponent<NetworkObject>();
                enemyBase.name = "Base (Enemy)";
                enemyBase.GetComponent<Base>().IsBot.Value = !isMultiplayer;
                enemyBase.GetComponent<Base>().IsBot.Value = !isMultiplayer;
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

                if (isMultiplayer)
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
            NetworkManager.Singleton.Shutdown();
            Application.Quit();
            // await lobbyManager.LeaveLobby();
            // UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager
            // .GetActiveScene().buildIndex);
        }
    }
}