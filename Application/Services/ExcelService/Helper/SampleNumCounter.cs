using NX_lims_Softlines_Command_System.Application.DTO;
using System.Drawing;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.Helper
{
    public class SampleNumCounter
    {
        /// <summary>
        /// 计算测点数，得到测点数组
        /// </summary>
        /// <param name="dtoSample">测点字段</param>
        /// <param name="afterWash">洗涤遍数参数</param>
        /// <param name="iron">熨烫</param>
        /// <returns>引入变量后计算得到的测点数组</returns>
        public static string[]? GetSample(
              string? dtoSample,
              string? afterWash,
              string? iron)
        {
            if (dtoSample == null)
            {
                return null;
            }

            var samples = dtoSample.Split(',').Select(s => s.Trim()).ToArray();
            List<string> expandedSamples = new List<string>();

            // 如果 afterWash 和 iron 都为 null，直接返回原始数组
            if (afterWash == null && (iron == null || iron == ""))
            {
                return samples;
            }

            // 如果 afterWash 为 null 但 iron 不为 null，需要扩展数组
            if (afterWash == null && iron != null && iron != "")
            {
                foreach (var sample in samples)
                {
                    expandedSamples.Add(sample);
                    expandedSamples.Add(sample + " " + iron);
                }
                return expandedSamples.ToArray();
            }

            // 处理 afterWash 不为 null 的情况
            var result = new Dictionary<string, List<int>>();
            var parts = afterWash!.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var elements = part.Trim().Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                string point = elements[0];
                var washNumbers = elements.Skip(1).Select(e =>
                {
                    var splitResult = e.Split(' ');
                    if (splitResult.Length > 1)
                    {
                        return int.Parse(splitResult[0]);
                    }
                    return 0; // 如果没有编号，返回 0 或其他默认值
                }).ToList();

                if (result.ContainsKey(point))
                {
                    result[point].AddRange(washNumbers);
                }
                else
                {
                    result[point] = washNumbers;
                }
            }

            // 根据 Wash 的数量扩展数组
            foreach (var sample in samples)
            {
                if (result.ContainsKey(sample))
                {
                    foreach (var washNumber in result[sample])
                    {
                        expandedSamples.Add(sample);
                        if (iron != null && iron != "")
                        {
                            expandedSamples.Add(sample + " " + iron);
                        }
                    }
                }
                else
                {
                    expandedSamples.Add(sample);
                    if (iron != null && iron != "")
                    {
                        expandedSamples.Add(sample + " " + iron);
                    }
                }
            }

            return expandedSamples.ToArray();
        }


        public static int[] ExpandWashNumbers(string[] samples, string afterWash,string? iron)
        {
            var washInfo = new Dictionary<string, List<int>>();
            var parts = afterWash.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var elements = part.Trim().Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (elements.Length == 0) continue;

                string point = elements[0];
                var washNumbers = elements.Skip(1).Select(e =>
                {
                    var splitResult = e.Split(' ');
                    if (splitResult.Length > 0 && int.TryParse(splitResult[0], out int number))
                    {
                        return number;
                    }
                    return 0;
                }).ToList();

                washInfo[point] = washNumbers;
            }

            var expandedWashNumbers = new List<int>();

            // 记录每个测点当前使用的洗涤遍数索引
            var washCounters = new Dictionary<string, int>();

            foreach (var sample in samples)
            {
                string basePoint = sample.Split(' ')[0];

                if (washInfo.ContainsKey(basePoint))
                {
                    // 初始化计数器
                    if (!washCounters.ContainsKey(basePoint))
                    {
                        washCounters[basePoint] = 0;
                    }

                    int currentCounter = washCounters[basePoint];
                    var washNumbers = washInfo[basePoint];

                    // 每个洗涤遍数对应2个测点（原始+熨烫）
                    int itemsPerWash = (iron != null && iron != "") ? 2 : 1;
                    int washIndex = currentCounter / itemsPerWash;

                    if (washIndex < washNumbers.Count)
                    {
                        expandedWashNumbers.Add(washNumbers[washIndex]);
                    }
                    else
                    {
                        expandedWashNumbers.Add(0);
                    }

                    // 增加计数器
                    washCounters[basePoint]++;
                }
                else
                {
                    expandedWashNumbers.Add(0);
                }
            }

            return expandedWashNumbers.ToArray();
        }
    }
}
