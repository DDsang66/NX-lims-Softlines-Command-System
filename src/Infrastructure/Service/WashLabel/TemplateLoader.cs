using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.WashLabel;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service.WashLabel;

/// <summary>
/// 加载模板图片，从文件名自动提取 ISO 标准名称作为 Ground Truth。
/// 每次请求时从中随机选择参考图作为视觉 Few-Shot 示例。
/// </summary>
public class TemplateLoader
{
    // Europe washing code → ISO standard name mapping
    private static readonly Dictionary<string, string> EuropeWashingMap = new()
    {
        ["3N"] = "Washing 30°C normal process",
        ["3M"] = "Washing 30°C mild process",
        ["3G"] = "Washing 30°C very mild process",
        ["3H"] = "Hand wash 30°C",
        ["4N"] = "Washing 40°C normal process",
        ["4M"] = "Washing 40°C mild process",
        ["4G"] = "Washing 40°C very mild process",
        ["4H"] = "Hand wash 40°C",
        ["5N"] = "Washing 50°C normal process",
        ["5M"] = "Washing 50°C mild process",
        ["6N"] = "Washing 60°C normal process",
        ["6M"] = "Washing 60°C mild process",
        ["7N"] = "Washing 70°C normal process",
        ["9N"] = "Washing 95°C normal process",
    };

    // Europe descriptive name → ISO standard name
    private static readonly Dictionary<string, string> EuropeNameMap = new()
    {
        ["No Wash"] = "Do not wash",
        ["Do not bleach"] = "Do not bleach",
        ["Non-chlorine bleaching"] = "Only non-chlorine bleach",
        ["Any Bleaching"] = "Chlorine bleach allowed",
        ["Cool iron"] = "Iron low (max 110°C)",
        ["Warm iron"] = "Iron medium (max 150°C)",
        ["Hot iron"] = "Iron high (max 200°C)",
        ["Do not iron"] = "Do not iron",
        ["DC Normal"] = "Dry clean P normal",
        ["DC Sensitive"] = "Dry clean P mild",
        ["Petroleum DC Normal"] = "Dry clean F normal",
        ["Petroleum DC Sensitive"] = "Dry clean F mild",
        ["Do not dry-clean"] = "Do not dry clean",
        ["Tumble dry"] = "Tumble dry normal",
        ["Tumble dry low"] = "Tumble dry low heat",
        ["Do not tumble dry"] = "Do not tumble dry",
        ["Line dry"] = "Line dry",
        ["Flat dry"] = "Dry flat",
        ["Drip dry"] = "Drip dry",
    };

    private readonly List<TemplateImage> _templates = new();
    private readonly Random _random = new();
    private readonly string _basePath;

    public TemplateLoader(string basePath)
    {
        _basePath = basePath;
        LoadAll();
    }

    public int Count => _templates.Count;

    /// <summary>
    /// 从指定类别中各选一张模板图作为 few-shot 示例。
    /// 如果某个类别没有模板图，跳过。
    /// </summary>
    public List<TemplateImage> PickFewShotExamples()
    {
        var categories = new[] { "washing", "bleaching", "drying", "ironing", "dryCleaning" };
        var selected = new List<TemplateImage>();

        foreach (var cat in categories)
            selected.AddRange(_templates.Where(t => t.Category == cat));

        return selected;
    }

    private void LoadAll()
    {
        LoadFolder("default", "default");
        LoadFolder("USA", "USA");
        LoadFolder("Europe", "Europe");
    }

    private void LoadFolder(string folderName, string region)
    {
        var dir = Path.Combine(_basePath, folderName);
        if (!Directory.Exists(dir))
            return;

        foreach (var file in Directory.GetFiles(dir))
        {
            if (IsImageFile(file))
                AddTemplate(file, folderName, region);
        }

        foreach (var subDir in Directory.GetDirectories(dir))
        {
            foreach (var file in Directory.GetFiles(subDir))
            {
                if (IsImageFile(file))
                    AddTemplate(file, Path.GetFileName(subDir), region);
            }
        }
    }

    private void AddTemplate(string filePath, string sourceDir, string region)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var ext = Path.GetExtension(filePath).ToLower();

        fileName = System.Text.RegularExpressions.Regex.Replace(fileName, @"_\d+$", "");

        var mediaType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/png"
        };

        var category = DetectCategory(fileName, sourceDir);
        var name = GetStandardName(fileName, region);

        try
        {
            var bytes = File.ReadAllBytes(filePath);
            var base64 = Convert.ToBase64String(bytes);

            _templates.Add(new TemplateImage
            {
                Name = name,
                Category = category,
                Base64Data = base64,
                MediaType = mediaType
            });
        }
        catch
        {
            // skip unreadable files
        }
    }

    private static string DetectCategory(string fileName, string sourceDir)
    {
        var dir = sourceDir.ToLower();
        var name = fileName.ToLower();

        if (dir == "washing") return "washing";
        if (dir == "bleach") return "bleaching";
        if (dir == "dry") return "drying";
        if (dir == "iron") return "ironing";
        if (dir == "dc") return "dryCleaning";

        if (name.Contains("wash") || name.Contains("hand wash")) return "washing";
        if (name.Contains("bleach")) return "bleaching";
        if (name.Contains("dry") || name.Contains("tumble") || name.Contains("line") || name.Contains("flat") || name.Contains("drip")) return "drying";
        if (name.Contains("iron")) return "ironing";
        if (name.Contains("solvent") || name.Contains("dry-clean") || name.Contains("petroleum")) return "dryCleaning";
        if (name == "dcdefault") return "dryCleaning";

        return "other";
    }

    private static string GetStandardName(string fileName, string region)
    {
        if (region == "Europe")
        {
            if (EuropeWashingMap.TryGetValue(fileName, out var mapped))
                return mapped;

            if (EuropeNameMap.TryGetValue(fileName, out var descMapped))
                return descMapped;
        }

        return fileName;
    }

    private static bool IsImageFile(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp";
    }
}
