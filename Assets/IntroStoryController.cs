using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroStoryController : MonoBehaviour
{
    [System.Serializable]
    public class StoryLine
    {
        public string speaker = "旁白";
        [TextArea(2, 4)] public string content = "";
    }

    [Header("Scene Flow")]
    public string battleSceneName = "SampleScene";

    [Header("Story")]
    public List<StoryLine> lines = new List<StoryLine>
    {
        new StoryLine
        {
            speaker = "旁白",
            content = "山门晨雾未散，你刚被收入门墙，在师傅指引下踏入练气期。"
        },
        new StoryLine
        {
            speaker = "师傅（元神）",
            content = "徒儿，莫慌。我虽只余元神，无法亲自出手，但护你周全仍绰绰有余。"
        },
        new StoryLine
        {
            speaker = "师傅（元神）",
            content = "前方那头小妖，不过山野低阶精怪。正好给你练手，记住呼吸与灵力运转。"
        },
        new StoryLine
        {
            speaker = "徒弟",
            content = "弟子明白！请师傅在旁指点，我来试一试。"
        },
        new StoryLine
        {
            speaker = "旁白",
            content = "妖兽嘶鸣着逼近，你握紧法诀，第一场真正的战斗开始了。"
        }
    };

    [Header("Typewriter")]
    [Min(0.005f)] public float typeInterval = 0.03f;

    private Text _nameText;
    private Text _dialogText;
    private Text _hintText;
    private Font _uiFont;
    private int _index;
    private bool _isTyping;
    private Coroutine _typingRoutine;

    private void Start()
    {
        EnsureCameraIfMissing();
        BuildUiIfNeeded();
        EnsureDefaultStoryIfNeeded();
        _index = 0;
        Debug.Log($"[IntroStory] Start: lines={(lines == null ? 0 : lines.Count)} battleScene={battleSceneName}");
        BeginCurrentLineTyping();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            HandleAdvanceInput();
        }
    }

    private void HandleAdvanceInput()
    {
        if (_isTyping)
        {
            CompleteCurrentLineInstantly();
            return;
        }

        _index++;
        if (_index >= lines.Count)
        {
            if (!Application.CanStreamedLevelBeLoaded(battleSceneName))
            {
                Debug.LogError($"IntroStoryController: 战斗场景未加入 Build Settings 或名称错误 -> {battleSceneName}");
                return;
            }
            SceneManager.LoadScene(battleSceneName);
            return;
        }
        BeginCurrentLineTyping();
    }

    private void BeginCurrentLineTyping()
    {
        if (_dialogText == null || _nameText == null) return;
        if (lines == null || lines.Count == 0)
        {
            _nameText.text = "旁白";
            _dialogText.text = "（暂无剧情文本）";
            _isTyping = false;
            if (_hintText != null) _hintText.text = "点击继续";
            return;
        }
        else
        {
            _index = Mathf.Clamp(_index, 0, lines.Count - 1);
            if (_typingRoutine != null) StopCoroutine(_typingRoutine);
            StoryLine line = lines[_index];
            _nameText.text = string.IsNullOrWhiteSpace(line.speaker) ? "旁白" : line.speaker;
            _typingRoutine = StartCoroutine(TypeLine(line.content ?? ""));
        }
    }

    private void EnsureDefaultStoryIfNeeded()
    {
        if (lines != null && lines.Count > 0)
        {
            bool hasAnyContent = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] != null && !string.IsNullOrWhiteSpace(lines[i].content))
                {
                    hasAnyContent = true;
                    break;
                }
            }
            if (hasAnyContent) return;
        }

        // 兼容：当你从旧版本脚本升级（lines 从 List<string> 变更为 List<StoryLine>）时，
        // 场景里旧序列化数据可能导致 lines 为空，这里补回默认剧情。
        lines = new List<StoryLine>
        {
            new StoryLine
            {
                speaker = "旁白",
                content = "山门晨雾未散，你刚被收入门墙，在师傅指引下踏入练气期。"
            },
            new StoryLine
            {
                speaker = "师傅（元神）",
                content = "徒儿，莫慌。我虽只余元神，无法亲自出手，但护你周全仍绰绰有余。"
            },
            new StoryLine
            {
                speaker = "师傅（元神）",
                content = "前方那头小妖，不过山野低阶精怪。正好给你练手，记住呼吸与灵力运转。"
            },
            new StoryLine
            {
                speaker = "徒弟",
                content = "弟子明白！请师傅在旁指点，我来试一试。"
            },
            new StoryLine
            {
                speaker = "旁白",
                content = "妖兽嘶鸣着逼近，你握紧法诀，第一场真正的战斗开始了。"
            }
        };
    }

    private System.Collections.IEnumerator TypeLine(string content)
    {
        _isTyping = true;
        _dialogText.text = "";
        if (_hintText != null) _hintText.text = "点击可跳过";
        for (int i = 0; i < content.Length; i++)
        {
            _dialogText.text = content.Substring(0, i + 1);
            yield return new WaitForSeconds(typeInterval);
        }

        _isTyping = false;
        if (_hintText != null) _hintText.text = "点击鼠标左键 / 空格 继续";
        _typingRoutine = null;
    }

    private void CompleteCurrentLineInstantly()
    {
        if (lines == null || lines.Count == 0) return;
        if (_typingRoutine != null)
        {
            StopCoroutine(_typingRoutine);
            _typingRoutine = null;
        }
        StoryLine line = lines[Mathf.Clamp(_index, 0, lines.Count - 1)];
        _dialogText.text = line.content ?? "";
        _isTyping = false;
        if (_hintText != null) _hintText.text = "点击鼠标左键 / 空格 继续";
    }

    private void BuildUiIfNeeded()
    {
        if (_uiFont == null)
        {
            _uiFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "SimSun", "Arial Unicode MS" },
                28
            );
            if (_uiFont == null)
            {
                _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null || !canvas.gameObject.activeInHierarchy)
        {
            GameObject canvasGo = new GameObject("IntroStoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        Transform root = canvas.transform.Find("IntroStoryRoot");
        if (root == null)
        {
            root = new GameObject("IntroStoryRoot", typeof(RectTransform)).transform;
            root.SetParent(canvas.transform, false);
        }
        RectTransform rootRect = root as RectTransform;
        if (rootRect != null)
        {
            StretchFull(rootRect);
            rootRect.localScale = Vector3.one;
            rootRect.SetAsLastSibling();
        }

        // 背景占位（留白）
        Image bg = GetOrCreateImage(root, "Background");
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        StretchFull(bgRect);
        bg.color = new Color(1f, 1f, 1f, 1f);

        // 对话框（底部）
        Image dialogBox = GetOrCreateImage(root, "DialogBox");
        RectTransform dialogRect = dialogBox.GetComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.05f, 0.03f);
        dialogRect.anchorMax = new Vector2(0.95f, 0.30f);
        dialogRect.offsetMin = Vector2.zero;
        dialogRect.offsetMax = Vector2.zero;
        dialogBox.color = new Color(0f, 0f, 0f, 0.65f);

        // 头像占位（在对话框上方）
        Image portrait = GetOrCreateImage(root, "PortraitPlaceholder");
        RectTransform portraitRect = portrait.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0.08f, 0.32f);
        portraitRect.anchorMax = new Vector2(0.23f, 0.62f);
        portraitRect.offsetMin = Vector2.zero;
        portraitRect.offsetMax = Vector2.zero;
        portrait.color = new Color(1f, 1f, 1f, 0.18f);

        Text portraitText = GetOrCreateText(portrait.transform, "PortraitText", "头像留白", _uiFont);
        StretchFull(portraitText.rectTransform);
        portraitText.alignment = TextAnchor.MiddleCenter;
        portraitText.fontSize = 26;

        _nameText = GetOrCreateText(dialogBox.transform, "NameText", "旁白", _uiFont);
        RectTransform nameRect = _nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0.04f, 0.82f);
        nameRect.anchorMax = new Vector2(0.40f, 0.98f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        _nameText.alignment = TextAnchor.MiddleLeft;
        _nameText.fontSize = 30;
        _nameText.color = new Color(1f, 0.95f, 0.7f, 1f);

        _dialogText = GetOrCreateText(dialogBox.transform, "DialogText", "", _uiFont);
        RectTransform textRect = _dialogText.rectTransform;
        textRect.anchorMin = new Vector2(0.04f, 0.12f);
        textRect.anchorMax = new Vector2(0.96f, 0.78f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        _dialogText.alignment = TextAnchor.UpperLeft;
        _dialogText.fontSize = 34;
        _dialogText.color = Color.white;
        _dialogText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _dialogText.verticalOverflow = VerticalWrapMode.Overflow;

        _hintText = GetOrCreateText(dialogBox.transform, "HintText", "点击鼠标左键 / 空格 继续", _uiFont);
        RectTransform hintRect = _hintText.rectTransform;
        hintRect.anchorMin = new Vector2(0.65f, 0.02f);
        hintRect.anchorMax = new Vector2(0.98f, 0.18f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;
        _hintText.alignment = TextAnchor.MiddleRight;
        _hintText.fontSize = 22;
        _hintText.color = new Color(1f, 1f, 1f, 0.9f);
    }

    private static Image GetOrCreateImage(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) return t.GetComponent<Image>();
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Image>();
    }

    private static Text GetOrCreateText(Transform parent, string name, string content, Font font)
    {
        Transform t = parent.Find(name);
        Text txt;
        if (t != null)
        {
            txt = t.GetComponent<Text>();
            if (txt == null)
            {
                txt = t.gameObject.AddComponent<Text>();
            }
        }
        else
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            txt = go.GetComponent<Text>();
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
        }
        txt.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = content;
        txt.enabled = true;
        txt.raycastTarget = false;
        txt.supportRichText = false;
        return txt;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void EnsureCameraIfMissing()
    {
        if (Camera.main != null) return;
        if (FindObjectOfType<Camera>() != null) return;

        GameObject camGo = new GameObject("IntroStoryCamera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        cam.tag = "MainCamera";
    }
}
