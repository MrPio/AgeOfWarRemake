using Managers.Singletons;
using Partials.Behaviour;
using TMPro;
using UnityEngine;

namespace UI.Menu
{
    public class LoadingMenu : MonoBehaviour
    {
        private static ToastManager _tm;
        [SerializeField] private TextMeshProUGUI text, lobbyCodeText;

        [SerializeField]
        private string multiplayerText = "Waiting for opponent to join...", singleplayerText = "Loading...";

        [SerializeField] private GameObject lobbyCodeContainer;
        [SerializeField] private Clickable lobbyCodeCopyButton;

        private void Awake()
        {
            _tm = GameObject.FindWithTag("ToastManager").GetComponent<ToastManager>();
        }

        public void Initialize(bool isMultiplayer, bool isHost, string lobbyCode = null)
        {
            text.text = isMultiplayer ? multiplayerText : singleplayerText;
            lobbyCodeContainer.SetActive(isMultiplayer && lobbyCode is not null);
            lobbyCodeCopyButton.gameObject.SetActive(isHost);
            lobbyCodeCopyButton.OnClick = () =>
            {
                GUIUtility.systemCopyBuffer = lobbyCode;
                _tm.MakeToast("Copied to clipboard!", ToastColor.Green);
            };
            lobbyCodeText.text = lobbyCode;
        }
    }
}