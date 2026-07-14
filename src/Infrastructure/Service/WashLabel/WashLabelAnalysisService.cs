using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.WashLabel;
using NX_lims_Softlines_Command_System.src.Application.Contract.WashLabel;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service.WashLabel;

/// <summary>
/// 通义千问 (Qwen-VL) 视觉识别服务。
/// 使用模板图片作为视觉 Few-Shot 示例提高识别精度。
/// </summary>
public class WashLabelAnalysisService : IWashLabelAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly TemplateLoader _templates;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _endpoint;

    public WashLabelAnalysisService(HttpClient httpClient, IConfiguration configuration, TemplateLoader templates)
    {
        _httpClient = httpClient;
        _templates = templates;
        _apiKey = configuration["WashLabelAI:ApiKey"]
            ?? throw new InvalidOperationException("WashLabelAI:ApiKey 未配置");
        _model = configuration["WashLabelAI:Model"] ?? "qwen-vl-max";
        _endpoint = configuration["WashLabelAI:Endpoint"]
            ?? "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
    }

    public async Task<AnalysisResult> AnalyzeImageAsync(byte[] imageBytes, string mediaType)
    {
        var userImageUri = $"data:{mediaType};base64,{Convert.ToBase64String(imageBytes)}";
        var examples = _templates.PickFewShotExamples();

        var contentList = new List<object>();

        if (examples.Count > 0)
        {
            contentList.Add(new { type = "text", text = "以下是洗护符号参考示例，帮助你准确识别：" });

            foreach (var ex in examples)
            {
                contentList.Add(new { type = "image_url", image_url = new { url = ex.DataUri } });
                contentList.Add(new { type = "text", text = $"↑ 参考符号的标准名称是：「{ex.Name}」（类别：{ex.Category}）" });
            }

            contentList.Add(new { type = "text", text = "现在请分析以下用户上传的洗标照片：" });
        }

        contentList.Add(new { type = "image_url", image_url = new { url = userImageUri } });
        contentList.Add(new { type = "text", text = "请识别这张洗标中的所有文字和洗护符号，返回 JSON。" });

        var requestBody = new
        {
            model = _model,
            temperature = 0.1,
            messages = new object[]
            {
                new { role = "system", content = GetSystemPrompt() },
                new { role = "user", content = contentList.ToArray() }
            }
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"API 返回 {(int)response.StatusCode}: {errorBody[..Math.Min(errorBody.Length, 300)]}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseContent);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        return ParseResponse(text);
    }

    private static string GetSystemPrompt()
    {
        return """
你是一个纺织品洗护标签识别专家，专精于 ISO 3758 标准。

你的任务：分析用户上传的洗标照片，识别其中所有的文字和洗护符号。

【识别规则】
参考图中展示了各类洗护符号的正确名称。请严格按照参考图中的命名方式，将用户图中的每个符号匹配到对应的 ISO 标准名称。

【五类符号命名规范】
- **水洗 (washing)**：洗涤盆符号。"Washing [温度]°C [normal/mild/very mild] process"、"Hand wash [温度]°C"、"Do not wash"
- **漂白 (bleaching)**：三角形符号。"Chlorine bleach allowed" / "Only non-chlorine bleach" / "Do not bleach"
- **干燥 (drying)**：正方形符号。"Tumble dry [normal/low heat]" / "Do not tumble dry" / "Line dry" / "Dry flat" / "Drip dry"
- **熨烫 (ironing)**：熨斗符号。"Iron [low/medium/high] (max [温度]°C)" / "Do not iron"
- **干洗 (dryCleaning)**：圆形符号。"Dry clean [P/F] [normal/mild]" / "Do not dry clean" / "Professional wet clean"

【重要规则】
1. name 字段必须使用上述 ISO 标准英文术语，与参考图命名风格一致
2. 不要遗漏任何一个符号！洗标上通常有多个符号，请逐一识别
3. 如果图片不是洗标或无法识别，symbols 返回空数组并在 summary 中说明
4. rawText 中如果有表格数据（如纤维成分表），请使用 Markdown 表格格式输出（| 纤维 | 含量 |），方便前端渲染

【欧洲洗涤代码速查表（ISO 6330）】
- 3N = Washing 30°C normal process
- 3M = Washing 30°C mild process
- 3G = Washing 30°C very mild process
- 3H = Hand wash 30°C
- 4N = Washing 40°C normal process
- 4M = Washing 40°C mild process
- 4G = Washing 40°C very mild process
- 4H = Hand wash 40°C
- 5N = Washing 50°C normal process
- 5M = Washing 50°C mild process
- 6N = Washing 60°C normal process
- 6M = Washing 60°C mild process
- 7N = Washing 70°C normal process
- 9N = Washing 95°C normal process
- No Wash = Do not wash

【视觉判别关键规则 — 判断 normal / mild / very mild 的唯一标准是横线数量】
洗涤盆符号内部水位线下方：
- 1 条横线 = normal process（如 3N=30°C normal）
- 2 条横线 = mild process（如 3M=30°C mild）
- 3 条横线 = very mild process（如 3G=30°C very mild）
- 没有横线 = Hand wash（如 3H=Hand wash）
请仔细观察洗涤盆内的横线数量，这是唯一判别依据。不要只看温度数字猜测 process 类型。

【输出格式】
你必须严格返回以下 JSON 格式，不要用 ```json 代码块包裹，不要输出任何其他文字。
• name 使用英文 ISO 标准术语（如 "Washing 30°C normal process"）
• meaning 使用中文说明（如 "最高30°C常规水洗"）
• category 使用英文小写分类名（washing / bleaching / drying / ironing / dryCleaning）

{
  "rawText": "提取的完整原文",
  "symbols": [
    {
      "name": "Washing 30°C normal process",
      "meaning": "最高30°C常规水洗",
      "category": "washing"
    }
  ],
  "summary": "综合洗护建议摘要"
}
""";
    }

    private static AnalysisResult ParseResponse(string text)
    {
        AnalysisResult? result = null;
        try
        {
            var cleaned = text.Trim();

            // 处理 ``` 代码块包裹
            if (cleaned.StartsWith("```"))
            {
                var startIdx = cleaned.IndexOf('\n') + 1;
                var endIdx = cleaned.LastIndexOf("```");
                if (endIdx > startIdx)
                    cleaned = cleaned[startIdx..endIdx].Trim();
            }

            result = DeserializeOrNull(cleaned) ?? Fallback(text);
            return result;
        }
        catch (Exception)
        {
            result = Fallback(text);
            return result;
        }
        finally
        {
            // 表格提取失败不影响主流程
            if (result != null && !string.IsNullOrEmpty(result.RawText))
            {
                try { result.Tables = ExtractTables(result.RawText); }
                catch { /* 静默跳过 */ }
            }
        }
    }

    /// <summary>
    /// 尝试从文本中反序列化 AnalysisResult。
    /// 先直接解析，失败则提取 { ... } JSON 片段再试。
    /// </summary>
    private static AnalysisResult? DeserializeOrNull(string text)
    {
        try
        {
            return JsonSerializer.Deserialize<AnalysisResult>(text, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            // AI 可能在 JSON 前后加了说明文字，尝试提取 { ... } 部分
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                var json = text.Substring(start, end - start + 1);
                try
                {
                    return JsonSerializer.Deserialize<AnalysisResult>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch { }
            }
            return null;
        }
    }

    /// <summary>
    /// 从 rawText 中提取所有 Markdown 表格（| col1 | col2 | 格式）。
    /// </summary>
    private static List<StructuredTable> ExtractTables(string rawText)
    {
        var tables = new List<StructuredTable>();
        if (string.IsNullOrEmpty(rawText)) return tables;

        var lines = rawText.Split('\n');
        var tableLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (Regex.IsMatch(trimmed, @"^\|.+\|$"))
            {
                tableLines.Add(trimmed);
            }
            else if (tableLines.Count > 0)
            {
                // 表格块结束，解析它
                ParseBlock(tableLines, tables);
                tableLines.Clear();
            }
        }

        // 处理文件末尾的表格
        if (tableLines.Count > 0)
            ParseBlock(tableLines, tables);

        return tables;
    }

    private static void ParseBlock(List<string> lines, List<StructuredTable> tables)
    {
        if (lines.Count == 0) return;

        // 跳过纯分隔线行（如 |---|---|）
        var dataLines = lines
            .Where(l => !Regex.IsMatch(l, @"^\|[\s\-:]+\|[\s\-:|]+$"))
            .ToList();

        if (dataLines.Count == 0) return;

        // 第一行 = Headers
        var headers = dataLines[0]
            .Trim('|')
            .Split('|')
            .Select(h => h.Trim())
            .ToList();

        if (headers.Count == 0 || headers.All(string.IsNullOrEmpty)) return;

        var table = new StructuredTable { Headers = headers };

        // 后续行 = Rows
        for (int i = 1; i < dataLines.Count; i++)
        {
            var cells = dataLines[i]
                .Trim('|')
                .Split('|')
                .Select(c => c.Trim())
                .ToList();

            var row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Count; j++)
            {
                var value = j < cells.Count ? cells[j] : string.Empty;
                row[headers[j]] = value;
            }
            table.Rows.Add(row);
        }

        tables.Add(table);
    }

    private static AnalysisResult Fallback(string text) => new()
    {
        RawText = text,
        Symbols = new(),
        Summary = "AI 返回格式异常，原始：" + text[..Math.Min(text.Length, 200)]
    };
}
