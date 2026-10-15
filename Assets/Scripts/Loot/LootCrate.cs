using System;
using ExpiryGame.Drop;
using UnityEngine;

namespace ExpiryGame.Loot
{
    // 부수면 음식을 떨구는 상자. 팀원 적과 같은 방식(IDamageable + IDropSource)으로 드롭 시스템에 연결된다.
    public class LootCrate : MonoBehaviour, IDamageable, IDropSource
    {
        private LootCrateSettings settings;
        private float current;
        private bool broken;

        public Vector2Int Chunk { get; private set; }
        public DropTable Table => settings.table;
        public float Threat01 => settings.threat01;
        public Vector3 DropPosition => transform.position + Vector3.up * 0.3f;

        public event Action<IDropSource> Died;
        public event Action<LootCrate> Broke;

        public void Setup(LootCrateSettings crateSettings, Vector2Int chunk)
        {
            settings = crateSettings;
            Chunk = chunk;
            current = crateSettings.health;
            broken = false;
        }

        private void OnEnable()
        {
            DropSourceRegistry.Register(this);
        }

        private void OnDisable()
        {
            DropSourceRegistry.Unregister(this);
        }

        public void TakeDamage(float amount)
        {
            if (broken || amount <= 0f) return;
            current -= amount;
            if (current > 0f) return;

            broken = true;
            Died?.Invoke(this);
            Broke?.Invoke(this);
        }
    }
}
