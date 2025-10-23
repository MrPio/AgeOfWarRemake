using Managers;
using TMPro;
using UnityEngine;

namespace UI.Menu
{
    public class LoadingMenu : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;

        [SerializeField]
        private string multiplayerText = "Waiting for opponent to join...", singleplayerText = "Loading...";

        private SceneManager _sm;

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        private void OnEnable()
        {
            text.text = _sm.IsMultiplayer ? multiplayerText : singleplayerText;
        }
    }
}