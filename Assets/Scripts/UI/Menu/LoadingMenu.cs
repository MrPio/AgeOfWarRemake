using System;
using Managers;
using Partials.Behaviour;
using TMPro;
using UnityEngine;

namespace UI.Menu
{
    public class LoadingMenu : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text, lobbyCodeText;

        [SerializeField]
        private string multiplayerText = "Waiting for opponent to join...", singleplayerText = "Loading...";

        [SerializeField] private GameObject lobbyCodeContainer;
        [SerializeField] private Clickable lobbyCodeCopyButton;

        public void Initialize(bool isMultiplayer, bool isHost, string lobbyCode = null)
        {
            text.text = isMultiplayer ? multiplayerText : singleplayerText;
            lobbyCodeContainer.SetActive(isMultiplayer && lobbyCode is not null);
            lobbyCodeCopyButton.gameObject.SetActive(isHost);
            lobbyCodeCopyButton.OnClick = () => { GUIUtility.systemCopyBuffer = lobbyCode; };
            lobbyCodeText.text = lobbyCode;
        }
    }
}