using System;
using Managers.Singletons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SettingEntry : MonoBehaviour
    {
        private static SettingsManager Sm => SettingsManager.Instance;
        [SerializeField] private SettingType settingType;
        [SerializeField] private TextMeshProUGUI titleText;
        [Header("Int")] [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI valueText;
        [Header("Bool")] [SerializeField] private Toggle toggle;
        [Header("String")] [SerializeField] private TMP_InputField inputField;
        public Action<object> OnValueChanged = null;

        private void Start()
        {
            titleText.text = Sm.GetDisplayName(settingType);

            #region Int

            if (slider != null)
            {
                // Initialize
                var minMaxValues = Sm.GetMinMaxValues(settingType);
                slider.minValue = minMaxValues.Item1;
                slider.maxValue = minMaxValues.Item2;
                slider.value = Sm.Get<int>(settingType);
                valueText.text = Sm.Get<int>(settingType).ToString();
                slider.wholeNumbers = true;

                // Listener
                slider.onValueChanged.AddListener(value =>
                {
                    SettingsManager.Instance.Set(settingType, (int)value);
                    valueText.text = ((int)value).ToString();
                    OnValueChanged?.Invoke((int)value);
                });
            }

            #endregion

            #region Bool

            if (toggle != null)
            {
                // Initialize
                toggle.isOn = Sm.Get<bool>(settingType);

                // Listener
                toggle.onValueChanged.AddListener(value =>
                {
                    SettingsManager.Instance.Set(settingType, value);
                    OnValueChanged?.Invoke(value);
                });
            }

            #endregion

            #region String

            if (inputField != null)
            {
                // Initialize
                inputField.text = Sm.Get<string>(settingType);
                inputField.characterLimit = Sm.GetMaxLength(settingType);

                // Listener
                inputField.onValueChanged.AddListener(value =>
                {
                    SettingsManager.Instance.Set(settingType, value);
                    OnValueChanged?.Invoke(value);
                });
            }

            #endregion
        }
    }
}