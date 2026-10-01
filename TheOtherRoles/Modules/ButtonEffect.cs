using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Modules;

public static class ButtonEffect
{
    public static bool KeyGuideEnabled => !OperatingSystem.IsAndroid();

    private static Sprite keyBindBackgroundSprite;
    private static SpriteSheet usesIconSheet;

    public static Texture2D LoadTexture(string path)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(path);
            if (stream == null)
            {
                TheOtherRolesPlugin.Logger.LogWarning("[ButtonEffect] Resource not found: " + path);
                return null;
            }

            var data = new byte[stream.Length];
            var read = 0;
            while (read < data.Length)
            {
                var n = stream.Read(data, read, data.Length - read);
                if (n <= 0) break;
                read += n;
            }

            var texture = new Texture2D(0, 0, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            texture.LoadImage(data, false);
            texture.hideFlags |= HideFlags.HideAndDontSave;
            return texture;
        }
        catch (Exception e)
        {
            TheOtherRolesPlugin.Logger.LogWarning("[ButtonEffect] Failed to load " + path + ": " + e.Message);
            return null;
        }
    }

    private static Sprite KeyBindBackgroundSprite
    {
        get
        {
            if (keyBindBackgroundSprite == null)
            {
                var texture = LoadTexture("TheOtherRoles.Resources.KeyBindBackground.png");
                if (texture == null) return null;
                keyBindBackgroundSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 100f);
                if (keyBindBackgroundSprite != null)
                    keyBindBackgroundSprite.hideFlags |= HideFlags.HideAndDontSave;
            }

            return keyBindBackgroundSprite;
        }
    }

    private static void DestroyKeyGuides(GameObject parent)
    {
        if (parent == null) return;
        var parentTransform = parent.transform;
        for (var i = parentTransform.childCount - 1; i >= 0; i--)
        {
            var child = parentTransform.GetChild(i);
            if (child != null && child.name == "HotKeyGuide") Object.Destroy(child.gameObject);
        }
    }

    public static GameObject AddKeyGuide(GameObject button, KeyCode key, Vector2 pos, bool removeExistingGuide)
    {
        if (!KeyGuideEnabled || button == null) return null;

        if (removeExistingGuide) DestroyKeyGuides(button);

        if (!KeyCodeInfo.AllKeyInfo.TryGetValue(key, out var info)) return null;
        var numSprite = info.Sprite;
        if (numSprite == null) return null;
        var background = KeyBindBackgroundSprite;
        if (background == null) return null;

        var obj = new GameObject();
        obj.name = "HotKeyGuide";
        obj.transform.SetParent(button.transform);
        obj.layer = button.layer;
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.transform.localPosition = (Vector3)pos + new Vector3(0f, 0f, -10f);
        renderer.sprite = background;

        var numObj = new GameObject();
        numObj.name = "HotKeyText";
        numObj.transform.SetParent(obj.transform);
        numObj.layer = button.layer;
        renderer = numObj.AddComponent<SpriteRenderer>();
        renderer.transform.localPosition = new Vector3(0f, 0f, -1f);
        renderer.sprite = numSprite;

        return obj;
    }

    public static GameObject SetKeyGuide(GameObject button, KeyCode key, bool removeExistingGuide = true)
    {
        return AddKeyGuide(button, key, new Vector2(0.48f, 0.48f), removeExistingGuide);
    }

    public static void ShowVanillaKeyGuide(this HudManager manager)
    {
        if (!KeyGuideEnabled || manager == null) return;

        var key = GetKeyForAction(RewiredConsts.Action.ToggleMap);
        if (key != null)
        {
            if (manager.MapButton != null) SetKeyGuide(manager.MapButton.gameObject, key.Value);
            if (manager.SabotageButton != null) SetKeyGuide(manager.SabotageButton.gameObject, key.Value);
        }

        key = GetKeyForAction(RewiredConsts.Action.ActionPrimary);
        if (key != null)
        {
            if (manager.UseButton != null) SetKeyGuide(manager.UseButton.gameObject, key.Value);
            if (manager.PetButton != null) SetKeyGuide(manager.PetButton.gameObject, key.Value);
        }

        key = GetKeyForAction(RewiredConsts.Action.ActionTertiary);
        if (key != null && manager.ReportButton != null)
            SetKeyGuide(manager.ReportButton.gameObject, key.Value);

        key = GetKeyForAction(RewiredConsts.Action.ActionSecondary);
        if (key != null && manager.KillButton != null)
            SetKeyGuide(manager.KillButton.gameObject, key.Value);

        key = GetKeyForAction(RewiredConsts.Action.UseVent);
        if (key != null && manager.ImpostorVentButton != null)
            SetKeyGuide(manager.ImpostorVentButton.gameObject, key.Value);
    }

    private static KeyCode? GetKeyForAction(int actionId)
    {
        try
        {
            var player = Rewired.ReInput.players.GetPlayer(0);
            if (player == null) return null;
            var map = player.controllers.maps.GetFirstButtonMapWithAction(actionId, true);
            if (map == null) return null;

            var key = map.keyCode;
            if (key != KeyCode.None) return key;

            var identifier = map.elementIdentifierName;
            if (string.IsNullOrEmpty(identifier)) return null;
            return (KeyCode)Enum.Parse(typeof(KeyCode), identifier, true);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static TextMeshPro FindChildText(GameObject parent)
    {
        var parentTransform = parent.transform;
        for (var i = 0; i < parentTransform.childCount; i++)
        {
            var tmp = parentTransform.GetChild(i).GetComponent<TextMeshPro>();
            if (tmp != null) return tmp;
        }

        return null;
    }

    public static GameObject ShowUsesIcon(this ActionButton button, int iconVariation, out TextMeshPro text)
    {
        text = null;
        if (button == null) return null;

        if (usesIconSheet == null)
            usesIconSheet = new SpriteSheet("TheOtherRoles.Resources.UsesIcon.png", 120f, 57, 56);
        var sprite = usesIconSheet.GetSprite(iconVariation);

        var renderer = button.usesRemainingSprite;
        var label = button.usesRemainingText;
        GameObject holder;
        var createdFreshText = false;

        if (renderer != null)
        {
            holder = renderer.gameObject;
        }
        else
        {
            GameObject template = null;
            var hud = HudManager.Instance;
            if (hud != null && hud.AbilityButton != null && hud.AbilityButton.usesRemainingSprite != null)
                template = hud.AbilityButton.usesRemainingSprite.gameObject;

            if (template != null)
            {
                holder = Object.Instantiate(template, button.transform);
                holder.transform.localPosition = template.transform.localPosition;
                holder.transform.localScale = template.transform.localScale;
                renderer = holder.GetComponent<SpriteRenderer>();
                if (label == null) label = FindChildText(holder);
            }
            else
            {
                holder = new GameObject("UsesIcon");
                holder.transform.SetParent(button.transform, false);
                holder.transform.localPosition = new Vector3(0.48f, 0.48f, -10f);
                holder.layer = button.gameObject.layer;
                renderer = holder.AddComponent<SpriteRenderer>();
            }
        }

        if (renderer != null)
        {
            if (sprite != null) renderer.sprite = sprite;
            renderer.gameObject.SetActive(true);
        }

        if (label == null && button.cooldownTimerText != null)
        {
            label = Object.Instantiate(button.cooldownTimerText, holder.transform);
            label.transform.localPosition = new Vector3(0f, 0f, -1f);
            label.transform.localScale = Vector3.one * 0.5f;
            createdFreshText = true;
        }

        if (label != null)
        {
            label.gameObject.SetActive(true);
            label.enableWordWrapping = false;
            if (!createdFreshText)
            {
                label.transform.localScale *= 0.85f;
                var localPosition = label.transform.localPosition;
                label.transform.localPosition =
                    new Vector3(localPosition.x, localPosition.y - 0.01f, localPosition.z);
            }

            text = label;
        }

        return holder;
    }
}

public class SpriteSheet
{
    private readonly string address;
    private readonly float pixelsPerUnit;
    private readonly int cellWidth;
    private readonly int cellHeight;
    private Sprite[] sprites;
    private Texture2D sheetTexture;
    private bool loaded;

    public SpriteSheet(string address, float pixelsPerUnit, int cellWidth, int cellHeight)
    {
        this.address = address;
        this.pixelsPerUnit = pixelsPerUnit;
        this.cellWidth = cellWidth;
        this.cellHeight = cellHeight;
    }

    public Sprite GetSprite(int index)
    {
        if (!loaded)
        {
            loaded = true;
            var texture = ButtonEffect.LoadTexture(address);
            if (texture == null || cellWidth <= 0 || cellHeight <= 0) return null;
            sprites = new Sprite[(texture.width / cellWidth) * (texture.height / cellHeight)];
            sheetTexture = texture;
        }

        if (sprites == null || index < 0 || index >= sprites.Length) return null;

        if (sprites[index] == null)
        {
            var columns = sheetTexture.width / cellWidth;
            var rows = sheetTexture.height / cellHeight;
            var x = index % columns;
            var y = index / columns;
            var rect = new Rect(x * cellWidth, (rows - y - 1) * cellHeight, cellWidth, cellHeight);
            var sprite = Sprite.Create(sheetTexture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
            if (sprite != null) sprite.hideFlags |= HideFlags.HideAndDontSave;
            sprites[index] = sprite;
        }

        return sprites[index];
    }
}

public class KeyCodeInfo
{
    public static string GetKeyDisplayName(KeyCode keyCode)
    {
        if (keyCode == KeyCode.Return) return "Return";
        return AllKeyInfo.TryGetValue(keyCode, out var val) ? val.TranslationKey : null;
    }

    public static readonly Dictionary<KeyCode, KeyCodeInfo> AllKeyInfo = new();

    public KeyCode keyCode { get; }
    public string TranslationKey { get; }

    private readonly SpriteSheet textureHolder;
    private readonly int num;

    public KeyCodeInfo(KeyCode keyCode, string translationKey, SpriteSheet spriteLoader, int num)
    {
        this.keyCode = keyCode;
        TranslationKey = translationKey;
        textureHolder = spriteLoader;
        this.num = num;
        AllKeyInfo[keyCode] = this;
    }

    public Sprite Sprite => textureHolder.GetSprite(num);

    static KeyCodeInfo()
    {
        SpriteSheet spriteLoader;
        spriteLoader = new SpriteSheet("TheOtherRoles.Resources.KeyBindCharacters0.png", 100f, 18, 19);
        new KeyCodeInfo(KeyCode.Tab, "Tab", spriteLoader, 0);
        new KeyCodeInfo(KeyCode.Space, "Space", spriteLoader, 1);
        new KeyCodeInfo(KeyCode.Comma, "<", spriteLoader, 2);
        new KeyCodeInfo(KeyCode.Period, ">", spriteLoader, 3);

        spriteLoader = new SpriteSheet("TheOtherRoles.Resources.KeyBindCharacters1.png", 100f, 18, 19);
        for (var key = KeyCode.A; key <= KeyCode.Z; key++)
            new KeyCodeInfo(key, ((char)('A' + key - KeyCode.A)).ToString(), spriteLoader, key - KeyCode.A);

        spriteLoader = new SpriteSheet("TheOtherRoles.Resources.KeyBindCharacters2.png", 100f, 18, 19);
        for (var i = 0; i < 15; i++)
            new KeyCodeInfo(KeyCode.F1 + i, "F" + (i + 1), spriteLoader, i);

        spriteLoader = new SpriteSheet("TheOtherRoles.Resources.KeyBindCharacters3.png", 100f, 18, 19);
        new KeyCodeInfo(KeyCode.RightShift, "RShift", spriteLoader, 0);
        new KeyCodeInfo(KeyCode.LeftShift, "LShift", spriteLoader, 1);
        new KeyCodeInfo(KeyCode.RightControl, "RControl", spriteLoader, 2);
        new KeyCodeInfo(KeyCode.LeftControl, "LControl", spriteLoader, 3);
        new KeyCodeInfo(KeyCode.RightAlt, "RAlt", spriteLoader, 4);
        new KeyCodeInfo(KeyCode.LeftAlt, "LAlt", spriteLoader, 5);

        spriteLoader = new SpriteSheet("TheOtherRoles.Resources.KeyBindCharacters4.png", 100f, 18, 19);
        for (var i = 0; i < 6; i++)
            new KeyCodeInfo(KeyCode.Mouse1 + i, "Mouse " + (i == 0 ? "Right" : i == 1 ? "Middle" : (i + 1).ToString()),
                spriteLoader, i);

        spriteLoader = new SpriteSheet("TheOtherRoles.Resources.KeyBindCharacters5.png", 100f, 18, 19);
        for (var i = 0; i < 10; i++)
            new KeyCodeInfo(KeyCode.Alpha0 + i, i.ToString(), spriteLoader, i);
    }
}
