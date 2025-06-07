using Interfaces;
using Managers;
using Model.Bases;
using Model.Units;
using UnityEngine;
using UnityEngine.UI;

namespace Prefabs
{
    public class Base : MonoBehaviour, IDamageable
    {
        public Model.Bases.Base Model;

        [SerializeField] private GameObject basePrefab;
        [SerializeField] private Transform hpBarPoint;
        public bool isEnemy;

        private SceneManager _sm;
        private Transform _spawnPoint;
        private HpBar _hpBar;

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        private void Start()
        {
            _spawnPoint = basePrefab.transform.Find("spawnPoint");
            InvokeRepeating(nameof(Spawn), 0, 0.5f);
            Model = new Cave();
            Damage(1);
        }

        public void Evolve()
        {
            //TODO evolve base
        }

        public void Spawn()
        {
            var unit = Instantiate(new Caveman1().Prefab, _spawnPoint.position, Quaternion.identity);
            unit.GetComponent<Prefabs.Unit>().IsEnemy = isEnemy;
        }

        public void Damage(float damage)
        {
            if (damage <= 0) return;
            Model.hp = Mathf.Clamp(Model.hp - damage, 0, Model.maxHp);
            if (_hpBar is null)
            {
                var go = Instantiate(_sm.hpBarVertical, _sm.canvas.transform);
                _hpBar = go.GetComponent<HpBar>();
                _hpBar.Target = hpBarPoint;
            }

            _hpBar.SetValue(Model.hp / Model.maxHp, alsoText: true);
        }
    }
}