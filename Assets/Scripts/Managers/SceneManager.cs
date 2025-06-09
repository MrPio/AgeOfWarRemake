using System;
using System.Threading.Tasks;
using Prefabs;
using Unity.Netcode;
using UnityEngine;
using Logger = UI.Logger;

namespace Managers
{
    public class SceneManager : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] public float fieldLenght = 22f;

        [Header("Prefabs")] public GameObject hpBarHorizontal, hpBarVertical;
        [SerializeField] private GameObject gameManagerPrefab, basePrefab;

        [Header("References")] [NonSerialized] public Base BaseAlly, BaseEnemy;
        public Camera cam;
        public Canvas canvas;
        public Logger logger;
        [NonSerialized] public GameManager GameManager;
        [SerializeField] private GameObject statisticsScreen, waitForClientScreen;

        private void Start()
        {
            GameManager = Instantiate(gameManagerPrefab).GetComponent<GameManager>();
            waitForClientScreen.SetActive(true);
        }

        public void StartGame()
        {
            waitForClientScreen.SetActive(false);

            // Host only
            if (NetworkManager.Singleton.IsServer)
            {
                // Spawn the bases
                Instantiate(basePrefab).GetComponent<NetworkObject>().SpawnWithOwnership(GameManager.HostId);
                Instantiate(basePrefab).GetComponent<NetworkObject>().SpawnWithOwnership(GameManager.ClientId);
            }
        }

        public void EndGame()
        {
            GameManager.Winner = BaseAlly.Model.Value.Hp <= 0.01 ? BaseEnemy.OwnerClientId : BaseAlly.OwnerClientId;
            logger.Log(
                $"Winner is {GameManager.Winner}! {BaseEnemy.OwnerClientId}-{BaseAlly.OwnerClientId} {BaseAlly.Model.Value.Hp} {BaseEnemy.Model.Value.Hp}");
            statisticsScreen.SetActive(true);
        }

        public async Task QuitLobby()
        {
            NetworkManager.Singleton.Shutdown();
            // await lobbyManager.LeaveLobby();
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().buildIndex);
        }
    }
}