using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Flow")]
    [Tooltip("点击开始后进入的场景（建议填开场剧情场景）")]
    public string startGameSceneName = "IntroStoryScene";

    [Header("Layout")]
    public Vector2 buttonSize = new Vector2(260f, 60f);
    public float buttonSpacing = 24f;

    private Canvas _canvas;
    private Transform _mainMenuPanel;

    private void Awake()
    {
        EnsureCameraIfMissing();
        BuildMenuIfNeeded();
    }

    public void StartGame()
    {
        if (string.IsNullOrEmpty(startGameSceneName))
        {
            Debug.LogError("MainMenuController: 未配置 startGameSceneName。");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(startGameSceneName))
        {
            Debug.LogError($"MainMenuController: 场景未加入 Build Settings 或名称错误 -> {startGameSceneName}");
            return;
        }

        if (_mainMenuPanel != null) _mainMenuPanel.gameObject.SetActive(false);

        Debug.Log($"[MainMenu] LoadScene: {startGameSceneName}");
        SceneManager.LoadScene(startGameSceneName, LoadSceneMode.Single);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BuildMenuIfNeeded()
    {
        _canvas = FindObjectOfType<Canvas>();
        if (_canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            es.transform.SetParent(null);
        }

        Transform existingPanel = _canvas.transform.Find("MainMenuPanel");
        if (existingPanel != null)
        {
            _mainMenuPanel = existingPanel;
            return;
        }

        GameObject panel = new GameObject("MainMenuPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(_canvas.transform, false);
        _mainMenuPanel = panel.transform;
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(360f, 220f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(1f, 1f, 1f, 0f); // 背景留白

        CreateButton(panel.transform, "Btn_StartGame", "开始游戏", new Vector2(0f, buttonSize.y * 0.5f + buttonSpacing * 0.5f), StartGame);
        CreateButton(panel.transform, "Btn_QuitGame", "退出游戏", new Vector2(0f, -(buttonSize.y * 0.5f + buttonSpacing * 0.5f)), QuitGame);
    }

    private void CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction callback)
    {
        GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);
        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = buttonSize;

        Image bg = buttonGo.GetComponent<Image>();
        bg.color = new Color(0.92f, 0.92f, 0.92f, 1f);

        Button btn = buttonGo.GetComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(callback);

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(buttonGo.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text txt = textGo.GetComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 28;
        txt.color = new Color(0.1f, 0.1f, 0.1f, 1f);
    }

    private void EnsureCameraIfMissing()
    {
        if (Camera.main != null) return;
        if (FindObjectOfType<Camera>() != null) return;

        GameObject camGo = new GameObject("MainMenuCamera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        cam.tag = "MainCamera";
    }
}
