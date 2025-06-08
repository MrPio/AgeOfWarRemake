using System;
using UnityEngine;

public class Notifiable : MonoBehaviour
{
    public Action OnNotify;
    private void Notify() => OnNotify?.Invoke();
}