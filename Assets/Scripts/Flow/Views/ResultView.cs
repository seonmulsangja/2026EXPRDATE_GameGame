using System;
using TMPro;
using UnityEngine;
using ExpiryGame.UI;

namespace ExpiryGame.Flow
{
    // 게임 오버/클리어가 같이 쓰는 결과 화면. 제목과 색만 다르다.
    public class ResultView : ScreenView
    {
        public event Action RetryClicked;
        public event Action TitleClicked;

        private FlowSettings settings;
        private TextMeshProUGUI days, kills, eaten, rotten, time, score;

        public void Build(UITheme theme, FlowSettings flowSettings, string heading, Color headingColor)
        {
            settings = flowSettings;
            FlowUi.Fill("Dim", transform, new Color(0.03f, 0f, 0f, 0.82f), true);
            FlowUi.Text(theme, transform, heading, 110, headingColor, new Vector2(0f, 330f), new Vector2(1400f, 160f));

            var body = theme != null ? theme.body.color : Color.white;
            days = Line(theme, 190f, body);
            kills = Line(theme, 130f, body);
            eaten = Line(theme, 70f, body);
            rotten = Line(theme, 10f, body);
            time = Line(theme, -50f, body);
            score = FlowUi.Text(theme, transform, "", 64, theme != null ? theme.accent : Color.white,
                new Vector2(0f, -140f), new Vector2(1200f, 90f));

            FlowUi.Button(theme, transform, settings.retryLabel, new Vector2(-230f, -300f), () => RetryClicked?.Invoke());
            FlowUi.Button(theme, transform, settings.toTitleLabel, new Vector2(230f, -300f), () => TitleClicked?.Invoke());
        }

        public void SetResult(GameStats stats, int scoreValue)
        {
            days.text = string.Format(settings.daysLine, stats.DaysSurvived);
            kills.text = string.Format(settings.killsLine, stats.Kills);
            eaten.text = string.Format(settings.eatenLine, stats.FoodEaten);
            rotten.text = string.Format(settings.rottenLine, stats.RottenEaten);
            time.text = string.Format(settings.timeLine, FlowUi.FormatTime(stats.PlaySeconds));
            score.text = string.Format(settings.scoreLine, scoreValue);
        }

        private TextMeshProUGUI Line(UITheme theme, float y, Color color)
        {
            return FlowUi.Text(theme, transform, "", 44, color, new Vector2(0f, y), new Vector2(1200f, 60f));
        }
    }
}
