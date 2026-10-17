using UnityEngine;

namespace ExpiryGame
{
    public enum EatHoldEvent
    {
        None,
        Started,
        Canceled,
        Completed
    }

    // "길게 눌러 먹기"의 순수 상태 머신. 시간/입력을 인자로 받으므로 EditMode 테스트가 된다.
    //  - 누르는 동안 진행도가 차고, holdSeconds에 닿으면 Completed
    //  - 중간에 놓거나 대상이 사라지면 즉시 Canceled (진행도 0)
    //  - 완료 뒤에는 키를 한 번 놓아야 다시 시작한다 (누른 채로 다음 음식이 연달아 먹히지 않게)
    public sealed class EatHoldTimer
    {
        private float elapsed;
        private bool needRelease;

        public bool IsActive { get; private set; }
        public float Progress01 { get; private set; }

        public EatHoldEvent Tick(float deltaTime, bool held, bool hasTarget, float holdSeconds)
        {
            if (!held) needRelease = false;

            if (IsActive)
            {
                if (!held || !hasTarget)
                {
                    Reset();
                    return EatHoldEvent.Canceled;
                }

                elapsed += deltaTime;
                Progress01 = holdSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / holdSeconds);
                if (Progress01 < 1f) return EatHoldEvent.None;

                Reset();
                needRelease = true;
                return EatHoldEvent.Completed;
            }

            if (!held || !hasTarget || needRelease) return EatHoldEvent.None;

            if (holdSeconds <= 0f)
            {
                needRelease = true;
                return EatHoldEvent.Completed;
            }

            IsActive = true;
            elapsed = 0f;
            Progress01 = 0f;
            return EatHoldEvent.Started;
        }

        private void Reset()
        {
            IsActive = false;
            elapsed = 0f;
            Progress01 = 0f;
        }
    }
}
