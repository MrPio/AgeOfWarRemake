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
        [NonSerialized] public Base BaseAlly, BaseEnemy;
        public GameObject hpBarHorizontal, hpBarVertical;
        public Camera cam;
        public Canvas canvas;
        public Logger logger;
        [NonSerialized] public GameManager GameManager;
        [SerializeField] private GameObject gameManagerPrefab, basePrefab, waitForClientScreen;

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
    }
}