using System;
using System.Collections.Generic;
using Interfaces;
using Managers.Serializer;
using Managers.Utils;
using UnityEngine;

namespace Managers.Singletons
{
    public enum SettingType
    {
        Fullscreen,
        Username,
        MusicVolume,
        EffectsVolume
    }

    #region Internal

    internal abstract class SettingPropertyBase
    {
        public SettingType Type { get; }
        public string DisplayName { get; }
        public int MinValue { get; }
        public int MaxValue { get; }
        public int MaxLength { get; }

        protected SettingPropertyBase(SettingType type, string displayName, int minValue, int maxValue, int maxLength)
        {
            Type = type;
            DisplayName = displayName;
            MinValue = minValue;
            MaxValue = maxValue;
            MaxLength = maxLength;
        }

        public abstract object GetDefaultValue();
        public abstract object GetCurrentValue();
        public abstract void SetCurrentValue(object value);

        public string FileName => DisplayName.ToLower().Replace(" ", "_");
    }

    internal sealed class SettingProperty<T> : SettingPropertyBase
    {
        public T DefaultValue { get; }
        public T Value;

        public SettingProperty(SettingType type, string displayName, T defaultValue, int minValue = 0,
                               int maxValue = 100, int maxLength = 99)
            : base(type, displayName, minValue, maxValue, maxLength)
        {
            DefaultValue = defaultValue;
            Value = defaultValue;
        }

        public override object GetDefaultValue() => DefaultValue;
        public override object GetCurrentValue() => Value;

        public override void SetCurrentValue(object value)
        {
            if (value is T typed)
                Value = typed;
            else
                throw new InvalidCastException(
                    $"Setting '{Type}' expected type {typeof(T)}, received {value?.GetType()}");
        }
    }

    #endregion

    public sealed class SettingsManager : SingletonMonoBehaviour<SettingsManager>
    {
        private readonly ISerializer _serializer = BinarySerializer.Instance;
        private Debouncer _debouncer;

        private readonly Dictionary<SettingType, SettingPropertyBase> _settings = new()
        {
            { SettingType.Fullscreen, new SettingProperty<bool>(SettingType.Fullscreen, "Fullscreen", true) },
            {
                SettingType.Username,
                new SettingProperty<string>(SettingType.Username, "Username", "Player", maxLength: 20)
            },
            {
                SettingType.MusicVolume,
                new SettingProperty<int>(SettingType.MusicVolume, "Music Volume", 75)
            },
            { SettingType.EffectsVolume, new SettingProperty<int>(SettingType.EffectsVolume, "Effects Volume", 100) },
        };

        #region Events

        protected override void Awake()
        {
            base.Awake();
            _debouncer = new Debouncer(this);
            Load();
        }

        private void Start()
        {
            // Initialize settings
            Screen.fullScreen = Get<bool>(SettingType.Fullscreen);
            MusicManager.Instance.SetMusicVolume(Get<int>(SettingType.MusicVolume));
            MusicManager.Instance.SetEffectsVolume(Get<int>(SettingType.EffectsVolume));
        }

        private void Update()
        {
            // Fullscreen management
#if UNITY_STANDALONE || UNITY_EDITOR_WIN
            var f11Pressed = Input.GetKeyDown(KeyCode.F11);
            var altEnterPressed = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) &&
                                  Input.GetKeyDown(KeyCode.Return);
            if (f11Pressed || altEnterPressed)
            {
                var newValue = !Screen.fullScreen;
                Screen.fullScreen = newValue;
                Set(SettingType.Fullscreen, newValue);
            }
#endif
        }

        #endregion

        #region API

        public T Get<T>(SettingType type) => ((SettingProperty<T>)_settings[type]).Value;

        public void Set<T>(SettingType type, T value)
        {
            // Do nothing if value hasn't changed
            var actualValue = ((SettingProperty<T>)_settings[type]).Value;
            if (EqualityComparer<T>.Default.Equals(actualValue, value)) return;

            ((SettingProperty<T>)_settings[type]).Value = value;

            // Request a save action.
            // The min distance between 2 Savings is guaranteed to be 1 second
            _debouncer.Debounce(Save);
        }

        public string GetDisplayName(SettingType type) => _settings[type].DisplayName;

        public Tuple<int, int> GetMinMaxValues(SettingType type) =>
            new(_settings[type].MinValue, _settings[type].MaxValue);
        public int GetMaxLength(SettingType type) => _settings[type].MaxLength;

        #endregion

        #region Private

        private void Save()
        {
            foreach (var setting in _settings)
                _serializer.Serialize(
                    obj: setting.Value.GetCurrentValue(),
                    dir: ISerializer.SettingsDir,
                    filename: setting.Value.FileName
                );
            print("Saved!");
        }

        private void Load()
        {
            foreach (var setting in _settings)
                setting.Value.SetCurrentValue(_serializer.Deserialize(
                    dir: ISerializer.SettingsDir,
                    filename: setting.Value.FileName,
                    ifNotExist: setting.Value.GetDefaultValue()
                ));
        }

        #endregion
    }
}