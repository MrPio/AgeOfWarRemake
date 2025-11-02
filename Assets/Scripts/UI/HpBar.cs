using System;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HpBar : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI text;
        private SceneManager _sm;
        [NonSerialized] public Transform Target;
        private bool _destroyed = false;

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        public void SetValue(float hp, float maxHp, bool alsoText)
        {
            if (_destroyed)
                return;
            hp = Mathf.Clamp(hp, 0, maxHp);
            var value = hp / maxHp;
            if (value <= 0 && !_destroyed)
            {
                Destroy(gameObject);
                value = 0;
            }

            slider.value = value;
            text.gameObject.SetActive(alsoText && value < 1);
            if (alsoText)
                text.text = hp.ToString("N0") + " HP";
        }

        private void Update()
        {
            if (Target)
                transform.position = _sm.cam.WorldToScreenPoint(Target.position);
        }
    }
}