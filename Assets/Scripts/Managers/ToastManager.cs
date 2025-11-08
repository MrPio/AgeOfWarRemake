using System.Collections.Generic;
using UI;
using UnityEngine;

namespace Managers
{
    public enum ToastColor
    {
        Green,
        Cyan
    }

    public class ToastManager : MonoBehaviour
    {
        [SerializeField] private GameObject toastPrefab;
        private Transform _canvas;

        private readonly Dictionary<ToastColor, Color> _toastColors = new()
        {
            { ToastColor.Green, new Color(0.6494527f, 0.9254902f, 0.5978667f, 0.75f) },
            { ToastColor.Cyan, new Color(0.5978667f, 0.8816885f, 0.9254902f, 0.75f) }
        };

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void MakeToast(string message, ToastColor color)
        {
            _canvas = GameObject.Find("Canvas").transform;
            Instantiate(toastPrefab, _canvas).GetComponent<Toast>().Initialize(message, _toastColors[color]);
        }
    }
}