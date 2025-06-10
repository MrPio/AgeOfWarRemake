using System;
using Managers;
using Model.Bases;
using Prefabs;
using Unity.Netcode;
using UnityEngine;
using Base = Prefabs.Base;

public class Turret : NetworkBehaviour
{
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private GameObject bulletPrefab;
    private Animator _animator;
    [NonSerialized] public Base Base;
    private SceneManager _sm;
    private Unit _target;

    #region NetworkVariables

    // Readonly
    [NonSerialized]
    public NetworkVariable<byte> Index = new(byte.MaxValue, writePerm: NetworkVariableWritePermission.Server);

    // Readonly
    [NonSerialized]
    public NetworkVariable<Model.Turrets.Turret> Model = new(writePerm: NetworkVariableWritePermission.Owner);

    #endregion

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
    }

    public override void OnNetworkSpawn()
    {
        Base = IsOwner ? _sm.GameManager.BaseAlly : _sm.GameManager.BaseEnemy;
        var i = Index.Value;
        Base.BasePrefab.Turrets[i] = this;
        transform.position = Base.BasePrefab.turretsPos[i].transform.position;
    }

    public override void OnNetworkDespawn()
    {
        var i = Index.Value;
        Base.BasePrefab.Turrets[i] = null;
    }

    private void FixedUpdate()
    {
        CheckCollision();
    }

    // Owner only
    private void CheckCollision()
    {
        if (!IsOwner) return;

        // Get the nearest enemy within reach
        var enemies = _sm.GameManager.UnitsEnemy;
        var inFrontEnemy = enemies.Count > 0 ? enemies[0] : null;
        if (inFrontEnemy is not null && inFrontEnemy.transform.position.x - transform.position.x < Model.Value.Range)
            _target = inFrontEnemy;
    }
}