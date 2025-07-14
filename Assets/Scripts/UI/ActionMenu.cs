using Managers;
using UnityEngine;
using Clickable = Partials.Clickable;

namespace UI
{
    public class ActionMenu : MonoBehaviour
    {
        private SceneManager _sm;
        [SerializeField] private Clickable melee, range, tank;

        private void Start()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            melee.OnClick += () => Buy(0);
            range.OnClick += () => Buy(1);
            tank.OnClick += () => Buy(2);
        }

        private void Buy(int type)
        {
            _sm.GameManager.BaseAlly.BuyUnitServerRpc((byte)type);
        }
    }
}