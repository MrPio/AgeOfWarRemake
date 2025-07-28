using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textUI;
        [SerializeField] private Image imageUI;
        [SerializeField] private Animator animator;

        public void Initialize(string text, Sprite sprite=null)
        {
            textUI.text = text;
            if (sprite != null)
            {
                animator.enabled = false;
                imageUI.sprite = sprite;
            }
        }
    }
}