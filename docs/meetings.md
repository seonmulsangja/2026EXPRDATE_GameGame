# 회의록 / 결정 기록

요청에서 "막히면 서브에이전트 회의(설계/비판/일정)"를 열라고 했으나, 이번 작업은 막히는 지점이 없어 **서브에이전트 회의는 열지 않았다.**
대신 설계 상 갈림길 5개를 혼자 판단해 아래에 근거와 함께 기록한다. 이견이 있으면 바꿔도 되는 지점들.

## D1. 공격 흐름: FoodWeapon을 유지하고 판정/연출만 위임
- 선택지: (a) FoodWeapon 삭제 후 Combat이 입력부터 처리 (b) FoodWeapon이 `IAttackDriver`에 위임
- 결정: (b). `WeaponUsed`, 내구도 소모, 인벤토리 제거가 handoff 계약이라 한곳에 남기는 게 안전. Survival은 Combat을 참조할 수 없으므로(어셈블리 방향) 인터페이스를 Survival에 두고 Combat이 구현.
- `attackDriver`가 비어 있으면 기존 즉발 원형 판정 그대로 동작(하위 호환).

## D2. "기존 적중 이벤트" 구독 문제
- 기존 `WeaponUsed(item, hitCount)`는 스윙당 1회, 위치/대상 정보 없음 → 스파크/넉백/플래시에 부족.
- 결정: 대상별 `SwingHitDetector.HitLanded(HitInfo)`를 신설, `WeaponUsed`는 그대로 유지. 무기 파괴는 기존 `FoodItem.Broken`을 재사용(스윙 동안만 구독).

## D3. 내구도 소모 시점
- 스윙이 끝나는(타격 구간 종료) 시점에 1회. 여운 중에 파괴 연출이 나오고, 도중에 중단돼도 공짜 사용이 없도록 쿨다운은 시작 시점부터 계산.

## D4. 데미지→연출 강도는 한 곳
- `HitFeelProfile.IntensityFor(damage)`가 0~1 강도를 내고, 각 효과는 자기 `FloatRange(약함, 강함)`로 보간만 한다. 기준 데미지(3~25)는 FoodBalanceConfig 값에 맞춰 둔 SO 필드이므로 밸런스 변경 시 같이 조정.

## D5. 플래시는 전용 셰이더, 넉백은 MovePosition
- SpriteRenderer.color 곱셈으로는 "흰색 번쩍"이 안 돼서 `ShelfLife/SpriteFlash`(URP 2D unlit) 머티리얼 교체 방식 채택. 위험: 이 환경에서 셰이더 컴파일은 검증 불가 → 유니티 Console 확인 필요.
- 적 AI가 velocity를 매 프레임 덮어쓸 수 있어 넉백은 `MovePosition` 가산 + `IsKnockedBack` 플래그 제공.

## D51. 적이 안 나오는 문제: 증거 기반 진단 → 설치 누락 확정 + 방어/진단 영구화 (회의 기록)
- **진행 방식**: 서브에이전트 회의는 열지 않았다. 막히는 지점이 없었고(원인이 파일 증거로 확정됨), 결정할 설계 쟁점도 아래 두 가지뿐이라 설계/비판/일정 관점을 이 기록에서 직접 따졌다.
- **증거(디스크 기준)**: `EnemyChunkSpawner`/`DifficultyService`/`GameFlowRoot`의 스크립트 GUID를 가진 씬·프리팹이 **하나도 없다**(grep). 저장된 씬 5개 중 `ChunkStreamer`가 있는 건 ArtV2Test/DropMapTest뿐이고 거기에도 스포너·난이도·플로우는 없다. `Game.unity`는 git 이력에도 없고, `Enemy_Integrated.prefab`, `Data/Integration`, `DifficultyProfile`/`FlowSettings` 에셋도 없다. 즉 `Setup Game (All In One)`(= Flow 설치 + 적 설치)의 결과가 저장된 적이 없다 → 스포너가 씬에 없으니 스폰 시도 자체가 0회. 코드 경로(타이틀→Playing 전환, 곡선 값 0.35~1.9, 모닥불 반경 4.5 vs 청크 16, 스폰 거리 11)는 읽어서 막는 지점을 못 찾았다.
- **같은 계열 잠재 문제(코드로 막음)**: ① 스포너가 `OnEnable` 시점 이후에 청크가 로드된 경우만 처리 → 구독 전에 로드된 시작 청크는 영구히 빈다. ② `AnimationCurve`가 비면 `Evaluate`가 0 → 스폰/체력/속도 배율 0(적이 안 나오거나 안 움직임). ③ 필수 참조(프리팹·설정 등)가 null이면 조용히 아무 일도 안 함.
- **결정 1 (설계)**: 소급 처리는 `IChunkEvents.BoundsOf` + 확장 메서드 `ForEachLoaded`(MapGen)로. 스포너 로직을 새 클래스로 쪼개지 않고, 순수 계산만 `SpawnMath`/`SpawnDiagnostics`(Flow)로 빼서 Integration(asmdef 없음)에서도 테스트가 닿게 했다(D47과 같은 이유).
- **결정 2 (비판 → 반영)**: "배율이 0이면 최소 1마리 강제 스폰" 같은 임시방편은 거부. 대신 곡선이 비면 기본값으로 채우고(`DifficultyProfile.EnsureCurves`, 설치/OnValidate/Awake), 스폰 배율에 하한(`minSpawnMultiplier` 0.2)을 둔다. 하한은 곡선을 의도적으로 낮춘 경우에도 0이 되지 않게 하는 안전망이다.
- **진단(영구)**: `SpawnDiagnostics`가 막힘 이유별(상태 아님/정지/프리팹·참조 없음/난이도 0/상한/청크 없음/안전지대/너무 가까움/장애물/후보 없음)로 센다. F1 디버그 메뉴(`DebugMenu`, UNITY_EDITOR/DEVELOPMENT_BUILD)에 패널: 활성/최대 수, 스폰 배율, 직전 시도 결과, 이유별 카운터, 강제 스폰, 전체 정지 토글. `DebugTools`가 Assembly-CSharp의 스포너를 직접 못 보므로 `ISpawnDebug` 계약(Flow)으로 찾는다.
- **일정**: 위 수정은 코드만으로 끝난다. 씬에 실제로 스포너를 붙이는 건 Unity 메뉴라서 사용자가 `Tools > ShelfLife > Setup Game (All In One)` 한 번 실행하고 저장해야 한다(에디터가 열려 있어 배치 실행 불가).
- **검증 범위**: 에디터가 프로젝트를 열고 있어 테스트 러너/배치 모드를 못 썼다. Unity 번들 Roslyn으로 MapGen/Flow/DebugTools/Assembly-CSharp(Integration+Enemy)/`Tests.Spawn` 컴파일 에러 0까지만 확인. EditMode 테스트 실행은 하지 못했다.

## 검증 범위
- Unity 에디터가 이미 열려 있어 배치모드 실행을 하지 않았다. 전체 소스를 Unity 참조 DLL로 `dotnet build` 해서 C# 컴파일 에러 0개 확인. 씬/프리팹 생성 도구와 셰이더는 실행해 보지 못함.

---

# 드롭 + 무한 맵 작업 (2026-10-15)

이번에도 막혀서 멈춘 지점은 없어 **서브에이전트 회의(설계/비판/일정)는 열지 않았다.** 갈림길 8개를 혼자 판단해 근거와 함께 남긴다.

## D6. 기존 `FoodPickup`은 그대로 두고 `FoodDrop` 프리팹을 새로 만든다
- `FoodPickup`은 `FoodData`만 들고 있다가 닿으면 `TryAdd(food)`(신선한 새 FoodItem 생성)로 먹힌다. 드롭은 "유통기한이 이미 깎인 FoodItem"을 그대로 넘겨야 하고, 바닥에서도 감소/썩음 처리가 필요해서 성격이 다르다.
- 기존 씬의 고정 픽업(10종 줄 세운 것)이 깨지지 않도록 기존 클래스/프리팹은 건드리지 않았다. (요청의 "FoodPickup 프리팹"에 해당하는 신규 프리팹 이름은 `FoodDrop`.)

## D7. 기존 코드 수정은 2곳뿐
- `FoodItem.SetShelfLife01(ratio)` 추가: 남은 유통기한 비율을 지정해 만드는 유일한 방법. 스탯(회복/데미지/내구도)은 기존대로 최대 유통기한 기준. `IFoodFactory`는 안 바꿨다 (`Create` 후 호출).
- `FoodVisual.Refresh()`를 `public`으로: 프리팹은 씬의 `GameClock`을 참조할 수 없어서, 드롭이 clock을 주입받아 직접 갱신을 호출한다 (틴트/썩은 스프라이트 로직은 `FoodVisual` 그대로 재사용).
- 적 AI / UI / 조합 코드는 수정하지 않았다.

## D8. 적과의 연결: `IDropSource` + `DropSourceRegistry`
- 적이 `DropSpawner` 참조를 몰라도 되도록 얇은 정적 등록소를 둠 (`OnEnable`에서 Register, `OnDisable`에서 Unregister). 정적 상태라 `RuntimeInitializeOnLoadMethod`로 플레이 시작 때 초기화.
- `Health`를 쓰는 적은 `HealthDropSource`(어댑터)만 붙이면 끝 — `Health.Died`를 그대로 구독. 자체 사망 처리가 있는 적은 `IDropSource`를 직접 구현.
- 다른 선택지(씬 전체에서 `FindObjectsByType`로 찾기 / `DropSpawner.Instance`)는 느리거나 싱글톤이라 기각.

## D9. "강한 적일수록 강한 음식" 계산은 `DropTable.EffectiveWeight` 한 곳
- 음식의 강함 = 표 안에서 유통기한이 짧을수록 1 (spec §5: 짧을수록 데미지가 높다). 가중치 = `weight × bias^(threat × (2·strength − 1))`. `threatWeightBias`(기본 3)가 SO 필드. 적 쪽은 `Threat01`(0~1) 숫자만 준다.

## D10. 바닥 음식의 유통기한 감소 = 기존 `ShelfLifeSystem`에 Register
- 새 감소 로직을 만들지 않고, 드롭 시점에 `IShelfLifeTracker.Register`. 획득하면 인벤토리 `TryAdd(FoodItem)`가 다시 Register(중복 무시)하므로 이어서 감소한다. 소멸 시에만 `Unregister`.
- 썩은 뒤 소멸 타이머는 실시간(`Time.deltaTime`) 기준. 게임 시계 기준이 필요하면 `DroppedFood.Update`만 바꾸면 된다.

## D11. 월드 드롭은 콜라이더/물리 없이 거리 판정
- 포물선은 루트(지면 위치) + 자식 `Body`(높이만큼 위로) 분리라 그림자가 지면에 남는다. 획득은 `DropCollector`가 수집자와의 거리로 판정 (반경 안 → 끌림 → 닿으면 `TryAdd`). 가방이 가득 차면 끌려가지도 않고 바닥에 남는다.

## D12. 무한 맵 구조
- 생성(`ChunkGenerator` → 순수 데이터 `ChunkData`)과 반영(`ChunkTilemapView`, `ObstaclePool`)과 스트리밍(`ChunkStreamer`)을 분리. 생성은 유니티 오브젝트 없이 결정적이라 EditMode 테스트(`Assets/Tests/DropMap`)로 같은 청크 동일성 / 간격 / 시작 지점 비움을 검증한다.
- **최소 간격은 청크 경계에서도 지켜야 한다** → 이웃 청크 정보 없이 보장하려고 장애물을 청크 가장자리에서 `ceil(spacing/2)` 타일 안쪽에만 배치.
- `OnChunkLoaded`의 `ChunkRandom`은 struct라 구독자마다 값 복사로 독립된 같은 난수열을 받는다. 서로 다른 시스템이 같은 값을 뽑지 않도록 `Fork(salt)` 사용을 문서화.
- 바이옴: `IFloorRule` / `IObstacleRule` / `IBiome` / `IBiomeProvider`로 분리. 지금은 `DefaultBiome` + `SingleBiomeProvider`뿐. Rule Tile / BSP / WFC는 쓰지 않음.
- 언로드 여유(`unloadMargin`)는 기본 0(요청대로 5x5만 유지). 경계를 오가며 재로드가 잦으면 1로.

## D13. 테스트 씬은 복사본(`DropMapTest.unity`)에 설치
- `ShelfLifeTest.unity`가 이미 커밋 안 된 수정 상태라, 올인원 메뉴는 그 씬을 복사해 새 씬에 설치한다. 고정 아레나(Floor/Walls/Obstacles)는 삭제하지 않고 비활성화만.
- 카메라가 고정이면 무한 맵이 의미 없어서 `CameraFollow2D`를 `MapGen`에 최소로 추가 (`CameraShake`와 공존하도록 절대 위치를 덮어쓰지 않고 보간).

## D14. (선택 항목) 음식 상자
- 새 시스템을 만들지 않고 `IDamageable + IDropSource`를 구현한 `LootCrate`로 기존 드롭 경로를 재사용. `OnChunkLoaded`를 구독해 청크 난수에서 Fork한 값으로 확률/위치를 정하므로 같은 청크는 같은 자리에 상자가 생긴다.
- 이번 플레이 중 부순 상자는 메모리에서 기억해 재로드해도 되살리지 않는다. 저장은 하지 않음 (게임 재시작 시 초기화).

## 검증 범위
- 열려 있는 Unity 에디터가 자동 컴파일한 결과(Editor.log)로 C# 컴파일 에러 확인. 씬 설치 메뉴 / 플레이 동작 / EditMode 테스트 실행은 이 환경에서 직접 실행해 보지 못함 → handoff의 체크리스트 참고.

---

# 아트 v2 + HUD + 낮밤 + 랜드마크 작업 (2026-10-16)

이번에도 진행이 막히는 지점이 없어 **서브에이전트 회의(설계/비판/일정)는 열지 않았다.** 비용이 큰 갈림길 8개를 혼자 판단해 근거를 남긴다. 이견이 있으면 바꿔도 되는 지점들.

## D15. 냉장고 배율: `IDecayRateModifier` + 정적 `DecayRateRegistry`
- `ShelfLifeSystem`(Survival)은 Landmark를 참조할 수 없다(어셈블리 방향). 그래서 인터페이스/등록소를 Survival에 두고 `ShelfLifeSystem`은 틱마다 `Combined()` 곱만 읽는다. `DropSourceRegistry`와 같은 방식.
- 한계: 배율이 플레이어 위치 기준으로 **추적 중인 모든 음식**에 걸린다 (바닥 드롭 포함). 음식별 위치 판정은 `FoodItem`에 위치가 없어 하지 않았다.

## D16. 낮밤 구간(새벽/낮/황혼/밤)과 기존 Day/Night 페이즈는 별개
- 기존 `GameClock.Phase`는 `nightStartNormalized`로 Day/Night 둘뿐이고 게임 로직(UI 아이콘 등)이 쓴다. 시각용 4구간은 `DayNightProfile`이 `TimeOfDay01`을 직접 해석한다 (기존 값/이벤트만 구독, 시계는 수정 안 함).
- 기본값(황혼 0.45~0.55)이 `nightStart` 0.5와 맞물리게 잡았다. 다르게 쓰려면 둘을 같이 조절.

## D17. 낮밤 계산은 `DayNightDirector` 한 곳, 소비자는 구독만
- 타일 페이드/발광/비네트/모닥불 빛이 각자 시간을 읽으면 곡선이 어긋난다. `DayNightSample.Night01`을 단일 진실로 두고 `Changed`로 전달. 프리팹(씬 참조 불가)은 `DayNightState.Instance`.
- 렌더 파이프라인 확인 결과 URP 2D → Light 2D + URP Volume Vignette. 오버레이 방식은 쓰지 않음.

## D18. 랜드마크 효과는 `LandmarkZone` + 효과별 독립 컴포넌트, 서비스는 `LandmarkContext`로 주입
- 프리팹은 씬 오브젝트를 참조할 수 없어서 스폰 직후 `ILandmarkPart.Bind(context)`로 (플레이어/체력/`IDropService`/위험 이벤트) 받는다. 효과 컴포넌트는 서로 모른다.
- 편의점 상호작용 키(F)는 기존 입력 에셋을 건드리지 않으려고 `LandmarkInteractor`가 `InputAction`을 직접 가진다.

## D19. 랜드마크 배치는 순수 계산(`LandmarkPlanner`), 스폰은 청크 이벤트에 얹기
- 지역별 seed 계산이라 로드 여부와 무관하게 "가장 가까운 랜드마크"(UI 화살표/텔레포트)를 물을 수 있다. 스폰/언로드는 `IChunkEvents`(기존 계약)로. 장애물 비우기는 MapGen에 `IObstacleFilter`만 추가.
- 지역 경계 여유(`regionMargin` 8타일)가 비움 영역보다 커서 이웃 지역을 보지 않아도 겹치지 않는다. 시작 랜드마크는 모닥불(기본), `LandmarkSettings.startLandmark`로 변경.

## D20. HUD: 도트 3배 정수 배율 + 값이 바뀔 때만 갱신, 컨트롤러는 패널 밖
- 1080p에서 도트 1px = 3px(`UITheme.pixelScale`). 9-slice는 `pixelsPerUnitMultiplier = 1/3`로 보더 두께 유지. 텍스트는 `ThemedText`가 `UITheme.font`를 입힌다 (폰트 지정은 한 곳).
- 숨겨졌다 켜지는 패널(상세/상호작용)의 구독이 끊기지 않게 컨트롤러 컴포넌트는 캔버스 쪽에 둔다.
- 낮/밤 텍스트의 시간은 "현재 구간(낮/밤)이 끝나기까지 남은 시간"(`GameClock.NightStartNormalized` 노출 1줄 추가).

## D21. 디버그 빨리감기는 `Time.timeScale`이 아니라 `GameClock.SpeedMultiplier`
- 히트스톱이 `timeScale`을 만지므로 충돌하고, 물리/이동까지 빨라지면 테스트가 어렵다. 시계 배속은 낮밤과 유통기한 감소만 빠르게 한다. 시각 점프는 `JumpToTimeOfDay` + 0초 틱.

## D22. 한글 폰트는 임시 폰트로 연결, 교체 지점은 `UITheme.font` 하나
- TMP 기본 폰트에는 한글이 없어 네모로 보인다. 설치 시 OS의 맑은 고딕을 TMP 동적 폰트로 만들어 연결한다 (배포용 아님, 라이선스 때문에 픽셀 폰트로 교체 필요).

## 검증 범위
- Unity를 실행하지 못해(스킬/배치 모드 미사용) **asmdef 경계를 그대로 살려 어셈블리별 `dotnet build`**(Unity 6000.3.16f1 참조 DLL)로 C# 컴파일 에러 0 확인. 셰이더, 씬/프리팹 설치 메뉴, 플레이 동작, EditMode 테스트(`ArtV2Tests`)는 실행해 보지 못함.


---

# 후속 수정 (2026-10-16)

이번에도 막히는 지점이 없어 **서브에이전트 회의는 열지 않았다.** 갈림길 4개를 혼자 판단해 남긴다.

## D23. 음식 위치는 `FoodItem.Anchor`(Transform)로, 배율은 위치를 받는 인터페이스로
- `FoodItem`은 순수 클래스라 위치가 없다. 위치를 아는 쪽(가방=`FoodInventory`, 바닥=`DropSpawner`)이 `Anchor`를 설정하고, `ShelfLifeSystem`은 음식마다 `Combined(anchor.position)`만 호출한다. 바닥 음식이 주워지면 `TryAdd`가 Anchor를 플레이어로 바꾼다.
- `IDecayRateModifier`는 이름을 유지하고 프로퍼티를 `GetDecayRateMultiplier(Vector2)`로 바꿨다 (직전 작업에서 만든 것이라 외부 구현체 없음). Anchor가 없는 음식은 배율 없음.

## D24. 장식은 `IBiome.Decor`(기본 null) + MapGen 안의 `DecorLighting`
- `IBiome`에 멤버를 더하되 default interface member로 두어 기존 바이옴이 깨지지 않는다.
- MapGen은 낮밤을 모르므로 장식의 밤 처리는 `DecorLighting.Night01`을 World가 넣어 준다. 장식 수백 개가 매 틱 갱신되지 않게 1% 단위로만 전달. 충돌 없음, 풀링.

## D25. 디버그 메뉴는 릴리스에서 빈 껍데기 컴포넌트로
- 파일을 통째로 `#if`로 막으면 씬에 남은 컴포넌트가 Missing Script가 된다. 릴리스에서는 같은 이름의 빈 `MonoBehaviour`만 컴파일한다.

## D26. HUD 레이아웃 보존 대신 확인창
- HUD는 코드 생성(수백 개 참조)이라 재설치 시 부분 병합이 오히려 위험하다. 덮어쓰기 전 확인창을 띄우고, 새 씬에 설치하는 올인원은 묻지 않는다.

## 검증 범위
- 직전과 동일: asmdef별 `dotnet build` 에러 0. 씬/에디터 메뉴/플레이/EditMode 테스트(`ArtV2Tests` 8개)는 실행하지 못함.


---

# UI 정리 (2026-10-16)

막히는 지점이 없어 **서브에이전트 회의는 열지 않았다.** 첨부한 스크린샷은 세션에 전달되지 않아 코드/씬 기준으로 판단했다.

## D27. 지난번 UI 축소안은 적용돼 있지 않았다 → 이번에 구현
- 확인 결과: DetailPanel은 항상 표시(선택 슬롯 위, 가운데), 배율 3배, `MapDebugOverlay`는 `#if` 없음. 슬롯 번호 항상 표시와 `DebugMenu`의 `#if`만 이미 있었다.
- 적용: `pixelScale` 2 통일(좌표는 `UISetup.P()`가 3배 기준 값을 정수로 환산, 텍스트는 `ThemedText`가 `TextScale`로 환산), DetailPanel은 Tab 고정 표시 + 선택 변경/썩을 때 `detailPreviewSeconds`(2초) 미리보기, `MapDebugOverlay`는 에디터/Development Build에서만 컴파일(릴리스는 빈 껍데기, D25와 같은 이유).

## D28. DetailPanel은 우하단 구석
- 슬롯 바 바로 위는 1080p에서도 플레이어 아래 영역과 가까워 구석(우하단)으로 정했다. 더 이상 선택 슬롯 x를 따라가지 않아 `slotBar` 참조를 제거했다.

## D29. HUD 안전 영역은 `UITheme.hudZones`, 값은 UISetup이 실제 패널 크기로 기록
- 손으로 적은 숫자는 패널을 바꾸면 어긋나므로, 설치 때 패널 크기를 읽어 기록한다. `LandmarkArrowView`는 화살표+라벨 사각형이 영역(+`hudZoneGap`)과 겹치면 화면 가장자리를 따라 가까운 빈 자리로 민다. 자리가 없으면 원래 위치 유지.

## D30. "+N 예상"은 실제 회복(>0)일 때만
- 체력 가득/회복 0/피해일 때 텍스트는 숨긴다. 피해는 체력바의 붉은 구간(ghost)으로만 표시된다.

## D31. 버그: `LandmarkInteractor`가 Missing Script
- 클래스가 `Interaction.cs`에 있어(파일명 ≠ 클래스명) Unity가 스크립트로 연결하지 못했고, `InteractPromptView`가 NullReference를 냈다. `LandmarkInteractor.cs`로 분리하고 씬 참조를 새 guid로 고쳤다. (`DecorPool.cs`의 `DecorInstance`, `ObstaclePool.cs`의 `PooledObstacle`도 같은 형태지만 코드로 AddComponent 하는 컴포넌트라 두었다.)

## 상시 표시 UI 면적 (1920x1080 = 2,073,600 px²)
| 항목 | 수정 전 (3배) | 수정 후 (2배) |
|---|---|---|
| 체력 패널 | 570×138 = 78,660 | 380×92 = 34,960 |
| 시간 패널 | 444×171 = 75,924 | 296×114 = 33,744 |
| 슬롯 5개 | 5×144² = 103,680 | 5×96² = 46,080 |
| DetailPanel | 488×360 = 175,680 (상시) | 0 (Tab/2초 미리보기 때 325×240 = 78,000) |
| **상시 합계** | **433,944 (20.9%)** | **114,784 (5.5%)** |

## 중앙 영역 확인
- 플레이어 주변 반경 300px 사각형 (x 660~1260, y 240~840) 기준: 슬롯 바 최상단 y≈128, DetailPanel 왼쪽 끝 x≈1571, 체력/시간 패널은 모서리. 상시 UI는 이 영역에 없다.
- 예외: 상호작용 안내("F 열기", `InteractPrompt`)는 랜드마크 근처에서만 중앙 아래(-110px)에 뜬다. 상시 표시가 아니라 그대로 뒀다.

## 검증 범위
- `ExpiryGame.Landmark/UI/UI.Editor/DebugTools` `dotnet build` 에러 0. 릴리스 분기(`#else`)는 별도로 빌드하지 않았다. Unity 실행, 씬 재설치, 플레이 동작은 확인하지 못했다.


---

# 적 통합 + 시작 아이템 + 게임 흐름 (2026-10-16)

막혀서 서브에이전트 회의(설계/비판/일정)를 열 만한 지점은 없었다. 갈림길은 혼자 판단해 근거를 남긴다. (회의를 열지 않은 이유: 구조적 장애물은 어셈블리 경계 하나였고 Unity 규칙상 답이 하나뿐이었다 — D32.)

## D32. 팀원 코드(Assembly-CSharp)를 아는 코드는 asmdef 없이 `Integration/`에, 나머지는 인터페이스로 뒤집기
- 내 asmdef는 `Assembly-CSharp`를 참조할 수 없다. 팀원 폴더에 asmdef를 넣으면 팀원이 새 스크립트를 폴더 밖에 추가할 때 깨지고 참조를 전부 손봐야 한다 → 폴더 무수정.
- `EnemyAdapter`/`EnemyChunkSpawner`만 팀원 타입을 알고, 흐름 쪽(통계·BGM)은 `IEnemyActor`/`EnemyRegistry`(Flow), 드롭은 `IDropSource`(Drop)로만 본다. 설치기도 같은 이유로 `Integration/Editor`(Assembly-CSharp-Editor)에 두고, 올인원은 `EditorApplication.ExecuteMenuItem`으로 호출한다 (내 Editor asmdef는 autoReferenced=false라 직접 참조 불가).

## D33. 팀원 스포너/풀은 쓰지 않고 `Enemy` 프리팹만 가져온다
- `EnemySpawner_JCY`는 고정 포인트 + 타이머이고 `SpawnEnemy`가 풀에서 꺼낸 오브젝트를 다시 `Instantiate`한다(복제 버그). 청크 이벤트 방식과 맞지 않아 `EnemyChunkSpawner`를 새로 만들었다.
- 단 `Enemy.OnEnable`이 `EnemyPooling.Instance.enemyDatas`를 읽으므로 `EnemyPooling`은 `poolSize=0`으로 씬에 둔다(풀/스폰 로직은 돌지 않는다).

## D34. 피격·체력은 어댑터가 가진다 (팀원 `Enemy`에 피격 처리가 없음)
- 최신 `Enemy`에는 `TackDamage`도 사망 처리도 없다. `EnemyAdapter`가 `IDamageable`로 체력을 관리하고 죽으면 `IDropSource.Died` → 기존 `DropSpawner`가 드롭. 팀원 SO 수치(체력 3~15, 데미지 1)는 안 건드리고 `EnemyIntegrationSettings.healthScale/damageScale`로 이 프로젝트 단위(플레이어 체력 100)에 맞춘다.
- 접촉 데미지는 팀원 코드가 첫 1회, 어댑터가 `contactInterval`마다 반복 — 붙어 있으면 계속 맞아야 위협이 되기 때문.
- 프리팹은 원본을 건드리지 않고 **변형(Variant)** `Enemy_Integrated.prefab`에 컴포넌트를 얹는다.

## D35. 스폰 목표 수는 "청크당 기대값 × 난이도 배율"을 확률 반올림, 보충은 주기적으로
- 1일차 배율이 0.35라 청크당 0.42마리 → 정수 반올림하면 0마리가 된다. 소수부를 확률로 올려 평균이 맞게 했다. 청크 로드 순간만 스폰하면 가만히 있는 플레이어에게 아무것도 안 오므로 `topUpInterval`마다 부족한 청크를 채운다(플레이어 `minSpawnDistance` 밖, 장애물 없는 자리, 틱당 `spawnsPerTick`마리 상한).
- 청크 난수(`ChunkRandom`)는 쓰지 않는다: 적 위치까지 결정적일 필요가 없고(죽인 적이 같은 자리에 다시 나오는 쪽이 오히려 어색함) 보충 스폰은 어차피 청크 로드와 무관한 시점이다.

## D36. 게임 흐름은 씬 리로드로 초기화, 리로드를 넘는 값은 "타이틀 건너뛰기" 1비트
- 다시하기/타이틀로 = `SceneManager.LoadScene`(에디터에서 Build Settings에 없는 씬이면 `LoadSceneInPlayMode`). 살아 있는 객체·이벤트 구독이 전부 새로 만들어지는 가장 확실한 방법. 정적 상태는 `DropSourceRegistry`/`EnemyRegistry`(OnDisable에서 해제 + 도메인 리로드를 꺼도 `SubsystemRegistration`에서 초기화), `FlowLaunch.skipTitleOnce`뿐.
- 플레인 C# 객체(`StatsTracker`, `GameFlowController`, `FlowScreens`, `PlayerHurtFeedback`, `AudioEventBinder`)는 전부 `IDisposable`이고 `GameFlowRoot.OnDestroy`에서 해제한다. 단위 테스트 `StatsTracker_...StopsAfterDispose`가 해제를 확인한다.

## D37. `GameStateMachine`은 상태만, 시간/입력/화면/규칙은 각자
- `GameStateMachine`(상태·허용 전환) / `GameFlowController`(사망→GameOver, 목표 달성→Clear, 일시정지 토글, 종료) / `GameplayGate`(timeScale·플레이어 입력) / `FlowScreens`(상태→화면) / 각 View(`TitleView`, `PauseView`, `ResultView`×2, `DayBannerView`, `ControlsHintView`, `HurtVignetteView`) / `GameFlowRoot`(조립만).
- 일시정지는 `Time.timeScale=0` + `PlayerInputReader.enabled=false`. 히트스톱이 복구 시점에 timeScale을 1로 돌려 일시정지를 풀 수 있어서 `GameplayGate.Enforce()`가 매 프레임 확인한다(플레이 중의 0은 히트스톱이 정당하게 만든 것이라 건드리지 않는다).

## D38. "5일 생존" = 1일차 새벽에 시작해 6일차 새벽이 되는 순간 Clear
- 요구문 "마지막 날 새벽에 Clear"가 두 가지로 읽힌다(5일차 새벽 / 5일을 다 버틴 뒤). "5일 생존"을 기준으로 5일을 완주(=`DayStarted(6)`)하면 Clear로 했다. 5일차 새벽에 끝내고 싶으면 `SurvivalGoal.IsCleared`의 `dayStarted - 1`을 `dayStarted`로 바꾸면 된다(테스트 `SurvivalGoal_ClearsWhenTargetDaysCompleted`도 같이).
- 결과 화면의 "생존 일수"는 완료한 날 수(`DayCount-1`).

## D39. 난이도는 연속 곡선(AnimationCurve) + 밤 배율, 값은 `DifficultyProfile` 한 곳
- x = 경과 일수(0 = 1일차 새벽, 연속). 낮밤 경계에서 갑자기 변하지 않게 일차 곡선은 연속값, 밤은 별도 배율을 곱한다. 소비자는 `IDifficultyService` 하나만 본다(스포너·어댑터).

## D40. 시작 아이템은 복사본 FoodData로 유통기한 덮어쓰기
- 생선 `FoodData`는 30초인데 요구는 45초다. 원본 SO를 바꾸면 드롭 표(`DropTable.Strength01`)와 다른 곳이 같이 바뀐다 → `StartingLoadout`의 항목에 `shelfLifeSeconds`를 두고, 다를 때만 `FoodData` 복사본을 만들어 `TryAdd(FoodData)`로 지급한다. 스탯은 기존 `FoodFactory`가 그 값으로 계산한다.

## D41. 소리: 카탈로그 SO + 이벤트 바인더, 파일 없으면 무시
- `Assets/Audio`가 없다. `AudioCatalog`의 빈 칸은 건너뛰고, 설치기가 `Assets/Audio`의 파일 이름(day/night/danger/gameover, eat/hit/rot/pickup/break)을 보고 채운다. 먹기·타격만 팀원 효과음(`Assets/JCY/06 Sounds`)을 임시로 연결. BGM 선택은 상태 > 위험(플레이어 반경 내 적 수 ≥ 기준) > 밤 > 낮.

## 검증 범위
- Unity 에디터가 열려 있어(자동 새로고침 없음) 컴파일은 `dotnet build`로 어셈블리별 확인: `ExpiryGame.Flow`, `ExpiryGame.Flow.Editor`, `Assembly-CSharp`(팀원 + Integration + DOTween 모듈), `Assembly-CSharp-Editor`, `ExpiryGame.Tests.Flow`, 그리고 릴리스 분기(`UNITY_EDITOR` 미정의: Survival/Flow/Assembly-CSharp) 모두 에러 0.
- 실행하지 못함: 설치 메뉴, 플레이 동작, EditMode 테스트(`FlowTests`).


---

# 냉장고 / 모닥불 상호작용 (2026-10-17)

막혀서 서브에이전트 회의를 열 만한 지점은 없었다. 요구가 두 가지로 읽히는 곳은 아래처럼 혼자 정하고 근거를 남긴다. (회의를 열지 않은 이유: 모두 수치/문구 선택이라 SO 값 하나로 바꿀 수 있고, 구조상 막히는 곳은 없었다.)

## D42. 기존 랜드마크 패턴 그대로: 효과 = 독립 컴포넌트, 수치 = SO, 서비스 접근 = `LandmarkContext`
- 새 효과는 `FridgeLootEffect`, `CampfireEnhanceEffect`, `CampfireSafeZone`(각각 `ILandmarkPart` + 필요하면 `ILandmarkInteractable`). `LandmarkInteractor`(F)와 `InteractableRegistry`는 수정하지 않았다.
- 컨텍스트에 `Inventory`(`IFoodInventory`)와 `Events`(`ILandmarkEventSink`)만 추가. 이미 만든 에셋/프리팹은 `Install Landmarks To Open Scene`이 비어 있는 참조와 빠진 컴포넌트만 채운다(직접 고친 값은 유지).

## D43. 상호작용 거리는 `interactRange`, 효과 반경(`effectRadius`)이 아니다
- "반경 안에서 F"의 반경을 효과 반경(냉장고 5 / 모닥불 4.5)으로 읽으면 멀리서도 눌려 어색하다. 편의점과 같은 `LandmarkData.interactRange`(기본 2, 신규 에셋 2.5)를 쓴다. 헷갈리지 않게 이 필드의 헤더를 "상호작용 공통"으로 바꿨다.

## D44. 냉장고/모닥불은 "불가능해도 대상" — 프롬프트로 사유를 보여준다
- `LandmarkInteractor`는 `CanInteract == false`인 대상을 건너뛴다(편의점은 열고 나면 프롬프트도 사라짐). 요구가 "비어 있음 표시"와 "굽기 불가 사유 표시"라서, 냉장고(`fridgeEmptyPrompt`)와 모닥불은 범위 안이면 항상 대상으로 두고 `PromptText`만 상태에 따라 바꾼다. 누르면 아무 일도 안 일어난다.
- 프롬프트 문자열은 상태(선택 음식/사유/남은 초)가 바뀔 때만 다시 만든다(Interactor가 매 프레임 읽으므로).

## D45. 냉장고 수치 분리 = 전용 `DropTable_Fridge` + `LandmarkData.fridge*`
- 편의점: `DropTable_Store`(신선도 50~100%, 한 번에 2~3개 × `lootRolls` 3 = 6~9개, 다량). 냉장고: `DropTable_Fridge`(남은 유통기한 90~100%, 2~3개 × `fridgeRolls` 1). 표/굴림 수/위협도(`fridgeThreat01` 0)/빈 문구를 서로 다른 필드·에셋으로 둬서 한쪽을 만져도 다른 쪽이 안 변한다.
- 유통기한 감소 배율은 `decayEnabled`(체크박스) + 기존 `decayMultiplier`. 꺼도 드롭은 한다.
- "글로우 끔"은 프리팹의 `Glow` 자식 오브젝트를 비활성화(재방문 리셋은 편의점처럼 청크 재로드 때 허용).

## D46. 강화 = `FoodItem`의 플래그 + 배율, 변환 로직은 건드리지 않음
- `FoodStatCalculator`(유통기한→스탯)는 그대로. `FoodItem.CurrentHeal/CurrentDamage`가 신선할 때 값에 `HealMultiplier/DamageMultiplier`(기본 1)를 곱할 뿐이다. 썩은 값은 강화와 무관(강화된 음식이 나중에 썩어도 썩은 쪽 값은 그대로).
- `FoodItem.TryEnhance`가 "음식당 1회 / 썩은 음식 불가"를 최종 보장하고, `FoodEnhancer`(순수 클래스)가 같은 규칙으로 "가능 여부 + 사유 + 회복 변화량"을 미리 계산해 프롬프트에 쓴다. 쿨다운은 `CooldownTimer`(시간을 인자로 받음, 모닥불마다 하나).
- **"남은 유통기한 +50%"** = 남은 값 × 1.5, 음식의 최대 유통기한을 넘지 않는다(`shelfLifeBonusRatio` 0.5). 최대치의 +50%로 읽으면 거의 항상 가득 차서 의미가 없다. 바꾸려면 `FoodItem.TryEnhance` 한 줄.
- 쿨다운 기본 10초(요구에 값이 없어서 임의로 정함 → `EnhanceProfile.cooldownSeconds`). 쿨다운은 모닥불 인스턴스마다라 청크 재로드 시 리셋된다.
- 가방 안 음식이 강화되면 `FoodInventory`가 `Changed`를 다시 발행해 슬롯/상세 패널이 기존 구독만으로 갱신된다(UI 쪽 새 구독 없음).

## D47. 안전지대: 스폰은 "배치 정보", 밀어내기는 "로드된 모닥불"로 판단
- 스폰 제외: `EnemyChunkSpawner`가 청크가 생길 때 `LandmarkLocator`(순수 계산, 프리팹 로드와 무관)로 주변 모닥불을 받아 `SafeZoneSet`을 청크에 붙여 둔다. 보충 스폰과 위험 구역 추가 스폰(burst) 모두 이 집합으로 걸러낸다. 순수 클래스라 EditMode 테스트가 된다(`Integration`은 asmdef가 없어 테스트가 못 닿는다 → 로직을 Landmark 쪽에 둔 이유).
- 밀어내기: 이미 있는 적은 `EnemyAdapter`가 `SafeZoneRegistry`(로드된 모닥불 등록소)에서 자기 위치를 찾아 `IKnockbackable.ApplyKnockback`으로 민다. 팀원 `Enemy`는 수정하지 않았다. 간격(`repelIntervalSeconds`), 최소 거리(`repelDistance`), 켜고 끄기(`repelEnemies`, 기본 ON), 반경(`radius`)은 전부 `SafeZoneProfile` SO.
- 반경 링은 `LandmarkData.RingRadius`(안전지대가 있으면 그 반경)로 그리고, 모닥불 `ringTint`가 이미 주황이라 틴트는 그대로다.

## D48. 위험 구역(쓰레기 더미)과 겹치면 안전이 우선 — 플레이어가 안전지대 안일 때 위험 이벤트를 내지 않는다
- `DumpDangerEffect`가 진입/이탈 이벤트 대신 매 프레임 `zone.IsInside && !SafeZoneRegistry.IsSafe(player)`로 위험 상태를 계산하고, 바뀔 때만 `Enter/Exit`을 발행한다. 그러면 `DangerZoneEvents.IsPlayerInDanger`(스포너의 위험 배율)와 `OnDangerZoneEntered`(burst)가 둘 다 자동으로 따라온다. 음식이 쏟아지는 건 위험이 아니라 그대로 한다.

## D49. 연출/소리는 이벤트만: `ILandmarkEvents.OnFoodEnhanced / OnFridgeOpened`
- 모닥불/냉장고 코드는 연출을 모른다. `AudioEventBinder`가 `AudioCatalog.sfxEnhance/sfxFridgeOpen`을 재생(비어 있으면 무음), 새 `EnhanceFeedback`이 기존 `HitFxSpawner`의 스파크 파티클(`IHitFx.BurstParticles`)로 불꽃을 낸다(수치는 `FlowSettings`). 이벤트 허브가 씬에 없으면 그냥 연결하지 않는다.

## D50. 강화 배지: `UITheme` 스프라이트가 비면 임시 도형(마름모)
- `EnhanceBadge`가 슬롯 우상단/상세 패널 아이콘 오른쪽 아래에 작은 배지를 필요할 때 만든다 → HUD를 다시 설치하지 않아도 된다. 아트가 생기면 `UITheme.sprites.enhanceBadge`만 채우면 된다(`docs/art_needed.md`).

## 검증 범위
- Unity 에디터가 이 프로젝트를 열고 있어 배치 모드를 쓰지 못했다. `dotnet build`로 Survival/Landmark/UI/Flow/Assembly-CSharp(Integration 포함)/각 Editor/테스트 어셈블리 에러 0.
- 실행하지 못함: 설치 메뉴, 플레이 동작, EditMode 테스트(`FridgeCampfireTests` 13개를 만들고 컴파일만 확인).
