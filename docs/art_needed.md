# 필요한 스프라이트 (도트 작업용)

현재 적 외형은 팀원의 **임시** 시트다(`Assets/JCY/05 Sprite/Enemy_Seet 1.png`, 1254×1254, PPU 32, 20컷). 이 프로젝트의 도트(32px=1유닛)에 맞춰 교체할 때 필요한 목록.

## 적 5종 (EnemySO 기준 — 크기는 EnemySO의 width×height × 32px 가 기준)
| 적 (EnemySO) | 몸 크기(권장) | 스프라이트 이름 | 비고 |
|---|---|---|---|
| 괴물 (`EnemySO.asset`) | 32×32 | `enemy_basic_chase_0..3.png` | 기본형. 체력10/속도5 |
| 해골 유령 (`EnemySO 1`) | 32×32 | `enemy_ghost_chase_0..3.png` | 가장 튼튼함(15) / 느림(3) → 가장 강한 음식 드롭 |
| 버섯 몬스터 (`EnemySO 2`) | 24×24 | `enemy_mushroom_chase_0..3.png` | 약하고(5) 빠름(8) |
| 땅벌레 괴물 (`EnemySO 3`) | 32×16 | `enemy_worm_chase_0..3.png` | 납작한 형태 |
| 사신 괴물 (`EnemySO 4`) | 16×40 | `enemy_reaper_chase_0..3.png` | 가장 약하고(3) 가장 빠름(10), 키 큼 |

- 모두 **오른쪽을 보는 모습**으로 그린다 (팀원 `Enemy.Move`가 X 스케일을 뒤집어 좌우를 만든다).
- 프레임 4장 = 추격 걷기 루프 (`Chase 1~4`). 팀원 애니메이터는 `Chase` 하나만 쓴다.
- 적용 방법: 새 PNG를 `Assets/Art/Sprites/enemies/`에 두고(PPU 32, Point, 압축 없음) 해당 `EnemyMonster*.spriteLib`의 `Chase 1~4` 슬롯만 교체. 코드/프리팹 수정 없음.
- 선택(있으면 좋음, 코드 지원 없음): `enemy_<id>_hurt_0.png`(피격 1컷). 지금 피격은 흰색 플래시(`FlashOnHit`)로 처리.

## 이번 작업에서 추가로 필요한 것
- 없음. 타이틀/일시정지/결과/배너/힌트는 UITheme 폰트와 단색 패널로 만들었고, 피격 비네트는 코드로 만든 텍스처다.
- (선택) 타이틀 로고 이미지: `ui_title_logo.png` (권장 256×64, PPU 32 → 화면에서 4배 확대). 쓰려면 `FlowSettings.gameTitle` 텍스트 대신 이미지를 넣도록 `TitleView`를 고치면 된다.
