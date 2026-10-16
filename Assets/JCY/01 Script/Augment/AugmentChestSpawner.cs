
using System.Collections;
using UnityEngine;

public class AugmentChestSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private float spawnInterval = 5f;

    [Header("Spawn Area")]
    [SerializeField] private float extraWidth = 3f;
    [SerializeField] private float extraHeight = 3f;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (chestPrefab == null || targetCamera == null)
                continue;

            SpawnChest();
        }
    }

    private void SpawnChest()
    {
        float depth = Mathf.Abs(
            transform.position.z - targetCamera.transform.position.z
        );

        Vector3 bottomLeft = targetCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, depth));

        Vector3 topRight = targetCamera.ViewportToWorldPoint(
            new Vector3(1f, 1f, depth));

        float left = bottomLeft.x;
        float right = topRight.x;
        float bottom = bottomLeft.y;
        float top = topRight.y;

        // 카메라보다 큰 바깥 사각형
        float outerLeft = left - extraWidth;
        float outerRight = right + extraWidth;
        float outerBottom = bottom - extraHeight;
        float outerTop = top + extraHeight;

        float spawnX;
        float spawnY;

        // 바깥 사각형에서 랜덤 좌표를 뽑되,
        // 카메라 화면 내부라면 다시 뽑기
        do
        {
            spawnX = Random.Range(outerLeft, outerRight);
            spawnY = Random.Range(outerBottom, outerTop);
        }
        while (spawnX >= left && spawnX <= right &&
               spawnY >= bottom && spawnY <= top);

        Vector3 spawnPosition = new Vector3(
            spawnX,
            spawnY,
            transform.position.z
        );

        Instantiate(
            chestPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }
}
