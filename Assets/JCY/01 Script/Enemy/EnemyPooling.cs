using System.Collections.Generic;
using UnityEngine;

public class EnemyPooling : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private List<EnemySO> enemyDatas;
    [SerializeField] private int poolSize = 5;

    private Stack<GameObject> enemyPool = new Stack<GameObject>();
    [SerializeField] private Transform _payerTrm;
    [SerializeField] private EnemySpawner_JCY _enemySpawner;

    private void Start()
    {
        GameObject enemy;
        for (int i = 0; i < poolSize; i++)
        { 
            enemy = Instantiate(enemyPrefab , transform.position , Quaternion.identity);
            enemy = EnemyPoolPush(enemy);
        }
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

        EnemySO randomData =
            enemyDatas[Random.Range(0, enemyDatas.Count)];

        enemyScript.Initialize(randomData);
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
