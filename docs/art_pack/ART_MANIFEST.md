# ShelfLife Art Pack (다크 톤 도트)

## Unity Import 설정 (전부 동일)
- Texture Type: Sprite (2D and UI)
- Pixels Per Unit: 32
- Filter Mode: Point (no filter)
- Compression: None
- 시트(sheets/)는 Sprite Mode: Multiple → Slice(Grid By Cell Size)
  - sheet_foods / sheet_player / sheet_world: 32x32
  - sheet_fx_slash / sheet_fx_hitspark: 48x48
- 개별 파일은 그대로 Single로 써도 됨 (시트와 내용 동일)

## 규격
- 원본은 16x16 기준 도트를 2배 확대한 32x32 (이펙트는 24x24 → 48x48, 파티클 4x4 → 8x8)
- 외곽선 색 #12111A 통일, 음식만 채도 높게, 월드는 저채도 청회색
- 피벗: 캐릭터/음식/장애물은 Bottom Center 권장, 이펙트는 아래 참고

## 파일
food/       food_{bread,can,milk,egg,apple,cheese,sausage,fish,ramen,jam}.png  (신선)
            food_{이름}_rotten.png                                            (썩음, 같은 이름 + _rotten)
player/     player_{down,up,left,right}.png, shadow.png (32x16, 발밑 그림자)
tiles/      floor_a, floor_b(금), floor_c(석판), wall
obstacles/  obstacle_{crate,barrel,rubble}.png
fx/         fx_slash_0~4.png   휘두르기 궤적 5프레임 (오른쪽 방향 기준, 피벗: 왼쪽 중앙(x=3/24,y=12/24))
            fx_hitspark_0~4.png 타격 스파크 5프레임 (피벗: 중앙)
            particle_{crumb,rot,spark,juice}.png 파티클 (8x8)

## 연결 가이드
- 신선도 연출: 기본은 fresh 스프라이트에 틴트, 완전히 썩으면 _rotten 스프라이트로 교체
- 음식 SO에 sprite(신선), spriteRotten(썩음) 필드 연결
- fx_slash는 오른쪽 기준이라 스윙 방향에 맞춰 회전시켜 사용
