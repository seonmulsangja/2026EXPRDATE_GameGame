using System;
using UnityEngine;

namespace ExpiryGame.Landmark
{
    // "플레이어가 효과 반경 안에 있는가"만 판단해 알린다. 효과 컴포넌트들은 이 구역을 읽기만 한다.
    public class LandmarkZone : MonoBehaviour, ILandmarkPart
    {
        private LandmarkContext context;

        public bool IsInside { get; private set; }
        public float Radius => context != null ? context.Data.effectRadius : 0f;
        public float PlayerDistance { get; private set; } = float.MaxValue;
        public LandmarkData Data => context?.Data;

        public event Action Entered;
        public event Action Exited;

        public void Bind(LandmarkContext ctx)
        {
            context = ctx;
        }

        private void Update()
        {
            if (context == null || context.Player == null) return;

            PlayerDistance = Vector2.Distance(context.Player.position, transform.position);
            var inside = PlayerDistance <= context.Data.effectRadius;
            if (inside == IsInside) return;
            IsInside = inside;
            if (inside) Entered?.Invoke();
            else Exited?.Invoke();
        }

        private void OnDisable()
        {
            if (!IsInside) return;
            IsInside = false;
            Exited?.Invoke();
        }
    }
}
