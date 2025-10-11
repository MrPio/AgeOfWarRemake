using Managers;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace UI.Menu
{
    public class EndGameMenu : MonoBehaviour
    {
        private SceneManager _sm;
        [SerializeField] private TextMeshProUGUI text;

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        private void OnEnable()
        {
            text.text = _sm.GameManager.Winner == NetworkManager.Singleton.LocalClientId ? "You won!" : "You lost!";
        }

        public async void QuitLobby()
        {
            await _sm.QuitLobby();
        }
    }
}