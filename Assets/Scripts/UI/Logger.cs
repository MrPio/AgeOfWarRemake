using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace UI
{
    public class Logger : MonoBehaviour
    {
        [SerializeField] private GameObject logTextPrefab;
        [SerializeField] private ushort logLineLength = 30;
        private CanvasGroup _canvasGroup;

        private void Start()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
        }

        // Print the log message to the screen and save it to the log file
        public void Log(string message, Color? color = null, bool alsoConsole = true)
        {
            message = $@"({DateTime.Now:hh\:mm\:ss}) - {message}";
            for (var i = 0; i < message.Length / logLineLength + 1; i++)
            {
                var text = Instantiate(logTextPrefab, transform).GetComponent<TextMeshProUGUI>();
                text.text =
                    message.Substring(i * logLineLength,
                        math.min(message.Length - i * logLineLength, logLineLength));
                text.color = color ?? Color.white;
            }

            if (alsoConsole) Debug.Log(message);
        }

        public void LogError(string message)
        {
            Log(message, Color.red, alsoConsole: false);
            Debug.LogError(message);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.KeypadMinus))
                _canvasGroup.alpha = 1 - _canvasGroup.alpha;
        }
    }
}