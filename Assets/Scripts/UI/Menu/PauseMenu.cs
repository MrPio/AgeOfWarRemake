using System;
using System.Threading.Tasks;
using Managers;
using Managers.Singletons;
using Managers.Statics;
using Partials.Behaviour;
using Partials.Camera;
using TMPro;
using UnityEngine;

namespace UI.Menu
{
    public class PauseMenu : MonoBehaviour
    {
        private SceneManager _sm;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Clickable resumeButton, exitButton;
        [SerializeField] private CameraEdgePan cameraEdgePan;
        [SerializeField] private CameraZoom cameraZoom;

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        private void Start()
        {
            resumeButton.OnClick = () => gameObject.SetActive(false);
            exitButton.OnClick = () => _ = QuitLobby();
        }

        private void OnEnable()
        {
            timerText.text = $"{_sm.GameManager.GameTime / 60f:00}m  {_sm.GameManager.GameTime % 60f:00}s";
            cameraEdgePan.enabled = false;
            cameraZoom.enabled = false;
            if (DataManager.GameMode is GameMode.Singleplayer)
                _sm.GameManager.IsGamePaused = true;
        }

        private void OnDisable()
        {
            cameraEdgePan.enabled = true;
            cameraZoom.enabled = true;
            if (DataManager.GameMode is GameMode.Singleplayer)
                _sm.GameManager.IsGamePaused = false;
        }

        public async Task QuitLobby()
        {
            await _sm.QuitLobby();
        }
    }
}