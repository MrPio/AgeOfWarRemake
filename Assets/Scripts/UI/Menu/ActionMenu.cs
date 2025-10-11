using System.Collections.Generic;
using System.Linq;
using Managers;
using Model.Bases;
using Model.Turrets;
using Model.Units;
using Partials.Behaviour;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using Clickable = Partials.Behaviour.Clickable;

namespace UI.Menu
{
    public class ActionMenu : MonoBehaviour
    {
        private SceneManager _sm;

        [Header("References")] [SerializeField]
        private TextMeshProUGUI descriptor;

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
            evolve,
            special;

        [SerializeField] private GameObject unitMenu, turretMenu, mainMenu;
        [Header("Prefabs")] [SerializeField] private GameObject positiveButtonPrefab;
        [SerializeField] private GameObject negativeButtonPrefab;

        private readonly List<GameObject> _buttons = new();
        private Base BaseModel => _sm.GameManager.BaseAlly.Model.Value;

        private static void ToggleMenu(GameObject from, GameObject to)
        {
            from.SetActive(false);
            to.SetActive(true);
        }

        private void Start()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();

            // Menu navigations
            unitBack.OnClick += () => ToggleMenu(unitMenu, mainMenu);
            turretBack.OnClick += () => ToggleMenu(turretMenu, mainMenu);
            buyUnits.OnClick += () => ToggleMenu(mainMenu, unitMenu);
            buyUnits.OnHover += () => descriptor.text = "Deploy a unit";
            buyUnits.OnExit += Exit;
            buyTurret.OnClick += () => ToggleMenu(mainMenu, turretMenu);
            buyTurret.OnHover += () => descriptor.text = "Build a turret";
            buyTurret.OnExit += Exit;
            special.OnClick += UseSpecial;
            special.OnHover += () => descriptor.text = "Use special attack";
            special.OnExit += Exit;

            // Main menu
            sellTurret.OnClick += SellTurret;
            sellTurret.OnHover += HoverSellTurret;
            sellTurret.OnExit += Exit;

            buyExpansion.OnClick += BuyExpansion;
            buyExpansion.OnHover += HoverExpansion;
            buyExpansion.OnExit += Exit;

            evolve.OnClick += Evolve;
            evolve.OnHover += HoverEvolution;
            evolve.OnExit += Exit;

            // Unit menu
            var unitButtons = new[] { unitMelee, unitRange, unitTank };
            for (var i = 0; i < unitButtons.Length; i++)
            {
                var idx = i;
                unitButtons[i].OnClick += () => BuyUnit(idx);
                unitButtons[i].OnHover += () => HoverUnit(idx);
                unitButtons[i].OnExit += Exit;
            }

            // Turret menu
            var turretButtons = new[] { turretS, turretM, turretL };
            for (var i = 0; i < turretButtons.Length; i++)
            {
                var idx = i;
                turretButtons[i].OnClick += () => BuyTurret(idx);
                turretButtons[i].OnHover += () => HoverTurret(idx);
                turretButtons[i].OnExit += Exit;
            }
        }

        private void Exit() => descriptor.text = "";

        #region Unit

        private void BuyUnit(int type) =>
            _sm.GameManager.BaseAlly.BuyUnitServerRpc((byte)type);

        private void HoverUnit(int type)
        {
            var model = UnitFactory.Units[BaseModel.Level - 1][type]();
            descriptor.text = $"{model.DisplayName.Message.Value} - ${model.Cost:N0}";
        }

        #endregion

        #region BuyTurret

        private void BuyTurret(int type)
        {
            var baseAlly = _sm.GameManager.BaseAlly;
            var turretModel = TurretFactory.Turrets[BaseModel.Level - 1][type]();

            // Money check
            if (BaseModel.Money < turretModel.Cost) return;

            // Spawn buttons on free expansions
            _buttons.ForEach(Destroy);
            _buttons.Clear();
            for (var i = 0; i < BaseModel.UnlockedExpansions; i++)
            {
                if (BaseModel.Turrets[i].HasValue) continue;
                var target = baseAlly.BasePrefab.turretsPos[i].transform;
                var button = Instantiate(positiveButtonPrefab, _sm.canvas.transform);
                button.GetComponent<Followable>().target = new FollowableTarget(target);

                var expansionIdx = (byte)i;
                var turretIdx = (byte)type;
                button.GetComponent<Clickable>().OnClick +=
                    () => _sm.GameManager.BaseAlly.BuyTurretServerRpc(expansionIdx, turretIdx);
                _buttons.Add(button);
            }
        }

        private void HoverTurret(int type)
        {
            var baseModel = _sm.GameManager.BaseAlly.Model.Value;
            var model = TurretFactory.Turrets[baseModel.Level - 1][type]();
            descriptor.text = $"{model.DisplayName.Message.Value} - ${model.Cost:N0}";
        }

        #endregion

        #region SellTurret

        private void SellTurret()
        {
            var baseAlly = _sm.GameManager.BaseAlly;

            // Spawn buttons on free expansions
            _buttons.ForEach(Destroy);
            _buttons.Clear();
            for (var i = 0; i < BaseModel.UnlockedExpansions; i++)
            {
                var turret = BaseModel.Turrets[i];
                if (!turret.HasValue) continue;
                var target = baseAlly.BasePrefab.turretsPos[i].transform;
                var button = Instantiate(negativeButtonPrefab, _sm.canvas.transform);
                button.GetComponent<Followable>().target = new FollowableTarget(target);
                var clickable = button.GetComponent<Clickable>();
                clickable.OnHover += () =>
                    descriptor.text = $"Sell {turret.DisplayName.Message.Value} at ${turret.SellPrice:N0}";
                clickable.OnExit += () => descriptor.text = "";

                var expansionIdx = (byte)i;
                button.GetComponent<Clickable>().OnClick +=
                    () => { _sm.GameManager.BaseAlly.SellTurretServerRpc(expansionIdx); };
                _buttons.Add(button);
            }
        }

        private void HoverSellTurret() =>
            descriptor.text = BaseModel.Turrets.Any(it => it.HasValue) ? "Sell a turret" : "You don't have any turrets";

        #endregion

        #region Expansion

        private void BuyExpansion()
        {
            _sm.GameManager.BaseAlly.BuyExpansionServerRpc();
        }

        private void HoverExpansion()
        {
            var idx = BaseModel.UnlockedExpansions - 1;
            var cost = BaseFactory.ExpansionCosts[idx];
            descriptor.text = BaseFactory.ExpansionCosts.Count > idx
                ? $"Buy an expansion for ${cost:N0}"
                : "Can't buy any more";
        }

        #endregion

        #region Evolution

        private void Evolve()
        {
            throw new System.NotImplementedException();
        }

        private void HoverEvolution()
        {
            descriptor.text = BaseModel.Level < BaseFactory.Bases.Count
                ? $"Evolve for {BaseModel.EvolveExpRequired} EXP"
                : "Can't evolve any more";
        }

        #endregion

        #region Special

        private void UseSpecial()
        {
            _sm.SpecialAttackManager.RainAttack(BaseModel.Level - 1, NetworkManager.Singleton.LocalClientId);
        }

        #endregion

        private void Update()
        {
            if (_buttons.Count > 0 && Input.GetMouseButton(0))
            {
                _buttons.ForEach(btn => Destroy(btn, 0.2f));
                _buttons.Clear();
            }
        }
    }
}