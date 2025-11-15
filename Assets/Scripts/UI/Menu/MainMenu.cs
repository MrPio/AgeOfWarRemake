using Managers.Statics;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            multiplayerJoin,
            multiplayerLobbyCodePaste;


        [SerializeField] private TMP_InputField usernameInput, joinLobbyCodeInput;
        [SerializeField] private GameObject mainMenu, multiplayerMenu;

        private void Awake()
        {
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
                SceneManager.LoadScene("Game", LoadSceneMode.Single);
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
            usernameInput.onEndEdit.AddListener(username => DataManager.Username = username);
            multiplayerHost.OnClick = () =>
            {
                DataManager.IsHost = true;
                SceneManager.LoadScene("Game", LoadSceneMode.Single);
            };
            joinLobbyCodeInput.onEndEdit.AddListener(code => DataManager.LobbyCode = code);
            multiplayerLobbyCodePaste.OnClick = () =>
            {
                joinLobbyCodeInput.text = (GUIUtility.systemCopyBuffer ?? string.Empty).Trim();
                if (joinLobbyCodeInput.text.Length > 16) joinLobbyCodeInput.text = joinLobbyCodeInput.text[..16];
                DataManager.LobbyCode = joinLobbyCodeInput.text;
            };
            multiplayerJoin.OnClick = () =>
            {
                print(DataManager.LobbyCode);
                if (DataManager.LobbyCode is null || DataManager.LobbyCode.Length < 6 ||
                    DataManager.LobbyCode.Length > 8) return;
                DataManager.IsHost = false;
                SceneManager.LoadScene("Game", LoadSceneMode.Single);
            };
        }
    }
}