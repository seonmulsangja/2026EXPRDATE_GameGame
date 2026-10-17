# Handoff — 이동 / 음식 / 낮밤

> ⚠️ **제출 전 폰트 파일 추가 필수.** 지금은 한글 픽셀 폰트가 프로젝트에 없어(`Assets/Fonts`가 비어 있음) OS 폰트를 복사한 임시 폰트가 쓰인다 — 도트 UI와 모양이 안 맞고 배포 라이선스도 문제.
> 라이선스가 확실한 한글 픽셀 폰트(.ttf/.otf)를 **`Assets/Fonts/`** 에 넣고 `Tools > ShelfLife > UI > Create TMP Font From Project Font` 를 실행하면, 래스터(Point) TMP 폰트 에셋이 만들어져 `UITheme.font` 한 곳만 바뀐다(HUD·흐름 화면 모두 따라감). 프로젝트의 한글 글자도 미리 아틀라스에 넣고 `UITheme.fontSnapUnit`(원본 도트 크기, 기본 16)을 기록한다. 폰트 원본 크기가 16이 아니면 실행 전에 `UITheme.fontSnapUnit`을 그 값으로 바꿔 둘 것.
> 교체 후 `Assets/ShelfLife/UI/Fonts/PlaceholderKorean*`은 삭제. (팀원 폰트 `Assets/JCY/08 Font/DOSIyagiBoldface`는 라이선스 확인 후 `Assets/Fonts`로 복사해 써도 된다. 팀원 파일 자체는 수정하지 않는다.)

설계 상세·이름 확정 목록은 `docs/spec.md` 참고. 코드: `Assets/Scripts/Survival/` (네임스페이스 `ExpiryGame`, asmdef `ExpiryGame.Survival`).

## 1. 구현 요약
| 영역 | 클래스 | 내용 |
|---|---|---|
| 이동 | `PlayerInputReader`, `PlayerMover`, `PlayerFacing` | Input System 입력 → Rigidbody2D 속도, 마지막 이동 방향 보관 |
| 낮밤 | `GameClock`, `DayCycleConfig`, `DayNightLighting` | 1일 60초(SO), 낮/밤 페이즈, Light2D 색 그라디언트 |
| 음식 | `FoodData`, `FoodBalanceConfig`, `FoodStatCalculator`, `FoodItem` | 유통기한→회복/데미지/내구도 (수식은 spec §5) |
| 감소 | `ShelfLifeSystem` | 등록된 `FoodItem`을 시계 틱마다 감소, 0이면 썩음 |
| 보유 | `FoodInventory`, `FoodPickup`, `FoodFactory` | 슬롯 5칸(인스펙터), 선택 슬롯, 월드 픽업 |
| 행동 | `FoodConsumer`, `FoodWeapon` | 먹기(썩으면 피해), 전방 원형 판정 공격 + 내구도 소모 |
| 디버그 | `DebugHud` | 임시 OnGUI. UI 완성 후 삭제 |

썩은 음식: 회복 × `rottenHealMultiplier`(기본 −0.5 → 먹으면 피해), 데미지 × `rottenDamageMultiplier`(0.5).
조작: 이동 WASD, 먹기 **E (길게 누름 — 시간은 `EatSettings.holdSeconds`, 기본 0.4초. 아래 "넉백 약화 + 먹기 진행 바" 참고)**, 공격 좌클릭/Enter, 다음 음식 **2**.

## 2. 팀원 연결용 인터페이스 / 이벤트
```csharp
using ExpiryGame;

// 적 AI: 구현만 하면 플레이어 무기에 맞음 (Health 컴포넌트를 붙여도 됨)
IDamageable.TakeDamage(float)          // 적이 구현 (공격 대상 레이어는 FoodWeapon.targetMask)
IHealable.Heal(float)

// UI
IGameClock : Ticked(float) PhaseChanged(DayPhase) DayStarted(int) / TimeOfDay01 DayCount Phase SecondsToDays(sec)
Health     : HealthChanged(cur,max), Died / Current Max IsDead
IFoodInventory : Changed / Items Selected Capacity
FoodItem   : RemainingShelfLife ShelfLife01 RemainingDurability IsRotten CurrentHeal CurrentDamage
             Rotted(FoodItem) Broken(FoodItem)
FoodConsumer.FoodEaten(FoodItem)       FoodWeapon.WeaponUsed(FoodItem, hitCount)

// 조합 시스템 (결과물 생성 → 유통기한 감소 등록 → 인벤토리에 넣기)
IFoodFactory.Create(FoodData)  →  FoodItem
IFoodInventory.TryAdd(FoodItem) / TryAdd(FoodData) / Remove(FoodItem)   // TryAdd(FoodItem)이 자동 Register
IShelfLifeTracker.Register / Unregister  // 인벤토리를 거치지 않는 아이템을 감소시키고 싶을 때
```
- 인스펙터 필드는 구체 컴포넌트(`GameClock` 등)로 연결하고, 코드에서는 인터페이스로 받아 쓰면 됨.
- 조합 결과물의 스탯은 결과 `FoodData.shelfLifeSeconds` 기준으로 자동 계산됨.

## 3. 유니티에서 직접 할 일 (자동화 후 남은 것)
씬/프리팹/SO 세팅은 **`Tools > ShelfLife > Build Test Scene`** 이 전부 처리함
(`Assets/ShelfLife/{Data,Prefabs,Scenes}` 생성, `Enemy` 레이어 추가, 인스펙터 참조 연결).
- [ ] 스크립트 컴파일 에러 없는지 Console 확인 (이 환경에서는 Unity 없이 Roslyn으로만 컴파일 검증함)
- [ ] 메뉴 실행 → `ShelfLifeTest.unity` 생성 확인
- [ ] 다시 실행하면 프리팹/씬은 재생성됨 (SO 수치는 유지). 프리팹을 수정했다면 백업 후 실행
- [ ] 실제 게임 씬에 쓸 때: `GameSystems`(GameClock, ShelfLifeSystem, FoodFactory), `DayNightLighting`, `Player` 프리팹을 복사하고 `FoodInventory`의 tracker/factory 참조를 씬 오브젝트로 연결
- [ ] (선택) Window > General > Test Runner > EditMode 에서 `FoodTests` 실행

---

# 추가: 휘두르기 + 타격감 (Combat)

코드: `Assets/Scripts/Combat/` (asmdef `ExpiryGame.Combat`, 네임스페이스 `ExpiryGame.Combat`). 상세 결정은 `docs/meetings.md`.
Survival 쪽 변경은 2건뿐: `FoodWeapon`에 선택 필드 `attackDriver`(IAttackDriver) 추가, `IAttackDriver.cs` 신규. 기존 이벤트/인터페이스 이름 변경 없음.
(`FoodData.worldSprite` → `sprite` 이름 변경 + `spriteRotten` 추가는 아트 작업분.)

## 구조
| 클래스 | 책임 |
|---|---|
| `SwingController` | 손 피벗 기준 3단계 모션(준비/스윙/여운), 쿨다운 수치 제공. `IAttackDriver` 구현 |
| `SwingHitDetector` | 호(각도+거리) 판정, 스윙당 대상 1회. `HitLanded(HitInfo)` 발행 |
| `HitFeedbackPlayer` | 연출 총괄. 데미지→강도 변환(`HitFeelProfile.IntensityFor`)은 여기서 1회, 나머지에 위임 |
| `HitStop` / `CameraShake` / `HitFxSpawner` | 시간정지 / 카메라 흔들림 / 스파크·슬래시·파티클(풀링) |
| `KnockbackReceiver` / `FlashOnHit` | 맞는 쪽에 붙이는 컴포넌트 (넉백 / 흰색 번쩍) |
| `SwingProfile`, `HitFeelProfile` (SO) | 모든 수치 |

흐름: 좌클릭 → `FoodWeapon`(쿨다운) → `SwingController.Begin` → 스윙 중 `SwingHitDetector`가 `IDamageable.TakeDamage` 호출 + `HitLanded` →
`HitFeedbackPlayer`가 연출. 타격 구간이 끝나면 `FoodWeapon`이 내구도 소모 → 기존 `FoodItem.Broken`/`WeaponUsed` 그대로 발행(파괴 연출은 `Broken` 구독).
`HitLanded`는 대상별 위치/방향이 필요해서 새로 둔 이벤트다. `WeaponUsed(item, hitCount)`는 스윙당 1회로 기존 의미 그대로.

## 팀원 적 코드에 붙이는 법
1. **데미지**: `IDamageable` 구현 (또는 `Health` 컴포넌트). 콜라이더는 `Enemy` 레이어, 콜라이더와 같은 오브젝트/부모에 컴포넌트.
2. **넉백**: 적 오브젝트에 `KnockbackReceiver` 추가 (Rigidbody2D 있으면 `MovePosition`으로 밀어 AI의 velocity 덮어쓰기와 충돌 안 함).
   AI가 이동을 멈춰야 한다면 `GetComponent<IKnockbackable>().IsKnockedBack` 동안 자체 이동 생략. 무거운 적은 `multiplier` 낮추기.
3. **피격 플래시**: 적 오브젝트에 `FlashOnHit` 추가 + `flashMaterial`에 `Assets/ShelfLife/Data/Combat/SpriteFlash.mat` 연결.
   (직접 구현하려면 `IFlashable.Flash(float seconds)`)
4. 아무것도 안 붙여도 데미지는 들어가고, 넉백/플래시/스파크/카메라/히트스톱은 있는 것만 반응한다.
- 참고 구현: `Combat/Testing/TrainingTarget.cs` (IDamageable만 구현한 최소 예시), 프리팹 `CombatDummy`, `Scarecrow`.

## 유니티에서 할 일
- [ ] Console에 컴파일/셰이더 에러 없는지 확인 (`SpriteFlash.shader`는 Unity 없이는 검증하지 못함)
- [ ] `Tools > ShelfLife > Setup Combat Test (All In One)` 실행 → 아트 임포트 적용, 테스트 씬 재생성, 전투 설치, 다크 아트 적용, 저장
- [ ] Play: 이동 WASD, **마우스 좌클릭**으로 휘두르기(마우스 방향), 먼저 왼쪽 음식 픽업 위로 걸어가 줍고(10종 배치) `2`로 교체

---

# 추가: 적 처치 시 음식 드롭 (Drop) + 무한 맵 (MapGen) + 음식 상자 (Loot)

코드: `Assets/Scripts/Drop/`, `Assets/Scripts/MapGen/`, `Assets/Scripts/Loot/` (asmdef 각각 `ExpiryGame.Drop` / `.MapGen` / `.Loot`). 결정 근거는 `docs/meetings.md` D6~D14.
기존 코드 수정은 2건뿐: `FoodItem.SetShelfLife01(float)` 추가, `FoodVisual.Refresh()` public화. 기존 이벤트/인터페이스 이름 변경 없음. 적 AI/UI/조합 코드는 그대로.

## 팀원 연결 방법 1 — 적이 죽을 때 음식 드롭
**A. `Health`를 쓰는 적 (코드 수정 없음)**: 적 프리팹에 `HealthDropSource`를 추가하고 `Table`(DropTable)과 `Threat01`(0=약함 ~ 1=강함)만 채운다. `Health.Died`를 그대로 구독한다.

**B. 자체 사망 처리가 있는 적**: `IDropSource`를 구현한다 (참고: `Drop/Testing/DropTestDummy.cs`).
```csharp
using ExpiryGame.Drop;
public class MyEnemy : MonoBehaviour, IDropSource
{
    public DropTable Table => table;            // 무엇을 떨굴지
    public float Threat01 => threat;            // 강한 적일수록 1에 가깝게 (강한 음식 가중치 ↑)
    public Vector3 DropPosition => transform.position;
    public event Action<IDropSource> Died;      // 죽을 때 Died?.Invoke(this)

    void OnEnable()  { DropSourceRegistry.Register(this); }
    void OnDisable() { DropSourceRegistry.Unregister(this); }
}
```
- `DropSpawner`(씬에 1개)가 등록된 모든 소스의 `Died`를 구독해 자동으로 드롭한다. 적은 `DropSpawner` 참조가 필요 없다.
- 사망 이벤트 없이 직접 떨구려면 `IDropService.Drop(table, threat01, position)`.

## 드롭 이벤트 (UI/사운드용)
```csharp
IDropService : FoodDropped(FoodItem, Vector3 origin)   FoodCollected(FoodItem)   // DropSpawner가 구현
```
획득은 기존 `IFoodInventory.TryAdd(FoodItem)` → 기존 `Changed` 이벤트가 그대로 발행된다 (가방이 가득 차면 줍지 않고 바닥에 남음).

## 클래스 / SO
| 클래스 | 책임 |
|---|---|
| `DropTable` (SO) | 항목 {음식, 가중치, 남은 유통기한 비율 범위}, 드롭 확률, 개수 범위, `threatWeightBias`. 강한 적 가중치 계산은 `EffectiveWeight` **한 곳** |
| `DropFeelSettings` (SO) | 튕김/반짝임/획득 반경/끌림 속도/썩은 뒤 소멸 시간 전부 |
| `DropSpawner` | 사망 구독 → 표 굴리기 → `IFoodFactory.Create` + `IShelfLifeTracker.Register` → 풀링된 `FoodDrop` 발사 |
| `DroppedFood` / `DropArc` / `DropSparkle` / `DropCollector` | 월드 드롭 수명 / 포물선+바운스 / 착지 반짝임 / 반경 획득+끌림 |
| `HealthDropSource`, `DropSourceRegistry` | `Health` 어댑터, 소스 등록소 |
| `FoodVisual` (기존) | 신선도 틴트, 썩으면 `spriteRotten` 교체 — 드롭도 이걸 재사용 |

## 팀원 연결 방법 2 — 청크 이벤트 (적 스폰용)
```csharp
using ExpiryGame.MapGen;
IChunkEvents events = streamer;   // 씬의 ChunkStreamer
events.OnChunkLoaded   += (Vector2Int chunk, Bounds worldBounds, ChunkRandom rng) => { ... };
events.OnChunkUnloaded += (Vector2Int chunk) => { ... };   // 이 청크에서 스폰한 것 정리
// events.CurrentChunk, events.LoadedChunks 도 읽을 수 있음
```
- `rng`는 청크 좌표+seed에서 나온 결정적 난수다 (같은 청크 = 항상 같은 값). **값 복사로 전달되므로 구독자끼리 간섭하지 않지만, 시스템마다 `rng.Fork(고유숫자)`로 갈라 써야 서로 같은 값을 뽑지 않는다.**
  `rng.Value()` 0~1, `rng.Range(float, float)`, `rng.Range(int min, int maxExclusive)`.
- 청크는 로드 시마다 다시 만들어지므로 적/상자 상태는 저장되지 않는다. 필요하면 스폰한 쪽에서 청크 좌표를 키로 기억할 것 (`Loot/LootCrateSpawner.cs`가 참고 구현).
- 바이옴 확장: `IFloorRule` / `IObstacleRule` / `IBiome` / `IBiomeProvider` 구현 후 `ChunkStreamer.SetBiomeProvider(...)`.

## 음식 상자 (Loot, 선택 기능)
`LootCrateSpawner`가 `OnChunkLoaded`로 확률 배치, 상자는 `IDamageable + IDropSource`라 때리면 기존 드롭 경로로 음식이 나온다. 이번 플레이 중 부순 상자는 되살리지 않는다.

## 유니티에서 할 일
- [ ] Console에 컴파일 에러/경고 없는지 확인
- [ ] `Tools > ShelfLife > Setup Drop + Infinite Map Test (All In One)` 실행 → `Assets/ShelfLife/Scenes/DropMapTest.unity`(ShelfLifeTest 복사본)에 드롭/무한 맵/상자 설치. 이미 만든 씬에는 개별 메뉴: `Install Drop To Open Scene`, `Install Infinite Map To Open Scene`, `Install Loot Crates To Open Scene` (이 순서)
- [ ] (선택) Test Runner > EditMode > `DropMapTests` 5개 실행
- [ ] 수치 위치: `Assets/ShelfLife/Data/Drop/{DropTable_Default, DropFeelSettings, LootCrateSettings}.asset`, `Assets/ShelfLife/Data/MapGen/MapGenSettings.asset`


---

# 추가: 아트 v2 + HUD + 낮밤 시각 + 랜드마크 (UI / World / Landmark / DebugTools)

코드: `Assets/Scripts/{UI,World,Landmark,DebugTools}/` (asmdef `ExpiryGame.UI/.World/.Landmark/.DebugTools`, 에디터 `…Editor`). 결정 근거는 `docs/meetings.md` D15~, 아트 설명은 `docs/art.md` 하단.
**기존 코드 수정은 최소**: `ShelfLifeSystem`(감소량 × `DecayRateRegistry.Combined()`), `GameClock`(`JumpToTimeOfDay`, `SpeedMultiplier`, `NightStartNormalized` 추가), `MapGen`(night Tilemap 선택 필드, `IObstacleFilter`), 에디터 설치기의 플레이어 스프라이트 이름(`player_down` → `player_down_idle`). **적 AI / 조합 코드는 수정하지 않았다.** 기존 이벤트/인터페이스 이름 변경 없음.

## 새 이벤트 / 인터페이스
```csharp
using ExpiryGame;            // Survival
IDecayRateModifier.GetDecayRateMultiplier(Vector2 pos)   // 위치별 유통기한 감소 배율 제공자 (1 = 그대로). DecayRateRegistry.Register/Unregister 로 등록
FoodItem.Anchor                              // 음식의 현재 월드 위치(Transform)
DecayRateRegistry.Combined(Vector2)          // ShelfLifeSystem이 음식마다 읽는 곱 (등록 없으면 1)
GameClock.JumpToTimeOfDay(float01) / SpeedMultiplier / NightStartNormalized   // 디버그·UI용

using ExpiryGame.Landmark;
IDangerZoneEvents : OnDangerZoneEntered(DangerZoneInfo) OnDangerZoneExited(DangerZoneInfo) IsPlayerInDanger   // 쓰레기 더미. DangerZoneEvents 컴포넌트가 구현 (씬에 1개)
DangerZoneInfo : Source(LandmarkKind) Center Radius
ILandmarkLocator.GetNearby(Vector2 from, List<LandmarkInfo>)   // 가까운 순 (LandmarkLocator)
IInteractionPrompt : PromptChanged(string) CurrentPrompt        // LandmarkInteractor (F 키)
ILandmarkInteractable / InteractableRegistry                     // 상호작용 대상 추가용

using ExpiryGame.World;
IDayNightState : Current(DayNightSample: Part, Night01, AmbientColor …) Profile / Changed(DayNightSample)   // DayNightDirector. DayNightState.Instance 로도 접근

using ExpiryGame.MapGen;
IObstacleFilter.IsBlocked(Vector2)   ChunkStreamer.SetObstacleFilter(...)
```
- **적 쪽 연결**: 위험 구역 반응은 `IDangerZoneEvents.OnDangerZoneEntered`만 구독하면 된다 (`FindFirstObjectByType<DangerZoneEvents>()`). 적 코드는 건드리지 않았다.
- 냉장고 배율은 **음식이 놓인 위치 기준**이다: `IDecayRateModifier.GetDecayRateMultiplier(Vector2)`, `DecayRateRegistry.Combined(Vector2)`. `FoodItem.Anchor`(Transform)가 위치를 알려준다 — 가방 안은 `FoodInventory`가 플레이어로, 바닥 드롭은 `DropSpawner`가 드롭 오브젝트로 설정한다. `Anchor`가 비어 있으면 배율 없음.
- HUD는 기존 이벤트만 구독한다: `Health.HealthChanged`, `IFoodInventory.Changed`, `FoodItem.Rotted`, `FoodWeapon.WeaponUsed`, `IGameClock.Ticked/PhaseChanged/DayStarted`. 회복/데미지/사용횟수는 `FoodItem.CurrentHeal/CurrentDamage/RemainingDurability`를 그대로 읽는다 (UI에서 재계산 없음).

## 유니티에서 할 일
- [ ] Console에 컴파일 에러/경고, **`SpriteAdditive.shader` 컴파일 에러** 없는지 확인 (이 환경은 Unity 없이 asmdef별 `dotnet build`로 C#만 검증함)
- [ ] 메뉴 한 번: `Tools > ShelfLife > Setup Art V2 Test (All In One)` → `Assets/ShelfLife/Scenes/ArtV2Test.unity` 생성(DropMapTest 복사본). 이미 만든 씬에는 개별 메뉴 (이 순서): `Install Infinite Map…` → `Install Day-Night + Walk Anim To Open Scene` → `Install Landmarks To Open Scene` → `Install HUD To Open Scene` → `Install Debug Menu To Open Scene`
- [ ] (선택) Test Runner > EditMode > `ArtV2Tests` 6개
- [ ] 수치 위치: `Assets/ShelfLife/Data/World/{DayNightProfile,WalkAnimationProfile}.asset`, `Data/Landmark/{LandmarkSettings,Landmark_*,DropTable_Store,DropTable_Dump}.asset`, `Data/MapGen/MapGenSettings.asset`(night 타일), `ShelfLife/UI/UITheme.asset`(폰트·색·문구)
- [ ] 한글 폰트: 임시로 OS 맑은 고딕을 TMP 동적 폰트로 만들어 연결함(`ShelfLife/UI/Fonts`, 배포용 아님). 픽셀 폰트가 정해지면 **`UITheme.font` 한 곳만** 교체

## 후속 수정 (냉장고 위치 판정 / 바닥 풀·장식 / 디버그 / 폰트 / HUD)
- **냉장고**: 반경 안에 놓인 음식만 느려진다 (바닥 음식은 자기 위치, 가방은 플레이어 위치).
- **바닥 풀 + 장식**: `floor_d`는 바닥 변형 3번 (별도 노이즈 `grassNoiseScale/grassStart`). 장식은 `IDecorRule`(`PerlinDecorRule`)이 청크 생성에서 배치, `ChunkStreamer`가 `DecorPool`로 올린다 (충돌 없음). 랜드마크 주변은 `IObstacleFilter`로, 시작 지점/장애물 주변은 규칙에서 비운다. `IBiome.Decor`는 기본 구현이 null이라 기존 바이옴은 그대로 동작. 낮밤: weed/puddle은 day/night 쌍 알파 크로스페이드, 나머지는 밤 틴트 (`DecorLighting` ← World의 `DecorNightController`).
  수치는 전부 `MapGenSettings`: `grassStart`, `decor[]`(가중치/밤 틴트), `decorAttemptsPerChunk`, `decorDensityAtLow/High`, `minDecorSpacing`, `decorObstacleClearance`, `decorSortingOrder`.
- **F1 디버그 메뉴**는 `UNITY_EDITOR || DEVELOPMENT_BUILD`에서만 컴파일된다 (릴리스에서는 빈 컴포넌트 껍데기만 남음).
- **폰트**: 폰트 파일(.ttf/.otf)을 `Assets/ShelfLife/UI/Fonts/`에 넣고 `Tools > ShelfLife > UI > Create TMP Font From Project Font`를 실행하면 TMP 폰트 에셋이 만들어져 `UITheme.font`에 연결된다 (Project 창에서 폰트를 선택해도 됨). 이제 HUD 설치는 OS 폰트를 복사하지 않는다.
  - ⚠️ **제출 전 폰트 교체 필수**: 지금 프로젝트에 남아 있는 `PlaceholderKorean`은 OS 맑은 고딕을 복사한 임시 폰트라 배포 라이선스 문제가 있다. 제출 전에 한글 픽셀 폰트로 교체하고 `Assets/ShelfLife/UI/Fonts/PlaceholderKorean*`을 삭제할 것. (폰트 파일이 없는 새 환경에서는 TMP 기본 폰트가 연결돼 한글이 네모로 보인다.)
- **HUD 재설치**: `Install HUD To Open Scene`는 이미 HUD가 있으면 덮어쓸지 확인창을 띄운다 (취소하면 기존 레이아웃 유지).


---

# 추가: 팀원 적 통합 + 시작 아이템 + 게임 흐름 (Flow / Integration)

코드: `Assets/Scripts/Flow/`(asmdef `ExpiryGame.Flow`, 에디터 `.Flow.Editor`), `Assets/Scripts/Integration/`(**asmdef 없음** — 팀원 `Assembly-CSharp` 코드를 알아야 해서). 설계 근거 `docs/meetings.md` D32~D41, 팀원 코드 조사/연결 방식 `docs/merge_plan.md`, 필요한 도트 `docs/art_needed.md`.
**팀원 파일(`Assets/JCY/`) 수정 0건.** 기존 코드 수정은 `DebugHud`를 `#if UNITY_EDITOR || DEVELOPMENT_BUILD`로 감싼 것뿐. 기존 이벤트/인터페이스 이름 변경 없음.

## 새 이벤트 / 인터페이스
```csharp
using ExpiryGame.Flow;
IGameStateMachine : Current(GameState: Title/Playing/Paused/GameOver/Clear)  StateChanged(prev, next)  StartGame/Pause/Resume/Fail/Win
IEnemyActor       : Position Threat01  Killed(IEnemyActor)          // 적 어댑터가 구현. EnemyRegistry.Register/Unregister
IDifficultyService: DayPosition SpawnMultiplier HealthMultiplier SpeedMultiplier DamageMultiplier
IStatsSource      : Snapshot() → GameStats{DaysSurvived Kills FoodEaten RottenEaten PlaySeconds}
```
- 통계(`StatsTracker`)는 기존 이벤트만 구독한다: 처치 = `IEnemyActor.Killed`, 먹기 = `FoodConsumer.FoodEaten`(썩음 여부는 `item.IsRotten`).
- 팀원 적: `EnemyAdapter`가 `IDamageable`(휘두르기 피격) + `IDropSource`(죽으면 DropTable 드롭) + `IEnemyActor`를 구현. 스폰은 `EnemyChunkSpawner`가 `IChunkEvents`에 연결. 위험 구역은 스포너가 `IDangerZoneEvents.OnDangerZoneEntered` 구독.

## 클래스
| 클래스 | 책임 |
|---|---|
| `GameStateMachine` | 상태와 허용 전환만 |
| `GameFlowController` | 사망→GameOver, 목표 일수→Clear, Esc 토글, 다시하기/타이틀/종료 |
| `GameplayGate` | 상태별 `timeScale` + 플레이어 입력 on/off |
| `FlowScreens` + `TitleView/PauseView/ResultView/DayBannerView/ControlsHintView/HurtVignetteView` | 화면 (UITheme 폰트·색, 코드로 생성) |
| `StatsTracker`, `ScoreCalculator` | 통계 수집 / 점수 공식(수치는 `ScoreRules`) |
| `DifficultyService` | 일차·낮밤 배율 (`DifficultyProfile`) |
| `LoadoutGiver` | 시작 음식 지급 (`StartingLoadout`) |
| `PlayerHurtFeedback` | 체력 감소 시 비네트 + 카메라 흔들림 + 플래시 |
| `AudioManager`, `AudioEventBinder` | BGM 크로스페이드(낮/밤/위험/게임오버) / 효과음 이벤트 연결 (`AudioCatalog`) |
| `GameFlowRoot` | 위 전부를 조립만 하는 지점 (씬에 `GameFlow` 오브젝트 1개) |
| `EnemyAdapter`, `EnemyChunkSpawner`, `EnemyIntegrationSettings` | 팀원 적 연결 (Integration) |

## 유니티에서 할 일 (순서대로)
- [ ] Console에 컴파일 에러 없는지 확인 (이 환경에서는 Unity 없이 `dotnet build`로만 검증)
- [ ] `Tools > ShelfLife > Setup Game (All In One)` — ArtV2Test를 복사해 `Assets/ShelfLife/Scenes/Game.unity`를 만들고 흐름·적·소리·제출 설정까지 설치 (테스트 더미/픽업/DebugHud는 이 씬에서 제거). 이미 만든 씬에는 개별 메뉴: `Install Game Flow To Open Scene` → `Install Enemies To Open Scene`
- [ ] `Game.unity`를 열고 Play (Title부터 시작). EditMode 테스트: Test Runner > `FlowTests`(9개)
- [ ] Build Settings에 씬이 없어도 에디터에서는 다시하기가 동작한다(`LoadSceneInPlayMode`)

## 수치 위치
| 무엇 | 에셋 |
|---|---|
| 시작 음식 (통조림 300 / 빵 120 / 생선 45초) | `Assets/ShelfLife/Data/Flow/StartingLoadout.asset` |
| 목표 일수(5) | `…/Flow/SurvivalGoal.asset` |
| 일차별 스폰량/체력/속도/데미지 곡선 + 밤 배율 | `…/Flow/DifficultyProfile.asset` (x = 경과 일수, 0 = 1일차 새벽) |
| 점수 공식 | `…/Flow/ScoreRules.asset` |
| 문구, 조작 힌트(10초), 일차 배너 시간, 피격 연출 | `…/Flow/FlowSettings.asset` |
| BGM/효과음 클립, 위험 BGM 기준(반경 8, 적 4마리) | `…/Flow/AudioCatalog.asset` |
| 적 체력·데미지 단위 변환, 청크당 적 수, 최대 수, 스폰 거리, 위험 구역 증가량 | `Assets/ShelfLife/Data/Integration/EnemyIntegrationSettings.asset` |
| 적 종류별 원본 수치(체력/속도/데미지) | 팀원 `Assets/JCY/02 SO/Enemy/EnemySO*.asset` (건드리지 않음) |

## 빌드 방법 (제출용)
1. `Tools > ShelfLife > Prepare Submission Settings` (올인원에 포함됨): Build Settings 씬 목록 = `Game` 하나, 1920×1080, 전체화면 창(Alt+Enter 전환).
2. 폰트 교체 (이 문서 맨 위 경고).
3. Build Profiles/Build Settings → Windows → Build. **Development Build를 끄면** F1 디버그 메뉴, `DebugHud`, `MapDebugOverlay`가 빈 껍데기가 된다(켜면 동작).
4. 종료 버튼: 빌드에서는 `Application.Quit()`, 에디터에서는 Play 중지.
5. 확인된 것: 릴리스 분기(`UNITY_EDITOR` 미정의) 컴파일 에러 0. **실제 빌드는 해보지 못했다.**

## 플레이해서 확인할 항목
- [ ] 타이틀: 어두운 낮 맵 위에 제목/시작/종료/조작법 한 줄. 시작 버튼(또는 Enter/Space)으로 시작, 종료 버튼 동작
- [ ] 시작: 슬롯 1~3에 통조림/빵/생선 (신선도 100%), 첫 실행에만 10초 조작 힌트 (다시 보려면 PlayerPrefs `ShelfLife.ControlsHintShown` 삭제), "1일차" 배너
- [ ] 적: 화면 밖에서 나타나 추격, 휘두르면 맞고 넉백·플래시, 죽으면 음식 드롭(강한 적 → 짧은 유통기한 음식 쪽), 쓰레기 더미 근처에서 적이 늘어남
- [ ] 적에게 맞으면 체력 감소 + 화면 붉은 비네트 + 짧은 흔들림 + 플레이어 플래시. 붙어 있으면 `contactInterval`마다 반복
- [ ] 난이도: 1일차가 확실히 쉽다(적 적고 약함), 밤이 낮보다 어렵다, 일차가 갈수록 증가
- [ ] Esc 일시정지 → 재개 / 타이틀로 / 종료. 일시정지 중 먹기·휘두르기 안 됨, 시계 멈춤
- [ ] 사망 → 게임 오버(생존 일수/처치/먹은 음식/썩은 음식/시간/점수) → 다시하기(바로 시작) / 타이틀. 6일차 새벽 → 클리어 화면
- [ ] 다시하기를 여러 번 반복해도 에러·이중 처리(처치 수 2배, 배너 두 번 등)가 없는지
- [ ] 이 환경에서 확인 못 한 것: 위 전부 + Light2D에서 적 스프라이트 밝기, 버튼 클릭(`InputSystemUIInputModule`)


---

# 추가: 냉장고 / 모닥불 상호작용 (Landmark / Survival / Flow / Integration / UI)

설계 근거 `docs/meetings.md` D42~D50, 필요한 도트 `docs/art_needed.md`. **팀원 파일(`Assets/JCY/`) 수정 0건** — 적 쪽은 `EnemyAdapter`(내 코드)가 기존 `KnockbackReceiver`/`IKnockbackable`로만 연결. 기존 이벤트/인터페이스 이름 변경 없음(추가만).
기존 코드 수정: `FoodItem`(강화 플래그/배율), `FoodInventory`(강화 시 `Changed` 재발행), `LandmarkContext/Spawner`(참조 2개), `DumpDangerEffect`(안전지대 우선), `ZoneRingView`(링 반경), `EnemyChunkSpawner`/`EnemyAdapter`(안전지대), `AudioEventBinder`/`GameFlowRoot`(이벤트 연결), `FoodSlotView`/`FoodDetailPanelView`(배지), `UITheme`(배지 필드), `Flow` asmdef(Landmark 참조 추가).

## 새 이벤트 / 인터페이스
```csharp
using ExpiryGame.Landmark;
ILandmarkEvents     : OnFoodEnhanced(FoodEnhancedInfo{Food Position HealBefore HealAfter})  OnFridgeOpened(FridgeOpenedInfo{Position})   // LandmarkEvents 컴포넌트(씬에 1개, Install Landmarks가 만든다)
ILandmarkEventSink  : FoodEnhanced(...) FridgeOpened(...)             // 효과 컴포넌트가 발행 (LandmarkContext.Events)
ISafeZone           : Center Radius RepelEnemies RepelDistance RepelIntervalSeconds   // CampfireSafeZone이 구현
SafeZoneRegistry    : Register/Unregister / TryFind(pos, out ISafeZone) / IsSafe(pos)  // 로드된 모닥불
SafeZoneSet         : From(IEnumerable<LandmarkInfo>) / Contains(pos)                  // 배치 정보만으로 (청크 스폰 제외용)
FoodEnhancer        : Check(FoodItem, cooldownLeft) → EnhanceCheck{Block CooldownLeft HealBefore HealAfter}  Apply(FoodItem)
CooldownTimer       : Start(now, seconds) Remaining(now) IsReady(now)

using ExpiryGame;   // Survival
FoodItem : IsEnhanced HealMultiplier DamageMultiplier  TryEnhance(heal, damage, shelfLifeBonusRatio)  event Enhanced(FoodItem)
```
- `CurrentHeal/CurrentDamage`는 신선할 때 `Stats × 배율`. 유통기한→스탯 변환(`FoodStatCalculator`)은 그대로이고 복사하지 않았다.
- 팀원 쪽에서 쓸 만한 것: 강화/냉장고 개봉은 `FindFirstObjectByType<LandmarkEvents>()`의 `OnFoodEnhanced/OnFridgeOpened` 구독. 안전지대 판단은 `SafeZoneRegistry.IsSafe(pos)`.

## 클래스 / SO
| 클래스 | 책임 |
|---|---|
| `FridgeLootEffect` | F로 `fridgeTable` 1회 드롭 → "비어 있음", 글로우 끔 |
| `FridgeDecayEffect` | (기존) 반경 안 유통기한 ×0.3. 이제 `decayEnabled`로 끌 수 있음 |
| `CampfireEnhanceEffect` | F로 선택 슬롯 음식 강화 + 쿨다운 + 프롬프트(변화량/사유) |
| `CampfireSafeZone` | 안전지대 등록 (`SafeZoneProfile`) |
| `EnemyChunkSpawner` / `EnemyAdapter` | 안전지대 안 스폰 제외 / 들어온 적 밀어내기 |
| `EnhanceFeedback`, `AudioEventBinder` | 불꽃 파티클 / 효과음 (이벤트 구독만) |
| `EnhanceBadge` | 슬롯·상세 패널의 작은 배지 (임시 도형) |

## 수치 위치
| 무엇 | 에셋 / 필드 |
|---|---|
| 강화 배율 1.3/1.3, 유통기한 +50%, 쿨다운(10초), 프롬프트·사유 문구 | `Assets/ShelfLife/Data/Landmark/EnhanceProfile.asset` |
| 안전지대 반경(4.5), 밀어내기 ON/OFF·거리·간격 | `…/Landmark/SafeZoneProfile.asset` |
| 냉장고 드롭 표 (신선도 90~100%, 2~3개) | `…/Landmark/DropTable_Fridge.asset` |
| 냉장고: 유통기한 감소 on/off·0.3배, 드롭 굴림 수·위협도, 빈 문구 / 상호작용 거리 | `…/Landmark/Landmark_Fridge.asset` |
| 모닥불: 위 두 SO 연결, 회복량, 상호작용 거리 | `…/Landmark/Landmark_Campfire.asset` |
| 불꽃 파티클 개수·속도, 효과음 클립(강화/냉장고 열기) | `…/Flow/FlowSettings.asset`, `…/Flow/AudioCatalog.asset` |
| 배지 색/스프라이트 | `Assets/ShelfLife/UI/UITheme.asset` (`enhancedBadgeColor`, `sprites.enhanceBadge`) |

## 유니티에서 할 일
- [ ] Console에 컴파일 에러 없는지 확인 (이 환경에서는 Unity 없이 `dotnet build`로만 검증)
- [ ] **열려 있는 씬마다** `Tools > ShelfLife > Install Landmarks To Open Scene` 한 번 더 (새 SO 3개 생성, 냉장고/모닥불 프리팹에 컴포넌트 추가, 씬에 `LandmarkEvents` 추가). HUD/적 재설치는 필요 없음
- [ ] Test Runner > EditMode > `FridgeCampfireTests` (13개)


## 추가: 적 스폰 진단 / 방어 (Flow / Integration / MapGen / DebugTools)
- `IChunkEvents.BoundsOf(coord)` + `ForEachLoaded(...)`: 구독 전에 이미 로드된 청크를 소급 처리 (`EnemyChunkSpawner.OnEnable`이 사용). 새 `IChunkEvents` 구현은 `BoundsOf`를 구현해야 한다.
- `DifficultyProfile.EnsureCurves()`: 비어 있는 곡선을 기본값으로 채움 (설치 / OnValidate / `DifficultyService.Awake`). `minSpawnMultiplier`(기본 0.2)가 스폰 배율의 하한.
- `SpawnDiagnostics` / `SpawnBlockReason` / `ISpawnDebug` (`Flow/Spawning`): 막힌 이유별 카운터. `EnemyChunkSpawner`가 구현. 시작 시 필수 참조가 비면 `Debug.LogError`로 무엇이 비었는지 한 번에 알려 주고 스폰을 멈춘다.
- F1 디버그 메뉴에 "적 스폰 상태" 패널: 활성/최대, 배율, 직전 결과, 이유별 카운터, 강제 스폰, 전체 정지/재개.
- **유니티에서 할 일**: `Tools > ShelfLife > Setup Game (All In One)` 실행 후 저장 → `Assets/ShelfLife/Scenes/Game.unity`를 열고 Play. 테스트는 Test Runner > EditMode > `SpawnPathTests`.


---

# 추가: 넉백 약화 + 먹기 진행 바 (Combat / Survival / UI / Integration)

설계 근거·원인 조사 수치 `docs/meetings.md` D52~D58. **팀원 파일(`Assets/JCY/`) 수정 0건.**
기존 이름은 그대로(`KnockbackReceiver`, `FoodConsumer`, `EnemyAdapter`, `HitFeelProfile` …). 바뀐 계약은 아래 두 줄뿐.
- `IKnockbackable`: `Resistance`(0~1)와 `ApplyKnockback(dir, distance, KnockbackProfile)` 오버로드 추가. `IsKnockedBack`은 이제 "밀림 + 경직" 동안 true.
- `IPlayerInput`: `EatHeld`(E를 누르고 있는 동안 true) 추가. `HitFeelProfile.knockbackDistance`는 `KnockbackProfile`로 이사(방향 섞기 `knockbackRadialBlend`는 그대로).

## 클래스 / SO
| 클래스 | 책임 |
|---|---|
| `KnockbackProfile` (SO) | 넉백 수치 전부: 기본 거리·데미지 보너스 곡선·최대 거리·지속시간·ease-out·최대 속도·경직·연속 피격 감쇠 |
| `KnockbackMotion` | 넉백 한 번의 계산(순수 클래스, 테스트 대상). 상한/감쇠/누적 방지/속도 제한/장애물 정지/경직 |
| `KnockbackReceiver` | 계산된 변위를 `MovePosition`으로 적용, `Rigidbody2D.Cast`로 장애물·다른 적 앞에서 멈춤 |
| `EatSettings` (SO) | 길게 누르는 시간(0.4초), 진행 바 크기/위치/색, 안내 문구 |
| `EatHoldTimer` | 길게 눌러 먹기 상태 머신(순수). 놓으면 즉시 취소 |
| `EatAdvisor` | 체력 가득 / 썩은 음식 판정 + 안내 문구 (먹기를 막지는 않음) |
| `FoodConsumer` | `EatHoldTimer`로 먹기. `EatStarted/EatCanceled/FoodEaten`, `Progress01` 노출 |
| `EatProgressView` (UI) | 먹는 중에만 머리 위(월드 좌표 캔버스)에 바 + 안내 문구. 씬 로드 직후 자동 부착 |

## 수치 위치
| 무엇 | 에셋 / 필드 |
|---|---|
| 타격 넉백: 거리 0.3~0.6(상한 1) · 지속 0.1초 · ease-out · 경직 0.15초 · 연속 피격 감쇠 · 최대 속도 | `Assets/Resources/ShelfLife/Knockback_Hit.asset` |
| 모닥불 밀어내기(훨씬 약하고 느리게, 경직 없음) | `Assets/Resources/ShelfLife/Knockback_SafeZone.asset` (밀어낼 거리·간격은 기존 `SafeZoneProfile`) |
| 먹기 길게 누르기 0.4초 · 바 크기/높이/색 · 안내 문구 | `Assets/Resources/ShelfLife/EatSettings.asset` |
| 적별 넉백 저항(0~1, 기본 0) | `EnemyAdapter.knockbackResistance`(프리팹) — EnemySO에 같은 이름 필드가 생기면 그 값이 우선 |
| 스윙 반동 | `SwingProfile.recoilDistance` (플레이어 `KnockbackReceiver.defaultProfile` 수치를 따름) |

연결이 비어 있어도 `Resources/ShelfLife/…` → 코드 기본값 순으로 찾으므로 설치 메뉴를 다시 돌리지 않아도 바로 적용된다.
(`Install Combat…`/`Install Enemies…`를 다시 실행하면 인스펙터 칸에도 연결해 준다.)

## 플레이해서 확인할 항목
- [ ] 허수아비/적을 때렸을 때 한 번에 밀리는 거리가 약 0.3~0.6타일(대략 반 칸), 가장 센 음식도 1칸을 넘지 않는다. 연속으로 때리면 점점 덜 밀린다 — `Knockback_Hit.asset`: `baseDistance`, `bonusDistanceAtMaxDamage`, `maxDistance`, `comboFalloff`
- [ ] 맞은 적이 잠깐(0.15초) 멈칫하고 바로 접촉 데미지를 주지 않는다 — `staggerSeconds`. 타격감(히트스톱/흔들림/플래시)은 그대로인지 — 약하면 `duration`/`staggerSeconds`를 올리고 거리는 두기
- [ ] 벽/나무/다른 적에 붙어서 때려도 뚫고 밀리지 않고 그 앞에서 멈춘다
- [ ] 모닥불 안쪽에 있는 적이 휙 날아가지 않고 천천히 경계 밖으로 나간다 — `Knockback_SafeZone.asset`: `maxSpeed`, `duration`
- [ ] E를 누르면 플레이어 머리 위에 작은 바가 차오르고(0.4초) 다 차면 먹는다. 중간에 놓으면 바로 사라진다 — `EatSettings.asset`: `holdSeconds`, `barSize`, `headGap`
- [ ] 체력이 가득일 때 / 썩은 음식을 먹을 때 바 위에 사유 문구가 뜬다 (한글이 깨지면 `UITheme.font` 확인)
- [ ] 하단 패널 옆에 먹기 관련 바가 더 이상 뜨지 않는다 (원래부터 먹기용 바 오브젝트는 없었음 — meetings D57)
- [ ] Test Runner > EditMode > `KnockbackEatTests` (17개)


---

# 추가: 텍스트 박스 맞춤 + 모닥불 연료 (UI / Landmark / Flow / DebugTools)

설계 근거 `docs/meetings.md` D59~D68. **팀원 파일(`Assets/JCY/`) 수정 0건.** 기존 이름 유지(추가만): `ILandmarkEvents`에 `OnCampfireStageChanged`, `ILandmarkEventSink`에 `CampfireStageChanged`, `EnhanceBlock`에 `CampfireOut/LowFuel`, `SafeZoneSet.From(landmarks, store = null)`(옵션 인자).

## 1. 텍스트가 박스에 맞게
| 클래스 | 책임 |
|---|---|
| `UITextFit` | 순수 계산: 도트 배율 단위 올림(`SnapUp`), 패널 크기(`PanelSize`), 폰트 정수배(`SnapFontSize`), 고정 박스용 줄임(`ConfigureShrink`) |
| `AutoSizePanel` | 안내 박스(InteractPrompt): 글자 크기 + 패딩에 맞춰 늘어나고, 최대 폭을 넘으면 줄바꿈 + 높이 증가. 크기는 `pixelScale` 배수(가운데 피벗은 2배수)로만 변해 9-slice가 안 뭉개짐 |
| `AutoWidthPanel` | 음식 상세 패널: 가장 긴 행에 맞춰 가로로 늘어남(`detailMaxWidth`까지) |
| `ThemedText` | 고정 박스 글자(슬롯 숫자, 체력/시간, 일차, 상세 행, 랜드마크 라벨): 넘치면 최소 비율까지 줄이고 그래도 넘치면 `…`. 폰트 정수배 스냅, 줄 간격 |
| `FlowUi.Text` | 타이틀/일시정지/게임오버/클리어/버튼/일차 배너 문구도 같은 방식으로 줄임 |
- 씬 재설치 없이도 동작: `InteractPromptView`/`FoodDetailPanelView`가 실행 시 필요한 컴포넌트를 스스로 붙인다. (HUD를 새로 설치하면 설치 코드가 같은 컴포넌트를 붙인다.)
- 수치 위치: `Assets/ShelfLife/UI/UITheme.asset` → "텍스트 박스 맞춤"(패딩 X/Y, 최소 폭/높이, **안내 박스 최대 폭**, **상세 패널 최대 폭**, 줄임 최소 비율), "폰트 렌더링"(`fontSnapUnit`, `lineSpacing`, `panelTextAlign`).
- F1 디버그 메뉴 → "안내 문구 순환 표시": 짧은/긴/여러 줄/영문/숫자 문구를 1.5초마다 박스에 띄운다(이전/다음 버튼도 있음). 에디터/Development Build 전용.

## 2. 모닥불 연료
| 클래스 | 책임 |
|---|---|
| `CampfireFuelProfile` (SO) | 모든 수치: 최대 연료, 반경 안 초당 감소, 반경 밖 초당 회복, 밤 배율, 재점화 임계값, 굽기 1회 소모량, 단계 경계, 바닥일 때의 빛/안전지대/회복 배율, 게이지 설정 |
| `CampfireFuelModel` | 연료 규칙(순수): `Tick`, `TryConsume`, `Stage`, `Strength(atEmpty)`, `CatchUpAway` |
| `CampfireStateStore` / `CampfireState` / `LandmarkId` | 랜드마크 ID(종류+위치)로 키를 잡는 상태 보관. `LandmarkSpawner`가 씬마다 새로 만든다 |
| `CampfireFuel` (컴포넌트) | **연료 상태만**. 다른 컴포넌트가 `Ratio/Stage/IsLit/Strength()`를 읽는다 |
| `CampfireLight`, `CampfireHealEffect`, `CampfireSafeZone`, `ZoneRingView`, `CampfireAppearance`, `CampfireFuelGauge` | 각자 연료를 읽어 반응(빛 반경·밝기 / 회복량 / 안전지대 반경 / 링 크기 / 스프라이트 단계+페이드+글로우 / 월드 게이지) |
| `CampfireEnhanceEffect` | 꺼짐/연료 부족이면 굽기 불가(프롬프트 "꺼짐"), 굽기 성공 시 연료 소모 |
| `CampfireStageFeedback` (Flow) | 단계가 바뀔 때 스파크 파티클 (이벤트 구독만) |
- 단계 스프라이트: 가득 `landmark_campfire_0~2`(기존 루프) / 약해짐 `_low_0~1` / 잉걸불 `_embers` / 꺼짐 `_out` — `Assets/Art/Sprites/landmarks/`로 옮겨서 기존 임포트 규칙(PPU 32, Point, 피벗 바닥)을 받는다.
- `LandmarkData.fuelProfile`을 비우면 연료 없는 모닥불(이전 동작) — 안전망.

### 수치 위치
| 무엇 | 에셋 / 필드 |
|---|---|
| 연료·감소·회복·밤 배율·재점화 임계값·굽기 소모·단계 경계·바닥 배율·페이드·게이지(표시 50%/깜빡임 20%) | `Assets/ShelfLife/Data/Landmark/CampfireFuelProfile.asset` |
| 안전지대 최대 반경, 반경 변화 속도 `radiusChangePerSecond` | `…/Landmark/SafeZoneProfile.asset` |
| 굽기 불가 사유 문구 ("꺼짐", "연료 부족") | `…/Landmark/EnhanceProfile.asset` |
| 단계 전환 파티클 개수/속도 | `…/Flow/FlowSettings.asset` ("모닥불 연료 단계 전환 연출") |

## 유니티에서 할 일
- [ ] Console에 컴파일 에러 없는지 확인 (이 환경에서는 Unity 없이 `dotnet build`로만 검증)
- [ ] 열린 씬마다 `Tools > ShelfLife > Install Landmarks To Open Scene` 한 번 (연료 SO 생성, 모닥불 프리팹에 연료/게이지/외형 컴포넌트와 단계 스프라이트 연결). HUD 재설치는 필수 아님 — 새 HUD 설치 코드를 쓰려면 `Install HUD To Open Scene`
- [ ] Test Runner > EditMode > `CampfireFuelTests` (15개), `FridgeCampfireTests`
- [ ] (제출 전) 위 폰트 안내 참고
