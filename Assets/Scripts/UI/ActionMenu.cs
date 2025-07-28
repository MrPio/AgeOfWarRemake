using Managers;
using UnityEngine;
using Clickable = Partials.Clickable;

namespace UI
{
    public class ActionMenu : MonoBehaviour
    {
        private SceneManager _sm;

        [SerializeField] private Clickable unitMelee,
            unitRange,
            unitTank,
            unitBack,
            turretS,
            turretM,
            turretL,
            turretBack,
            buyUnits,
            buyTurret,
            sellTurret,
            buyExpansion,
            evolve;

        [SerializeField] private GameObject unitMenu, turretMenu, mainMenu;

        private void Start()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();

            // Menu navigations
            unitBack.OnClick += () =>
            {
                mainMenu.SetActive(true);
                unitMenu.SetActive(false);
            };
            turretBack.OnClick += () =>
            {
                mainMenu.SetActive(true);
                turretMenu.SetActive(false);
            };
            buyUnits.OnClick += () =>
            {
                mainMenu.SetActive(false);
                unitMenu.SetActive(true);
            };
            buyTurret.OnClick += () =>
            {
                mainMenu.SetActive(false);
                turretMenu.SetActive(true);
            };

            // Main menu
            sellTurret.OnClick += () => { };
            buyExpansion.OnClick += () => { };
            evolve.OnClick += () => { };

            // Unit menu
            unitMelee.OnClick += () => BuyUnit(0);
            unitRange.OnClick += () => BuyUnit(1);
            unitTank.OnClick += () => BuyUnit(2);

            // Turret menu
            turretS.OnClick += () => BuyTurret(0);
            turretM.OnClick += () => BuyTurret(1);
            turretL.OnClick += () => BuyTurret(2);
        }

        private void BuyUnit(int type) =>
            _sm.GameManager.BaseAlly.BuyUnitServerRpc((byte)type);

        private void BuyTurret(int type)
        {
            // show expansions clickables   
            //_sm.GameManager.BaseAlly.BuyTurretServerRpc()
        }
    }
}