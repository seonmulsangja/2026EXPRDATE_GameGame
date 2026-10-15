using System;
using ExpiryGame.Visual;
using UnityEngine;

namespace ExpiryGame.Drop
{
    // 바닥에 떨어진 음식 하나의 수명 관리: 던져짐 → 착지(반짝) → 획득 가능 → (썩으면) 일정 시간 뒤 소멸.
    // 유통기한 감소는 DropSpawner가 ShelfLifeSystem에 등록해 처리하고, 이 클래스는 결과(FoodItem)를 읽기만 한다.
    public class DroppedFood : MonoBehaviour
    {
        [SerializeField] private FoodVisual visual;
        [SerializeField] private DropArc arc;
        [SerializeField] private DropCollector collector;
        [SerializeField] private DropSparkle sparkle;

        private DropContext context;
        private float collectableAt;
        private bool landed;
        private bool rotting;
        private float rottenFor;
        private Vector3 restScale;

        public FoodItem Item { get; private set; }

        public event Action<DroppedFood> Collected;
        public event Action<DroppedFood> Expired;

        private bool IsShrinking => rotting && rottenFor >= context.Settings.rottenLifetime;

        private void Awake()
        {
            restScale = transform.localScale;
            arc.Touchdown += OnTouchdown;
            arc.Settled += OnSettled;
            collector.Reached += OnReached;
        }

        public void Launch(FoodItem item, Vector3 from, Vector3 to, DropContext ctx)
        {
            Item = item;
            context = ctx;
            landed = false;
            rotting = false;
            rottenFor = 0f;
            transform.localScale = restScale;

            visual.Bind(item);
            visual.Refresh();
            item.Rotted += OnRotted;
            ctx.Clock.Ticked += OnTicked;
            if (item.IsRotten) OnRotted(item);

            collector.Bind(ctx.Collector, ctx.Inventory, ctx.Settings);
            collector.SetActive(false);
            arc.Play(from, to, ctx.Settings);
        }

        // 풀로 돌려보내기 전에 호출 (이벤트 구독 해제, 연출 정지)
        public void Cleanup()
        {
            if (Item != null) Item.Rotted -= OnRotted;
            if (context != null) context.Clock.Ticked -= OnTicked;
            arc.Cancel();
            sparkle.Stop();
            collector.SetActive(false);
            visual.Clear();
            Item = null;
            context = null;
        }

        private void OnTouchdown()
        {
            sparkle.Play(context.Settings);
        }

        private void OnSettled()
        {
            landed = true;
            collectableAt = Time.time + context.Settings.pickupDelay;
        }

        private void OnTicked(float _)
        {
            visual.Refresh();
        }

        private void OnRotted(FoodItem _)
        {
            rotting = true;
            rottenFor = 0f;
            visual.Refresh();
        }

        private void OnReached()
        {
            if (Item == null || !context.Inventory.TryAdd(Item)) return;
            Collected?.Invoke(this);
        }

        private void Update()
        {
            if (Item == null) return;
            if (landed) collector.SetActive(Time.time >= collectableAt && !IsShrinking);
            if (!rotting) return;

            rottenFor += Time.deltaTime;
            if (!IsShrinking) return;

            var settings = context.Settings;
            var shrink01 = (rottenFor - settings.rottenLifetime) / settings.shrinkDuration;
            transform.localScale = restScale * Mathf.Clamp01(1f - shrink01);
            if (shrink01 >= 1f) Expired?.Invoke(this);
        }
    }
}
