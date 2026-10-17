using ExpiryGame.Combat;
using ExpiryGame.Drop;
using ExpiryGame.Landmark;
using ExpiryGame.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ExpiryGame.Flow
{
    // 게임 흐름의 조립 지점(composition root). 씬 참조와 SO만 받아서 나머지를 만들고 이어 붙인다.
    // 규칙/화면/시간/통계/소리는 각자 별도 클래스이고, 여기서는 서로를 연결하고 매 프레임 입력만 확인한다.
    public class GameFlowRoot : MonoBehaviour
    {
        private const int CanvasSortingOrder = 100;

        [Header("설정 (SO)")]
        [SerializeField] private UITheme theme;
        [SerializeField] private FlowSettings settings;
        [SerializeField] private SurvivalGoal goal;
        [SerializeField] private ScoreRules scoreRules;
        [SerializeField] private StartingLoadout loadout;
        [SerializeField] private AudioCatalog audioCatalog;

        [Header("씬 참조")]
        [SerializeField] private GameClock clock;
        [SerializeField] private Health playerHealth;
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private FoodConsumer consumer;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] private SwingHitDetector hitDetector;
        [SerializeField] private DropSpawner dropSpawner;

        private GameStateMachine machine;
        private GameplayGate gate;
        private GameFlowController controller;
        private FlowScreens screens;
        private StatsTracker stats;
        private PlayerHurtFeedback hurtFeedback;
        private AudioEventBinder audioBinder;
        private EnhanceFeedback enhanceFeedback;
        private LoadoutGiver loadoutGiver;
        private DayBannerView banner;
        private ControlsHintView hint;
        private bool runBegan;

        public IGameStateMachine Machine => machine;
        public IStatsSource Stats => stats;

        private void Awake()
        {
            Time.timeScale = 1f;
            machine = new GameStateMachine(FlowLaunch.ConsumeSkipTitle() ? GameState.Playing : GameState.Title);

            var canvas = FlowUi.CreateCanvas("FlowCanvas", CanvasSortingOrder);
            canvas.SetParent(transform, false);
            FlowUi.EnsureEventSystem();

            // 아래층(가려지는 것)부터 위층(메뉴) 순서로 만든다
            var vignette = Child<HurtVignetteView>(canvas, "HurtVignette");
            vignette.Build(settings);
            hint = Child<ControlsHintView>(canvas, "ControlsHint");
            hint.Build(theme, settings);
            banner = Child<DayBannerView>(canvas, "DayBanner");
            banner.Build(theme, settings);

            var title = Child<TitleView>(canvas, "Title");
            title.Build(theme, settings);
            var pause = Child<PauseView>(canvas, "Pause");
            pause.Build(theme, settings);
            var gameOver = Child<ResultView>(canvas, "GameOver");
            gameOver.Build(theme, settings, settings.gameOverTitle, theme != null ? theme.dangerText : Color.red);
            var clear = Child<ResultView>(canvas, "Clear");
            clear.Build(theme, settings, settings.clearTitle, theme != null ? theme.healText : Color.green);

            stats = new StatsTracker(clock, consumer);
            controller = new GameFlowController(machine, goal, clock, playerHealth);
            screens = new FlowScreens(machine, controller, stats, scoreRules, title, pause, gameOver, clear);
            hurtFeedback = new PlayerHurtFeedback(playerHealth, settings, vignette, cameraShake);
            loadoutGiver = new LoadoutGiver();

            var audio = new GameObject("AudioManager").AddComponent<AudioManager>();
            audio.transform.SetParent(transform, false);
            audio.Initialize(audioCatalog, clock, machine, playerHealth.transform);

            // 모닥불/냉장고 이벤트는 씬에 없어도(이전 버전 씬) 그냥 연결하지 않는다
            var landmarkEvents = FindFirstObjectByType<LandmarkEvents>();
            audioBinder = new AudioEventBinder(audio, audioCatalog, consumer, hitDetector, inventory, dropSpawner, landmarkEvents);
            enhanceFeedback = new EnhanceFeedback(landmarkEvents, FindFirstObjectByType<HitFxSpawner>(), settings);

            clock.DayStarted += OnDayStarted;
            machine.StateChanged += OnStateChanged;
        }

        private void Start()
        {
            // 다른 컴포넌트의 OnEnable이 모두 끝난 뒤에 입력/시간을 상태에 맞춘다
            gate = new GameplayGate(input, machine.Current);
            screens.Apply(machine.Current);
            if (machine.Current == GameState.Playing) BeginRun();
        }

        private void Update()
        {
            gate.Enforce();

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) controller.TogglePause();
                else if (machine.Current == GameState.Title && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                    controller.StartGame();
            }

            if (machine.Current == GameState.Playing) stats.AddPlayTime(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (clock != null) clock.DayStarted -= OnDayStarted;
            if (machine != null) machine.StateChanged -= OnStateChanged;
            screens?.Dispose();
            controller?.Dispose();
            hurtFeedback?.Dispose();
            audioBinder?.Dispose();
            enhanceFeedback?.Dispose();
            stats?.Dispose();
            loadoutGiver?.Release();
            Time.timeScale = 1f;
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            gate.Apply(to);
            if (to == GameState.Playing && !runBegan) BeginRun();
        }

        // 한 판의 시작: 시작 음식 지급, 첫 실행 힌트, 1일차 배너
        private void BeginRun()
        {
            runBegan = true;
            loadoutGiver.Give(inventory, loadout);
            hint.BeginIfFirstRun();
            banner.Show(clock.DayCount, goal.surviveDays == 1);
        }

        private void OnDayStarted(int day)
        {
            if (machine.Current != GameState.Playing) return;
            banner.Show(day, day == goal.surviveDays);
        }

        private static T Child<T>(Transform parent, string childName) where T : Component
        {
            var rt = FlowUi.Stretch(childName, parent);
            return rt.gameObject.AddComponent<T>();
        }
    }
}
