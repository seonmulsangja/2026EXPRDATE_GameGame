# ShelfLife Art Pack v2 (다크 톤 도트) — 추가/교체 분

공통 임포트: Sprite(2D and UI) / PPU 32 / Filter Point / Compression None
(이펙트·UI 포함 전부 동일. 모든 스프라이트는 "네이티브 도트 × 2" 크기로 내보냄)

## 교체되는 것
- player/ : player_{down,up,left,right}_{idle,walk_a,walk_b}.png (12장, 32x32) + shadow.png
  - v1의 player_{방향}.png는 삭제하고 이걸로 교체
  - 걷기: walk_a → idle → walk_b → idle 순서 루프 권장 (0.12~0.15초/프레임)
  - 뒤모습은 음식 가방(배낭)이 보이고, 옆모습은 등 뒤에 가방이 보임

## 낮/밤 바닥 (tiles/)
- floor_{a,b,c,d}_{day,night}.png  (32x32, 같은 종류끼리 배치 구조가 동일)
  - a: 기본 / b: 금 / c: 석판 / d: 풀(밤엔 청록색으로 은은하게 빛남)
- 적용: Tilemap 2장(day, night)을 같은 좌표에 겹쳐서 깔고, night 쪽 알파를 낮밤 진행도로 크로스페이드
- decor/ : 풀·웅덩이는 day/night 쌍, 조약돌·생선뼈·쓰레기는 공용(틴트로 처리). 충돌 없는 장식

## 랜드마크 (landmarks/) — 피벗: Bottom Center
| 파일 | 크기(px) | 용도(기획 제안) |
|---|---|---|
| landmark_store.png (+_glow) | 96x80 | 폐 편의점: 음식 상자 다수, 가까이 가면 상호작용 |
| landmark_fridge.png (+_glow) | 64x80 | 대형 냉장고: 주변에서 유통기한 감소 속도 느려짐(안전지대) |
| landmark_campfire_0~2.png | 48x48 | 모닥불(3프레임 루프): 밤에 주변 체력 회복 |
| landmark_dump.png (+_glow) | 80x64 | 쓰레기 더미: 음식 많지만 대부분 썩음, 위험 |
- *_glow.png = 밤에만 켜지는 발광 부분(간판, 창문, 냉장고 내부, 독성 웅덩이). 같은 위치에 겹치고
  Additive 머티리얼(또는 스프라이트 알파)로 밤 진행도에 맞춰 페이드 인
- fx/fx_zone_ring.png (128x128): 효과 반경 표시용 점선 원(틴트로 색 변경)
- fx/fx_glow_soft.png (128x128): 부드러운 원형 빛. 모닥불·창문 빛번짐용(Additive)

## UI (ui/)
- ui_panel.png / ui_panel_accent.png (32x32): 9-slice, Border 6/6/6/6. accent는 선택/강조용
- ui_slot.png / ui_slot_selected.png (48x48): 고정 크기 슬롯
- ui_bar_frame.png (24x16): 9-slice, Border 4/4/4/4
- ui_bar_fill.png (8x8), ui_bar_ghost.png (8x8, 사선 줄무늬): 흰색이라 Image.color로 틴트
  - ghost = 음식 먹으면 차오를 체력 예상 구간 (초록 틴트)
- icon_heart / icon_heal / icon_weapon / icon_shelflife / icon_sun / icon_moon, ui_pip_full / ui_pip_empty (사용 횟수 점), ui_arrow (랜드마크 방향 표시, 회전해서 사용)
- 포인트 컬러: #E0793A (주황) 하나로 통일. 신선도 바만 초록/노랑/빨강

## 미리보기
preview_day.png / preview_night.png : 낮밤 비교, preview_ui.png : UI 배치 참고(이걸 보고 만들면 됨), preview_sheet.png : 전체 모음
