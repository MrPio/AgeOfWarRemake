using System;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Partials.Behaviour
{
    /// <summary>
    /// Adds clickable visual + sound  effects to a UI go.
    /// - Colorization (hover + down) must be customized
    /// - Sounding is fixed
    /// - OnClick/OnHover can be registered
    /// </summary>
    public class Clickable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
                             IPointerDownHandler, IPointerUpHandler
    {
        private MusicManager _musicManager;
        private Image _image;
        private TextMeshProUGUI _text;
        private Color _startColor;

        [SerializeField] private Color hoverColor = new Color(0.75f, 0.75f, 0.75f, 1f),
            downColor = new Color(0.9f, 0.9f, 0.9f, 0.75f);

        [SerializeField] private GameObject toShow = null;

        [NonSerialized] public Action OnClick, OnHover, OnExit;


        private void Start()
        {
            _musicManager = GameObject.FindWithTag("MusicManager").GetComponent<MusicManager>();
            _image = GetComponent<Image>();
            _text = GetComponent<TextMeshProUGUI>();
            _startColor = _image ? _image.color : _text.color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _musicManager.PlayUI("hover");
            if (_image != null) _image.color = hoverColor;
            if (_text != null) _text.color = hoverColor;
            if (_text != null) toShow?.SetActive(true);
            OnHover?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_image != null) _image.color = _startColor;
            if (_text != null) _text.color = _startColor;
            if (_text != null) toShow?.SetActive(false);
            OnExit?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_image != null) _image.color = downColor;
            if (_text != null) _text.color = downColor;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_image != null) _image.color = _startColor;
            if (_text != null) _text.color = _startColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _musicManager.PlayUI("click");
            OnClick?.Invoke();
        }
    }
}