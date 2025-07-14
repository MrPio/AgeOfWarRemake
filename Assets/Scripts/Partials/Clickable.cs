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
        private Image image;
        private Color startColor;
        [SerializeField] private Color hoverColor, downColor;
        [NonSerialized] public Action OnClick;


        private void Start()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            image = GetComponent<Image>();
            startColor = image.color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _sm.musicManager.PlayUI("hover");
            image.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            image.color = startColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            image.color = downColor;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            image.color = startColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _sm.musicManager.PlayUI("click");
            OnClick?.Invoke();
        }
    }
}