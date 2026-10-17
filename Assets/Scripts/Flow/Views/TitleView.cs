using System;
using UnityEngine;
using ExpiryGame.UI;

namespace ExpiryGame.Flow
{
    // 타이틀: 어두운 막 + 제목 + 시작/종료 + 조작법 한 줄. 뒤에 낮 맵이 비친다.
    public class TitleView : ScreenView
    {
        public event Action StartClicked;
        public event Action QuitClicked;

        public void Build(UITheme theme, FlowSettings settings)
        {
            FlowUi.Fill("Dim", transform, FlowUi.Dim, true);
            FlowUi.Text(theme, transform, settings.gameTitle, 120, Color.white, new Vector2(0f, 250f), new Vector2(1500f, 180f));
            FlowUi.Button(theme, transform, settings.startLabel, new Vector2(0f, 20f), () => StartClicked?.Invoke());
            FlowUi.Button(theme, transform, settings.quitLabel, new Vector2(0f, -85f), () => QuitClicked?.Invoke());
            FlowUi.Text(theme, transform, settings.controlsLine, 30, theme != null ? theme.small.color : Color.gray,
                new Vector2(0f, -300f), new Vector2(1600f, 60f));
        }
    }
}
