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

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        public void SetValue(float hp, float maxHp, bool alsoText)
        {
            var value = hp / maxHp;
            if (value <= 0)
            {
                Destroy(gameObject);
                value = 0;
            }

            slider.value = value;
            text.gameObject.SetActive(alsoText && value < 1);
            if (alsoText)
            {
                text.text = hp.ToString("N0") + " HP";
            }
        }

        private void Update()
        {
            transform.position = _sm.cam.WorldToScreenPoint(Target.position);
        }
    }
}