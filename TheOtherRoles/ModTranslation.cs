using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using TheOtherRoles.Patches;
using UnityEngine;


namespace TheOtherRoles
{
	public class TranslationInfo
	{
        public TranslationInfo(string text)
            : this(text, Color.white)
        {
        }

        public TranslationInfo(string text, Color color)
		{
            this.text = text;
            this.color = color;
        }

        public TranslationInfo(string category, int id)
            : this(category, id, Color.white)
        {
        }

        public TranslationInfo(string category, int id, Color color)
		{
            this.category = category;
            this.id = id;
            this.color = color;
        }

        public TranslationInfo(RoleId roleId)
            : this(roleId, Color.white)
        {
        }

        public TranslationInfo(RoleId roleId, Color color)
        {
            this.roleId = roleId;
            this.color = color;
        }

        public void AddHeadText(string text)
		{
            headText = text;
        }
        public void AddTailText(string text)
        {
            tailText = text;
        }

        public string GetString()
		{
            if (!string.IsNullOrEmpty(text))
                return Helpers.cs(color, headText + text + tailText);
            if (roleId != (RoleId)int.MaxValue)
                return Helpers.cs(color, headText + ModTranslation.GetRoleName(roleId, color) + tailText);
            return Helpers.cs(color, headText + ModTranslation.GetString(category, id) + tailText);
		}

        public override string ToString()
        {
            return GetString();
        }

        string headText;
        string text;
        string tailText;
        string category;
        int id;
        Color color;
        RoleId roleId = (RoleId)int.MaxValue;
    }


	
    public class ModTranslation
    {
        // Dictionary<Category, Dictionary<Category-Id, Dictionary<Lang-Id, Str>>>
        public static Dictionary<string, Dictionary<int, Dictionary<int, string>>> stringTable;

        // Language code (filename) -> SupportedLangs int id
        private static readonly Dictionary<string, int> langCodeToId = new()
        {
            { "en",       0 },
            { "es_419",   1 },
            { "pt_BR",    2 },
            { "pt",       3 },
            { "ko",       4 },
            { "ru",       5 },
            { "nl",       6 },
            { "fil",      7 },
            { "fr",       8 },
            { "de",       9 },
            { "it",      10 },
            { "ja",      11 },
            { "es",      12 },
            { "zh_Hans", 13 },
            { "zh_Hant", 14 },
            { "ga",      15 },
        };

        public static void Load()
        {
            stringTable = new();
            var assembly = Assembly.GetExecutingAssembly();

            // Find all translation JSON files embedded as resources
            // Resource names follow pattern: TheOtherRoles.Resources.Translations.XX.json
            string prefix = "TheOtherRoles.Resources.Translations.";
            string suffix = ".json";
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith(prefix) && n.EndsWith(suffix));

            foreach (var resourceName in resourceNames)
            {
                // Extract language code from resource name
                // e.g. "TheOtherRoles.Resources.Translations.en.json" -> "en"
                string langCode = resourceName.Substring(prefix.Length, resourceName.Length - prefix.Length - suffix.Length);

                if (!langCodeToId.TryGetValue(langCode, out int langId))
                    continue;

                // Read and parse the JSON file
                using Stream stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) continue;
                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();
                var parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (parsed == null) continue;

                // Parse each key-value pair: "Category,Id" -> "text"
                foreach (var kvp in parsed)
                {
                    string key = kvp.Key;
                    string text = kvp.Value;
                    if (string.IsNullOrEmpty(text)) continue;

                    int commaIndex = key.IndexOf(",");
                    if (commaIndex < 0) continue;

                    string categoryName = key.Substring(0, commaIndex);
                    if (!int.TryParse(key.Substring(commaIndex + 1), out int categoryId))
                        continue;

                    // Get or create category dict
                    if (!stringTable.TryGetValue(categoryName, out var categoryDict))
                    {
                        categoryDict = new();
                        stringTable[categoryName] = categoryDict;
                    }

                    // Get or create id dict
                    if (!categoryDict.TryGetValue(categoryId, out var langDict))
                    {
                        langDict = new();
                        categoryDict[categoryId] = langDict;
                    }

                    // Store the translation
                    langDict[langId] = (text == blankText) ? "" : text;
                }
            }
        }

        public static string GetString(string category, int id, string def = null)
        {
            if (!stringTable.TryGetValue(category, out var t))
                return def;
            if (!t.TryGetValue(id, out var t2))
                return def;
            int langId = (int)AmongUs.Data.DataManager.Settings.Language.CurrentLanguage;
            if (t2.ContainsKey(langId))
                return t2[langId];
            else if (t2.ContainsKey(defaultLangId))
                return t2[defaultLangId];

            return def;
        }

        public static TranslationInfo GetRoleName(RoleId roleId, Color? color = null)
        {
            return new TranslationInfo("Role-Name", (int)roleId, color.HasValue ? color.Value : Color.white);
        }

        public static TranslationInfo GetRoleIntroDesc(RoleId roleId, Color? color = null)
        {
            return new TranslationInfo("Role-IntroDesc", (int)roleId, color.HasValue ? color.Value : Color.white);
        }

        public static TranslationInfo GetRoleShortDesc(RoleId roleId, Color? color = null)
        {
            return new TranslationInfo("Role-ShortDesc", (int)roleId, color.HasValue ? color.Value : Color.white);
        }

        public static TranslationInfo GetRoleDesc(RoleId roleId)
        {
            return new TranslationInfo("Role-Desc", (int)roleId, Color.white);
        }

        const string blankText = "[BLANK]";
        const int defaultLangId = (int)SupportedLangs.English;
    }

    [HarmonyPatch(typeof(LanguageSetter), nameof(LanguageSetter.SetLanguage))]
    class SetLanguagePatch
    {
        static void Postfix()
        {
            ClientOptionsPatch.UpdateTranslations();
        }
    }
}
