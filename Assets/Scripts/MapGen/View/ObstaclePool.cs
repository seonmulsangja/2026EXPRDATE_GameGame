using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.MapGen
{
    // 풀에 되돌릴 때 어떤 종류인지 알기 위한 표식
    public class PooledObstacle : MonoBehaviour
    {
        public int TypeIndex;
    }

    // 장애물 GameObject 풀 (스프라이트 + 박스 콜라이더). 종류별 스택.
    public class ObstaclePool
    {
        private readonly MapGenSettings settings;
        private readonly Transform root;
        private readonly Stack<PooledObstacle>[] free;

        public ObstaclePool(MapGenSettings settings, Transform root)
        {
            this.settings = settings;
            this.root = root;
            free = new Stack<PooledObstacle>[settings.obstacles.Length];
            for (var i = 0; i < free.Length; i++) free[i] = new Stack<PooledObstacle>();
        }

        public PooledObstacle Get(ObstaclePlacement placement)
        {
            var stack = free[placement.TypeIndex];
            var obstacle = stack.Count > 0 ? stack.Pop() : Create(placement.TypeIndex);
            obstacle.transform.position = placement.Position;
            obstacle.gameObject.SetActive(true);
            return obstacle;
        }

        public void Release(PooledObstacle obstacle)
        {
            obstacle.gameObject.SetActive(false);
            free[obstacle.TypeIndex].Push(obstacle);
        }

        private PooledObstacle Create(int typeIndex)
        {
            var definition = settings.obstacles[typeIndex];
            var go = new GameObject($"Obstacle_{definition.name}");
            go.transform.SetParent(root, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = definition.sprite;
            renderer.sortingOrder = settings.obstacleSortingOrder;
            if (settings.obstacleMaterial != null) renderer.sharedMaterial = settings.obstacleMaterial;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = definition.colliderSize;
            box.offset = definition.colliderOffset;

            var obstacle = go.AddComponent<PooledObstacle>();
            obstacle.TypeIndex = typeIndex;
            return obstacle;
        }
    }
}
