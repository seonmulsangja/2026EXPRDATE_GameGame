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

## D81. 팀원 증강 연결: 막힘 없음 → 회의 소집 없이 리드 결정 (D78 번복 포함)
- 조사 결과 막히는 결정은 없었다(설계 선택지는 아래 D82~D86에 근거와 함께 기록). 그래서 서브에이전트 회의(설계/비판/일정)는 열지 않았다. 비판 관점은 각 항목의 "대가"로 적었다.
- **D78 번복**: 당시엔 "팀원이 완료 이벤트를 달면 어댑터를 만든다"며 폴링/리플렉션 어댑터를 보류했다. 이번 요청은 팀원 파일을 수정하지 않고 지금 연결하는 것이라, 우회 어댑터를 만든다. 대신 리플렉션은 한 클래스(`TeammateAugmentChooser.ManagerAccess`)에 가두고, 필드가 없으면 에러 로그 + 대체 보상으로 진행한다. 팀원이 이벤트를 달면 그 클래스만 지우면 된다(`connect_todo.md` T5).

## D82. 어셈블리 경계: Flow는 팀원 타입을 모르고, Integration만 안다
- Flow에 `AugmentDefinition`(팀원 `AugmentSO`를 옮긴 데이터), `IAugmentChooser`(화면 열기), `IAugmentEffects`, `IAugmentPresenterConsumer`, `IAugmentDebug` 계약을 두고, `Integration/`에 `TeammateAugmentCatalog/Chooser/Presenter`만 둔다. 대가: 클래스가 늘지만 후보 부족·에러 복구·중첩·초기화를 팀원 코드 없이 EditMode 테스트로 검증할 수 있다.
- 능력치 파이프라인은 없던 것이라 새로 만들었다: `StatKey`(AttackPower/MoveSpeed/AttackCooldownReduction), `PlayerStatSheet`(기본값+수정자), `PlayerStatApplier`(PlayerMover/FoodWeapon에 씀). 수정자는 (source,key,op)당 하나라 같은 증강 중첩은 값만 올라간다(레벨 N → perLevel×N, 팀원 규칙과 같음).

## D83. 팀원 `AugmentState`는 플레이어에 붙이지 않는다
- 이 컴포넌트는 `Awake`에서 이동 속도·쿨다운(감소량을 쿨다운 값으로 잘못 넘김)을 직접 덮어써 우리 파이프라인과 충돌한다. 별도 오브젝트 `AugmentBackend (JCY)`에 두고 매니저의 기록용으로만 쓴다. 대가: 스킬 증강 선택 시 팀원 경고 한 줄(무해, T6). 대안(플레이어에 붙이고 매 프레임 덮어쓰기)은 두 곳이 같은 값을 쓰는 경쟁이라 버렸다.

## D84. 후보는 우리가 뽑고 팀원 매니저에는 그 후보만 넣어 연다
- 팀원 규칙 그대로(최대 레벨 제외, 스킬은 스킬 최대 레벨도, 3개, 중복 없음) 우리가 뽑아야 "첫 번째 후보" 복구와 디버그 강제 지정이 된다. 완료는 후보의 `GetAugmentLevel` 상승으로, 선택 없이 닫힘은 패널 비활성으로 감지한다(Update라 timeScale 0에서도 동작). 자동 선택 타이머는 없다.
- 예외: 후보 없음 → `NullAugmentPresenter`(체력 회복) / 화면 에러·선택 없이 닫힘 → 에러 로그 + 첫 후보 적용 후 재개 / 첫 후보도 없으면 대체 보상. 전부 `GuardedAugmentPresenter`에 모았다.

## D85. 큐는 Playing을 거치지 않고 이어 가고, 선택 중 사망은 게임 오버로 끝낸다
- `LevelUpCoordinator`: 큐가 남았으면 `AugmentSelect`를 유지한 채 다음 선택을 열어 중간 재개(다른 구독자가 Playing 전환을 보는 것)가 없다. `Fail()`은 AugmentSelect에서도 가능(기존 테스트의 "선택 중엔 Fail 불가"를 바꿈): 같은 프레임에 처치 경험치와 접촉 피해가 겹치면 죽은 채로 선택을 마치고 재개되는 문제가 있었다. 사망 시 열린 선택 화면은 `Cancel`로 접는다. 선택 중에는 timeScale 0(물리 접촉 피해 없음).

## D86. 수치 변환 배율과 공격력 경로
- 팀원 SO의 `value`(이동 1, 공격 속도 1)가 설명 문구(0.5, 0.025)와 달라 SO를 못 고치므로 `AugmentIntegrationSettings`의 배율로 설명에 맞췄다(기본 0.5/0.025). 팀원이 SO를 고치면 배율을 1로.
- 공격력은 팀원이 `FoodWeapon` 즉발 경로에만 넣어 실제 스윙 경로에서는 무효였다 → `IDamageBonusReceiver`로 스윙 피해에 더한다.
- 팀원 씬 미배선(루트 비활성, `slots`/`allAugments` 비어 있음)을 설치 메뉴가 프리팹 사본에서 고친다. 팀원 씬은 열어서 읽기만 하고 저장하지 않는다.

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


---

# 넉백 약화 + 먹기 진행 바 (2026-10-17)

서브에이전트 회의는 열지 않았다. 막히는 지점(원인이 코드로 확정되지 않거나 구조가 충돌하는 곳)이 없었고, 갈림길은 모두 SO 값 하나로 되돌릴 수 있는 선택이라 아래처럼 근거를 남기고 직접 정했다.

## 넉백 원인 조사 (수정 전 코드 기준 계산값)
적이 밀리는 경로는 아래 5개다. 거리 = 총 이동(타일), 속도 = 순간 최대 속도, 시간 = 밀리는 동안(`IsKnockedBack`, 이 동안 적 AI가 멈춘다).

| # | 경로 | 힘(거리) | 순간 속도 | 시간 | 비고 |
|---|---|---|---|---|---|
| 1 | 휘두르기 적중 → `KnockbackReceiver` 임펄스 (`HitFeelProfile.knockbackDistance` 0.3~1.4) | 데미지 3 → 0.3, 10 → 0.63, 25 → 1.4 | 거리 × damping 12 = 3.6 ~ **16.8** | 속도 0.3 이하까지 ln(v0/0.3)/12 = 0.24 ~ **0.34초** | `velocity +=`라 **연속 피격이 상한 없이 누적**(0.1초 안에 두 번 맞으면 최대 2.8타일) |
| 2 | 모닥불 안전지대 밀어내기 (`EnemyAdapter.RepelFromSafeZone`) | `max(repelDistance 1.5, 경계에서 안쪽 깊이 + 0.25)` = 1.5 ~ **4.75** (반경 4.5) | 거리 × 12 = 18 ~ **57** | 0.4 ~ 0.5초 | 0.25초마다 반복. **타격 넉백과 같은 리시버·같은 damping**을 써서 모닥불 근처에서 때리면 "1타일 + 1.5타일 이상"이 연달아 붙는다 |
| 3 | `Rigidbody2D` 충돌 (적 프리팹: Dynamic, 질량 1, 선형 드래그 0, 중력 0, 회전 고정) | 적 이동 속도(SO 3~10) 만큼의 접촉 충격 | 3 ~ 10 | `Enemy.Update`가 매 프레임 `linearVelocity`를 덮어써서 **다음 프레임에 사라짐** | 드래그가 0이라 덮어쓰기 전 한 프레임은 유지. 주원인은 아님. 다만 `MovePosition`(경로 1·2)이 다른 적(질량 1)에 막히면 그 적을 밀어 **이웃이 같이 튀는** 현상이 생김 |
| 4 | 플레이어 반동 (`SwingProfile.recoilDistance`) | 0.18 | 2.2 | 0.09초 | 플레이어에게만. 약함 |
| 5 | 적 → 플레이어 접촉 (`Enemy.OnCollisionEnter2D`/Stay) | 플레이어 질량 1 vs 적 속도 3~10 | 적 속도 | 접촉 동안 | 플레이어가 밀리는 물리. 적을 미는 힘은 아님 |

**주원인**: 1번이다. 약한 음식(데미지 10 안팎)도 0.6타일을 초당 7.6타일로 밀고 0.27초 동안 AI를 멈추게 하고, 센 음식은 1.4타일을 초당 16.8타일로 민다. 거기에 **모닥불 근처에서는 2번이 이어붙어** 가장 세게 느껴진다(최대 4.75타일, 초당 57타일). 상한도 연속 피격 감쇠도 없었다(누적).

## D52. 넉백 수치는 `KnockbackProfile`(SO) 하나로, 타격/안전지대는 에셋 두 개
- `Assets/Resources/ShelfLife/Knockback_Hit.asset`(타격)과 `Knockback_SafeZone.asset`(밀어내기). 둘 다 같은 SO 클래스라 항목(거리/지속시간/감속 곡선/데미지 곡선/상한/연속 피격 감쇠/최대 속도/경직)이 같다. 안전지대는 값만 훨씬 약하고 느리다(거리 1.5~5 상한, 0.6초, 최대 속도 3타일/초, 경직 0, 감쇠 없음).
- **Resources 폴더에 둔 이유**: `HitFeedbackPlayer`/`KnockbackReceiver`/`FoodConsumer`는 Player 프리팹이 아니라 4개 씬의 Player 인스턴스에 설치기가 얹은 오버라이드라, 연결하려면 씬 YAML을 일일이 고쳐야 한다. 연결이 비어 있으면 `Resources/ShelfLife/…` → 코드 기본값 순으로 찾게 해서 **설치 메뉴를 다시 돌리지 않아도 바로 적용**된다. 설치기(`CombatSetup`, `EnemyIntegrationInstaller`)는 같은 에셋을 인스펙터 칸에도 연결해 준다(`HitFeedbackPlayer.hitKnockback`, `KnockbackReceiver.defaultProfile`, `EnemyIntegrationSettings.safeZoneKnockback`).
- 기본값(요구 반영): 기본 거리 0.3 + 데미지 보너스 최대 0.3 → 한 대 맞으면 **0.3~0.6타일**, 상한 `maxDistance` 1타일, 지속 0.1초, ease-out 곡선, 연속 피격 윈도우 0.6초 / 배율 0.6씩 / 최저 0.2, 경직 0.15초, 순간 속도 상한 14타일/초.

## D53. 움직임 계산은 순수 클래스 `KnockbackMotion`, 리시버는 물리 적용만
- 리시버(God Class 방지): 계산(거리·감쇠·상한·속도·경직)은 `KnockbackMotion`, 리시버는 변위를 `MovePosition`으로 옮기고 `Rigidbody2D.Cast`로 장애물만 판단한다. 계산 쪽이 물리를 몰라서 EditMode 테스트가 된다(막힘은 `clip` 콜백으로 주입).
- **누적 방지**: 예전엔 속도를 더했다. 이제는 "남은 거리 + 새 거리"를 `maxDistance` 안에서 합쳐 한 번의 움직임으로 다시 만든다(방향은 거리 가중 평균).
- **속도 제한**: 곡선의 최대 기울기로 순간 속도를 계산하고, `maxSpeed`를 넘으면 **시간을 늘린다**(거리는 유지). 안전지대 밀어내기가 "천천히" 움직이는 것도 이 규칙에서 나온다(1.5타일 → 약 1초).
- **장애물/다른 적**: 매 스텝 이동 방향으로 `Cast`해서 닿기 전(skin 0.02)까지만 가고, 막히면 거기서 넉백을 끝낸다(경직은 유지). 멀어지는 쪽에 붙어 있는 물체는 무시한다(법선·방향 내적) — 안 그러면 플레이어와 맞닿은 적이 플레이어 반대쪽으로도 못 밀린다.

## D54. 경직 = `IsKnockedBack`을 "밀림 + 경직"으로 확장, 접촉 데미지도 막는다
- `IKnockbackable.IsKnockedBack`이 경직 시간(0.15초) 동안도 true라서 `EnemyAdapter`가 쓰던 기존 규칙(이동 속도 0)이 그대로 경직이 된다. 추가로 `OnCollisionStay2D`의 접촉 데미지 반복도 경직 중에는 건너뛴다(즉시 반격 방지). 팀원 `Enemy.OnCollisionEnter2D`의 첫 접촉 1회는 팀원 파일이라 건드리지 않았다 — 경직 중에는 속도가 0이라 새로 접촉(Enter)하지 않는다.
- 히트스톱/카메라 흔들림/플래시/파티클은 코드 한 줄도 바꾸지 않았다(`HitFeelProfile`에서 `knockbackDistance` 필드만 `KnockbackProfile`로 이사). 넉백 거리만 줄이고 경직 0.15초로 "맞은 느낌"을 보존한다.

## D55. 저항값은 EnemySO에 없으면 0 — 팀원 파일은 수정하지 않는다
- 요구는 "적 SO/프리팹마다 `knockbackResistance`(0~1)를 어댑터에서 읽기, 없으면 0". 팀원 `EnemySO`는 수정 금지라 필드가 아직 없다. `EnemyAdapter`가 (1) 풀의 `EnemySO` 중 이름이 같은 것에 **`knockbackResistance`라는 public float 필드가 있으면** 그 값 → (2) 없으면 어댑터의 프리팹 값(`knockbackResistance`, 기본 0)을 쓰고, 리시버(`IKnockbackable.Resistance`)에 넣는다. 팀원이 SO에 같은 이름 필드를 추가하면 코드 수정 없이 바로 먹는다.
- 효과: 거리 × (1 − 저항), 1이면 이동도 경직도 없다.

## D56. 먹기: 입력은 "누르고 있나"만, 시간은 `EatSettings`
- 기존 E는 입력 에셋(`InputSystem_Actions`의 Interact)에 **Hold 인터랙션(기본 0.4초)** 이 걸려 있어 시간을 SO로 못 바꾸고 진행 표시도 없었다. 입력 에셋은 건드리지 않고 `IPlayerInput.EatHeld`(= `action.IsPressed()`, 인터랙션과 무관하게 눌린 상태)를 추가해 `FoodConsumer`가 `EatHoldTimer`(순수 상태 머신)로 직접 센다. `EatPerformed` 이벤트는 그대로 두었다(더 이상 먹기에 쓰지 않음).
- `EatSettings.holdSeconds` 기본 0.4초(0이면 누르자마자 먹기). 중간에 놓으면 같은 프레임에 취소(진행도 0), 먹는 중 선택 음식이 바뀌어도 취소, 완료 뒤에는 한 번 놓아야 다음 음식을 먹는다(누른 채 연달아 먹는 사고 방지).

## D57. 진행 바: 월드 좌표 캔버스를 머리 위에, 먹는 중에만
- `EatProgressView`(UI)가 `FoodConsumer`의 `EatStarted/EatCanceled/FoodEaten`만 구독한다. 화면 고정 HUD가 아니라 월드 좌표 캔버스(플레이어 콜라이더 윗면 + `headGap`)라 하단 패널과 겹칠 수 없다. 바 크기는 기본 0.6 × 0.07타일(작고 얇게), 수치는 전부 `EatSettings`.
- 플레이어가 좌우 반전(scale.x < 0)돼도 바가 뒤집히지 않게 플레이어의 자식이 아니라 별도 루트가 따라다닌다. 씬에 손으로 붙이지 않아도 `FoodConsumer`가 있으면 씬 로드 직후 자동으로 붙는다(`RuntimeInitializeOnLoadMethod`). 4개 씬을 일일이 고치지 않으려는 선택.
- **제거한 오브젝트: 없음.** 코드와 `Game.unity`/`Player.prefab`을 모두 찾았지만 먹기 진행 바(채움 이미지, 빈 프레임)는 처음부터 없었다 — E의 Hold는 화면에 아무것도 그리지 않았다. 이름이 비슷한 `EatIcon`/`EatText`는 상세 패널 안의 "먹으면 HP +N" 줄이고, `HpBar`/`TimeBar`/`SlotBar`는 각자 HUD라 겹침이나 빈 프레임으로 볼 근거가 없어 그대로 뒀다.

## D58. "먹기 가능한 상태" 안내 문구 = 막지 않고 이유만 보여준다
- 요구 문장("체력이 가득이거나 썩은 음식이면 안내 문구로 이유를 표시")이 "먹지 못하게 하라"인지 "먹어도 아쉬우니 알려라"인지 모호했다. 썩은 음식을 먹으면 피해를 입는 것이 기존 핵심 규칙(handoff §1)이고, 체력 가득에서 먹기를 막으면 음식 소비가 막혀 기존 흐름이 바뀐다. 그래서 **먹기는 막지 않고**, 먹는 동안 바 위에 사유를 띄운다: 썩음 → "썩은 음식! HP -N"(썩음이 체력 가득보다 우선), 체력 가득 → "체력이 가득 찼어요". 문구는 `EatSettings`. 막고 싶으면 `FoodConsumer.Update`에서 `CurrentWarning`으로 `hasTarget`만 false로 만들면 되는 한 줄이다.

## 검증 범위
- Unity 에디터가 프로젝트를 열고 있어 테스트 러너를 못 썼다. `dotnet build`로 Survival / Combat / UI / Flow / Assembly-CSharp(Integration 포함) / Assembly-CSharp-Editor / Combat.Editor / `Tests.Knockback`·`Tests.Spawn`·`Tests.Flow` 에러 0. 테스트 `KnockbackEatTests`(17개)는 만들고 컴파일만 확인했고, 핵심 수치(곡선 기울기, 시간, 순간 속도)는 같은 식을 파이썬으로 따로 계산해 단언 범위 안인지 확인했다.
- 병행 중이던 다른 작업(`Landmark/Fuel`, `CampfireAppearance`, `ILandmarkEventSink.CampfireStageChanged`)이 중간 상태라 `ExpiryGame.Landmark`가 그대로는 컴파일되지 않았다. 내 변경과 무관하므로 건드리지 않았고, 검증할 때만 생성된 csproj에서 그 파일을 뺐다(`Tests.Campfire`는 같은 이유로 컴파일 에러가 남아 있다).
- 실행하지 못함: 플레이 동작, 직접 만든 `.asset` YAML의 임포트(에디터가 열면 확인), `EatHeld`가 Hold 인터랙션과 함께 기대대로 `true`가 되는지(`InputAction.IsPressed`).


---

# 텍스트 박스 맞춤 + 모닥불 연료 (2026-10-17)

**서브에이전트 회의(설계/비판/일정)는 열지 않았다.** 막혀서 멈춘 지점이 없었다(원인이 코드로 확정됨, 구조는 기존 패턴 재사용). 갈림길 10개를 근거와 함께 남긴다. 같은 작업 트리에서 넉백/먹기 바(D52~D58) 작업이 병행 중이어서 그쪽 파일은 건드리지 않았다.

## D59. 글자 넘침의 원인 = 고정 크기 패널 + 줄바꿈 없는 고정 폭 라벨
- 확인: `UISetup.BuildPrompt`가 패널을 `P(260)×P(66)`(pixelScale 2 → 173×44 px), 라벨을 `P(240)×P(50)` / `NoWrap` / `Overflow`로 만든다. "F  굽기 불가 (이미 강화됨)"는 15자 × 약 19px ≈ 285px → 박스보다 훨씬 길어 테두리를 넘는다.
- 전수 점검한 동적 텍스트와 처리: InteractPrompt(→ `AutoSizePanel`), 음식 상세 패널 행(→ `AutoWidthPanel` + 줄임), 슬롯 숫자·체력/시간 숫자·일차·페이즈·미리보기·랜드마크 라벨(→ `ThemedText` 줄임, 라벨은 폭도 글자에 맞춤), 타이틀/게임오버/클리어/일시정지/버튼/일차 배너/컨트롤 안내(→ `FlowUi.Text` 줄임).

## D60. "늘어나는 박스"와 "줄이는 글자"를 구분한다
- 안내 박스·상세 패널은 글자 쪽이 가변이므로 박스를 키운다(최대 폭 넘으면 줄바꿈 + 높이 증가). 숫자·라벨·타이틀처럼 위치가 레이아웃에 묶인 고정 박스는 키울 수 없으니 최소 70%까지 줄이고 그래도 넘치면 `…`.
- 줄임은 도트 정수배가 보장되지 않는 타협이다. 도트 폰트가 들어오면 `UITheme.fontSnapUnit`으로 정수배 스냅하고, 고정 박스는 `minFitRatio`를 1로 두면 줄임을 끌 수 있다.
- 패널 크기는 `pixelScale` 배수로만 올림(가운데 피벗은 2배수라 반 픽셀 위치가 안 생긴다) → 9-slice 테두리가 뭉개지지 않는다. 패딩/최소·최대 폭은 전부 `UITheme`.

## D61. HUD 재설치 없이 동작하도록 런타임에 필요한 컴포넌트를 붙인다
- 저장된 씬에는 새 컴포넌트가 없다. `InteractPromptView`/`FoodDetailPanelView`가 `Awake`에서 없으면 붙인다(설치 코드도 같은 걸 붙여서 새 설치와 일치). 비활성 패널의 글자는 `Awake` 전이라 폰트/크기가 아직 적용 전일 수 있어 측정 전에 `ThemedText.Apply()`를 먼저 부른다.

## D62. 폰트: 한글 픽셀 폰트가 프로젝트에 없어 현재 폰트를 유지한다
- 확인: `Assets/Fonts`는 없고, 프로젝트의 폰트 파일은 팀원 `DOSIyagiBoldface.ttf`(JCY), `CookieRun Regular.otf`(픽셀 아님), TMP 기본 Liberation뿐. 라이선스를 확인할 수 없는 파일을 임의로 UI 폰트로 지정하지 않았다.
- 메뉴(`Create TMP Font From Project Font`)는 `Assets/Fonts`의 폰트(또는 선택한 폰트)로 **래스터(비 SDF) + Point 필터** TMP 폰트를 만들고, 프로젝트 코드/에셋/씬에 나오는 한글 음절을 미리 아틀라스에 넣는다(나머지는 동적 추가). `UITheme.fontSnapUnit`에 원본 도트 크기를 기록해 글자 크기를 정수배로 맞춘다.
- 폰트가 없을 때 위치 보정: `UITheme.lineSpacing`, 패널 안 글자 세로 정렬 `panelTextAlign`(기본 Capline — 문구가 바뀌어도 위치가 흔들리지 않음), 패딩. 제출 전 폰트 추가 필수는 handoff 맨 위에 적었다.

## D63. 안내 문구 순환은 `LandmarkInteractor`에 디버그 오버라이드로
- `DebugTools`는 UI를 참조하지 않는다. 안내 문구의 출처인 `LandmarkInteractor`에 `#if UNITY_EDITOR || DEVELOPMENT_BUILD`로 `SetDebugPrompt`를 두고, 메뉴가 문구를 돌려 넣는다 → 실제 `InteractPromptView` 경로(패널 크기 계산 포함)를 그대로 검증한다.

## D64. 연료 구조: 상태 하나, 반응은 각자
- `CampfireFuel`은 연료 상태만, 규칙은 순수 `CampfireFuelModel`, 보관은 `CampfireStateStore`. 빛/안전지대/회복/링/외형/게이지는 `Ratio·Stage·Strength(바닥일 때 배율)`를 읽어 각자 반응한다(서로 모른다). 새 반응을 추가해도 연료 코드는 안 바뀐다.
- 꺼짐/재점화는 히스테리시스: 0이 되면 꺼지고, 반경 밖에서 `relightThreshold01`(25%) 이상 차야 켜진다. 반경 안에 계속 있으면 꺼진 채 유지(가까이서 지켜보면 안 살아난다 — 의도).
- 감소 기준 반경은 고정 효과 반경(`effectRadius`)이지 줄어드는 안전지대 반경이 아니다(줄어든 반경을 기준으로 하면 줄어들수록 덜 닳는 되먹임이 생긴다).
- `LandmarkData.fuelProfile`이 비면 연료 없는 모닥불(이전 동작).

## D65. 상태 보관: ID 키 저장소, 언로드 중 회복은 경과 시간만큼 따라잡는다
- ID = 종류 + 위치(센티 단위 반올림). 같은 seed면 로드/언로드와 무관하게 같다. 저장소는 `LandmarkSpawner.Awake`에서 새로 만들어 씬 리로드(다시하기) 때 자연 초기화된다(`CampfireStateStore.Active` 정적 참조는 `SafeZoneRegistry`와 같은 방식).
- 언로드된 동안은 플레이어가 멀리 있다는 뜻이므로 다시 로드될 때 `Time.time` 경과만큼 회복을 따라잡는다. `SafeZoneSet`(청크 생성 때 스폰 제외 판단)도 같은 저장소를 읽어 꺼진 모닥불은 막지 않고 줄어든 반경을 쓴다.

## D66. 안전지대 반경은 변화 속도를 제한하고, 꺼지면 즉시 0
- 반경 = 최대 반경 × `Strength(safeRadiusAtEmpty)`. 늘거나 줄 때는 `SafeZoneProfile.radiusChangePerSecond`(기본 1타일/초) 이하로만 움직여 경계가 적을 훅 훑고 지나가지 않는다. 꺼지면 영역이 사라지므로 밀어낼 대상이 없어 즉시 0. 적을 미는 속도 자체는 병행 작업(D52, `Knockback_SafeZone`)이 맡는다.
- 쓰레기 더미 규칙(D48)은 그대로: 안전 판단은 `SafeZoneRegistry.IsSafe`가 현재 반경을 쓰므로 모닥불이 켜져 있을 때만 안전이 우선한다.

## D67. 단계 전환 연출: 페이드는 로컬, 파티클은 이벤트
- 스프라이트 전환 페이드는 `CampfireAppearance`가(옛 스프라이트를 위에 겹쳐 투명하게), 파티클은 `ILandmarkEvents.OnCampfireStageChanged`를 구독하는 `CampfireStageFeedback`이 기존 스파크 파티클로 낸다(D49와 같은 방식). 전용 연기 스프라이트가 없어 스파크로 대신했다 → `docs/art_needed.md`.

## D68. 단계 스프라이트를 `Assets/Art/Sprites/landmarks/`로 옮겼다
- `Assets/campfire_states/`의 PNG는 임포트가 Multiple/PPU 100/Bilinear/중앙 피벗이라 기존 모닥불(Single/PPU 32/Point/바닥 피벗)과 크기·위치가 어긋난다. `ArtImporter`가 이 폴더 규칙을 자동 적용하므로 GUID를 유지한 채 옮기고 meta를 기존 모닥불 설정으로 맞췄다.

## 검증 범위
- Unity 에디터가 열려 있어 배치 모드를 쓰지 못했다. 각 asmdef/Editor/테스트 프로젝트를 `dotnet build`로 컴파일해 에러 0 확인(Unity 참조 DLL 사용). `CampfireFuelTests`는 컴파일만 확인했고 실행은 못 했다. 설치 메뉴, 폰트 메뉴, 플레이 동작은 실행하지 못했다.


---

# 초반 난이도 완화 + 경험치/레벨업 (2026-10-17)

**서브에이전트 회의(설계/비판/일정)는 열지 않았다.** 막혀서 멈춘 지점이 없었다 — 갈림길마다 대안과 비용을 직접 비교해 결정했고, "대안/비용" 줄이 그 비판 몫이다. 시작 전 미커밋 변경은 없었다(`git status` clean). 팀원 파일(`Enemy.cs`, `EnemySO`, 프리팹, `Augment*`)은 수정하지 않았다. 진단 수치와 변경 전후 표는 `docs/balance.md`.

## D69. 난이도 값은 두 종류로 나눈다: 배율 곡선(이어짐) / 일차별 값(계단)
- `spawn/health/damage`는 기존대로 하루 안에서도 다음 날 값으로 이어지는 곡선. `speedRatio/speedCapRatio/maxAlive/contactInterval/detectRadius`는 **일차 번호로만** 읽는다(`Evaluate(일차 인덱스)`).
- 이유: "1일차 동시 3마리, 2일차 4마리…"를 곡선으로 두면 1일차 끝 무렵에 3.9 → 4마리가 되어 약속이 깨진다. 속도 60~70%/85% 이하도 날짜 안에서 흔들리면 검증이 어렵다.
- 대안/비용: 전부 계단식으로 통일 → 체력/피해가 날짜 경계에서 툭 뛴다(기존 곡선 사용자 체감 변화가 큼). 그래서 나눴다. `DifficultyProfile`의 `speed` 곡선은 `speedRatio`(플레이어 대비 비율)로 의미가 바뀌어 이름도 바꿨다 → 저장된 에셋 값은 새 값으로 다시 썼다.

## D70. 속도 = 플레이어 속도 × 일반 비율 × 종류 보정, 상한, 빠른 적 슬롯
- `EnemySO.speed`가 3~10이라 그대로 비율로 쓰면 5종 중 3종이 1일차부터 플레이어보다 빠르다. 종류 보정 = `(SO속도 ÷ 5)^0.5`(0.77~1.41배로 압축)에 일차 상한(`speedCapRatio`)을 얹고, 상한을 넘으려는 종류는 `EnemySpeedPolicy`의 슬롯(3일차부터 동시 1마리, 상한 1.10배)을 얻어야만 한다.
- 속도는 매 프레임 다시 계산한다(`enemy._speed`). 낮→밤, 날짜 변경, 디버그 슬라이더가 이미 나와 있는 적에게도 바로 반영된다. 비용: 적 수 × 몇 번의 속성 읽기(상한 12마리면 무시할 수준).
- 대안: 종류마다 속도 필드를 따로 둔다 → `EnemySO`가 팀원 파일이라 불가. SO 이름 키 표 → 이름이 바뀌면 조용히 깨짐. 그래서 SO 속도 자체에서 보정을 유도했다.

## D71. 팀원 `Enemy.OnCollisionEnter2D`의 즉시 피해는 `_damage = 0`으로 무력화한다
- 문제: 팀원 코드가 닿는 즉시 `health.TakeDamage(_damage)`를 줘서 "첫 타격 지연 0.4초"가 불가능하다. 프리팹/스크립트는 수정할 수 없다.
- 검토한 대안: ① `Enemy` 컴포넌트 비활성화 — 비활성 컴포넌트의 물리 콜백이 불리는지 보장할 수 없고 `Update`(이동)도 같이 꺼진다. ② `Health` 쪽에서 가로채기 — 호출자를 알 수 없어 썩은 음식 자해와 구분이 안 된다. ③ 충돌을 끈다 — 적이 플레이어를 막는 물리가 사라진다.
- 선택: 어댑터가 `Setup`에서 `_damage`를 0으로 만든다(`Health.TakeDamage`는 0 이하를 무시). 실제 피해량은 어댑터가 `baseContactDamage`로 따로 들고 맞히는 순간의 난이도 배율로 계산한다. 위협도 계산은 0으로 만들기 전에 끝낸다.
- 비용: `Enemy._damage` 값을 다른 곳에서 읽는 코드가 생기면 0이 보인다(지금은 없음 — `Attack()`은 주석 처리됨).

## D72. 무적은 "맞는 쪽"이 갖는다: `IContactDamageReceiver` + 순수 `HitInvulnerability`
- 플레이어의 `PlayerHitReceiver`(`GameFlowRoot`가 런타임에 붙임, D61 방식)가 무적 창을 지키고 깜빡이며 `Health`를 때린다. 적은 수신기가 있으면 거기로, 없으면 `Health`로 직접 준다.
- 무적에 막힌 공격도 "공격은 한 것"으로 보고 적의 주기를 다시 센다(막힌 뒤 즉시 재시도하면 3마리가 0.8초마다 번갈아 때리는 꼴이 된다). 막힌 시도는 무적 창을 늘리지 않는다.
- `Health`는 건드리지 않았다 → 썩은 음식을 먹은 자해 피해는 무적과 무관.
- 무적 시간은 `DifficultyProfile.playerInvulnerableSeconds`(디버그 슬라이더로 임시 변경 가능).

## D73. 접촉 시간은 `Update`에서 센다, `OnCollisionStay2D`에 의존하지 않는다
- 첫 타격 지연 동안 적이 멈추면 리지드바디가 잠들어(슬립) `Stay` 콜백이 끊길 수 있다. `Enter/Exit`으로 "닿아 있나"만 기록하고, 타격 시점은 `Update`의 `Time.time`으로 본다. 예고 중에는 `targetTrm`을 비우고 `LateUpdate`에서 속도를 0으로 눌러 멈춘다(넉백 경직과 같은 방식). 떨어지면 예고 취소, 다시 닿으면 처음부터.

## D74. 추격 감지: `Enemy.targetTrm`을 비우면 팀원 이동이 멈추는 점을 이용한다
- 감지 반경 밖(+히스테리시스 ×1.3)에서는 `targetTrm = null`이라 `Enemy.Update`가 속도를 건드리지 않는다. 그 사이에 어댑터가 걷기/멈추기를 반복하는 배회 속도를 직접 준다. 팀원 파일 수정 없음.
- 스폰이 화면 밖이라 처음엔 대부분 감지 반경 바깥이다: 위/아래 스폰(약 8.5칸)은 1일차 감지 반경 9 안이라 곧장 다가오고, 좌우(13.9칸 이상)는 배회한다 → 방향에 따라 다른 긴장감. 너무 한가하면 `detectRadius` 곡선을 키우면 된다.

## D75. 스폰 제한은 순수 함수로 뽑고, "화면 밖"은 카메라 사각형 + 여유로 판정한다
- `SpawnMath.EarlyCap(경과, 유예, 1마리 구간, 일차 상한)`과 `SpawnMath.IsOnScreen`. 경과 시간은 `StatsTracker`의 플레이 시간(Playing에서만 누적 — 일시정지/레벨업 선택 시간은 안 센다).
- 기존 `minSpawnDistance` 11은 화면 절반 폭(12.4)보다 작아서 좌우로 화면 안에 스폰되던 **기존 버그**였다. 이제 화면 밖 판정이 하한을 정하고 `minSpawnDistance`는 8로 낮춘 안전망이다. 위험 구역 진입 추가 스폰도 같은 판정을 쓴다.
- `maxSpawnDistance`(22)를 새로 뒀다: 청크 안 무작위 위치라 로드된 25청크 어디든 후보가 되어, 너무 멀어 만날 일 없는 적이 상한을 차지하는 걸 막는다.
- 새 막힘 이유 3개(유예/화면 안/너무 멂)를 진단 카운터에 추가 → F1 "적 스폰 상태"에 이유별로 보인다.

## D76. 디버그 슬라이더 = 임시 값(`DifficultyTuning`)만 바꾸고, 버튼으로 프로필에 굽는다
- 슬라이더가 에셋을 직접 건드리면 플레이 중 실수가 곧바로 저장된다. 임시 값을 `DifficultyEvaluator`가 곡선 값에 곱하거나 덮어쓰고, "현재 값을 DifficultyProfile에 반영"을 눌러야 곡선 키에 곱해 굽는다(에디터 전용 `SetDirty` + 저장, Development Build에서는 비활성). 굽고 나면 임시 값은 초기화된다.
- 속도 배율은 `min(비율×종류, 상한)×배율`이라 `speedRatio`, `speedCapRatio`, `fastSpeedRatio`를 같이 곱해야 같은 결과가 된다. 최대 동시 적 수 고정은 "지금 일차의 곡선 값 → 입력값"이 되도록 곡선 전체에 배율을 곱한다.

## D77. 경험치: 계산/표/전달을 셋으로 나눈다
- `ExperienceService`(누적과 레벨 계산만), `XpCurve`(SO, `IXpTable`로 주입), `KillExperienceBridge`(처치 이벤트 → 경험치). 적이 얼마를 주는지는 어댑터가 `IExperienceReward`로 알린다(`IEnemyActor`에 멤버를 추가하면 기존 구현/테스트 가짜가 전부 깨져서 별도 인터페이스). 종류별 지급량은 `EnemyIntegrationSettings.experienceByEnemy`, 없으면 기본 8.
- 한 번에 여러 레벨이 오르면 `OnLevelUp(level)`을 오른 순서대로 여러 번, `OnExperienceChanged`는 최종 값으로 한 번. 필요량이 0 이하로 새는 표가 있어도 한 번에 100레벨까지만 올린다(무한 루프 방지).
- 첫 레벨업 시점은 추정이다(`balance.md` §3) — 곡선 값만 바꾸면 되도록 SO로 뺐다.

## D78. 레벨업 → 증강 선택: 상태 하나 추가 + 큐 코디네이터 + 요청 번호
- `GameState.AugmentSelect`(Playing ↔ AugmentSelect만 오갈 수 있음). 정지/입력 차단은 기존 `GameplayGate`가 "Playing이 아니면 정지"라 그대로 동작하고, 선택 중 ESC(일시정지)는 무시된다 → 선택이 끝나기 전에는 재개되지 않는다.
- `LevelUpCoordinator`: 레벨업을 큐에 쌓고 Playing일 때 하나씩 꺼낸다. 선택 요청마다 번호를 올려 **늦게 오거나 두 번 불린 완료 콜백은 무시**한다(중복 호출이 다음 선택을 건너뛰는 사고 방지).
- 다시하기/타이틀: 씬을 다시 불러오므로 모든 게 새로 만들어지지만, `GameFlowController.RunEnding` 이벤트로 경험치/큐/열린 선택을 명시적으로도 비운다(테스트 가능, 씬 리로드 방식이 바뀌어도 안전).
- `IAugmentSelectionPresenter`(`RequestSelection(level, onComplete)`, `Cancel()`). 슬롯: `GameFlowRoot.augmentPresenter`(없으면 `NullAugmentPresenter`: "레벨 업!" 배너 + 최대 체력의 10% 회복(`FlowSettings.levelUpHealRatio`) 후 1.2초 뒤 자동 재개).
- 팀원 `AugmentManager`/`AugmentUI`는 현재 **선택 완료를 알리는 이벤트가 없고** 상자(`AugmentChest`)가 열도록 되어 있다. 억지 어댑터(리플렉션으로 `_isSelecting` 폴링)는 만들지 않았다 — 팀원이 완료 이벤트를 달면 얇은 어댑터 하나(`OpenAugmentSelection()` 호출 + 완료 시 `onComplete`)로 끝난다. `AugmentUI.ShowAugments`가 `Time.timeScale`을 직접 0/복원하는 점은 주의: 우리 쪽이 먼저 정지시킨 뒤 호출되므로 "이전 값 = 0"을 저장했다가 복원하면 0이 된다. 우리 `Playing` 전환이 1로 되돌리므로, 어댑터는 **팀원 UI가 `timeScale`을 복원한 뒤에** `onComplete`를 불러야 한다(순서가 반대면 복원이 1을 다시 0으로 덮는다).

## D79. 경험치 바는 체력 패널 안에 붙이고, 도트 단위로만 채운다
- 체력 패널 아래에 행 하나를 더해 패널이 `pixelScale × 20`(2배율에서 40px) 길어진다. 새 패널을 따로 두지 않아 화면 요소 수는 그대로. 프레임/채움은 `ui_bar_frame/fill` 그대로, 높이 10도트(테두리 4+4, 속 2), 체력바와 같은 x/폭. 채움 길이는 안쪽 도트 수 단위로 끊어 반 픽셀이 안 생긴다.
- D61과 같은 방식: 저장된 씬에 없어도 `GameFlowRoot`가 런타임에 만들고(`ExperienceBarView.Install`, 이미 있으면 재사용), `UISetup`도 같은 코드를 써서 재설치와 결과가 같다. HUD 점유 영역(`hudZones`)은 `UISetup`이 실제 패널 크기로 다시 기록하고, 저장된 에셋은 직접 156으로 고쳤다(화살표/라벨이 새 높이를 피한다).
- 레벨업 순간 바가 가득 찬 채 0.06초 간격으로 번쩍인다(실시간 기준이라 정지 중에도 보임). `UITheme`에 `xpFill`, `xpFlash`, `levelFormat`을 추가했다.

## D80. 통계/결과 화면
- `GameStats.LevelReached`(`StatsTracker`가 `IExperienceSource`에서 읽음, 없으면 1). 게임오버/클리어 화면에 "도달 레벨 N" 줄을 추가했다 — 줄 간격을 60→55로 줄여 제목/버튼 자리는 그대로 두었다.

## 검증 범위
- 실행한 것: 모든 asmdef/Editor/Assembly-CSharp/테스트 프로젝트 `dotnet build` **에러 0**(새 파일이 csproj에 아직 없어 임시 csproj로 파일을 포함해 빌드). 순수 로직(무적, 경험치, 다중 레벨업 큐, 중복 콜백, 일시정지 중 레벨업, 재시작 초기화, 빠른 적 슬롯, 스폰 유예, 처치 경험치 전달) 30개 검사를 Unity 없이 콘솔 하네스로 **실제 실행해 전부 통과**.
- 실행하지 못한 것: Unity 에디터가 열려 있어 Test Runner(NUnit) 실행 불가 — `DifficultyAndExperienceTests`(32개)는 컴파일만 확인. 곡선 평가(`DifficultyEvaluator`)는 네이티브 `AnimationCurve`가 필요해 하네스에서 못 돌렸다(NUnit 쪽에만 있음). 어댑터의 접촉/추격/배회, 스폰 화면 밖 판정, 경험치 바 레이아웃, 배너, F1 슬라이더는 플레이로만 확인할 수 있다.
