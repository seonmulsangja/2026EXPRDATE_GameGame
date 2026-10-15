using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class EnemySpawner_JCY : MonoBehaviour
{
    [SerializeField] private EnemyPooling _enemyPooling;
    public Transform[] _spawnPoints;

    public bool _isSpawn = true;

    private void Update()
    {
        //테스트용
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            SpawnEnemy();
        }
    }

    public GameObject SpawnEnemy()
    {
        if(!_isSpawn)
            return null;

        int num = Random.Range(0, _spawnPoints.Length);

        GameObject enemy = _enemyPooling.SpawnRandomEnemy();
        Instantiate(enemy, _spawnPoints[num].position , Quaternion.identity);
        return enemy;
    }
}
