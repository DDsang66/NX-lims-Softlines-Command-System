using DocumentFormat.OpenXml.Bibliography;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model;
using System.Text.Json.Nodes;

namespace NX_lims_Softlines_Command_System.Infrastructure.Tool
{
    public class FiberContentHelper
    {
        private readonly LabDbContextSec _db;

        public FiberContentHelper(LabDbContextSec db)
        {
            _db = db;
        }

        #region old

        /// <summary>
        /// 最大成分的名称
        /// </summary>
        public string? MaxComposition(List<FiberDto> composition)
        {
            if (composition == null || composition.Count == 0)
                return null;

            var maxFiber = composition
                .OrderByDescending(f => f.Rate)
                .FirstOrDefault();
            var key = char.ToUpper(maxFiber!.Composition![0]) + maxFiber.Composition.Substring(1).ToLower();
            return key;
        }

        /// <summary>
        /// 给定成分名称的 rate
        /// </summary>
        public double? CompositionRate(List<FiberDto> composition,string compositionSelect)
        {
            if (composition == null || composition.Count == 0 || compositionSelect==null)
                return null;

            var totalRate = composition
                .Where(j => compositionSelect.Contains(char.ToUpper(j!.Composition![0]) + j.Composition.Substring(1).ToLower()))
                .Sum(j => j.Rate);

            return totalRate;
        }



        /// <summary>
        /// 返回 rate 总和最大的那一类 FiberSource
        /// </summary>
        public async Task<string?> MaxCompositionType(List<FiberDto> composition)
        {
            if (composition == null || composition.Count == 0)
                return null;

            // 一次性取出 FiberName -> FiberSource 的映射
            var nameToSource = await _db.Compositions
                .Where(c => c.FiberName != null)
                .ToDictionaryAsync(c => c.FiberName!, c => c.FiberSource);

            // 按 FiberSource 汇总 rate,对composition中的Composition属性的值进行了格式处理
            var rateBySource = composition
                .Where(f => f.Composition != null)
                .GroupBy(f => nameToSource.GetValueOrDefault(char.ToUpper(f!.Composition![0]) + f.Composition.Substring(1).ToLower()),
                         f => f.Rate)
                .ToDictionary(g => g.Key!, g => g.Sum());

            // 找出总和最大的 FiberSource
            return rateBySource
                .OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .FirstOrDefault();
        }

        /// <summary>
        /// 找出Type对应的成分是否存在
        /// </summary>
        public bool IsCompositionExist(string Type, List<FiberDto> Composition)
        {
            bool isExist = false;
            foreach (var item in Composition)
            {
                var key = char.ToUpper(item.Composition![0]) +
                      item.Composition.Substring(1).ToLower();
                var fiber = _db.Compositions.FirstOrDefault(f => f.FiberName == key);
                string? type = fiber?.FiberSource;
                if (type == Type) { isExist = true; }
            }
            return isExist;
        }

        /// <summary>
        /// 找出Description对应的成分是否存在
        /// </summary>
        public bool IsCompositionDescExist(string Desc, List<FiberDto> Composition)
        {
            bool isExist = false;
            foreach (var item in Composition)
            {
                var key = char.ToUpper(item.Composition![0]) +
                      item.Composition.Substring(1).ToLower();
                var fiber = _db.Compositions.FirstOrDefault(f => f.FiberName == key);
                string? description = fiber?.FiberSource;
                if (description == Desc) { isExist = true; }
            }
            return isExist;
        }



        /// <summary>
        /// Type对应的成分的总和
        /// </summary>
        public double? IsCompositionTypeExist(string type, List<FiberDto> composition)
        {
            if (composition == null) return null;

            // 从数据库中查出该 type 对应的所有 FiberName
            var fiberNames = _db.Compositions
                .Where(c => c.FiberType == type)
                .Select(c => c.FiberName)
                .ToHashSet(); // 用于快速查找

            if (!fiberNames.Any()) return null;

            // 从 composition 中找出 FiberName 在 fiberNames 中的项，并累加 rate
            var totalRate = composition
                .Where(j => fiberNames.Contains(char.ToUpper(j!.Composition![0]) + j.Composition.Substring(1).ToLower()))
                .Sum(j => j.Rate);

            return totalRate;
        }


        /// <summary>
        /// Source对应的成分的总和
        /// </summary>
        public double? IsCompositionSourceExist(string source, List<FiberDto> composition)
        {
            if (composition == null) return null;

            // 从数据库中查出该 source 对应的所有 FiberName
            var fiberNames = _db.Compositions
                .Where(c => c.FiberSource == source)
                .Select(c => c.FiberName)
                .ToHashSet(); // 用于快速查找

            if (!fiberNames.Any()) return null;

            // 从 composition 中找出 FiberName 在 fiberNames 中的项，并累加 rate
            var totalRate = composition
                .Where(j => fiberNames.Contains(char.ToUpper(j!.Composition![0]) + j.Composition.Substring(1).ToLower()))
                .Sum(j => j.Rate);

            return totalRate;
        }


        /// <summary>
        /// 排序后的成分顺序
        /// </summary>
        /// <param name="composition"></param>
        /// <param name="Sample"></param>
        /// <returns></returns>
        public List<string> SortedComposition(List<FiberDto> composition)
        {
            if (composition == null || composition.Count == 0)
                return new List<string>();

            static string Normalize(string s)
            {
                s = s?.Trim() ?? string.Empty;
                if (s.Length == 0) return s;
                return char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
            }

            var sorted = composition
                .Where(f => !string.IsNullOrWhiteSpace(f?.Composition))
                .GroupBy(f => Normalize(f.Composition!))
                .Select(g => new { Key = g.Key, TotalRate = g.Sum(f => f.Rate) })
                .OrderByDescending(x => x.TotalRate)
                .ThenBy(x => x.Key, StringComparer.InvariantCulture) // 可选：总和相同时稳定排序
                .Select(x => x.Key)
                .ToList();

            return sorted;
        }

        #endregion


        #region new
        /// <summary>
        ///选择的样品，最大成分的名称
        /// </summary>
        public string? MaxCompositionNew(List<FiberInfoNew> composition, string? Sample)
        {
            if (composition == null || composition.Count == 0||string.IsNullOrWhiteSpace(Sample))
                return null;

            var SelectedSample = composition
                .OrderByDescending(f => f.Sample)
                .FirstOrDefault();
            if (SelectedSample == null) return null;
            var MaxComposition = SelectedSample.Composition!.OrderByDescending(f => f.Rate).FirstOrDefault();
            var key = char.ToUpper(MaxComposition!.Composition![0]) + MaxComposition.Composition.Substring(1).ToLower();
            return key;
        }
        #endregion

    }
}
