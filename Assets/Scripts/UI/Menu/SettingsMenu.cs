using Managers.Singletons;
using Partials.Behaviour;
using UnityEngine;

namespace UI.Menu
{
    public class SettingsMenu : MonoBehaviour
    {
        [SerializeField] private SettingEntry musicVolumeSetting, effectsVolumeSetting, fullscreenSetting;

        private void Start()
        {
            musicVolumeSetting.OnValueChanged = value => MusicManager.Instance.SetMusicVolume((int)value);
            effectsVolumeSetting.OnValueChanged = value => MusicManager.Instance.SetEffectsVolume((int)value);
            fullscreenSetting.OnValueChanged = value => Screen.fullScreen = (bool)value;
        }
    }
}