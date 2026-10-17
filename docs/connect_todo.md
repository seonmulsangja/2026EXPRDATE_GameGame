# 증강 연결 TODO (connect_todo)

증강 통합(`docs/augment_merge_plan.md`)에서 **우리 능력치 파이프라인(StatKey)으로 옮기지 못했거나, 우회 없이는 못 붙인 것**. 억지로 우회하지 않고 여기에 남긴다.
팀원 파일은 수정하지 않는다는 조건 때문에 "팀원이 고쳐야 하는 것"과 "우리 쪽에 연결 지점을 새로 만들어야 하는 것"을 나눠 적었다.

## 개수 요약
| 구분 | 개수 | 내용 |
|---|---|---|
| 증강 총 종류 | 6 | DamageUp, MoveSpeedUp, AttackSpeedUp 1, FoodUpSkill, HealSkill_JCY, BloodAugmentSO |
| **StatKey 파이프라인으로 연결됨** | **3** | 공격력 강화, 사뿐한 발걸음, 숙달된 공격 |
| **스킬 부여로 연결됨** (팀원 `PlayerSkillController`가 실행, StatKey 없음) | **3** | 비축하기, 회복의 가호, 혈액 보충 — 부여는 확인함. 아래 T1·T2에 동작상 문제가 있다 |
| 선택 화면 진입 경로 | 레벨업 1 / 상자 0 | 레벨업은 연결, **증강 상자(AugmentChest)는 미연결** (T3) |
| TODO 항목 | 8 | 아래 표 |

## TODO

| # | 증강 / 대상 | 효과 | 필요한 연결 지점 | 어디를 어떻게 고쳐야 하는가 | 난이도 |
|---|---|---|---|---|---|
| T1 | **비축하기** (FoodUpSkill → FoodEatUPSkill1) | 음식을 **먹을 때마다** 최대 체력 + 레벨 | ① 발동 시점: 팀원은 `PlayerInputReader.EatPerformed`(입력 액션의 performed)에 걸었는데, 실제 먹기 성공은 `FoodConsumer.FoodEaten`이다. 먹기를 중간에 놓거나 음식이 바뀌어 취소돼도 performed가 먼저 불리면 체력이 오른다 ② 최대 체력을 올리는 `StatKey`가 없어 `Health.IncreaseMaxHealth`를 직접 부른다(누적 상한/초기화 규칙 없음) | 우리 쪽: `StatKey.MaxHealth` 추가 + `PlayerStatApplier`가 `Health`에 반영하는 줄 추가(`Health`에 최대 체력 setter 필요 — 팀원이 넣은 `IncreaseMaxHealth`는 누적만 가능). 팀원 쪽: `PlayerSkillController.OnEat`를 `FoodConsumer.FoodEaten` 구독으로 바꾸거나, `FoodEatUPSkill.Execute`가 아니라 우리 어댑터가 `FoodEaten`에서 호출. **지금은 `EatPerformed` 시점 그대로 둠** | 중 |
| T2 | **혈액 보충** (BloodAugmentSO → __Blood_SKill) | 체력 20% 이하일 때 체력 + 레벨×10, 120초 쿨 | 팀원 `PlayerSkillController.SkillRoutine`: `Blood_SKill` 분기가 `Execute`를 부른 뒤 **회복 여부와 상관없이** `nextActivationTime = Time.time + 120`을 건다 → 스킬을 얻은 직후(체력이 가득일 때) 한 번 헛발동하고 **120초 동안 잠긴다**(플레이 검증에서 체력 10%인데 회복 안 됨을 확인) | 팀원이 `Execute`가 실제로 발동했는지(체력 조건 충족)를 돌려받아 그때만 쿨을 걸도록 수정. 우리 쪽 우회는 하지 않음. 우리 쪽 연결 지점이 필요하면 `Health.HealthChanged` 구독으로 "20% 이하 진입" 이벤트를 받아 발동하는 방식이 정석 | 하 (팀원 수정) |
| T3 | **증강 상자** (`AugmentChest`, `AugmentChestSpawner`) | 상자에 닿으면 증강 선택 | 상자가 `AugmentManager.OpenAugmentSelection()`을 **직접** 불러 상태 머신(AugmentSelect)·입력 차단·타임스케일 관리를 거치지 않는다. 우리 선택 화면 흐름과 섞이면 입력이 안 막히고 완료 처리(효과 적용)도 빠진다 → Game.unity에는 스포너를 넣지 않았다 | 우리 쪽: `LevelUpCoordinator`에 "외부 요청"(`RequestExtraSelection()`)을 추가해 큐에 한 칸 넣고, 상자는 `ILevelUpLike`/이벤트로 요청만 하도록 어댑터(`AugmentChestAdapter`) 작성. 팀원 쪽: 상자가 `OpenAugmentSelection()` 대신 이벤트를 쏘면 가장 깔끔 | 중 |
| T4 | **이동 속도 / 공격 속도 수치** | `MoveSpeedUp`(설명 "레벨×0.5", SO value 1), `AttackSpeedUp 1`(설명 "레벨×0.025", SO value 1) | SO `value`와 설명 문구가 불일치. SO를 못 고쳐서 `AugmentIntegrationSettings`의 배율(0.5, 0.025)로 설명에 맞췄다 | 팀원이 SO `value`를 0.5 / 0.025로 고치면 `AugmentIntegrationSettings`의 두 배율을 1로 | 하 |
| T5 | **팀원 AugmentManager에 의존하는 우회** | 선택 완료 감지, 후보 지정 | 팀원 쪽에 ① 선택 완료 이벤트 ② 후보 지정 함수가 없어 `TeammateAugmentChooser`가 매 프레임 레벨 변화를 확인하고 `allAugments`/`_isSelecting`/`_currentChoices`(private)를 리플렉션으로 만진다 | 팀원이 `public event Action<AugmentSO> Chosen`과 `OpenAugmentSelection(IReadOnlyList<AugmentSO> candidates)`를 추가하면 `TeammateAugmentChooser.ManagerAccess`와 `Tick()` 폴링을 지우고 이벤트 구독으로 교체(어댑터 한 파일). 필드 이름이 바뀌면 에러 로그를 남기고 대체 보상으로 진행(게임은 멈추지 않음) | 중 |
| T6 | **팀원 `AugmentState`** | 선택 결과 적용 | `Awake`에서 쿨다운 0.4를 "감소량"으로 `FoodWeapon`에 넘겨 시작부터 쿨다운이 최소가 되고, 이동 속도를 직접 덮어쓴다 → 플레이어에 붙이지 않고 별도 오브젝트에 둠(기록용). 스킬 증강을 고를 때 "PlayerSkillController가 플레이어에 없습니다" 경고가 한 줄 찍힌다(무해) | 팀원이 `AugmentState`의 스탯 적용 부분을 이벤트(`StatChanged`)로 바꾸고 `AcquireSkill`의 경고를 `_skillController == null` 아닐 때만 내면 경고 제거 | 하 |
| T7 | **팀원 선택 화면 씬 배선** | `Test_JCY.unity`의 `AugmentCanvas` | 팀원 씬은 `AugmentCanvas` 루트가 꺼져 있고 `AugmentUI.slots`·`AugmentManager.allAugments`가 비어 있다(저장 시점에 배선이 빠진 듯) — 그대로면 팀원 테스트 씬에서도 클릭해도 아무 일이 없다. 우리 설치 메뉴가 프리팹 복사본에서 채운다 | 팀원 씬은 건드리지 않음. 팀원에게 알릴 것 | 하 |
| T8 | **선택 화면 외형** | 폰트/색/크기 | 팀원 UI는 자체 폰트·색이라 우리 `UITheme`(도트 폰트, 어두운 톤)와 다르다. 후보 설명 길이에 대한 글자 맞춤(`UITextFit`)도 없다 | 아트 정리 때 `AugmentCanvas` 프리팹(`Assets/ShelfLife/Prefabs/AugmentCanvas.prefab`, 우리 사본)의 TMP들을 `ThemedText`로 바꾸고 슬롯 크기를 맞춘다 | 하~중 |
