using UnityEngine;

namespace ExpiryGame.Flow
{
    // 게임 흐름 화면의 문구와 연출 수치. 폰트/색은 UITheme.
    [CreateAssetMenu(menuName = "ShelfLife/Flow/Flow Settings", fileName = "FlowSettings")]
    public class FlowSettings : ScriptableObject
    {
        [Header("문구")]
        public string gameTitle = "유통기한 서바이벌";
        public string controlsLine = "WASD 이동 · 좌클릭 휘두르기 · E(길게) 먹기 · 2 다음 음식";
        public string startLabel = "시작";
        public string quitLabel = "종료";
        public string resumeLabel = "재개";
        public string toTitleLabel = "타이틀로";
        public string retryLabel = "다시하기";
        public string pausedTitle = "일시정지";
        public string gameOverTitle = "게임 오버";
        public string clearTitle = "클리어!";
        public string lastDayLabel = "마지막 날";

        [Header("결과 화면 줄 ({0}에 값)")]
        public string daysLine = "생존 일수  {0}일";
        public string killsLine = "처치 수  {0}";
        public string eatenLine = "먹은 음식  {0}개";
        public string rottenLine = "썩은 음식 먹은 횟수  {0}";
        public string timeLine = "플레이 시간  {0}";
        public string scoreLine = "점수  {0}";

        [Header("조작 힌트 (첫 실행만)")]
        public string[] hintLines =
        {
            "2 : 다음 음식 선택 (슬롯 1~3)",
            "E (길게) : 먹기",
            "좌클릭 : 휘두르기",
        };
        [Min(1f)] public float hintSeconds = 10f;
        public string hintPrefsKey = "ShelfLife.ControlsHintShown";

        [Header("일차 배너")]
        [Min(0.5f)] public float dayBannerSeconds = 2.5f;

        [Header("피격 연출")]
        public Color hurtColor = new Color(0.85f, 0.05f, 0.05f, 1f);
        [Range(0f, 1f)] public float hurtMaxAlpha = 0.65f;
        [Tooltip("체력 비율 대비 데미지가 이 값 이상이면 최대 강도")]
        [Range(0.01f, 1f)] public float hurtFullIntensityRatio = 0.2f;
        [Min(0.1f)] public float hurtFadeSeconds = 0.6f;
        [Min(0f)] public float hurtShakeAmplitude = 0.25f;
        [Min(0.01f)] public float hurtShakeSeconds = 0.2f;
        [Min(0.01f)] public float hurtFlashSeconds = 0.12f;

        [Header("모닥불 강화 연출 (기존 스파크 파티클 재사용)")]
        [Min(0)] public int enhanceSparkCount = 14;
        [Min(0.1f)] public float enhanceSparkSpeedScale = 0.7f;
        [Range(0f, 180f)] public float enhanceSparkSpread = 70f;
        [Tooltip("모닥불 발밑 기준 불꽃이 나오는 높이 (타일)")]
        [Min(0f)] public float enhanceSparkHeight = 0.5f;
    }
}
