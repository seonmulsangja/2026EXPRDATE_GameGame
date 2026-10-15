# Handoff — 이동 / 음식 / 낮밤

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
조작: 이동 WASD, 먹기 **E (길게 누름 — 템플릿 Interact가 Hold)**, 공격 좌클릭/Enter, 다음 음식 **2**.

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
