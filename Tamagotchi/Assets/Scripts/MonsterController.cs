using UnityEngine;
using UnityEngine.UI;

/// <summary>Small virtual-pet care loop; assignment rules remain fixed constants.</summary>
public sealed class MonsterController : MonoBehaviour
{
    public const float Maximum = 100f;
    public const float StaminaThreshold = 20f;
    private const float HappinessDecayPerSecond = 0.01f;
    private const float ActionStaminaCost = 2f;
    private const float FoodStaminaGain = 15f;
    private const float FoodHappinessGain = 5f;
    private const string SaveKey = "Tamagotchi.Care.v2";

    [Header("Starting care values")]
    [SerializeField, Range(0f, Maximum)] private float stamina = Maximum;
    [SerializeField, Range(0f, Maximum)] private float happiness = 75f;
    [SerializeField, Range(0f, Maximum)] private float fullness = 75f;
    [SerializeField, Range(0f, Maximum)] private float learning = 0f;
    [SerializeField] private bool rememberCare = true;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button studyButton;
    [SerializeField] private Button sleepButton;
    [SerializeField] private Button eatButton;
    [Header("Four care meters")]
    [SerializeField] private Image staminaFill;
    [SerializeField] private Image happinessFill;
    [SerializeField] private Image fullnessFill;
    [SerializeField] private Image learningFill;
    [SerializeField] private Text staminaLabel;
    [SerializeField] private Text happinessLabel;
    [SerializeField] private Text fullnessLabel;
    [SerializeField] private Text learningLabel;
    [SerializeField] private Text careMessage;
    [SerializeField] private Animator monsterAnimator;
    [SerializeField] private GameObject guessingChoices;
    [SerializeField] private Text leftChoiceLabel;
    [SerializeField] private Text rightChoiceLabel;
    [SerializeField] private Text roundTimerLabel;
    [SerializeField] private GameAudioController gameAudio;
    private int previousNeed;

    private float actionTimeLeft;
    private float careTickSeconds;
    private float autosaveSeconds;
    private bool applicationPaused;
    private bool waitingForGuess;
    private bool waitingForStudyAnswer;
    private readonly int[] studyAnswers = new int[2];
    private int correctStudyChoice;
    private int hiddenDirection;
    private bool showGuessDirection;
    private int gamesWon;
    private string actionState = "Idle";
    private string actionMessage;
    private int currentAnimation;
    public float Stamina => stamina;
    public float Happiness => happiness;
    public float Fullness => fullness;
    public float Learning => learning;
    public bool IsBusy => actionTimeLeft > 0f;
    public float RoundTimeRemaining => waitingForGuess || waitingForStudyAnswer ? actionTimeLeft : 0f;
    public string CurrentAnimationState => happiness == Maximum ? "Happy" : happiness <= 50 ? "Sad" : IsBusy ? actionState : "Idle";

    private void Awake() { ClampValues(); RefreshPresentation(); }
    private void Start()
    {
        // Editor sessions always use Inspector values for repeatable assignment demos.
        if (rememberCare && !Application.isEditor && PlayerPrefs.HasKey(SaveKey))
            TryRestoreCareState(PlayerPrefs.GetString(SaveKey));
        ApplyChanges();
    }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private void Update() { if (!applicationPaused) Tick(Time.deltaTime); }
    private void Tick(float seconds)
    {
        if (applicationPaused || !Finite(seconds) || seconds <= 0) return;
        // Exact required decay: 0.01 units per elapsed game second, independent of FPS.
        happiness -= HappinessDecayPerSecond * seconds;
        // Like a handheld care meter, food/energy change on minute-long ticks.
        // There is no instant drain on launch. Happiness keeps the required rate.
        careTickSeconds += seconds;
        int careTicks = Mathf.FloorToInt(careTickSeconds / 60f);
        if (careTicks > 0)
        {
            careTickSeconds -= careTicks * 60f;
            int fedTicks = Mathf.Min(careTicks, Mathf.CeilToInt(fullness));
            stamina -= fedTicks + (careTicks - fedTicks) * 2f;
            fullness -= careTicks;
        }
        actionTimeLeft = Mathf.Max(0f, actionTimeLeft - seconds);
        if ((waitingForGuess || waitingForStudyAnswer) && actionTimeLeft <= 0)
        {
            actionMessage = waitingForStudyAnswer ? $"TIME'S UP! ANSWER: {studyAnswers[correctStudyChoice]}" : "LET'S TRY AGAIN SOON!";
            waitingForGuess = waitingForStudyAnswer = false;
            if (gameAudio != null) gameAudio.PlayCue(GameAudioController.Cue.Wrong);
            actionTimeLeft = 2f;
        }
        ApplyChanges();
        autosaveSeconds += seconds;
        if (autosaveSeconds >= 30f) { autosaveSeconds %= 30f; SaveCare(); }
    }

    public void Play()
    {
        if (applicationPaused || IsBusy || stamina < StaminaThreshold) return;
        stamina -= ActionStaminaCost;
        happiness += 0.5f; // Required assignment gain; do not multiply by deltaTime.
        fullness -= 1f;
        hiddenDirection = Random.Range(0, 2);
        waitingForGuess = true;
        BeginAction("Playing", "WHICH WAY WILL I LOOK?", 8f);
    }
    public void ChooseLeft() { if (waitingForStudyAnswer) ResolveStudy(0); else ResolveGuess(0); }
    public void ChooseRight() { if (waitingForStudyAnswer) ResolveStudy(1); else ResolveGuess(1); }
    private void ResolveGuess(int direction)
    {
        if (applicationPaused || !waitingForGuess) return;
        waitingForGuess = false;
        bool correct = direction == hiddenDirection;
        if (gameAudio != null) gameAudio.PlayCue(correct ? GameAudioController.Cue.Correct : GameAudioController.Cue.Wrong);
        if (correct && gamesWon < int.MaxValue) gamesWon++;
        // Play's required +0.5 was awarded once at round start; no double rewards.
        string directionName = hiddenDirection == 0 ? "LEFT" : "RIGHT";
        showGuessDirection = true;
        actionMessage = correct ? $"{directionName}! GOOD GUESS! WINS: {gamesWon}" : $"I LOOKED {directionName}! TRY AGAIN!";
        actionTimeLeft = 2f;
        RefreshPresentation();
    }

    public void Study()
    {
        if (applicationPaused || IsBusy || stamina < StaminaThreshold) return;
        stamina -= ActionStaminaCost;
        happiness -= 0.5f; // Required assignment loss.
        fullness -= 1f;
        int first = Random.Range(1, 10);
        bool subtract = Random.value < 0.5f;
        int second = subtract ? Random.Range(1, first + 1) : Random.Range(1, 10);
        int answer = subtract ? first - second : first + second;
        int alternative = answer + (Random.value < 0.5f && answer > 0 ? -1 : 1);
        correctStudyChoice = Random.Range(0, 2);
        studyAnswers[correctStudyChoice] = answer;
        studyAnswers[1 - correctStudyChoice] = alternative;
        waitingForStudyAnswer = true;
        BeginAction("Studying", $"QUICK QUIZ: {first} {(subtract ? "-" : "+")} {second} = ?", 12f);
    }
    private void ResolveStudy(int choice)
    {
        if (applicationPaused || !waitingForStudyAnswer) return;
        waitingForStudyAnswer = false;
        bool correct = choice == correctStudyChoice;
        int earned = correct ? Mathf.RoundToInt(Mathf.Min(10, Maximum - learning)) : 0;
        learning += earned;
        if (gameAudio != null) gameAudio.PlayCue(correct ? GameAudioController.Cue.Correct : GameAudioController.Cue.Wrong);
        actionMessage = correct ? (earned > 0 ? $"CORRECT! STUDY +{earned}" : "PERFECT! STUDY IS FULL!") : $"GOOD TRY! ANSWER: {studyAnswers[correctStudyChoice]}";
        actionTimeLeft = 2.5f;
        ApplyChanges();
    }

    public void Eat()
    {
        if (applicationPaused || IsBusy) return;
        stamina += FoodStaminaGain;
        happiness += FoodHappinessGain;
        fullness += 25f;
        BeginAction("Eating", "YUM! THANK YOU!", 2f);
    }
    public void Sleep()
    {
        if (applicationPaused || IsBusy || stamina >= StaminaThreshold) return;
        // Assignment requires restoration to max; animation is a short rest afterwards.
        stamina = Maximum;
        BeginAction("Sleeping", "A LITTLE REST...", 3f);
    }
    private void BeginAction(string state, string message, float duration)
    {
        if (gameAudio != null)
        {
            var cue = state == "Playing" ? GameAudioController.Cue.Play : state == "Studying" ? GameAudioController.Cue.Study :
                state == "Eating" ? GameAudioController.Cue.Eat : GameAudioController.Cue.Sleep;
            gameAudio.PlayActionCue(cue);
        }
        showGuessDirection = false;
        actionState = state; actionMessage = message; actionTimeLeft = duration;
        ApplyChanges();
    }
    private void ApplyChanges() { ClampValues(); RefreshPresentation(); }
    private void ClampValues()
    {
        stamina = Mathf.Clamp(stamina, 0, Maximum);
        happiness = Mathf.Clamp(happiness, 0, Maximum);
        fullness = Mathf.Clamp(fullness, 0, Maximum);
        learning = Mathf.Clamp(learning, 0, Maximum);
    }
    private void RefreshPresentation()
    {
        int need = stamina < 20 ? 1 : fullness <= 25 ? 2 : happiness <= 50 ? 3 : 0;
        if (!IsBusy && !applicationPaused && need != previousNeed)
        {
            if (need != 0 && gameAudio != null) gameAudio.PlayCue(GameAudioController.Cue.Attention);
            previousNeed = need;
        }
        bool canWork = !applicationPaused && !IsBusy && stamina >= StaminaThreshold;
        if (playButton != null) playButton.interactable = canWork;
        if (studyButton != null) studyButton.interactable = canWork;
        if (sleepButton != null) sleepButton.interactable = !applicationPaused && !IsBusy && stamina < StaminaThreshold;
        if (eatButton != null) eatButton.interactable = !applicationPaused && !IsBusy;
        bool choicesVisible = waitingForGuess || waitingForStudyAnswer;
        if (guessingChoices != null) guessingChoices.SetActive(choicesVisible);
        if (leftChoiceLabel != null) leftChoiceLabel.text = waitingForStudyAnswer ? studyAnswers[0].ToString() : "LEFT";
        if (rightChoiceLabel != null) rightChoiceLabel.text = waitingForStudyAnswer ? studyAnswers[1].ToString() : "RIGHT";
        var leftButton = leftChoiceLabel != null ? leftChoiceLabel.GetComponentInParent<Button>(true) : null;
        var rightButton = rightChoiceLabel != null ? rightChoiceLabel.GetComponentInParent<Button>(true) : null;
        if (leftButton != null) leftButton.interactable = !applicationPaused && choicesVisible;
        if (rightButton != null) rightButton.interactable = !applicationPaused && choicesVisible;
        if (roundTimerLabel != null)
        {
            roundTimerLabel.gameObject.SetActive(choicesVisible);
            if (choicesVisible) roundTimerLabel.text = $"TIME: {Mathf.CeilToInt(actionTimeLeft)}s";
        }
        Meter(staminaFill, staminaLabel, stamina, true); Meter(happinessFill, happinessLabel, happiness);
        Meter(fullnessFill, fullnessLabel, fullness); Meter(learningFill, learningLabel, learning);
        if (careMessage != null)
        {
            careMessage.rectTransform.anchoredPosition = new Vector2(0, choicesVisible ? 38 : 0);
            careMessage.text = IsBusy ? actionMessage : stamina < 20 ? "I NEED SOME SLEEP..." :
                fullness <= 25 ? "MY TUMMY IS RUMBLING!" : happiness <= 50 ? "LET'S PLAY TOGETHER!" :
                happiness == 100 ? "SO HAPPY TO BE WITH YOU!" : "WHAT SHALL WE DO TODAY?";

        }

        // Mood thresholds take priority over action clips, preserving the assignment.
        string state = CurrentAnimationState;
        int hash = Animator.StringToHash("Base Layer." + state);
        if (monsterAnimator != null)
            monsterAnimator.transform.localScale = new Vector3(IsBusy && showGuessDirection && hiddenDirection == 0 ? -1 : 1, 1, 1);
        if (monsterAnimator != null && monsterAnimator.isActiveAndEnabled &&
            monsterAnimator.runtimeAnimatorController != null && hash != currentAnimation)
        { monsterAnimator.Play(hash, 0, 0); currentAnimation = hash; }
    }
    private static void Meter(Image fill, Text label, float value, bool floorValue = false)
    {
        // Both text and fill use the same whole-number view; no tiny per-frame drain.
        int displayed = Mathf.Clamp(floorValue ? Mathf.FloorToInt(value) : Mathf.RoundToInt(value), 0, 100);
        if (fill != null) fill.fillAmount = displayed / Maximum;
        if (label != null) label.text = $"{displayed}/100";
    }
    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveCare();
        applicationPaused = paused;
        RefreshPresentation();
    }
    private void OnApplicationQuit() { SaveCare(); }
    private void SaveCare()
    {
        if (!rememberCare || !Application.isPlaying || Application.isEditor) return;
        PlayerPrefs.SetString(SaveKey, CaptureCareState());
        PlayerPrefs.Save();
    }

    // Kept separate from disk access so save compatibility/corruption can be checked in Editor.
    public string CaptureCareState() => JsonUtility.ToJson(new CareSave {
        version = 3, stamina = stamina, happiness = happiness, fullness = fullness,
        learning = learning, gamesWon = gamesWon, careTickSeconds = careTickSeconds });

    public bool TryRestoreCareState(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        foreach (string required in new[] { "stamina", "happiness", "fullness", "learning" })
            if (!json.Contains("\"" + required + "\"")) return false;
        try
        {
            var saved = JsonUtility.FromJson<CareSave>(json);
            if (saved == null || saved.version > 3 || !Finite(saved.stamina) || !Finite(saved.happiness) ||
                !Finite(saved.fullness) || !Finite(saved.learning) || !Finite(saved.careTickSeconds) ||
                saved.careTickSeconds < 0 || saved.careTickSeconds >= 60) return false;
            stamina = saved.stamina; happiness = saved.happiness; fullness = saved.fullness; learning = saved.learning;
            gamesWon = Mathf.Max(0, saved.gamesWon); careTickSeconds = saved.careTickSeconds;
            waitingForGuess = waitingForStudyAnswer = showGuessDirection = false;
            actionTimeLeft = 0; autosaveSeconds = 0;
            ApplyChanges();
            return true;
        }
        catch (System.ArgumentException) { return false; }
    }
    [System.Serializable] private sealed class CareSave
    {
        public int version;
        public float stamina, happiness, fullness, learning, careTickSeconds;
        public int gamesWon;
    }
}
