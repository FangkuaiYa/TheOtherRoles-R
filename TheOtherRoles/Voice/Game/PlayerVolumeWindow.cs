using System;
using System.Linq;
using TheOtherRoles.Voice.Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheOtherRoles.Voice.Game;

public class PlayerVolumeWindow : MonoBehaviour
{
    private const KeyCode ToggleKey = KeyCode.F4;

    private const float WinW = 640f;
    private const float WinH = 760f;
    private const float TitleBarH = 64f;
    private const float RowH = 68f;
    private const float ContentW = WinW - 96f;
    private const float ContentRight = ContentW / 2f - 40f;

    private bool _built;
    private Canvas _canvas;
    private RectTransform _content;
    private float _refreshTimer;
    private ScrollRect _scroll;

    private GameObject _uiRoot;
    private RectTransform _winRt;

    private float _y;

    public PlayerVolumeWindow(IntPtr ptr) : base(ptr)
    {
    }

    public static PlayerVolumeWindow Instance { get; private set; }
    public bool ShowWindow { get; private set; }

    private bool _isAndroid => Application.platform == RuntimePlatform.Android;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey)) Toggle();
        if (ShowWindow && Input.GetKeyDown(KeyCode.Escape)) Close();

        if (ShowWindow)
        {
            _refreshTimer -= Time.deltaTime;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = 0.5f;
                RebuildContent();
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_uiRoot != null) Destroy(_uiRoot);
    }

    public static PlayerVolumeWindow EnsureInstance()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("VCGlobalPlayerVolumeWindow");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<PlayerVolumeWindow>();
        return Instance;
    }

    private float F(float px)
    {
        return _isAndroid ? px * 1.28f : px;
    }

    public void Toggle()
    {
        if (ShowWindow) Close();
        else Open();
    }

    public void Open()
    {
        try
        {
            if (!_built) BuildUI();
            if (_uiRoot == null)
            {
                TheOtherRolesPlugin.Logger?.LogError("[VC] Player volume UI failed to build (_uiRoot is null).");
                return;
            }

            try
            {
                var cam = FindObjectOfType<Camera>();
                if (cam != null && _canvas != null) _canvas.targetDisplay = cam.targetDisplay;
            }
            catch
            {
            }

            // Avoid stacking multiple VC windows on top of each other.
            try
            {
                VoiceSettingsWindow.Instance?.Close();
            }
            catch
            {
            }

            try
            {
                PublicLobbyWindow.Instance?.Close();
            }
            catch
            {
            }

            _uiRoot.SetActive(true);
            ShowWindow = true;

            var opt = FindObjectOfType<OptionsMenuBehaviour>();
            if (opt) opt.Close();

            _refreshTimer = 0f;
            RebuildContent();
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;
        }
        catch (Exception e)
        {
            TheOtherRolesPlugin.Logger?.LogError($"[VC] Open player volume window failed: {e}");
            _built = false;
            _uiRoot = null;
        }
    }

    public void Close()
    {
        ShowWindow = false;
        if (_uiRoot != null) _uiRoot.SetActive(false);
    }

    // ========================================================
    //  Window frame (built once)
    // ========================================================
    private void BuildUI()
    {
        if (_uiRoot != null) Destroy(_uiRoot);
        _canvas = VCUiKit.EnsureCanvas();

        _uiRoot = new GameObject("VCPlayerVolumeUI");
        _uiRoot.transform.SetParent(_canvas.transform, false);
        var rootRt = _uiRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        _uiRoot.SetActive(false);

        var dim = VCUiKit.CreateImage(_uiRoot.transform, "Dim", Vector2.zero, Vector2.zero, VCUiKit.PixelSprite,
            new Color(0f, 0f, 0f, 0.65f));
        var dimRt = (RectTransform)dim.transform;
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        var dimBtn = dim.gameObject.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener((Action)(() => Close()));

        _winRt = VCUiKit.CreatePanel(_uiRoot.transform, "Window", new Vector2(WinW, WinH),
            new Color(0.35f, 0.35f, 0.38f, 1f), new Color(0.06f, 0.06f, 0.08f, 0.98f), 4f);
        _winRt.anchorMin = _winRt.anchorMax = new Vector2(0.5f, 0.5f);
        _winRt.anchoredPosition = Vector2.zero;

        BuildTitleBar(_winRt);
        BuildScrollArea(_winRt);
        _built = true;
    }

    private void BuildTitleBar(Transform win)
    {
        var title = VCUiKit.CreateText(win, "Title", "Player Volume",
            Vector2.zero, new Vector2(400f, 44f), F(28f), new Color(0.92f, 0.95f, 1f, 1f),
            FontStyles.Bold);
        var titleRt = (RectTransform)title.transform;
        titleRt.anchorMin = new Vector2(0f, 0.5f);
        titleRt.anchorMax = new Vector2(0f, 0.5f);
        titleRt.pivot = new Vector2(0f, 0.5f);
        titleRt.anchoredPosition = new Vector2(30f, WinH / 2f - TitleBarH / 2f);

        var close = VCUiKit.CreateButton(win, "X", Vector2.zero, new Vector2(44f, 44f),
            new Color(0.58f, 0.22f, 0.24f, 1f), () => Close(), F(24f));
        var closeRt = (RectTransform)close.transform;
        closeRt.anchorMin = closeRt.anchorMax = new Vector2(1f, 0.5f);
        closeRt.anchoredPosition = new Vector2(-34f, WinH / 2f - TitleBarH / 2f);
    }

    private void BuildScrollArea(Transform win)
    {
        var topY = WinH / 2f - TitleBarH - 10f;
        var bottomY = -WinH / 2f + 20f;
        var viewH = topY - bottomY;

        var viewport = VCUiKit.NewRect(win, "Viewport");
        viewport.anchorMin = viewport.anchorMax = new Vector2(0.5f, 0.5f);
        viewport.anchoredPosition = new Vector2(0f, (topY + bottomY) / 2f);
        viewport.sizeDelta = new Vector2(ContentW + 20f, viewH);
        viewport.gameObject.AddComponent<RectMask2D>();

        _content = VCUiKit.NewRect(viewport, "Content");
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(0f, 1f);
        _content.pivot = new Vector2(0f, 1f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = new Vector2(ContentW, 10f);

        var bg = VCUiKit.CreateImage(_content, "ScrollBG", Vector2.zero, _content.sizeDelta, VCUiKit.PixelSprite,
            Color.clear);
        var bgRt = bg.rectTransform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bgRt.SetAsFirstSibling();

        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = _content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;
        scroll.inertia = true;
        scroll.verticalNormalizedPosition = 1f;
        _scroll = scroll;
    }

    private void RebuildContent()
    {
        if (_content == null) return;
        var keepScroll = _scroll != null ? _scroll.verticalNormalizedPosition : 1f;

        for (var i = _content.childCount - 1; i >= 0; i--)
        {
            var child = _content.GetChild(i);
            if (child.name == "ScrollBG") continue;
            Destroy(child.gameObject);
        }

        _y = 0f;

        try
        {
            var room = VoiceRoom.Current;
            if (room == null)
            {
                RenderInfoRow("Not connected to a voice room.");
            }
            else
            {
                var players = room.AllClients
                    .Where(c => c.PlayerId != byte.MaxValue)
                    .OrderBy(c => c.PlayerName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (players.Count == 0)
                    RenderInfoRow("No other players connected yet.");
                else
                    foreach (var p in players)
                        RenderPlayerRow(p);
            }
        }
        catch (Exception e)
        {
            TheOtherRolesPlugin.Logger?.LogError($"[VC] Player volume RebuildContent failed: {e}");
        }

        _content.sizeDelta = new Vector2(ContentW, _y + 24f);
        if (_scroll != null) _scroll.verticalNormalizedPosition = keepScroll;
    }

    private RectTransform AddRow()
    {
        var row = VCUiKit.NewRect(_content, "Row");
        row.anchorMin = row.anchorMax = new Vector2(0f, 1f);
        row.pivot = new Vector2(0f, 1f);
        row.anchoredPosition = new Vector2(0f, -_y);
        row.sizeDelta = new Vector2(ContentW, RowH);
        _y += RowH;

        var div = VCUiKit.CreateDivider(row, Vector2.zero, new Vector2(ContentW - 40f, 2f));
        var divRt = div.rectTransform;
        divRt.anchorMin = new Vector2(0f, 0f);
        divRt.anchorMax = new Vector2(0f, 0f);
        divRt.pivot = new Vector2(0f, 0f);
        divRt.anchoredPosition = new Vector2(20f, 3f);
        divRt.sizeDelta = new Vector2(ContentW - 40f, 2f);
        return row;
    }

    private void RenderInfoRow(string text)
    {
        var row = AddRow();
        VCUiKit.CreateText(row, "Info", text,
            new Vector2(-ContentW / 2f + 70f + 260f, 0f), new Vector2(ContentW - 120f, RowH - 12f),
            F(19f), Color.gray, FontStyles.Normal, TextAlignmentOptions.Left, true);
    }

    private void RenderPlayerRow(VCPlayer p)
    {
        var row = AddRow();

        var displayName = "...";
        if (p.IsMapped)
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                if (pc != null && pc.PlayerId == p.PlayerId)
                {
                    var data = pc.Data;
                    if (data != null && !string.IsNullOrWhiteSpace(data.PlayerName))
                        displayName = data.PlayerName;
                    break;
                }

        if (displayName == "..." && !string.IsNullOrWhiteSpace(p.PlayerName))
            displayName = p.PlayerName;

        VCUiKit.CreateText(row, "Name", displayName,
            new Vector2(-ContentW / 2f + 70f + 150f, 0f), new Vector2(300f, RowH - 12f),
            F(21f), Color.white, FontStyles.Bold, TextAlignmentOptions.Left, true);

        var valueTmp = VCUiKit.CreateText(row, "Value", $"{p.Volume * 100f:F0}%", Vector2.zero,
            new Vector2(70f, RowH - 12f), F(20f), new Color(1f, 0.86f, 0.55f, 1f),
            FontStyles.Bold, TextAlignmentOptions.Right);
        var vrt = (RectTransform)valueTmp.transform;
        vrt.anchorMin = vrt.anchorMax = new Vector2(1f, 0.5f);
        vrt.anchoredPosition = new Vector2(-40f, 0f);

        var sliderW = 240f;
        var pid = p.PlayerId;
        var pname = p.PlayerName;
        VCUiKit.CreateSlider(row, new Vector2(ContentRight - 40f - 70f - 20f - sliderW / 2f, 0f),
            new Vector2(sliderW, 44f), 0f, 2f, p.Volume,
            v =>
            {
                p.SetVolume(v);
                VoiceConfig.SetPlayerVolume(pname, v);
                valueTmp.text = $"{v * 100f:F0}%";
            });
    }
}