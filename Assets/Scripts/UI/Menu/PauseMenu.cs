using System;
using System.Threading.Tasks;
using Managers;
using Partials.Behaviour;
using TMPro;
using UnityEngine;

namespace UI.Menu
{
    public class PauseMenu : MonoBehaviour
    {
        private SceneManager _sm;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Clickable resumeButton, exitButton;

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            resumeButton.OnClick = () => gameObject.SetActive(false);
            exitButton.OnClick = () => _ = QuitLobby();
        }

        private void OnEnable()
        {
            timerText.text = $"{_sm.GameManager.GameTime / 60f:00}m  {_sm.GameManager.GameTime % 60f:00}s";
            _sm.GameManager.IsGamePaused = true;
        }

        private void OnDisable()
        {
            _sm.GameManager.IsGamePaused = false;
        }

        public async Task QuitLobby()
        {
            await _sm.QuitLobby();
        }
    }
}