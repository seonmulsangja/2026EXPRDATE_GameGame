> **렌더 파이프라인: URP 2D** (`UniversalRP.asset` + `Renderer2D.asset`, URP 17.3 / Unity 6000.3).
> 따라서 어둠은 **Light 2D**(Global + Point)로 구현한다. 화면 가장자리 비네트만 URP **Volume(Vignette)** 후처리를 쓴다 (`Renderer2D.asset`에 Post Process Data 연결 확인됨, 카메라 Post Processing 켬). 오버레이 방식은 쓰지 않음.

# 다크 톤 아트 적용

코드: `Assets/Scripts/Visual/` (asmdef `ExpiryGame.Visual`, 에디터 `ExpiryGame.Visual.Editor`, 네임스페이스 `ExpiryGame.Visual`).
`Survival` 쪽 변경은 `FoodData.worldSprite` 필드 추가 1줄뿐이다. 음식 로직 클래스(`FoodItem`, `ShelfLifeSystem` 등)와 handoff 이벤트/인터페이스 이름은 그대로.

## 1. 클래스

| 클래스 | 책임 | 구독/사용 |
|---|---|---|
| `ArtTheme` (SO) | 팔레트·그라디언트·커브 등 모든 아트 수치 | — |
| `FoodVisual` | 음식 스프라이트 + 신선도 오버레이 | `FoodItem.Rotted`, `IGameClock.Ticked`, `ShelfLife01`, `IsRotten` |
| `HeldFoodVisual` | 선택된 음식을 `FoodVisual`에 연결 (플레이어 머리 위) | `IFoodInventory.Changed` |
| `AmbientLightController` | 글로벌 라이트 색/밝기 (낮밤 부드럽게) | `IGameClock.Ticked`, `TimeOfDay01` |
| `PlayerLight` | 플레이어 Point Light 2D (밤에 강해짐) | `IGameClock.Ticked`, `TimeOfDay01` |
| `CameraBackdrop` | 카메라 배경색 | — |

신선도 표현: 원본 스프라이트 위에 같은 모양의 반투명 오버레이를 얹는다 (RGB=덮는 색, A=세기).
`SpriteRenderer.color` 곱하기만으로는 채도를 못 줄이기 때문에 이 방식을 택함 → 셰이더 불필요, 스프라이트 교체해도 동작.
- 신선: 오버레이 A=0 (원본 색)
- 시듦: `ArtTheme.witherOverlay` 그라디언트 (x = 1 − ShelfLife01) → 회색 오버레이가 점점 진해짐 = 채도 감소
- 썩음: `ArtTheme.rottenOverlay` (탁한 녹색, `Rotted` 이벤트 시점에 전환)

> 기존 `DayNightLighting`(Survival)은 같은 글로벌 라이트를 건드리므로, `AmbientLightController`를 쓰는 씬에서는 **비활성화**한다 (설치 메뉴가 자동으로 꺼 줌).

## 2. 팔레트 / 수치 기본값 (`ArtTheme.ApplyDarkDefaults`, 에셋에서 수정)

| 항목 | 값 |
|---|---|
| 배경 | `#14161C` |
| 바닥 / 바닥 점 | `#2A3140` / `#252B38` |
| 플레이어 / 적 | `#8E9BAE` / `#6E4F5A` (저채도) |
| 외곽선 | 본체색 × (1 − 0.55) |
| 음식 (고채도) | 생선 `#3DB8FF`, 빵 `#F2A93B`, 통조림 `#E2553B` |
| 시듦 오버레이 | RGB `#8A93A3→#7C8390`, A 0 → 0.15(x=.45) → 0.65(x=1) |
| 썩음 오버레이 | `#5C6B48`, A 0.8 |
| 글로벌 라이트 색 (x=TimeOfDay01) | 0 `#6F7C92` · .15~.4 `#9AA7BD` · .5 `#7A6A78` · .62 `#232A44` · .9 `#1A2036` · 1 `#6F7C92` |
| 글로벌 밝기 | 0 .7 · .15~.4 .9 · .55 .5 · .65~.9 .25 · 1 .7 |
| 플레이어 라이트 | 색 `#FFD9A0`, 반경 1.5 / 7, 밝기 0 .2 · .4 .2 · .55 .8 · .65~.9 1.2 · 1 .2 |
| 픽셀 | 32×32, PPU 32, Point 필터, 무압축, 피벗 중앙 |

## 3. 에셋 (실제 아트팩 적용됨)

원본 아트팩은 `Assets/Art/Sprites/{food,fx,obstacles,player,tiles}`. 시트/미리보기/매니페스트는 `docs/art_pack/`에 보관(Assets 밖).
임포트 설정(Sprite Single / PPU 32 / Point / 압축 없음 / 피벗)은 `ArtImporter`(에디터 AssetPostprocessor)가 `Assets/Art` 아래 PNG에 자동 적용한다.
수동 재적용: `Tools > ShelfLife > Apply Art Import Settings`.

- 음식: `FoodData.sprite`(신선) / `FoodData.spriteRotten`(썩음) — `Tools > ShelfLife > Link Food Sprites`가 10종 SO를 만들고/연결한다. (`worldSprite`는 `sprite`로 이름 변경, `FormerlySerializedAs`로 기존 연결 유지)
- `FoodVisual`: 썩으면 `spriteRotten`으로 교체(오버레이 끔). 없으면 기존처럼 썩음 오버레이.
- 플레이어: `PlayerSpriteFacing`이 이동 방향에 따라 4방향 스프라이트 선택, `shadow.png`는 자식 Shadow.
- 바닥/벽/장애물: `Tools > ShelfLife > Apply Dark Art To Open Scene`이 배치 (바닥 floor_a 타일링, 벽 4면 + 콜라이더, 장애물 6개).
- 임시 아트 생성기(`PlaceholderArtGenerator`)와 임시 PNG는 삭제함.


---

# 아트 v2 (낮밤 바닥 / UI / 랜드마크)

원본: `docs/art_pack/v2/` (매니페스트 `ART_MANIFEST_V2.md`, 미리보기, 시트, zip). 스프라이트는 `Assets/Art/Sprites/{decor,fx,landmarks,player,tiles,ui}`에 v1과 같은 규칙으로 배치.
`ArtImporter`가 PPU 32 / Point / 무압축을 자동 적용하고, 9-slice 보더(`ui_panel`·`ui_panel_accent` 6/6/6/6, `ui_bar_frame` 4/4/4/4)와 랜드마크 피벗(Bottom Center)도 여기서 설정한다. 수동 재적용: `Tools > ShelfLife > Apply Art Import Settings`.
v1 `player_{방향}.png` 4장은 삭제했고 `shadow.png`는 같은 파일(GUID 유지)에 v2를 덮어썼다. `floor_d`(풀)와 `decor_*`는 청크 생성에서 배치한다 (아래 handoff의 "후속 수정" 참고).

## 낮밤 시각 차이 (`Assets/Scripts/World`)
| 클래스 | 책임 |
|---|---|
| `DayNightProfile` (SO) | 새벽/낮/황혼/밤 비율, 구간별 전역 조명 색(Gradient)·밝기·밤 진행도(Curve), 비네트/발광 세기 |
| `DayNightDirector` | `IGameClock.Ticked`를 구독해 프로필로 `DayNightSample`을 **한 번** 계산 → 전역 Light 2D 적용 → `Changed` 발행 (`DayNightState.Instance`로 프리팹도 접근) |
| `NightTilemapFader` | 같은 좌표에 겹친 night Tilemap 알파 = `Night01` |
| `NightGlowFader` | `*_glow` / `fx_glow_soft` Additive 스프라이트 알파 = `Night01` (프리팹용) |
| `NightVignette` | URP Volume Vignette 세기 = lerp(낮, 밤, `Night01`) |
| `PlayerWalkAnimator` + `WalkAnimationProfile` (SO) | walk_a→idle→walk_b→idle, `frameSeconds`로 간격 조절 |

- 시간 0에서 새벽으로 시작: 새벽(밤→낮 페이드) → 낮 → 황혼(낮→밤) → 밤. 기존 `DayCycleConfig.nightStartNormalized`(페이즈/UI용)와 시각 구간은 별개 값이다 — 기본값은 황혼이 0.45~0.55라 대략 맞물린다.
- 무한 맵: `ChunkStreamer.nightTilemap`(선택) + `MapGenSettings.nightFloorTiles`. `ChunkTilemapView.Show/Hide`가 두 Tilemap을 동시에 채우고 비운다.
- 밤 플레이어 주변광: 기존 `PlayerLight`(Light 2D) 그대로 + `NightGlow`(fx_glow_soft Additive) 자식.
- 새 셰이더 `ShelfLife/SpriteAdditive` (URP 2D unlit, `Blend SrcAlpha One`). 이 환경에서는 컴파일 검증을 못 했다 → Console 확인.
- 기존 `AmbientLightController` / `DayNightLighting`은 같은 전역 라이트를 건드리므로 설치 시 `enabled = false`.

## 랜드마크 (`Assets/Scripts/Landmark`)
- 지역(`chunksPerRegion`=6 → 96타일) 단위로 `LandmarkPlanner`가 seed로 0~1개 결정 (순수 계산, 캐시). 시작 지점 지역에는 `startLandmark`(기본 모닥불)가 `startPoint+startOffset` 근처에 항상 놓인다.
- 장애물 비움은 `MapGen/Contracts/IObstacleFilter` + `ChunkStreamer.SetObstacleFilter` (`LandmarkObstacleFilter`가 등록).
- 효과는 프리팹 안의 독립 컴포넌트: `LandmarkZone`(반경 판정) · `FridgeDecayEffect` · `CampfireHealEffect` · `StoreLootEffect` · `DumpDangerEffect` · `ZoneRingView` · `LandmarkFlipbook` · `CampfireLight`. 수치는 전부 `LandmarkData`(SO).
