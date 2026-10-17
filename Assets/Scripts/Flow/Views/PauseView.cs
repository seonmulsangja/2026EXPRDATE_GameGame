using System;
using UnityEngine;
using ExpiryGame.UI;

namespace ExpiryGame.Flow
{
    public class PauseView : ScreenView
    {
        public event Action ResumeClicked;
        public event Action TitleClicked;
        public event Action QuitClicked;

        public void Build(UITheme theme, FlowSettings settings)
        {
            FlowUi.Fill("Dim", transform, new Color(0f, 0f, 0f, 0.6f), true);
            FlowUi.Text(theme, transform, settings.pausedTitle, 90, Color.white, new Vector2(0f, 220f), new Vector2(1000f, 130f));
            FlowUi.Button(theme, transform, settings.resumeLabel, new Vector2(0f, 60f), () => ResumeClicked?.Invoke());
            FlowUi.Button(theme, transform, settings.toTitleLabel, new Vector2(0f, -45f), () => TitleClicked?.Invoke());
            FlowUi.Button(theme, transform, settings.quitLabel, new Vector2(0f, -150f), () => QuitClicked?.Invoke());
        }
    }
}
