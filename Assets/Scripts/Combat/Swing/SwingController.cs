using System;
using UnityEngine;

namespace ExpiryGame.Combat
{
    // 스윙 모션 담당: 손 피벗 기준 호 회전, 3단계(준비/스윙/여운). 판정은 ISwingHitSink에 위임한다.
    // 시간은 Time.deltaTime을 쓰므로 히트스톱 중에는 모션도 함께 멈춘다.
    public class SwingController : MonoBehaviour, IAttackDriver
    {
        [SerializeField] private SwingProfile profile;
        [Tooltip("IAimSource 구현 (AimSource)")]
        [SerializeField] private MonoBehaviour aimSource;
        [Tooltip("ISwingHitSink 구현 (SwingHitDetector)")]
        [SerializeField] private MonoBehaviour hitSink;
        [Tooltip("무기가 회전하는 중심(플레이어 손 위치). 이 오브젝트가 회전한다")]
        [SerializeField] private Transform handPivot;
        [Tooltip("handPivot 아래의 무기 스프라이트 (피벗 Bottom Center)")]
        [SerializeField] private SpriteRenderer weaponRenderer;
        [Tooltip("스윙 중 숨길 오브젝트 (머리 위에 든 음식 등, 선택)")]
        [SerializeField] private GameObject[] hideWhileSwinging = Array.Empty<GameObject>();

        private SwingTimeline timeline;
        private SwingInfo info;
        private Action<int> onCommitted;
        private float elapsed;
        private float prevRel;
        private int nextSign = 1;
        private bool strikeStarted;
        private bool committed;

        public bool IsBusy { get; private set; }
        public float CooldownSeconds => profile.cooldownSeconds;
        public float WeaponAngle { get; private set; }
        public Vector2 Origin => handPivot.position;
        public Vector2 WeaponTip => Origin + AngleToDir(WeaponAngle) * profile.reach;
        public SwingInfo Current => info;

        public event Action<SwingInfo> SwingStarted;
        public event Action<SwingInfo> StrikeStarted;
        public event Action<SwingInfo> SwingEnded;

        private void Awake()
        {
            timeline = new SwingTimeline(profile);
            weaponRenderer.enabled = false;
        }

        public void Begin(FoodItem item, Action<int> committedCallback)
        {
            if (IsBusy) return;
            IsBusy = true;
            onCommitted = committedCallback;
            strikeStarted = false;
            committed = false;
            elapsed = 0f;

            var origin = (Vector2)handPivot.position;
            var dir = ((IAimSource)aimSource).GetAimDirection(origin);
            var sign = profile.alternateDirection ? nextSign : 1;
            if (profile.alternateDirection) nextSign = -nextSign;

            info = new SwingInfo(item, origin, dir, sign);
            prevRel = timeline.StrikeStartRel;
            ((ISwingHitSink)hitSink).BeginSwing(info);

            weaponRenderer.sprite = item.Data.SpriteFor(item.IsRotten);
            weaponRenderer.transform.localScale = Vector3.one * profile.weaponScale;
            weaponRenderer.transform.localPosition = new Vector3(0f, profile.weaponHandOffset, 0f);
            weaponRenderer.enabled = true;
            foreach (var go in hideWhileSwinging) if (go != null) go.SetActive(false);

            Apply(SwingPhase.Windup, timeline.StrikeStartRel, 0f);
            SwingStarted?.Invoke(info);
        }

        private void Update()
        {
            if (!IsBusy) return;

            elapsed += Time.deltaTime;
            var phase = timeline.Evaluate(elapsed, out var rel, out var phase01);

            if (phase != SwingPhase.Windup && !strikeStarted)
            {
                strikeStarted = true;
                StrikeStarted?.Invoke(info);
            }

            if (phase == SwingPhase.Strike) SweepTo(rel);
            if (phase >= SwingPhase.Recovery && !committed)
            {
                SweepTo(timeline.StrikeEndRel);
                committed = true;
                onCommitted?.Invoke(((ISwingHitSink)hitSink).EndSwing());
            }

            if (phase == SwingPhase.Done)
            {
                Finish();
                return;
            }

            Apply(phase, rel, phase01);
        }

        private void SweepTo(float rel)
        {
            var from = Mathf.Clamp(prevRel, timeline.StrikeStartRel, timeline.StrikeEndRel);
            var to = Mathf.Clamp(rel, timeline.StrikeStartRel, timeline.StrikeEndRel);
            prevRel = rel;
            if (Mathf.Approximately(from, to)) return;
            ((ISwingHitSink)hitSink).Sweep(handPivot.position, info.AimAngle + info.Sign * from, info.AimAngle + info.Sign * to);
        }

        private void Apply(SwingPhase phase, float rel, float phase01)
        {
            WeaponAngle = info.AimAngle + info.Sign * rel;
            handPivot.rotation = Quaternion.Euler(0f, 0f, WeaponAngle - 90f);

            var alpha = 1f;
            if (phase == SwingPhase.Windup && profile.fadeInPortionOfWindup > 0f)
                alpha = Mathf.Clamp01(phase01 / profile.fadeInPortionOfWindup);
            else if (phase == SwingPhase.Recovery && profile.fadeOutPortionOfRecovery > 0f)
                alpha = 1f - Mathf.Clamp01((phase01 - (1f - profile.fadeOutPortionOfRecovery)) / profile.fadeOutPortionOfRecovery);
            weaponRenderer.color = new Color(1f, 1f, 1f, alpha);
        }

        private void Finish()
        {
            IsBusy = false;
            weaponRenderer.enabled = false;
            foreach (var go in hideWhileSwinging) if (go != null) go.SetActive(true);
            SwingEnded?.Invoke(info);
        }

        private void OnDisable()
        {
            if (IsBusy) Finish();
        }

        private static Vector2 AngleToDir(float degrees)
        {
            var r = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        }
    }
}
