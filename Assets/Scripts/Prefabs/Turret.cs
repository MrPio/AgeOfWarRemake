using System;
using Unity.Netcode;
using UnityEngine;

public class Turret : NetworkBehaviour
{
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private GameObject bulletPrefab;
    private Animator _animator;
    [NonSerialized] public Model.Bases.Turret Model;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }
}