using System;
using Managers;
using Managers.Serializer;
using TMPro;
using UnityEngine;
using Clickable = Partials.Behaviour.Clickable;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace UI.Menu
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private Clickable singleplayerButton,
            multiplayerButton,
            quitButton,
            multiplayerBack,
            multiplayerHost,
            multiplayerJoin;

        [SerializeField] private TMP_InputField usernameInput, joinLobbyCodeInput;
        [SerializeField] private GameObject mainMenu, multiplayerMenu;
        private ISerializer _serializer;

        private void Awake()
        {
            _serializer = BinarySerializer.Instance;

            // Load stored username
            DataManager.Username = _serializer.Deserialize<string>(ISerializer.ConfigsDir, "username", null);
            usernameInput.text = DataManager.Username;
        }

        private void Start()
        {
            // Setup menu
            mainMenu.SetActive(true);
            multiplayerMenu.SetActive(false);

            quitButton.OnClick = Application.Quit;
            singleplayerButton.OnClick = () =>
            {
                DataManager.IsMultiplayer = false;
                SceneManager.UnloadSceneAsync("MainMenu");
                SceneManager.LoadScene("Game");
            };
            multiplayerButton.OnClick = () =>
            {
                DataManager.IsMultiplayer = true;
                mainMenu.SetActive(false);
                multiplayerMenu.SetActive(true);
            };
            multiplayerBack.OnClick = () =>
            {
                mainMenu.SetActive(true);
                multiplayerMenu.SetActive(false);
            };
            usernameInput.onEndEdit.AddListener(SaveUsername);
            multiplayerHost.OnClick = () =>
            {
                DataManager.IsHost = true;
                SceneManager.UnloadSceneAsync("MainMenu");
                SceneManager.LoadScene("Game");
            };
            joinLobbyCodeInput.onEndEdit.AddListener(code => DataManager.LobbyCode = code);
            multiplayerJoin.OnClick = () =>
            {
                // TODO: verify here the code, before changing scene.
                // GameManager, and NetworkManager should remain when changing scene
                DataManager.IsHost = false;
                SceneManager.UnloadSceneAsync("MainMenu");
                SceneManager.LoadScene("Game");
            };
        }

        private void SaveUsername(string username)
        {
            if (username.Length < 1)
                return;

            DataManager.Username = username;
            _serializer.Serialize(username, ISerializer.ConfigsDir, "username");
        }
    }
}