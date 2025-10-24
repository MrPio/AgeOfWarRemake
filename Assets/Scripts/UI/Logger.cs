using System;
using Managers;
using Managers.Serializer;
using TMPro;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace UI
{
    public enum LogType
    {
        Misc,
        Error,
        WaitingFor,
        NetworkSpawn,
        HostClientConnection,
        ReadingStatus
    }

    public class Logger : MonoBehaviour
    {
        [SerializeField] private GameObject logTextPrefab;
        [SerializeField] private ushort logLineLength = 30;
        private CanvasGroup _canvasGroup;

        private readonly Color[] _typesColors =
            { Color.white, Color.red, Color.blue, Color.green, Color.yellow, Color.gray };

        private string _history = "";
        [NonSerialized] public string LOGFileName = "";

        private readonly ISerializer _serializer = BinarySerializer.Instance;

        private void Start()
        {
            Log(
                $"[{LOGFileName}] (DataManager) Playing {(DataManager.IsMultiplayer ? "Multiplayer" : "Singleplayer")}");
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
        }

        // Print the log message to the screen and save it to the log file
        public void Log(string message, LogType type = LogType.Misc, bool alsoConsole = true)
        {
            var color = _typesColors[(int)type];
            message = $@"({DateTime.Now:hh\:mm\:ss\.fff}) - {message}";
            for (var i = 0; i < message.Length / logLineLength + 1; i++)
            {
                var text = Instantiate(logTextPrefab, transform).GetComponent<TextMeshProUGUI>();
                text.text =
                    message.Substring(i * logLineLength,
                        math.min(message.Length - i * logLineLength, logLineLength));
                text.color = color;
            }

            _history += $"(type={type}) {message}\n";
            if (LOGFileName.Length > 0)
                _serializer.Serialize(_history, ISerializer.LogsDir, LOGFileName);

            if (alsoConsole) UnityEngine.Debug.Log(message);
        }

        public void LogError(string message)
        {
            Log(message, LogType.Error, alsoConsole: false);
            UnityEngine.Debug.LogError(message);
        }

        private void Update()
        {
            // if (!Application.isEditor) return;
            if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.KeypadMinus))
                _canvasGroup.alpha = 1 - _canvasGroup.alpha;
        }
    }
}