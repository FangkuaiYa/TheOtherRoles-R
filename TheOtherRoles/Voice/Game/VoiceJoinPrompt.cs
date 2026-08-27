using TheOtherRoles.Voice.Game.UI;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Voice.Game;

public static class VoiceJoinPrompt
{
    private const float WinW = 560f;
    private const float WinH = 280f;
    private static bool _popupBuilt;
    private static GameObject _uiRoot;
    private static Canvas _canvas;

    public static bool HasJoinedVoice { get; private set; }

    public static bool HasAnswered { get; private set; }

    public static void Reset()
    {
        HasAnswered = false;
        HasJoinedVoice = false;
        _popupBuilt = false;
        if (_uiRoot != null)
        {
            try
            {
                Object.Destroy(_uiRoot);
            }
            catch
            {
            }

            _uiRoot = null;
        }
    }

    [HideFromIl2Cpp]
    internal static void Update()
    {
        if (HasAnswered) return;
        if (_popupBuilt) return;

        _popupBuilt = true;
        Build();
    }

    private static void Build()
    {
        _canvas = VCUiKit.EnsureCanvas();

        _uiRoot = new GameObject("VCJoinPrompt");
        _uiRoot.transform.SetParent(_canvas.transform, false);
        var rootRt = _uiRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // Dim background
        var dim = VCUiKit.CreateImage(_uiRoot.transform, "Dim", Vector2.zero, Vector2.zero,
            VCUiKit.PixelSprite, new Color(0f, 0f, 0f, 0.75f));
        var dimRt = (RectTransform)dim.transform;
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;

        // Panel (black background + subtle border)
        var panel = VCUiKit.CreatePanel(_uiRoot.transform, "Panel", new Vector2(WinW, WinH),
            new Color(0.35f, 0.35f, 0.38f, 1f), new Color(0.08f, 0.08f, 0.10f, 0.98f), 4f);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;

        // Title
        var title = VCUiKit.CreateText(panel, "Title", "Voice Chat",
            Vector2.zero, new Vector2(WinW - 40f, 50f), 30f,
            new Color(0.92f, 0.95f, 1f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
        var titleRt = (RectTransform)title.transform;
        titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -20f);

        // Description
        var desc = VCUiKit.CreateText(panel, "Desc",
            "Do you want to join voice chat?\n\n" +
            "Press H to toggle the voice panel.\n" +
            "Click the mic button to switch channels.",
            Vector2.zero, new Vector2(WinW - 80f, 120f), 20f,
            new Color(0.78f, 0.82f, 0.90f, 1f), FontStyles.Normal, TextAlignmentOptions.Center);
        var descRt = (RectTransform)desc.transform;
        descRt.anchorMin = descRt.anchorMax = new Vector2(0.5f, 0.5f);
        descRt.pivot = new Vector2(0.5f, 0.5f);
        descRt.anchoredPosition = new Vector2(0f, 25f);

        // Yes button
        var yesBtn = VCUiKit.CreateButton(panel, "Yes",
            new Vector2(-110f, -90f), new Vector2(160f, 48f),
            new Color(0.18f, 0.55f, 0.34f, 1f), OnClickYes, 22f);
        var yesRt = (RectTransform)yesBtn.transform;
        yesRt.anchorMin = yesRt.anchorMax = new Vector2(0.5f, 0f);
        yesRt.pivot = new Vector2(0.5f, 0f);

        // No button
        var noBtn = VCUiKit.CreateButton(panel, "No",
            new Vector2(110f, -90f), new Vector2(160f, 48f),
            new Color(0.58f, 0.22f, 0.24f, 1f), OnClickNo, 22f);
        var noRt = (RectTransform)noBtn.transform;
        noRt.anchorMin = noRt.anchorMax = new Vector2(0.5f, 0f);
        noRt.pivot = new Vector2(0.5f, 0f);
    }

    private static void OnClickYes()
    {
        HasJoinedVoice = true;
        HasAnswered = true;
        if (_uiRoot != null)
        {
            try
            {
                Object.Destroy(_uiRoot);
            }
            catch
            {
            }

            _uiRoot = null;
        }

        TheOtherRolesPlugin.Logger?.LogInfo("[VC] Player chose to join voice chat.");
    }

    private static void OnClickNo()
    {
        HasJoinedVoice = false;
        HasAnswered = true;
        if (_uiRoot != null)
        {
            try
            {
                Object.Destroy(_uiRoot);
            }
            catch
            {
            }

            _uiRoot = null;
        }

        TheOtherRolesPlugin.Logger?.LogInfo("[VC] Player declined voice chat.");
    }
}