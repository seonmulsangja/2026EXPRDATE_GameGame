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
