using System;
using System.Threading.Tasks;
using Prefabs;
using UnityEngine;
using Logger = UI.Logger;

namespace Managers
{
    public class SceneManager : MonoBehaviour
    {
        public Base baseAlly, baseEnemy;
        public GameObject hpBarHorizontal, hpBarVertical;
        public Camera cam;
        public Canvas canvas;
        public Logger logger;
        [NonSerialized] public GameManager GameManager;
        [SerializeField] private GameObject gameManagerPrefab;

        private void Start()
        {
            GameManager = Instantiate(gameManagerPrefab).GetComponent<GameManager>();
        }

        public void StartGame()
        {
            baseAlly.Spawn();
        }
    }
}