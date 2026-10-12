using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Строит Canvas со всеми экранами и управляет их переключением.
/// Шрифт задаётся в инспекторе полем Pixel Font.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Font")]
    [Tooltip("Импортируй .ttf в Assets (например, Press Start 2P) и назначь сюда.")]
    [SerializeField] private Font pixelFont;

    [Header("Colors")]
    [SerializeField] private Color textColor = new Color(1f, 1f, 0.4f);
    [SerializeField] private Color buttonBg = new Color(0.15f, 0.15f, 0.45f);
    [SerializeField] private Color buttonHover = new Color(0.25f, 0.25f, 0.7f);
    [SerializeField] private Color overlayBg = new Color(0f, 0f, 0f, 0.85f);

    private Canvas canvas;
    private Font resolvedFont;

    private GameObject mainMenuPanel;
    private GameObject settingsPanel;
    private GameObject hudPanel;
    private GameObject endPanel;

    private Text scoreText;
    private Text endMessage;
    private Transform livesIconsRoot;
    private Transform bonusIconsRoot;

    private const int MaxLivesIcons = 4;
    private const int MaxBonusIcons = 4;

    private int lastScore = -1;
    private int lastLives = -1;
    private int lastBonuses = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Build()
    {
        resolvedFont = ResolveFont();
        BuildCanvas();
        BuildMainMenu();
        BuildSettings();
        BuildHud();
        BuildEndScreen();

        ShowOnly(mainMenuPanel);
    }

    // ───────── Публичный интерфейс ─────────

    public void OnGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                ShowOnly(mainMenuPanel);
                break;
            case GameState.Playing:
                ShowOnly(hudPanel);
                break;
            case GameState.GameOver:
                ShowEnd("GAME OVER");
                break;
            case GameState.Win:
                ShowEnd("YOU WIN");
                break;
                // Dying — HUD остаётся видимым, ничего не переключаем
        }
    }

    public void RefreshScore(int score)
    {
        if (scoreText == null) return;
        scoreText.text = "SCORE: " + score.ToString("D5");
    }

    public void RefreshLives(int lives)
    {
        if (livesIconsRoot == null) return;
        for (int i = 0; i < livesIconsRoot.childCount; i++)
            livesIconsRoot.GetChild(i).gameObject.SetActive(i < lives);
    }

    public void RefreshBonuses(int count)
    {
        if (bonusIconsRoot == null) return;
        for (int i = 0; i < bonusIconsRoot.childCount; i++)
            bonusIconsRoot.GetChild(i).gameObject.SetActive(i < count);
    }

    // ───────── Update ─────────

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || hudPanel == null || !hudPanel.activeSelf) return;

        if (gm.Score != lastScore) { lastScore = gm.Score; RefreshScore(gm.Score); }
        if (gm.Lives != lastLives) { lastLives = gm.Lives; RefreshLives(gm.Lives); }
        if (gm.SpecialBonusesCollected != lastBonuses)
        {
            lastBonuses = gm.SpecialBonusesCollected;
            RefreshBonuses(lastBonuses);
        }
    }

    // ───────── Построение Canvas ─────────

    private Font ResolveFont()
    {
        if (pixelFont != null) return pixelFont;

        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    private void BuildCanvas()
    {
        var go = new GameObject("UICanvas");
        go.transform.SetParent(transform, false);

        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    // ───────── Main Menu ─────────

    private void BuildMainMenu()
    {
        mainMenuPanel = CreateFullScreenPanel("MainMenu", canvas.transform);

        CreateText(mainMenuPanel.transform, new Vector2(0, 220), new Vector2(1200, 200),
            "PAC-MAN", 140, TextAnchor.MiddleCenter);

        CreateButton(mainMenuPanel.transform, new Vector2(0, 0), new Vector2(420, 90),
            "PLAY", () => GameManager.Instance.RequestStartGame());

        CreateButton(mainMenuPanel.transform, new Vector2(0, -130), new Vector2(420, 90),
            "SETTINGS", () => ShowOnly(settingsPanel));
    }

    // ───────── Settings ─────────

    private void BuildSettings()
    {
        settingsPanel = CreateFullScreenPanel("Settings", canvas.transform);

        CreateText(settingsPanel.transform, new Vector2(0, 260), new Vector2(900, 140),
            "SETTINGS", 100, TextAnchor.MiddleCenter);

        CreateText(settingsPanel.transform, new Vector2(0, 100), new Vector2(700, 60),
            "MUSIC VOLUME", 40, TextAnchor.MiddleCenter);

        var musicSlider = CreateSlider(settingsPanel.transform, new Vector2(0, 40), new Vector2(560, 40),
            AudioSettings.MusicVolume, v => AudioSettings.MusicVolume = v);

        CreateText(settingsPanel.transform, new Vector2(0, -60), new Vector2(700, 60),
            "SFX VOLUME", 40, TextAnchor.MiddleCenter);

        var sfxSlider = CreateSlider(settingsPanel.transform, new Vector2(0, -120), new Vector2(560, 40),
            AudioSettings.SfxVolume, v => AudioSettings.SfxVolume = v);

        CreateButton(settingsPanel.transform, new Vector2(0, -280), new Vector2(320, 80),
            "BACK", () => ShowOnly(mainMenuPanel));

        // На случай, если значения поменяются извне — синхронизируем
        AudioSettings.OnChanged += () =>
        {
            if (musicSlider != null && !Mathf.Approximately(musicSlider.value, AudioSettings.MusicVolume))
                musicSlider.SetValueWithoutNotify(AudioSettings.MusicVolume);
            if (sfxSlider != null && !Mathf.Approximately(sfxSlider.value, AudioSettings.SfxVolume))
                sfxSlider.SetValueWithoutNotify(AudioSettings.SfxVolume);
        };
    }

    // ───────── HUD ─────────

    private void BuildHud()
    {
        hudPanel = CreateFullScreenPanel("HUD", canvas.transform, withBackground: false);
        // Score — верх слева
        scoreText = CreateText(hudPanel.transform, new Vector2(60, -50), new Vector2(600, 60),
            "SCORE: 00000", 44, TextAnchor.MiddleLeft);
        var srt = scoreText.rectTransform;
        srt.anchorMin = srt.anchorMax = new Vector2(0f, 1f);
        srt.pivot = new Vector2(0f, 1f);
        srt.anchoredPosition = new Vector2(60, -40);

        // Lives — низ слева
        var livesRoot = new GameObject("LivesIcons", typeof(RectTransform));
        livesRoot.transform.SetParent(hudPanel.transform, false);
        var lrt = livesRoot.GetComponent<RectTransform>();
        lrt.anchorMin = lrt.anchorMax = new Vector2(0f, 0f);
        lrt.pivot = new Vector2(0f, 0f);
        lrt.anchoredPosition = new Vector2(50, 30);
        lrt.sizeDelta = new Vector2(500, 70);
        livesIconsRoot = livesRoot.transform;

        for (int i = 0; i < MaxLivesIcons; i++)
            CreateIcon(livesRoot.transform, SpriteFactory.PacmanSprite, Color.yellow,
                new Vector2(i * 80f, 0f), new Vector2(60f, 60f),
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));

        // Bonus — низ справа
        var bonusRoot = new GameObject("BonusIcons", typeof(RectTransform));
        bonusRoot.transform.SetParent(hudPanel.transform, false);
        var brt = bonusRoot.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(1f, 0f);
        brt.anchoredPosition = new Vector2(-50, 30);
        brt.sizeDelta = new Vector2(500, 70);
        bonusIconsRoot = bonusRoot.transform;

        for (int i = 0; i < MaxBonusIcons; i++)
            CreateIcon(bonusRoot.transform, SpriteFactory.CherrySprite, Color.white,
                new Vector2(-i * 80f, 0f), new Vector2(60f, 60f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
    }

    private Image CreateIcon(Transform parent, Sprite sprite, Color tint,
                             Vector2 pos, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("Icon", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = tint;
        img.preserveAspect = true;
        go.SetActive(false);
        return img;
    }

    // ───────── End screen ─────────

    private void BuildEndScreen()
    {
        endPanel = CreateFullScreenPanel("EndScreen", canvas.transform);
        var bg = endPanel.GetComponent<Image>();
        if (bg != null) { bg.color = overlayBg; bg.raycastTarget = true; }

        endMessage = CreateText(endPanel.transform, new Vector2(0, 220), new Vector2(1600, 200),
            "GAME OVER", 140, TextAnchor.MiddleCenter);

        CreateButton(endPanel.transform, new Vector2(0, -20), new Vector2(420, 90),
            "SHARE", () => Debug.Log("[UI] Share — заглушка"));

        CreateButton(endPanel.transform, new Vector2(0, -140), new Vector2(420, 90),
            "RESTART", () => GameManager.Instance.RequestStartGame());

        CreateButton(endPanel.transform, new Vector2(0, -260), new Vector2(420, 90),
            "MAIN MENU", () => GameManager.Instance.RequestMainMenu());
    }

    private void ShowEnd(string message)
    {
        if (endMessage != null) endMessage.text = message;
        ShowOnly(endPanel);
    }

    // ───────── Хелперы построения ─────────

    private GameObject CreateFullScreenPanel(string name, Transform parent, bool withBackground = true)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        if (withBackground)
        {
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.9f);
            img.raycastTarget = false;
        }

        return go;
    }

    private Text CreateText(Transform parent, Vector2 pos, Vector2 size,
                            string content, int fontSize, TextAnchor align)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var txt = go.AddComponent<Text>();
        txt.font = resolvedFont;
        txt.fontSize = fontSize;
        txt.color = textColor;
        txt.alignment = align;
        txt.text = content;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        return txt;
    }

    private Button CreateButton(Transform parent, Vector2 pos, Vector2 size,
                                string label, Action onClick)
    {
        var go = new GameObject("Button_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = buttonBg;

        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        cb.selectedColor = Color.white;
        btn.colors = cb;
        btn.targetGraphic = img;

        var txtGo = new GameObject("Label", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        var txt = txtGo.AddComponent<Text>();
        txt.font = resolvedFont;
        txt.fontSize = 32;
        txt.color = textColor;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = label;
        txt.raycastTarget = false;

        if (onClick != null) btn.onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            onClick();
        });

        return btn;
    }

    private Slider CreateSlider(Transform parent, Vector2 pos, Vector2 size,
                                float initialValue, UnityEngine.Events.UnityAction<float> onChanged)
    {
        var go = new GameObject("Slider", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var slider = go.AddComponent<Slider>();

        // Background
        var bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(go.transform, false);
        var bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = new Vector2(0f, 0.35f);
        bgrt.anchorMax = new Vector2(1f, 0.65f);
        bgrt.offsetMin = Vector2.zero;
        bgrt.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f);

        // Fill Area
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var fart = fillArea.GetComponent<RectTransform>();
        fart.anchorMin = new Vector2(0f, 0.35f);
        fart.anchorMax = new Vector2(1f, 0.65f);
        fart.offsetMin = Vector2.zero;
        fart.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        var frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = textColor;

        // Handle Slide Area
        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        var hart = handleArea.GetComponent<RectTransform>();
        hart.anchorMin = Vector2.zero;
        hart.anchorMax = Vector2.one;
        hart.offsetMin = Vector2.zero;
        hart.offsetMax = Vector2.zero;

        var handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        var hrt = handle.GetComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(30f, 0f);
        hrt.anchorMin = new Vector2(0f, 0f);
        hrt.anchorMax = new Vector2(0f, 1f);
        hrt.offsetMin = new Vector2(-15f, -8f);
        hrt.offsetMax = new Vector2(15f, 8f);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        slider.fillRect = frt;
        slider.handleRect = hrt;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(Mathf.Clamp01(initialValue));

        if (onChanged != null) slider.onValueChanged.AddListener(onChanged);

        return slider;
    }

    private void ShowOnly(GameObject panel)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(panel == mainMenuPanel);
        if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
        if (hudPanel != null) hudPanel.SetActive(panel == hudPanel);
        if (endPanel != null) endPanel.SetActive(panel == endPanel);

        // Сброс кеша HUD, чтобы сразу отрисовались актуальные значения
        lastScore = -1;
        lastLives = -1;
        lastBonuses = -1;
    }
}