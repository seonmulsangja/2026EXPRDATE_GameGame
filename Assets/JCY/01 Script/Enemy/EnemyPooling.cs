using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyPooling : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private GameObject enemyPrefab;
    public List<EnemySO> enemyDatas;
    [SerializeField] private int poolSize = 5;

    private Stack<GameObject> enemyPool = new Stack<GameObject>();
    [SerializeField] private Transform _payerTrm;
    [SerializeField] private EnemySpawner_JCY _enemySpawner;

    public static EnemyPooling Instance { get; private set; }

    private void Start()
    {
        GameObject enemy;
        for (int i = 0; i < poolSize; i++)
        { 
            enemy = Instantiate(enemyPrefab , transform.position , Quaternion.identity);
            enemy = EnemyPoolPush(enemy);
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    public GameObject SpawnRandomEnemy()
    {
        GameObject enemy;

        if (enemyPool.Count > 1)
        {
            enemy = enemyPool.Pop();
        }
        else
        { 
            enemy = Instantiate(enemyPrefab , transform.position , Quaternion.identity);
            enemy = EnemyPoolPush(enemy);
        }

        Enemy enemyScript = enemy.GetComponent<Enemy>();
        enemyScript._enemyPooling = this;

        
        enemyScript.targetTrm = _payerTrm;
        enemyScript._enemyPooling = this;
        
        enemy.SetActive(true);

        return enemy;
    }

    public GameObject EnemyPoolPush(GameObject enemy)
    {
        enemy.SetActive(false);
        enemy.transform.position = _enemySpawner._spawnPoints[Random.Range(0, _enemySpawner._spawnPoints.Length)].position;
        enemyPool.Push(enemy);
        return enemy;
    }

    public void ReturnEnemy(GameObject enemy)
    {
        enemy.SetActive(false);
        enemy.transform.SetParent(transform);
        enemyPool.Push(enemy);
    }
}
