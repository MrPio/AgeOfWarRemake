using System;
using Managers;
using TMPro;
using UnityEngine;

public class LoadingMenu : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    [SerializeField]
    private string multiplayerText = "Waiting for opponent to join...", singleplayerText = "Loading...";

    private SceneManager _sm;

    private void Awake()
    {
        _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
    }

    private void OnEnable()
    {
        text.text = _sm.isMultiplayer ? multiplayerText : singleplayerText;
    }
}