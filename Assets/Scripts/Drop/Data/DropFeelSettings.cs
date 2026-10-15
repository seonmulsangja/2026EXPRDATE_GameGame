using UnityEngine;

namespace ExpiryGame.Drop
{
    // 드롭 연출/획득/소멸 수치 전부. 코드에 숫자를 박지 않는다.
    [CreateAssetMenu(menuName = "ShelfLife/Drop Feel Settings", fileName = "DropFeelSettings")]
    public class DropFeelSettings : ScriptableObject
    {
        [Header("튕겨 나오기 (월드 단위 / 초)")]
        [Min(0f)] public float scatterMinDistance = 0.6f;
        [Min(0f)] public float scatterMaxDistance = 1.4f;
        [Min(0f)] public float arcHeight = 0.9f;
        [Min(0.05f)] public float arcDuration = 0.45f;
        [Tooltip("착지 후 한 번 더 튀는 높이 (arcHeight 대비 비율, 0 = 안 튐)")]
        [Range(0f, 1f)] public float bounceHeightRatio = 0.3f;
        [Min(0.05f)] public float bounceDuration = 0.2f;
        [Tooltip("공중에 있는 동안 그림자가 줄어드는 최소 비율")]
        [Range(0.1f, 1f)] public float shadowMinScale = 0.6f;

        [Header("반짝임 (착지 순간)")]
        public Sprite sparkleSprite;
        public Color sparkleColor = new Color(1f, 0.95f, 0.7f, 1f);
        [Min(0.05f)] public float sparkleDuration = 0.35f;
        [Min(0f)] public float sparkleStartScale = 0.4f;
        [Min(0f)] public float sparkleEndScale = 1.3f;

        [Header("획득")]
        [Tooltip("이 반경 안에 들어오면 끌려오기 시작")]
        [Min(0f)] public float magnetRadius = 2f;
        [Tooltip("이 반경 안이면 실제로 획득")]
        [Min(0.05f)] public float collectRadius = 0.45f;
        [Tooltip("반경 가장자리에서의 끌림 속도 (유닛/초)")]
        [Min(0f)] public float magnetMinSpeed = 1.5f;
        [Tooltip("획득 직전의 끌림 속도")]
        [Min(0f)] public float magnetMaxSpeed = 9f;
        [Tooltip("착지 후 획득 가능해질 때까지 대기 (바로 주워지는 것 방지)")]
        [Min(0f)] public float pickupDelay = 0.15f;

        [Header("바닥에서 썩은 뒤")]
        [Tooltip("썩고 나서 사라지기 시작할 때까지 (초)")]
        [Min(0f)] public float rottenLifetime = 15f;
        [Min(0.05f)] public float shrinkDuration = 0.6f;
    }
}
