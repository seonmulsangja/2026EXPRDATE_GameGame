using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class EnemySpawner_JCY : MonoBehaviour
{
    [SerializeField] private EnemyPooling _enemyPooling;
    public Transform[] _spawnPoints;

    public bool _isSpawn = true;

    [SerializeField , Min(0.1f)] private float _spawnTime;


    private void Start()
    { 
        StartCoroutine(SpawnCo());
    }

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

    public IEnumerator SpawnCo()
    {
        yield return new WaitForSeconds(_spawnTime);
        
        if(_spawnTime > 0.5)
            _spawnTime -= 0.01f;
        
        SpawnEnemy();
        StartCoroutine(SpawnCo());
    }
}
