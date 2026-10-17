# 증강 시스템 통합 계획 (augment_merge_plan)

팀원(JCY)이 만든 증강 시스템을 레벨업 흐름에 연결한 기록. 결정 근거는 `docs/meetings.md` D81~D86, 못 연결한 것은 `docs/connect_todo.md`.
**팀원 파일은 수정하지 않았다.** 팀원 타입을 아는 코드는 `Assets/Scripts/Integration/`(Assembly-CSharp)에만 있고, 나머지(Flow)는 인터페이스로만 만난다.

## 0. 병합 결과
- 시작 전: 미커밋 변경 없음(작업트리 깨끗). 백업 브랜치 `backup/pre-augment-merge-20261017` 생성.
- `git fetch` 후 `origin/JCY`(54ac6b7)를 `LDY`에 병합: **충돌 없음**.
- 병합 직후 **컴파일 에러 1건 발견·수정**: 팀원이 `PlayerMover`에 `MoveSpeed` 속성을 추가했는데 우리 쪽에 이미 읽기 전용 `MoveSpeed`가 있어 같은 이름이 두 번 정의됨(우리 파일이라 우리 쪽을 정리). 읽기/쓰기 하나만 남김.
- 팀원이 우리 파일에 넣은 변경 3건(확인함, 그대로 둠): `FoodWeapon.SetAttackStats(power, cooldown)`, `Health.IncreaseMaxHealth(amount)`, `PlayerMover.MoveSpeed` setter.

## 1. 팀원 증강 시스템 조사

### 1-1. 파일
| 파일 (Assets/JCY) | 역할 |
|---|---|
| `01 Script/Augment/AugmentSO.cs` | 증강 데이터 SO (`augmentName, description, icon, augmentType, effectType, value, skill, maxLevel`) |
| `AugmentManager.cs` | 후보 뽑기 + 선택 검증 + 증강별 레벨 기록 (`OpenAugmentSelection()` / `ChooseAugment()` / `GetAugmentLevel()`) |
| `AugmentUI.cs`, `AugmentSlotUI.cs` | 선택 화면(패널 + 슬롯 3개, 클릭 버튼). 열 때 `Time.timeScale=0`, 닫을 때 복원 |
| `AugmentState.cs` | 선택 결과 적용 (공격력/이동속도/쿨다운 수치 + 스킬 레벨) |
| `PlayerSkillController.cs`, `SkillSO.cs`, `SkillActionSO.cs` | 스킬 증강의 런타임: 코루틴으로 주기 발동, 먹기 이벤트 연동 |
| `AugmentChest.cs`, `AugmentChestSpawner.cs` | 화면 밖에 상자 생성 → 플레이어가 닿으면 `OpenAugmentSelection()` (레벨업과 무관한 두 번째 진입점) |
| `02 SO/Augment/*.asset`, `02 SO/Skill/*.asset` | 증강 6종 / 스킬 3종 + 액션 SO |
| `Test_JCY.unity` | 선택 화면은 **프리팹이 아니라 씬 오브젝트** (`AugmentCanvas` 하위). 매니저(`AugmentManager`)는 별도 오브젝트 |

### 1-2. 데이터 구조
- `AugmentType { Stat, Skill }`, `AugmentEffectType { AttackPower, MoveSpeed, AttackSpeed }`.
- Stat 증강: `effectType` + `value`. 고를 때마다 `value`가 **한 번씩 더해진다** (레벨 N이면 value × N).
- Skill 증강: `skill`(SkillSO)을 넘겨 `PlayerSkillController.AddOrLevelUpSkill(skill, level)`. 표시 이름/설명/아이콘은 SkillSO 것을 쓴다.
- `maxLevel`(기본 5)이 최대 중첩. 스킬 증강은 `SkillSO.maxLevel`도 확인.

### 1-3. 증강 목록과 바꾸는 값
| 증강 SO | 화면 이름 | 종류 | 바꾸는 값 | SO `value` | maxLevel | 우리 쪽 연결 |
|---|---|---|---|---|---|---|
| DamageUp | 공격력 강화 | Stat | 공격력 | 1 | 5 | `StatKey.AttackPower` +1/레벨 |
| MoveSpeedUp | 사뿐한 발걸음 | Stat | 이동 속도 | 1 (설명은 "레벨×0.5") | 5 | `StatKey.MoveSpeed` +0.5/레벨 |
| AttackSpeedUp 1 | 숙달된 공격 | Stat | 공격 쿨다운 감소 | 1 (설명은 "레벨×0.025") | 5 | `StatKey.AttackCooldownReduction` 0.025초/레벨 |
| FoodUpSkill | 비축하기 | Skill | 음식을 먹을 때 최대 체력 + 레벨 | — | 5 | 스킬 부여(팀원 런타임) |
| HealSkill_JCY | 회복의 가호 | Skill | 15초(−레벨마다 1초)마다 체력 + 레벨 | — | 5 | 스킬 부여(팀원 런타임) |
| BloodAugmentSO | 혈액 보충 | Skill | 체력 20% 이하일 때 체력 + 레벨×10 (120초 쿨) | — | 5 | 스킬 부여(팀원 런타임) |

- **수치 불일치**: `MoveSpeedUp`/`AttackSpeedUp`의 SO `value`는 1인데 설명 문구는 0.5/0.025다. SO를 고칠 수 없어서 변환 배율(`AugmentIntegrationSettings`)로 설명에 맞췄다 → 아래 "플레이 확인" 참고.
- `HealSkill_JCY` 증강의 `augmentName/description`에 "공격력 강화" 문구가 복붙돼 있지만, 스킬 증강이라 화면에는 SkillSO의 이름("회복의 가호")이 나온다.

### 1-4. 선택 화면 진입 / 결과 전달 / 후보 로직
- **진입**: 함수 호출 `AugmentManager.OpenAugmentSelection()` (이벤트·프리팹 활성화 아님). 안에서 `AugmentUI.ShowAugments(후보[])`가 패널을 켠다.
- **결과 전달**: 이벤트 **없음**. 슬롯 버튼 → `AugmentUI.SelectAugment` → `AugmentManager.ChooseAugment` → `AugmentState.ApplyAugment/AcquireSkill`을 직접 호출하고 패널을 끈다.
- **후보 뽑기**: 전체 목록에서 ① 증강 레벨이 maxLevel이면 제외 ② 스킬 증강은 스킬 레벨이 스킬 maxLevel이면 제외 ③ 섞어서 앞의 `choiceCount`(기본 3)개. **희귀도 없음**, 한 화면 안 중복 없음. 후보가 0이면 로그만 남기고 화면을 열지 않는다(조용히 끝남).
- **입력/캔버스/타임스케일**: 마우스 클릭 버튼(EventSystem 필요), Canvas는 Screen Space Overlay `sortingOrder=1`(HUD 아래로 깔릴 수 있음), 열 때 `timeScale`을 0으로 두고 닫을 때 **열기 직전 값으로 복원**. Animator/DOTween 없음 → unscaled time 문제 없음.

### 1-5. 통합에서 걸리는 점 (팀원 코드를 못 고치므로 우회)
1. 선택 완료 이벤트가 없다 → 어댑터가 매 프레임 레벨 변화/패널 꺼짐을 확인(Tick).
2. 후보를 외부에서 지정할 수 없다 → 후보는 우리가 뽑고, 팀원 매니저의 `allAugments`(private)에 그 후보만 넣고 연다. **리플렉션은 한 클래스(`TeammateAugmentChooser.ManagerAccess`)에만** 있다. 필드 이름이 바뀌면 에러 로그 + 대체 보상으로 진행한다.
3. `AugmentState`는 `Awake`에서 쿨다운 0.4를 "쿨다운 **감소량**"으로 `FoodWeapon`에 넘겨 시작부터 쿨다운이 최소(0.05초)가 된다 + 이동 속도를 직접 덮어쓴다 → **플레이어에 붙이지 않고** 별도 오브젝트(`AugmentBackend (JCY)`)에 둬서 매니저의 기록용으로만 쓴다. 실제 효과는 우리 능력치 파이프라인이 적용.
4. `AugmentChest`는 상태 머신을 거치지 않고 `OpenAugmentSelection()`을 직접 부른다 → Game.unity에는 상자 스포너를 넣지 않는다(레벨업 경로만 연결). 상자를 쓰려면 `connect_todo.md` 참고.
5. **팀원 씬은 배선이 덜 된 채 저장돼 있다**: `AugmentCanvas` 루트가 꺼져 있고(켜지기 전에는 `AugmentUI.Awake`가 안 돌아 패널이 안 숨겨지고, 패널을 켜도 보이지 않음), `AugmentUI.slots`가 비어 있어(슬롯에 클릭 리스너가 안 붙음) 클릭해도 아무 일이 없고, `AugmentManager.allAugments`도 비어 있다. 설치 메뉴가 프리팹 사본에서 루트를 켜고 `slots`를 채우며, 어댑터가 실행 때 루트가 꺼져 있으면 켠다. (실제 클릭 흐름은 플레이 모드 테스트로 확인)
6. 공격력은 `FoodWeapon`의 즉발 판정 경로에만 반영돼 있었다(팀원 수정). 실제 게임은 스윙(`SwingController`/`SwingHitDetector`) 경로라 공격력 증강이 피해에 반영되지 않았다 → `IDamageBonusReceiver`(Survival)로 `FoodWeapon`이 스윙 직전에 보너스를 넘기고 `SwingHitDetector`가 더하도록 연결했다.
7. 스킬 증강을 고르면 팀원 `AugmentState.AcquireSkill`이 "PlayerSkillController가 플레이어에 없습니다" 경고를 한 줄 남긴다(기록용 `AugmentState`가 플레이어가 아니라서). 동작에는 영향 없음. 실제 스킬 부여는 어댑터가 플레이어의 `PlayerSkillController`에 직접 한다.

## 2. 우리 쪽에 이미 있던 것 / 이번에 만든 것
| 항목 | 상태 |
|---|---|
| 경험치/레벨업 이벤트 `IExperienceSource.OnLevelUp` (다중 레벨업 시 레벨마다 한 번) | **있었음** (`ExperienceService`) |
| `GameState.AugmentSelect`, `GameStateMachine.BeginAugmentSelect/EndAugmentSelect` | **있었음**. 이번에 `Fail()`이 선택 중에도 되도록 확장 (선택 중 사망) |
| `IAugmentSelectionPresenter`, `NullAugmentPresenter` (대체 보상: 체력 소량 회복) | **있었음** (이름 유지) |
| `LevelUpCoordinator` (레벨업 큐) | **있었음**. 이번에 "큐가 남았으면 Playing을 거치지 않고 다음 선택으로" 수정 |
| `GameplayGate` (Playing이 아니면 timeScale 0 + 입력 끔) | **있었음** → 선택 중 이동/휘두르기/먹기 차단, Esc는 `TogglePause`가 Playing/Paused에서만 동작해 무시 |
| 능력치 파이프라인 `StatKey` / `IStatModifier` | **없었음 → 신규** (`Flow/Contracts/StatKey.cs`, `IPlayerStats.cs`, `Core/PlayerStatSheet.cs`, `PlayerStatApplier.cs`) |
| 증강 효과 적용/기록 `AugmentEffectService` | **신규** (중첩·최대 레벨·해제·초기화·기록) |
| 대체 보상/에러 복구 `GuardedAugmentPresenter` | **신규** |
| 팀원 어댑터 `TeammateAugmentPresenter/Chooser/Catalog` + `AugmentIntegrationSettings` | **신규** (Integration/) |
| 설치 `Tools > ShelfLife > Install Augments To Open Scene` (Setup Game에서 자동 호출) | **신규** |

## 3. 연결 구조
```
ExperienceService.OnLevelUp ─▶ LevelUpCoordinator (큐) ─▶ GameStateMachine.BeginAugmentSelect (timeScale 0, 입력 끔)
                                   │ presenter.RequestSelection(level, onComplete)
                                   ▼
        TeammateAugmentPresenter (IAugmentSelectionPresenter, Integration)
           └ GuardedAugmentPresenter (Flow)  ── 후보 없음/에러/선택 없이 닫힘 → 대체 보상 or 첫 후보
                └ TeammateAugmentChooser ── 후보 뽑기 → 팀원 AugmentManager.OpenAugmentSelection() → 팀원 AugmentUI
                        Tick(Update): 고름/닫힘 감지 ─▶ AugmentEffectService.Apply(AugmentDefinition)
                                                         ├ 능력치: PlayerStatSheet 수정자 ─▶ PlayerStatApplier ─▶ PlayerMover / FoodWeapon
                                                         ├ 스킬: PlayerSkillController.AddOrLevelUpSkill
                                                         └ 기록: StatsTracker ─▶ 게임오버/클리어 화면 "증강" 줄
        onComplete ─▶ LevelUpCoordinator ─▶ 남은 큐 있으면 다음 선택 / 없으면 EndAugmentSelect (timeScale 1, 입력 켬)
```
- 다시하기/타이틀: `RunEnding` → `experience.Reset(); levelUps.Reset(); augmentEffects.Reset()` 후 씬 리로드 → 모든 수정자·선택 기록·레벨이 초기화된다.
- 선택 중 사망: `Fail()`이 AugmentSelect에서도 GameOver로 전환 → 코디네이터가 열려 있는 선택 화면을 접는다 (`Cancel`). 선택 중에는 timeScale 0이라 피해 자체가 없다(접촉 피해는 물리 기반).
