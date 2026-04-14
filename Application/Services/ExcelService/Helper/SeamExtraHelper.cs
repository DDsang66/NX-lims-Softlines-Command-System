using NX_lims_Softlines_Command_System.Application.DTO;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.Helper
{
    public static class SeamExtraHelper
    {
        /// <summary>
        /// 从 CheckListDto.Extra 数组里第 objIndex 个对象中取 fieldName 字段值
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="dto">CheckListDto 实例</param>
        /// <param name="fieldName">extra 数组里对象的字段名</param>
        /// <param name="objIndex">extra 数组里的对象序号（0 基）</param>
        /// <returns>取到就转，取不到就 default</returns>
        public static T? GetExtraField<T>(CheckListDto dto,
                                          string fieldName,
                                          int objIndex = 0)
        {
            if (dto?.Extra == null) return default;

            // 1. 拿到纯 JSON 字符串
            string json = dto.Extra switch
            {
                string s => s,
                JsonElement je => je.GetRawText(),
                _ => dto.Extra.ToString()!
            };

            // 2. 反序列化成“字典列表”
            var dictList = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json);
            if (dictList == null ||
                objIndex < 0 || objIndex >= dictList.Count) return default;

            // 3. 读字段并转型
            dictList[objIndex].TryGetValue(fieldName, out var raw);
            if (raw == null) return default;

            return (T)Convert.ChangeType(raw, typeof(T));
        }
    }
}
