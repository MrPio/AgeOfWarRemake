using Managers;
using UnityEngine;
using Clickable = Partials.Behaviour.Clickable;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace UI.Menu
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private Clickable singlePlayerButton, multiPlayerButton, quitButton;

        private void Start()
        {
            quitButton.OnClick = Application.Quit;
            singlePlayerButton.OnClick = () =>
            {
                DataManager.IsMultiplayer = false;
                SceneManager.UnloadSceneAsync("MainMenu");
                SceneManager.LoadScene("Game");
            };
            multiPlayerButton.OnClick = () =>
            {
                DataManager.IsMultiplayer = true;
                SceneManager.UnloadSceneAsync("MainMenu");
                SceneManager.LoadScene("Game");
            };
        }
    }
}