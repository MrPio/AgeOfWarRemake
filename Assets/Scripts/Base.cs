using System;
using Model;
using UnityEngine;

public class Base : MonoBehaviour
{
    [SerializeField] private GameObject basePrefab;
    public bool isEnemy;
    private Transform _spawnPoint;

    private void Start()
    {
        _spawnPoint = basePrefab.transform.Find("spawnPoint");
        InvokeRepeating(nameof(Spawn), 0, 2);
    }

    public void Evolve()
    {
        //TODO evolve base
    }

    public void Spawn()
    {
        var unit = Instantiate(Unit.FromName("caveman_1").Prefab, _spawnPoint.position, Quaternion.identity);
        unit.GetComponent<Prefabs.Unit>().IsEnemy = isEnemy;
    }
}