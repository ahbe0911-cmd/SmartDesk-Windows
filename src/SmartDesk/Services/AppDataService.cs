using System.Text.Encodings.Web;
using System.Text.Json;
using SmartDesk.Models;

namespace SmartDesk.Services;

public sealed class AppDataService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public AppData Load()
    {
        Directory.CreateDirectory(AppPaths.DataDirectory);

        if (!File.Exists(AppPaths.DataFile))
        {
            var initial = CreateDefault();
            Save(initial);
            return initial;
        }

        try
        {
            var json = File.ReadAllText(AppPaths.DataFile);
            var data = JsonSerializer.Deserialize<AppData>(json, _jsonOptions) ?? CreateDefault();
            Normalize(data);
            return data;
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "خواندن تنظیمات برنامه");
            BackupCorruptFile();
            var cleanData = CreateDefault();
            Save(cleanData);
            return cleanData;
        }
    }

    public void Save(AppData data)
    {
        Normalize(data);
        Directory.CreateDirectory(AppPaths.DataDirectory);
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var temporaryPath = AppPaths.DataFile + ".tmp";
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, AppPaths.DataFile, true);
    }

    public void Export(AppData data, string destination)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        File.WriteAllText(destination, json);
    }

    public AppData Import(string source)
    {
        var json = File.ReadAllText(source);
        var data = JsonSerializer.Deserialize<AppData>(json, _jsonOptions)
                   ?? throw new InvalidDataException("ساختار فایل پشتیبان معتبر نیست.");
        Normalize(data);

        foreach (var item in data.QuickLinks)
        {
            if (!BrowserUriService.TryNormalize(item.Url, out _))
            {
                throw new InvalidDataException($"نشانی میانبر «{item.Title}» معتبر نیست.");
            }
        }

        Save(data);
        return data;
    }

    private static AppData CreateDefault() => new()
    {
        QuickLinks =
        [
            new QuickLink
            {
                Title = "سامانه سامان",
                Url = "https://www.samanpl.ir/",
                Category = "کتابخانه",
                Icon = "ک",
                AccentHex = "#14B8A6",
                SortOrder = 0
            },
            new QuickLink
            {
                Title = "جست‌وجوی گوگل",
                Url = "https://www.google.com/",
                Category = "جست‌وجو",
                Icon = "ج",
                AccentHex = "#4285F4",
                SortOrder = 1
            },
            new QuickLink
            {
                Title = "نهاد کتابخانه‌ها",
                Url = "https://www.iranpl.ir/",
                Category = "کتابخانه",
                Icon = "ن",
                AccentHex = "#F59E0B",
                SortOrder = 2
            }
        ]
    };

    private static void Normalize(AppData data)
    {
        data.Settings ??= new AppSettings();
        data.QuickLinks ??= [];
        data.Settings.LastOpenTabs ??= [];

        var usedIds = new HashSet<Guid>();
        foreach (var item in data.QuickLinks)
        {
            if (item.Id == Guid.Empty || !usedIds.Add(item.Id))
            {
                item.Id = Guid.NewGuid();
                usedIds.Add(item.Id);
            }

            item.Title = (item.Title ?? string.Empty).Trim();
            item.Url = (item.Url ?? string.Empty).Trim();
            item.Category = string.IsNullOrWhiteSpace(item.Category) ? "عمومی" : item.Category.Trim();
            item.Icon = string.IsNullOrWhiteSpace(item.Icon) ? "●" : item.Icon.Trim();
            item.AccentHex = string.IsNullOrWhiteSpace(item.AccentHex) ? "#18A999" : item.AccentHex.Trim();
        }
    }

    private static void BackupCorruptFile()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.BackupDirectory);
            var destination = Path.Combine(AppPaths.BackupDirectory, $"corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Copy(AppPaths.DataFile, destination, true);
        }
        catch
        {
            // Best-effort backup only.
        }
    }
}
