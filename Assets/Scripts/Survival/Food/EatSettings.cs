using UnityEngine;

namespace ExpiryGame
{
    // 먹기 수치 전부. 길게 누르는 시간, 진행 바 모양/위치, 안내 문구.
    // 에셋이 연결되지 않았으면 Resources/ShelfLife/EatSettings 를, 그것도 없으면 이 기본값을 쓴다.
    [CreateAssetMenu(menuName = "ShelfLife/Eat Settings", fileName = "EatSettings")]
    public class EatSettings : ScriptableObject
    {
        public const string ResourcePath = "ShelfLife/EatSettings";

        [Header("길게 누르기")]
        [Tooltip("E를 이 시간(초) 동안 누르고 있으면 먹는다. 중간에 놓으면 즉시 취소. 0이면 누르자마자 먹는다")]
        [Min(0f)] public float holdSeconds = 0.4f;

        [Header("진행 바 (먹는 중에만 표시, 플레이어 머리 위)")]
        [Tooltip("플레이어 콜라이더 위쪽에서 더 띄우는 거리 (월드 유닛)")]
        public float headGap = 0.15f;
        [Tooltip("플레이어 콜라이더가 없을 때 pivot에서 바까지의 높이")]
        public float fallbackHeight = 0.9f;
        [Tooltip("바 크기 (월드 유닛). 작고 얇게")]
        public Vector2 barSize = new Vector2(0.6f, 0.07f);
        public Color barBackColor = new Color(0.05f, 0.06f, 0.08f, 0.75f);
        public Color barFillColor = new Color(0.95f, 0.78f, 0.25f, 1f);

        [Header("안내 문구 (먹는 중 바 위에 표시)")]
        [Tooltip("체력이 가득일 때 (먹어도 회복이 낭비된다)")]
        public string fullHealthNotice = "체력이 가득 찼어요";
        [Tooltip("썩은 음식일 때. {0} = 체력 변화량")]
        public string rottenNotice = "썩은 음식! HP {0}";
        [Tooltip("안내 문구 글자 높이 (월드 유닛)")]
        [Min(0.05f)] public float noticeFontSize = 0.16f;

        private static EatSettings fallback;

        // 연결된 에셋 → Resources 에셋 → 코드 기본값 순서
        public static EatSettings Resolve(EatSettings assigned)
        {
            if (assigned != null) return assigned;
            var loaded = Resources.Load<EatSettings>(ResourcePath);
            if (loaded != null) return loaded;
            if (fallback == null) fallback = CreateInstance<EatSettings>();
            return fallback;
        }
    }
}
