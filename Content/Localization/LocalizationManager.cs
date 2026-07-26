using Godot;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TheSignal.Systems;

namespace TheSignal.Content.Localization;

/// <summary>
/// Localization pipeline: CSV export from Yarn, 9-language string tables,
/// fallback chains, font atlas management.
/// </summary>
[GlobalClass]
public partial class LocalizationManager : Node
{
    public static LocalizationManager Instance { get; private set; }

    private const string LANG_DIR = "res://Data/Localization/";
    private const string EXPORT_DIR = "res://Data/Localization/Export/";
    private const string FALLBACK_LANG = "en";

    /// <summary>Supported locales (ISO 639-1).</summary>
    public static readonly string[] SUPPORTED_LOCALES = { "en", "es", "fr", "de", "ja", "zh", "ko", "pt", "ru" };

    private Dictionary<string, Dictionary<string, string>> _stringTables = new(); // lang -> { key -> text }
    private string _currentLocale = "en";
    private Dictionary<string, string> _activeTable = new();
    private Dictionary<string, Font> _fontAtlases = new();

    public string CurrentLocale => _currentLocale;
    public event Action<string> OnLocaleChanged;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        LoadAllStringTables();
    }

    // ========== STRING TABLE LOADING ==========

    private void LoadAllStringTables()
    {
        foreach (string lang in SUPPORTED_LOCALES)
        {
            string path = $"{LANG_DIR}{lang}.csv";
            if (!File.Exists(ProjectSettings.GlobalizePath(path))) continue;

            var table = new Dictionary<string, string>();
            using var reader = new StreamReader(ProjectSettings.GlobalizePath(path));

            // Read CSV header line to get line IDs
            string header = reader.ReadLine();
            if (string.IsNullOrEmpty(header)) continue;

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = ParseCsvLine(line);
                if (parts.Count >= 2)
                {
                    string key = parts[0].Trim();
                    string text = parts[1].Trim();
                    if (!string.IsNullOrEmpty(key))
                    {
                        table[key] = text;
                    }
                }
            }

            _stringTables[lang] = table;
            GD.Print($"[Localization] Loaded {table.Count} strings for '{lang}'");
        }

        SetLocale(_currentLocale);
    }

    public void ExportYarnToCsv()
    {
        GD.Print("[Localization] Exporting Yarn dialogue to CSV...");
        var exportDir = ProjectSettings.GlobalizePath(EXPORT_DIR);
        Directory.CreateDirectory(exportDir);

        // Collect all line IDs and default (en) text from Yarn files
        var lines = new Dictionary<string, string>();
        var dialogDir = ProjectSettings.GlobalizePath("res://Data/Dialogue/");
        if (Directory.Exists(dialogDir))
        {
            foreach (string file in Directory.GetFiles(dialogDir, "*.yarn"))
            {
                string content = File.ReadAllText(file);
                // Extract line IDs from Yarn comments or nodes
                foreach (string rawLine in content.Split('\n'))
                {
                    if (rawLine.StartsWith("// ") || rawLine.StartsWith("//\t"))
                    {
                        string id = rawLine.Replace("// ", "").Replace("//\t", "").Trim();
                        if (!string.IsNullOrEmpty(id) && !id.StartsWith("<<") && !id.StartsWith("==="))
                        {
                            if (!lines.ContainsKey(id))
                            {
                                lines[id] = rawLine; // placeholder
                            }
                        }
                    }
                }
            }
        }

        // Write CSV for each locale
        foreach (string lang in SUPPORTED_LOCALES)
        {
            string csvPath = Path.Combine(exportDir, $"{lang}.csv");
            using var writer = new StreamWriter(csvPath, false, Encoding.UTF8);
            writer.WriteLine("key,text");

            foreach (var kvp in lines)
            {
                string text = _stringTables.GetValueOrDefault(lang, _stringTables[FALLBACK_LANG])
                    .GetValueOrDefault(kvp.Key, kvp.Value);
                writer.WriteLine($"{EscapeCsv(kvp.Key)},{EscapeCsv(text)}");
            }

            GD.Print($"[Localization] Exported {lines.Count} lines to {csvPath}");
        }
    }

    // ========== STRING LOOKUP ==========

    public string GetString(string key)
    {
        if (_activeTable.TryGetValue(key, out string text))
            return text;

        // Fallback chain: current → fallback → key itself
        if (_currentLocale != FALLBACK_LANG && _stringTables.TryGetValue(FALLBACK_LANG, out var fallbackTable))
        {
            if (fallbackTable.TryGetValue(key, out string fallbackText))
                return fallbackText;
        }

        return key;
    }

    public string GetString(string key, params object[] args)
    {
        string template = GetString(key);
        return string.Format(template, args);
    }

    public void SetLocale(string locale)
    {
        if (!_stringTables.ContainsKey(locale))
        {
            GD.PrintErr($"[Localization] Locale '{locale}' not loaded, falling back to '{FALLBACK_LANG}'");
            locale = FALLBACK_LANG;
        }

        _currentLocale = locale;
        _activeTable = _stringTables[locale];
        TranslationServer.SetLocale(locale);
        OnLocaleChanged?.Invoke(locale);
        GD.Print($"[Localization] Switched to locale: {locale}");
    }

    // ========== FONT ATLAS MANAGEMENT ==========

    public void LoadFontAtlas(string locale)
    {
        string fontPath = $"res://Assets/Fonts/{locale}/atlas.tres";
        if (ResourceLoader.Exists(fontPath))
        {
            var font = GD.Load<Font>(fontPath);
            _fontAtlases[locale] = font;
        }
    }

    public Font GetFontForLocale(string locale)
    {
        return _fontAtlases.GetValueOrDefault(locale);
    }

    public void ApplyFontToTree(Control root, string locale = "")
    {
        if (string.IsNullOrEmpty(locale)) locale = _currentLocale;
        var font = GetFontForLocale(locale);
        if (font == null) return;

        if (root.Theme == null)
            root.Theme = new Theme();

        root.Theme.DefaultFont = font;
    }

    // ========== CSV UTILITIES ==========

    private List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result;
    }

    private string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    // ========== SAMPLE LOCALE DATA ==========

    public string GetSampleString(string key, string locale)
    {
        // Generate sample based on locale and key for testing
        var prefixes = new Dictionary<string, string>
        {
            ["en"] = "",
            ["es"] = "[ES] ",
            ["fr"] = "[FR] ",
            ["de"] = "[DE] ",
            ["ja"] = "[JP] ",
            ["zh"] = "[CN] ",
            ["ko"] = "[KR] ",
            ["pt"] = "[PT] ",
            ["ru"] = "[RU] "
        };

        string prefix = prefixes.GetValueOrDefault(locale, "");
        return $"{prefix}{key}";
    }

    public static string GetLocaleDisplayName(string locale)
    {
        return locale switch
        {
            "en" => "English",
            "es" => "Español",
            "fr" => "Français",
            "de" => "Deutsch",
            "ja" => "日本語",
            "zh" => "中文",
            "ko" => "한국어",
            "pt" => "Português",
            "ru" => "Русский",
            _ => locale
        };
    }
}