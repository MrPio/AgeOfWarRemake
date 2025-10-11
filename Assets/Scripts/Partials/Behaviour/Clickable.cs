using System;
using Managers;
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
        private SceneManager _sm;
        private Image _image;
        private Color _startColor;

        [SerializeField] private Color hoverColor = new Color(0.75f, 0.75f, 0.75f, 1f),
            downColor = new Color(0.9f, 0.9f, 0.9f, 0.75f);

        [NonSerialized] public Action OnClick, OnHover, OnExit;


        private void Start()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            _image = GetComponent<Image>();
            _startColor = _image.color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _sm.musicManager.PlayUI("hover");
            _image.color = hoverColor;
            OnHover?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _image.color = _startColor;
            OnExit?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _image.color = downColor;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _image.color = _startColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _sm.musicManager.PlayUI("click");
            OnClick?.Invoke();
        }
    }
}