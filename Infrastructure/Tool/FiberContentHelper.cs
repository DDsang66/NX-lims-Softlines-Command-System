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

        public  FiberContentHelper(LabDbContextSec db)
        {
            _db = db;
        }
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
        public bool? IsCompositionExist(string Type,List<FiberDto> Composition) 
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

    }
}
