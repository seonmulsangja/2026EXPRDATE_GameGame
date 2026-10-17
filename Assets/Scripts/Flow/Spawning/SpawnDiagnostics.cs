using System;
using System.Collections.Generic;

namespace ExpiryGame.Flow
{
    // 스폰이 막힌 이유. 카운터 하나가 이유 하나에 대응한다.
    public enum SpawnBlockReason
    {
        NotPlaying,     // 게임 상태가 Playing이 아님 (타이틀/일시정지/종료)
        Paused,         // 디버그 토글로 스폰 전체 정지
        NoPrefab,       // 설정 에셋/적 프리팹/필수 참조가 비어 있음
        DifficultyZero, // 난이도 배율로 계산한 목표 수가 0
        Cap,            // 살아 있는 적 수가 상한에 도달
        NoChunks,       // 로드된 청크가 하나도 없음
        SafeZone,       // 후보 위치가 모닥불 안전지대 안
        TooClose,       // 후보 위치가 플레이어에게 너무 가까움
        Obstacle,       // 후보 위치에 장애물/콜라이더가 있음
        NoCandidate,    // 한 청크에서 시도를 다 썼는데 쓸 만한 위치가 없음
        Grace,          // 게임 시작 직후 스폰 유예 시간
        OnScreen,       // 후보 위치가 카메라 화면 안
        TooFar,         // 후보 위치가 너무 멀어 플레이어가 만날 일이 없음
    }

    // 디버그 메뉴(F1)가 스포너를 들여다보고 조작하는 계약. 게임 로직은 이 인터페이스를 몰라도 동작한다.
    public interface ISpawnDebug
    {
        int ActiveEnemies { get; }
        int MaxEnemies { get; }
        int LoadedChunks { get; }
        bool Paused { get; set; }
        float SpawnMultiplier { get; }
        SpawnDiagnostics Diagnostics { get; }

        // 플레이어 근처에 적 한 마리를 강제로 내보낸다 (상태/정지/상한/안전지대 무시). 성공 여부 반환
        bool ForceSpawnNearPlayer();
    }

    // 스폰 시도/성공/막힘을 이유별로 센다 (순수 데이터, 유니티 의존 없음).
    public sealed class SpawnDiagnostics
    {
        private readonly int[] blocked = new int[Enum.GetValues(typeof(SpawnBlockReason)).Length];

        public int Ticks { get; private set; }
        public int Spawned { get; private set; }
        public string LastResult { get; private set; } = "(아직 시도 없음)";

        public int Blocked(SpawnBlockReason reason) => blocked[(int)reason];

        public int TotalBlocked
        {
            get
            {
                var sum = 0;
                foreach (var count in blocked) sum += count;
                return sum;
            }
        }

        public void RecordTick()
        {
            Ticks++;
        }

        public void RecordSpawn(string detail = null)
        {
            Spawned++;
            LastResult = string.IsNullOrEmpty(detail) ? "스폰 성공" : "스폰 성공: " + detail;
        }

        public void RecordBlocked(SpawnBlockReason reason, string detail = null)
        {
            blocked[(int)reason]++;
            LastResult = string.IsNullOrEmpty(detail) ? "막힘: " + Label(reason) : $"막힘: {Label(reason)} ({detail})";
        }

        public void Reset()
        {
            Array.Clear(blocked, 0, blocked.Length);
            Ticks = 0;
            Spawned = 0;
            LastResult = "(아직 시도 없음)";
        }

        public IEnumerable<KeyValuePair<SpawnBlockReason, int>> All()
        {
            foreach (SpawnBlockReason reason in Enum.GetValues(typeof(SpawnBlockReason)))
                yield return new KeyValuePair<SpawnBlockReason, int>(reason, blocked[(int)reason]);
        }

        public static string Label(SpawnBlockReason reason)
        {
            switch (reason)
            {
                case SpawnBlockReason.NotPlaying: return "상태가 Playing 아님";
                case SpawnBlockReason.Paused: return "디버그 정지";
                case SpawnBlockReason.NoPrefab: return "프리팹/설정 없음";
                case SpawnBlockReason.DifficultyZero: return "난이도 0";
                case SpawnBlockReason.Cap: return "상한 도달";
                case SpawnBlockReason.NoChunks: return "로드된 청크 없음";
                case SpawnBlockReason.SafeZone: return "안전지대";
                case SpawnBlockReason.TooClose: return "플레이어와 너무 가까움";
                case SpawnBlockReason.Obstacle: return "장애물";
                case SpawnBlockReason.NoCandidate: return "후보 위치 없음";
                case SpawnBlockReason.Grace: return "시작 유예";
                case SpawnBlockReason.OnScreen: return "화면 안";
                case SpawnBlockReason.TooFar: return "너무 멂";
                default: return reason.ToString();
            }
        }
    }

    // 청크당 목표 수 / 전체 상한 계산 (스포너에서 분리해 테스트 가능하게 둔다)
    public static class SpawnMath
    {
        // perChunk = 청크당 평균 목표 수. roll(0~1)이 소수 부분보다 작으면 하나 더 (평균이 perChunk가 되도록)
        public static int ChunkTarget(float perChunk, float roll)
        {
            if (perChunk <= 0f) return 0;
            var whole = (int)System.Math.Floor(perChunk);
            return whole + (roll < perChunk - whole ? 1 : 0);
        }

        // 게임 시작 후 경과 시간(플레이 시간)에 따른 동시 적 수 상한.
        // 유예 시간 동안은 0, 그 뒤 singleSeconds까지는 1마리, 그 후에는 일차별 상한(dayCap)
        public static int EarlyCap(float runSeconds, float graceSeconds, float singleSeconds, int dayCap)
        {
            if (runSeconds < graceSeconds) return 0;
            if (runSeconds < singleSeconds) return System.Math.Min(1, dayCap);
            return dayCap;
        }

        // 카메라가 보는 사각형(중심, 절반 크기)을 margin만큼 넓힌 영역 안인가
        public static bool IsOnScreen(float x, float y, float camX, float camY, float halfWidth, float halfHeight, float margin)
        {
            return System.Math.Abs(x - camX) <= halfWidth + margin && System.Math.Abs(y - camY) <= halfHeight + margin;
        }

        public static int Cap(int maxAlive, int hardCap, float multiplier)
        {
            var scaled = (int)System.Math.Ceiling(maxAlive * System.Math.Max(0.5f, multiplier));
            return System.Math.Min(hardCap, scaled);
        }
    }
}
