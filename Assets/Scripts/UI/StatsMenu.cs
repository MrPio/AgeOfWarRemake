using Managers;
using TMPro;
using UnityEngine;
using Clickable = Partials.Clickable;

namespace UI
{
    public class StatsMenu : MonoBehaviour
    {
        private SceneManager _sm;
        [SerializeField] private TextMeshProUGUI moneyText, expText;

        private void Start()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        public void UpdateUI(int money, int exp)
        {
            moneyText.text = money.ToString("N0");
            // expText.text = exp.ToString("N0");
        }
    }
}