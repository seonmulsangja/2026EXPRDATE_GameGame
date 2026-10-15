# 유통기한 서바이벌 — 담당 파트 설계 (이동 / 음식 / 낮밤)

네임스페이스 `ExpiryGame`, 코드 위치 `Assets/Scripts/Survival/` (asmdef `ExpiryGame.Survival`).
팀원 코드(Assembly-CSharp)는 이 어셈블리를 자동 참조하므로 `using ExpiryGame;` 만 하면 된다.

## 1. 기능 목록 (우선순위)
| P | 기능 |
|---|------|
| P0 | 플레이어 8방향 이동 (Input System) |
| P0 | 낮밤 타이머 (1일 = 60초, 낮/밤 구분, 이벤트) |
| P0 | 음식 데이터(SO) + 유통기한→스탯 변환 |
| P0 | 시간에 따른 유통기한 감소, 0이면 썩음 |
| P0 | 먹기(체력 회복) / 무기 사용(데미지 + 내구도 감소) |
| P1 | 인벤토리(선택 슬롯), 월드 픽업 |
| P1 | 낮밤 조명 연출 (Light2D 색 그라디언트) |
| P2 | 디버그 HUD, 테스트 씬 자동 생성 에디터 도구 |

## 2. 클래스 구조 (책임 한 줄)
**Core**
- `IDamageable` / `IHealable` — 피해 / 회복을 받을 수 있는 대상의 계약 (적 AI도 구현 가능)
- `Health` — 체력 수치와 변경/사망 이벤트만 관리

**Time**
- `DayCycleConfig`(SO) — 하루 길이(초), 밤 시작 비율 등 낮밤 수치
- `IGameClock` / `GameClock` — 게임 시간을 진행시키고 틱/페이즈/일 변경 이벤트 발행
- `DayNightLighting` — 시간대에 맞춰 글로벌 라이트 색만 바꾸는 뷰

**Player**
- `IPlayerInput` / `PlayerInputReader` — 입력 액션을 읽어 이동값과 행동 이벤트로 변환
- `PlayerMover` — 이동값으로 Rigidbody2D 속도만 설정
- `IFacing` / `PlayerFacing` — 마지막 이동 방향 보관

**Food**
- `FoodData`(SO) — 음식 종류 하나의 고정 데이터 (이름, 아이콘, 유통기한)
- `FoodBalanceConfig`(SO) — 유통기한 범위와 스탯 범위, 썩음 보정 수치
- `IFoodStatCalculator` / `FoodStatCalculator` — 유통기한 → 스탯 변환 수식 구현
- `FoodStats` — 변환 결과 값 묶음 (struct)
- `FoodItem` — 음식 한 개의 런타임 상태 (남은 기한, 내구도, 썩음)
- `IFoodFactory` / `FoodFactory` — FoodData로 FoodItem 생성
- `IShelfLifeTracker` / `ShelfLifeSystem` — 등록된 FoodItem의 유통기한을 시계 틱마다 감소
- `IFoodInventory` / `FoodInventory` — 보유 음식 목록과 선택 슬롯
- `FoodConsumer` — 선택된 음식을 먹어 체력에 반영
- `FoodPickup` — 월드 음식이 플레이어 인벤토리로 들어가게 함

**Combat**
- `FoodWeapon` — 선택된 음식으로 전방 범위를 공격하고 내구도 소모

**Debug / Editor**
- `DebugHud` — 임시 OnGUI 상태 표시 (UI 담당 완성 후 제거)
- `TestSceneBuilder` (Editor) — 메뉴로 테스트 씬/에셋/프리팹 자동 생성

## 3. 팀원 연결용 인터페이스 / 이벤트 (이름 확정, 변경 금지)
```csharp
interface IDamageable { void TakeDamage(float amount); }          // 적이 구현
interface IHealable   { void Heal(float amount); }

interface IGameClock {
  float DayLengthSeconds; float TimeOfDay01; int DayCount; DayPhase Phase;
  event Action<float>    Ticked;        // 매 프레임, 경과 게임초
  event Action<DayPhase> PhaseChanged;  // Day <-> Night
  event Action<int>      DayStarted;    // 새 하루 시작(일차)
  float SecondsToDays(float seconds);   // UI '일' 표기용
}
enum DayPhase { Day, Night }

interface IPlayerInput { Vector2 Move; event Action EatPerformed, AttackPerformed, NextFoodPerformed; }
interface IFacing { Vector2 FacingDirection; }

interface IFoodFactory     { FoodItem Create(FoodData data); }     // 조합 시스템이 결과물 생성에 사용
interface IShelfLifeTracker{ void Register(FoodItem); void Unregister(FoodItem); }  // 조합 결과물 감소 등록
interface IFoodInventory {
  IReadOnlyList<FoodItem> Items; FoodItem Selected; int Capacity;
  bool TryAdd(FoodItem item); bool TryAdd(FoodData data); bool Remove(FoodItem item); void SelectNext();
  event Action Changed;
}

class FoodItem {
  FoodData Data; FoodStats Stats; float RemainingShelfLife; float ShelfLife01; int RemainingDurability;
  bool IsRotten; float CurrentHeal; float CurrentDamage;
  event Action<FoodItem> Rotted; event Action<FoodItem> Broken;
}
class Health { float Current, Max; bool IsDead; event Action<float,float> HealthChanged; event Action Died; }
class FoodConsumer { event Action<FoodItem> FoodEaten; }
class FoodWeapon   { event Action<FoodItem,int> WeaponUsed; /* 맞춘 대상 수 */ }
```

## 4. 음식 ScriptableObject 필드
**FoodData**: `displayName`, `icon`, `shelfLifeSeconds`(최대 유통기한, 초)
**FoodBalanceConfig**:
- `shortestShelfLifeSeconds`, `longestShelfLifeSeconds` — 변환 기준 구간
- `heal`, `damage`, `durability` 각각 `StatRange{ atLongestShelfLife, atShortestShelfLife }`
- `rottenHealMultiplier`(음수 = 먹으면 피해), `rottenDamageMultiplier`
**DayCycleConfig**: `dayLengthSeconds`(60), `nightStartNormalized`, `startTimeNormalized`

## 5. 유통기한 → 스탯 변환 수식 (하나)
```
shortness = Clamp01( (longest − shelfLife) / (longest − shortest) )
stat      = Lerp( atLongestShelfLife, atShortestShelfLife, shortness )
```
heal / damage / durability 모두 동일 수식. 내구도는 반올림 후 최소 1.
썩으면: heal × rottenHealMultiplier, damage × rottenDamageMultiplier.
`shelfLife`는 FoodData의 최대 유통기한(종류 고유값). 남은 기한은 썩음 판정에만 쓴다.

## 6. 구현 순서
1. Core(Health, 인터페이스) + asmdef
2. 이동 (PlayerInputReader, PlayerMover, PlayerFacing)
3. 낮밤 (DayCycleConfig, GameClock, DayNightLighting)
4. 음식 데이터 (FoodData, FoodBalanceConfig, 계산기, FoodItem, 팩토리)
5. 유통기한 감소 (ShelfLifeSystem, 인벤토리, 픽업)
6. 먹기 / 무기 (FoodConsumer, FoodWeapon)
7. DebugHud + 테스트 씬 빌더 (Tools/ShelfLife/Build Test Scene)
8. EditMode 테스트(계산기/FoodItem/시계), handoff.md

## 결정 기록
- 스탯은 최대 유통기한(종류 고유값) 기준으로 계산 — 수식 하나로 끝나고 아이템 스탯이 시간에 따라 흔들리지 않아 UI 설명이 쉬움.
- 썩음은 스탯 배수(음수 회복 포함)로 처리 — 별도 상태 클래스 없이 SO 수치로 조절 가능.
- 입력은 New Input System + `InputActionReference` — 프로젝트가 New 전용이고 기존 InputSystem_Actions를 재사용.
- 이벤트는 C# event 사용 — UnityEvent보다 코드 연결이 빠르고 이름 고정이 쉬움.
- 체력(`Health`)을 내가 만든다 — '먹으면 회복'에 필요하고 적도 재사용 가능.
- 유통기한 감소는 `ShelfLifeSystem`에 등록된 아이템만 — 인벤토리 보유 중에만 시간이 흐르고, 월드 픽업은 정지(구현 단순).
- 시간 진행은 `Time.deltaTime` 기반 — timeScale=0 일시정지가 자동 적용.
- 인터페이스 필드는 직렬화 불가라 인스펙터에는 구체 컴포넌트를 연결하고 내부에서 인터페이스로 사용.
- 공격은 전방 원형 오버랩 근접 판정 — 3일 잼에 투사체/애니메이션 이벤트는 과함.
- 내 코드는 asmdef로 분리(`Assets/Scripts/Survival`) — 팀원의 Assembly-CSharp 코드와 충돌 없이 EditMode 테스트 가능.
- Unity 에디터가 열려 있어 배치 컴파일 검증이 불가 — 에디터 포커스 시 컴파일 에러 확인 필요(handoff에 명시).
