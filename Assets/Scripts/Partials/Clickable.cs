using System;
using System.Linq;
using Managers;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Partials
{
    public class Clickable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
                             IPointerDownHandler, IPointerUpHandler
    {
        private SceneManager _sm;
        private Image _image;
        private Color _startColor;
        [SerializeField] private Color hoverColor, downColor;
        [NonSerialized] public Action OnClick;


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
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _image.color = _startColor;
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