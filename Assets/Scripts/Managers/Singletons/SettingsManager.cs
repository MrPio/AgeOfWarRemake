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
        MusicBackground,
        MusicEffects
    }

    #region Internal

    internal abstract class SettingPropertyBase
    {
        public SettingType Type { get; }
        public string DisplayName { get; }

        protected SettingPropertyBase(SettingType type, string displayName)
        {
            Type = type;
            DisplayName = displayName;
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

        public SettingProperty(SettingType type, string displayName, T defaultValue)
            : base(type, displayName)
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
            { SettingType.Fullscreen, new SettingProperty<bool>(SettingType.Fullscreen, "Fullscreen Mode", true) },
            { SettingType.Username, new SettingProperty<string>(SettingType.Username, "Username", "Player") },
            {
                SettingType.MusicBackground,
                new SettingProperty<float>(SettingType.MusicBackground, "Music Volume", 0.75f)
            },
            { SettingType.MusicEffects, new SettingProperty<float>(SettingType.MusicEffects, "Effects Volume", 1f) },
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
            Screen.fullScreen = Get<bool>(SettingType.Fullscreen);
        }

        private void Update()
        {
            // Fullscreen management
#if UNITY_STANDALONE || UNITY_EDITOR_WIN
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Screen.fullScreen = false;
                Set(SettingType.Fullscreen, false);
            }

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

        #endregion

        #region Private

        private void Save()
        {
            foreach (var setting in _settings)
                _serializer.Serialize(
                    obj: setting.Value.GetCurrentValue(),
                    dir: ISerializer.ConfigsDir,
                    filename: setting.Value.FileName
                );
            print("Saved!");
        }

        private void Load()
        {
            foreach (var setting in _settings)
                setting.Value.SetCurrentValue(_serializer.Deserialize(
                    dir: ISerializer.ConfigsDir,
                    filename: setting.Value.FileName,
                    ifNotExist: setting.Value.GetDefaultValue()
                ));
        }

        #endregion
    }
}