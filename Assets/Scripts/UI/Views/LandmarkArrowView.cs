using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ExpiryGame.Landmark;

namespace ExpiryGame.UI
{
    // 화면 밖 가장 가까운 랜드마크를 가리키는 화살표 + "이름 · 거리". 목록 조회는 0.25초 간격, 위치만 매 프레임 갱신.
    public class LandmarkArrowView : MonoBehaviour
    {
        [SerializeField] private UITheme theme;
        [SerializeField] private LandmarkLocator locator;
        [SerializeField] private Transform player;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private RectTransform area;
        [SerializeField] private RectTransform arrow;
        [SerializeField] private TMP_Text label;
        [Tooltip("화면 가장자리에서 화살표 중심까지 (캔버스 px)")]
        [SerializeField, Min(0f)] private float edgeMargin = 60f;
        [Tooltip("화살표 중심에서 라벨 중심까지 안쪽으로 (캔버스 px)")]
        [SerializeField, Min(0f)] private float labelOffset = 60f;
        [Tooltip("라벨 최대 폭 (캔버스 px). 넘으면 글자가 줄어든다")]
        [SerializeField, Min(40f)] private float labelMaxWidth = 480f;
        [SerializeField, Min(0.05f)] private float refreshSeconds = 0.25f;
        [Tooltip("화면 안으로 판정하는 뷰포트 여유 (0.02 = 가장자리 2% 안쪽까지 보이는 것으로 취급)")]
        [SerializeField, Range(0f, 0.2f)] private float onScreenInset = 0.02f;

        private readonly List<Rect> zones = new List<Rect>();
        private readonly List<LandmarkInfo> nearby = new List<LandmarkInfo>();
        private LandmarkInfo? target;
        private float timer;
        private int shownDistance = -1;
        private string shownName;

        private void OnEnable()
        {
            timer = 0f;
            Show(false);
        }

        private void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f)
            {
                timer = refreshSeconds;
                PickTarget();
            }

            if (target == null || !PlaceOnEdge(target.Value.Position))
            {
                Show(false);
                return;
            }
            Show(true);
            RefreshLabel(target.Value);
        }

        private void PickTarget()
        {
            target = null;
            locator.GetNearby(player.position, nearby);
            foreach (var info in nearby)
            {
                if (!IsOnScreen(info.Position))
                {
                    target = info;
                    return;
                }
            }
        }

        private bool IsOnScreen(Vector2 world)
        {
            var viewport = viewCamera.WorldToViewportPoint(world);
            return viewport.x > onScreenInset && viewport.x < 1f - onScreenInset &&
                   viewport.y > onScreenInset && viewport.y < 1f - onScreenInset;
        }

        // 화면 중심에서 목표 방향으로 뻗은 선이 안쪽 사각형과 만나는 점에 화살표를 둔다. 화면 안이면 false.
        private bool PlaceOnEdge(Vector2 world)
        {
            if (IsOnScreen(world)) return false;

            var viewport = viewCamera.WorldToViewportPoint(world);
            var size = area.rect.size;
            var offset = new Vector2((viewport.x - 0.5f) * size.x, (viewport.y - 0.5f) * size.y);
            if (offset.sqrMagnitude < 0.0001f) return false;

            var halfW = size.x * 0.5f - edgeMargin;
            var halfH = size.y * 0.5f - edgeMargin;
            var scale = Mathf.Min(halfW / Mathf.Max(Mathf.Abs(offset.x), 0.0001f), halfH / Mathf.Max(Mathf.Abs(offset.y), 0.0001f));
            var position = offset * scale;
            var direction = offset.normalized;
            position = AvoidHudZones(position, direction, halfW, halfH);

            arrow.anchoredPosition = position;
            arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            ((RectTransform)label.transform).anchoredPosition = position - direction * labelOffset;
            return true;
        }

        // 화살표+라벨이 HUD 점유 영역(UITheme.hudZones)과 겹치면 화면 가장자리를 따라 가장 가까운 빈 자리로 민다
        private Vector2 AvoidHudZones(Vector2 position, Vector2 direction, float halfW, float halfH)
        {
            theme.GetZoneRects(area.rect.size, zones);
            if (zones.Count == 0) return position;

            var alongY = Mathf.Abs(position.x) >= halfW - 0.5f && Mathf.Abs(position.x) / halfW >= Mathf.Abs(position.y) / halfH;
            var limit = alongY ? halfH : halfW;
            var arrowHalf = arrow.rect.size * 0.5f;
            var labelSize = ((RectTransform)label.transform).rect.size;

            for (var pass = 0; pass < zones.Count + 1; pass++)
            {
                var bounds = Bounds(position, direction, arrowHalf, labelSize);
                var moved = false;
                foreach (var zone in zones)
                {
                    var padded = Rect.MinMaxRect(zone.xMin - theme.hudZoneGap, zone.yMin - theme.hudZoneGap,
                                                 zone.xMax + theme.hudZoneGap, zone.yMax + theme.hudZoneGap);
                    if (!padded.Overlaps(bounds)) continue;

                    var up = alongY ? padded.yMax - bounds.yMin : padded.xMax - bounds.xMin;
                    var down = alongY ? padded.yMin - bounds.yMax : padded.xMin - bounds.xMax;
                    var current = alongY ? position.y : position.x;
                    var upOk = Mathf.Abs(current + up) <= limit;
                    var downOk = Mathf.Abs(current + down) <= limit;
                    var shift = (upOk && (!downOk || Mathf.Abs(up) <= Mathf.Abs(down))) ? up : down;
                    if (!upOk && !downOk) continue;

                    if (alongY) position.y += shift; else position.x += shift;
                    moved = true;
                    break;
                }
                if (!moved) break;
            }
            return position;
        }

        private Rect Bounds(Vector2 position, Vector2 direction, Vector2 arrowHalf, Vector2 labelSize)
        {
            var labelCenter = position - direction * labelOffset;
            var a = Rect.MinMaxRect(position.x - arrowHalf.x, position.y - arrowHalf.y, position.x + arrowHalf.x, position.y + arrowHalf.y);
            var l = new Rect(labelCenter - labelSize * 0.5f, labelSize);
            return Rect.MinMaxRect(Mathf.Min(a.xMin, l.xMin), Mathf.Min(a.yMin, l.yMin), Mathf.Max(a.xMax, l.xMax), Mathf.Max(a.yMax, l.yMax));
        }

        private void RefreshLabel(LandmarkInfo info)
        {
            var distance = Mathf.RoundToInt(info.Distance);
            if (distance == shownDistance && info.Data.displayName == shownName) return;
            shownDistance = distance;
            shownName = info.Data.displayName;
            label.text = string.Format(theme.landmarkFormat, info.Data.displayName, distance);
            // 라벨 영역을 글자 폭에 맞춰 HUD 회피 계산(Bounds)이 실제 크기를 쓰게 한다
            UITextFit.FitLabelWidth(label, labelMaxWidth);
        }

        private void Show(bool visible)
        {
            if (arrow.gameObject.activeSelf != visible) arrow.gameObject.SetActive(visible);
            if (label.gameObject.activeSelf != visible) label.gameObject.SetActive(visible);
        }
    }
}
