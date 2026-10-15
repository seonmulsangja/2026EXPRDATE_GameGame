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

    private void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab, transform);
            enemy.SetActive(false);
            enemyPool.Push(enemy);
        }
    }

    public GameObject SpawnRandomEnemy()
    {
        GameObject enemy;

        if (enemyPool.Count > 0)
        {
            enemy = enemyPool.Pop();
        }
        else
        {
            enemy = Instantiate(enemyPrefab, transform);
        }

        Enemy enemyScript = enemy.GetComponent<Enemy>();

        EnemySO randomData =
            enemyDatas[Random.Range(0, enemyDatas.Count)];

        enemyScript.Initialize(randomData);
        enemyScript.targetTrm = _payerTrm;
        enemyScript._enemyPooling = this;
        
        enemy.SetActive(true);

        return enemy;
    }

    public void ReturnEnemy(GameObject enemy)
    {
        enemy.SetActive(false);
        enemy.transform.SetParent(transform);
        enemyPool.Push(enemy);
    }
}
