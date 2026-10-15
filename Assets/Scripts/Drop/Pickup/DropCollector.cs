using System;
using UnityEngine;

namespace ExpiryGame.Drop
{
    // 수집자(플레이어)가 반경 안에 들어오면 살짝 끌려가다가 닿으면 Reached를 발행한다.
    // 실제 인벤토리 추가는 하지 않는다 (DroppedFood가 결정).
    public class DropCollector : MonoBehaviour
    {
        private Transform target;
        private IFoodInventory inventory;
        private DropFeelSettings settings;
        private bool active;

        public event Action Reached;

        public void Bind(Transform collector, IFoodInventory collectorInventory, DropFeelSettings feel)
        {
            target = collector;
            inventory = collectorInventory;
            settings = feel;
        }

        public void SetActive(bool value)
        {
            active = value;
        }

        private void Update()
        {
            if (!active || target == null) return;
            // 가방이 가득 찼으면 끌려가지도 않는다 (줍지 못하는데 따라다니면 어색함)
            if (inventory != null && inventory.Items.Count >= inventory.Capacity) return;

            var toTarget = target.position - transform.position;
            toTarget.z = 0f;
            var distance = toTarget.magnitude;
            if (distance > settings.magnetRadius) return;

            if (distance <= settings.collectRadius)
            {
                Reached?.Invoke();
                return;
            }

            var closeness = 1f - Mathf.Clamp01(distance / Mathf.Max(0.0001f, settings.magnetRadius));
            var speed = Mathf.Lerp(settings.magnetMinSpeed, settings.magnetMaxSpeed, closeness);
            transform.position += toTarget / distance * Mathf.Min(speed * Time.deltaTime, distance);
        }
    }
}
