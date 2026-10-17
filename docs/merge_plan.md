# 팀원 적 시스템 통합 계획 (merge_plan)

작성: 통합 시작 시점 기준. 결과/검증은 맨 아래 "통합 결과". 결정 근거는 `docs/meetings.md` D32~D41.

## 0. 백업
- 통합 전 상태 커밋: `d43edc9 backup: pre-integration state` + 브랜치 `backup/pre-enemy-merge`
- 병합: `git merge origin/JCY` (JCY가 `a9f655a`, `39a762b`, `254076c` 3커밋 앞서 있었음). 충돌 없음, 고아 `.meta` 없음 확인.
- 되돌리기: `git reset --hard backup/pre-enemy-merge` (병합 + 이후 작업 전체 폐기)

## 1. 팀원 폴더 조사 (`Assets/JCY/`)
| 항목 | 내용 |
|---|---|
| 적 종류 | `EnemySO` 5종 — 괴물(체력10/속도5), 해골 유령(15/3), 버섯 몬스터(5/8), 땅벌레 괴물(10/7), 사신 괴물(3/10). 데미지는 전부 1. 외형은 `SpriteLibraryAsset`(EnemyMonster1~5.spriteLib, 카테고리 `Chase` 4프레임) |
| 체력/피격/사망 | `Enemy._health`(int)만 있고 **피격 처리가 없다**. 구버전은 `TackDamage(int)`→풀 반환이었으나 최신(origin/JCY)에서는 메서드 자체가 삭제됨. 사망 이벤트 없음 |
| 플레이어 공격 | `Enemy.OnCollisionEnter2D`에서 태그 `Player` + `ExpiryGame.Health`면 `TakeDamage(_damage)`. **접촉 시작 1회만** 데미지 |
| 이동 | `Update`에서 `targetTrm` 방향으로 `Rigidbody2D.linearVelocity` 직접 설정(거리 무제한 추격). X 스케일로 좌우 반전 |
| 스폰 | `EnemySpawner_JCY`: 고정 `_spawnPoints`에서 타이머로(`_spawnTime`부터 점점 빨라짐) 스폰, `T` 키 테스트 스폰. `EnemyPooling`: 스택 풀(`poolSize`), `Instance` 싱글톤. `Enemy.OnEnable`이 `EnemyPooling.Instance.enemyDatas`에서 랜덤 SO를 뽑음. `SpawnEnemy`는 풀에서 꺼낸 오브젝트를 다시 `Instantiate`하는 버그 있음 |
| 레이어/태그/입력 | 프리팹 레이어 8(`Enemy` — 내 프로젝트의 Enemy 레이어와 같은 번호). 태그는 플레이어 쪽에 `Player` 필요. 입력은 `Keyboard.current.tKey`(테스트 스폰)만 |
| 네임스페이스/어셈블리 | **전역 네임스페이스, asmdef 없음 → `Assembly-CSharp`**. `using ExpiryGame;`으로 `Health`만 사용 |
| 그 외(통합 대상 아님) | `Augment*`/`Skill*`(증강 선택·스킬), `SoundManager_JCY`, `UiMove_JCY`(DOTween), `AugmentChest`. 이번 통합에서는 건드리지 않음 |

## 2. 내 코드와 겹치는 이름
| 팀원 | 내 쪽 | 처리 |
|---|---|---|
| `Enemy` / `EnemySO` / `EnemyPooling` (전역) | `ExpiryGame.*`에 같은 이름 없음 | 충돌 없음. 내 코드는 전부 `ExpiryGame.*` 네임스페이스 |
| `SoundManager_JCY` (싱글톤, DontDestroyOnLoad) | 새 `ExpiryGame.Flow.AudioManager` | 서로 호출하지 않음. 아래 §6 참고 |
| `EnemySpawner_JCY`, `EnemyPooling`의 풀 로직 | 새 `EnemyChunkSpawner` | 팀원 스포너/풀 로직은 **씬에 넣지 않는다**(§4). `EnemyPooling`만 `Instance` 용도로 씬에 둠 |
| `Health` (내 Survival) | 팀원이 `using ExpiryGame`으로 사용 | 그대로 호환 |

## 3. 어셈블리 / GUID 경계
- 팀원 코드가 `Assembly-CSharp`이므로 내 asmdef(`ExpiryGame.*`)는 팀원 코드를 **참조할 수 없다**(Unity 규칙). 반대(팀원→내 코드)는 가능하다.
- 그래서 팀원 타입을 아는 코드(`EnemyAdapter`, `EnemyChunkSpawner`, 설치기)는 `Assets/Scripts/Integration/`에 **asmdef 없이** 두어 같은 `Assembly-CSharp`에 컴파일한다. 팀원 폴더에 asmdef를 추가하지 않았다(팀원이 새 스크립트를 추가할 때 깨질 위험).
- 반대 방향 의존(내 asmdef가 적을 아는 것)이 필요한 곳은 인터페이스로 뒤집었다: `IEnemyActor`/`EnemyRegistry`(Flow), `IDropSource`(Drop), `IDamageable`(Survival), `IDifficultyService`(Flow).
- `.meta`: 팀원 에셋 전부 `.meta`와 함께 병합됨(git merge). 병합 후 고아 `.meta` 0개 확인. 씬(`Test_JCY.unity`)은 텍스트 병합 없이 팀원 브랜치 그대로 두고 **내 씬에는 팀원 프리팹 변형만** 가져온다.

## 4. 연결 방식 (팀원 코드 수정 0건)
| 목표 | 방법 |
|---|---|
| 피격 | `EnemyAdapter : IDamageable` — 체력은 어댑터가 관리(팀원 `Enemy`에 피격 처리가 없어서). 팀원 `_health × healthScale × 난이도 배율`이 초기 체력 |
| 타격감 | 프리팹 **변형** `Assets/ShelfLife/Prefabs/Enemy_Integrated.prefab`에 `EnemyAdapter` + `KnockbackReceiver` + `FlashOnHit`을 얹음 (원본 `Enemy.prefab` 무수정). 넉백 중에는 어댑터가 `LateUpdate`에서 `Enemy`가 덮어쓴 속도를 0으로 되돌림 |
| 사망→드롭 | `EnemyAdapter : IDropSource` + `DropSourceRegistry` 등록 → 기존 `DropSpawner`가 `DropTable_Default`로 드롭. `Threat01`은 EnemySO들 중 상대 강도(체력0.5/속도0.3/데미지0.2 가중) → `DropTable.threatWeightBias`가 강한 음식 가중치를 올림 |
| 스폰 | `EnemyChunkSpawner`가 `IChunkEvents.OnChunkLoaded/Unloaded`에 연결. 로드된 청크마다 난이도 배율로 목표 수를 채우고(`topUpInterval`), 언로드되면 그 청크 출신 적을 조용히 치움(드롭/통계 없음). 팀원 `EnemySpawner_JCY`는 고정 스폰 포인트 + 풀 버그로 청크 방식에 맞지 않아 쓰지 않음. 팀원 `Enemy` 프리팹만 가져와 풀링 |
| `EnemyPooling` | `Enemy.OnEnable`이 `EnemyPooling.Instance`를 읽으므로 씬에 1개 둠. `enemyDatas`=EnemySO 5종, `poolSize=0`(미리 생성 안 함), 스포너 참조 없음 → 팀원 풀/스폰 로직은 실행되지 않음 |
| 위험 구역 | `EnemyChunkSpawner`가 `IDangerZoneEvents.OnDangerZoneEntered` 구독: 진입 순간 플레이어 주변에 추가 스폰, 구역 안에서는 청크 목표 수 ×`dangerSpawnMultiplier` |
| 적→플레이어 공격 | 팀원 `Enemy.OnCollisionEnter2D`(첫 1회) + 어댑터 `OnCollisionStay2D`(`contactInterval`마다 반복). 플레이어에 태그 `Player` 설정 |
| 플레이어 피격 연출 | `PlayerHurtFeedback`이 `Health.HealthChanged`(감소)를 구독: 붉은 비네트 + `CameraShake` + 플레이어 `FlashOnHit` |

## 5. 필요한 수정 목록
- **팀원 파일 수정: 0건.** (`Assets/JCY/` 아래 무수정)
- 내 기존 코드 수정: `Survival/Debug/DebugHud.cs` — `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 로 감쌈(D25와 같은 방식, 릴리스에서는 빈 껍데기). 그 외 기존 이벤트/인터페이스 이름 변경 없음.
- 씬 설치: 플레이어 태그 `Player`, 플레이어에 `FlashOnHit` 추가 (설치기가 처리).

## 6. 소리
- 팀원 `SoundManager_JCY`는 `PlaySound(SoundType)`/`PlayBGM()`을 가진 싱글톤이지만 팀원 코드 어디에서도 호출하지 않는다. 이번 통합에서는 쓰지 않는다.
- 새 `AudioManager`는 `AudioCatalog` SO에 클립을 끌어다 놓는 방식. `Assets/Audio`가 없어서 BGM은 비어 있고, 효과음 중 먹기/타격만 팀원 파일(`Assets/JCY/06 Sounds/Eat_Sound.mp3`, `Attack_Sound.mp3`)을 설치기가 임시로 연결한다.

## 7. 알려진 한계 (팀원 코드가 그대로라서)
- 팀원 `Enemy`는 접촉 시작 1회 데미지를 주고, 어댑터가 반복 데미지를 보충한다. 두 번째 이후 데미지의 쿨다운은 어댑터 기준.
- 풀에서 재사용할 때 `KnockbackReceiver`의 남은 속도가 이어질 수 있음(수백 ms 안에 감쇠, 영향 미미).
- 적 외형은 팀원 임시 스프라이트 시트. 필요한 스프라이트는 `docs/art_needed.md`.

## 8. 통합 결과 (검증 범위)
- 컴파일: Unity 에디터가 열려 있어(자동 새로고침 안 됨) `dotnet build`로 `ExpiryGame.Flow`, `ExpiryGame.Flow.Editor`, `Assembly-CSharp`(팀원 + Integration + DOTween 모듈), `Assembly-CSharp-Editor`, `ExpiryGame.Tests.Flow`, 릴리스 분기(`UNITY_EDITOR` 제거)까지 **에러 0**.
- **실행하지 못한 것**: 설치 메뉴(씬/프리팹/SO 생성), 플레이 동작, EditMode 테스트(`FlowTests` 9개) — Unity를 직접 돌리지 못했다. 확인 항목은 `docs/handoff.md` 맨 아래 "플레이 확인 항목".
