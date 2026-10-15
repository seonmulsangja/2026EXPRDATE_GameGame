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
