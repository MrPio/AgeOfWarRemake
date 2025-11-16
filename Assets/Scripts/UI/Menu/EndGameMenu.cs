using System;
using System.Threading.Tasks;
using Managers;
using Partials.Behaviour;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace UI.Menu
{
    public class EndGameMenu : MonoBehaviour
    {
        private SceneManager _sm;
        [SerializeField] private TextMeshProUGUI centerText, timerText;
        [SerializeField] private Clickable continueButton;

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        private void Start()
        {
            continueButton.OnClick = () => _ = QuitLobby();
        }

        private void OnEnable()
        {
            centerText.text = _sm.GameManager.Winner == NetworkManager.Singleton.LocalClientId
                ? "You won!"
                : "You lost!";
            timerText.text = $"{_sm.GameManager.GameTime / 60f:00}m  {_sm.GameManager.GameTime % 60f:00}s";
        }

        public async Task QuitLobby()
        {
            await _sm.QuitLobby();
        }
    }
}