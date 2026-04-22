using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Domain.Shared.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.IngredientAnalysis.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.IngredientAnalysis.ValueObj;
using NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.IngredientAnalysis
{
    public sealed class IngredientAnalysis : IAggregateRoot
    {
        public long Id { get; private set; } /*AnalysisId*/
        public string ReportNo { get; private set; } = string.Empty;//报告流水号
        public string Buyer { get; private set; } = string.Empty;//买家
        public List<string> Methods { get; private set; } = new();//方法

        private List<FiberComponent> _components = new();
        public IReadOnlyList<FiberComponent> Components => _components.AsReadOnly();
        public RemarkLabel RemarkGroup { get; private set; }
        public AnalysisType Type { get; private set; } // 枚举：单组分/多组分
        public AnalysisResult Result { get; private set; } = AnalysisResult.Empty();//字典映射



        /// <summary>
        /// 实体创建工厂方法，包含领域验证逻辑
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reportNo"></param>
        /// <param name="buyer"></param>
        /// <param name="methods"></param>
        /// <param name="type"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        /// <exception cref="DomainException"></exception>
        public static IngredientAnalysis Create(
                long id,
                string reportNo,
                string buyer,
                List<string> methods,
                AnalysisType type,
                List<FiberComponent> components)
        {
            // 领域验证
            if (id <= 0) throw new ArgumentException("Id is required");
            if (string.IsNullOrWhiteSpace(reportNo)) throw new ArgumentException("RepoNo. is required");
            if (methods == null || !methods.Any()) throw new ArgumentException("Methods are required");

            return new IngredientAnalysis
            {
                Id = id,
                ReportNo = reportNo,
                Buyer = buyer,
                Methods = methods,
                Type = type,
                _components = components ?? new List<FiberComponent>()
            };
        }


        /// <summary>
        /// 计算逻辑
        /// </summary>
        public async Task Calculator() 
        {
            var resultDic = new Dictionary<string, string>();
            resultDic = BasicParamCalculateAsync(this.ReportNo, this.Buyer, this.Methods, resultDic);
            resultDic = await AnalysisCalculateAsync(this._components, this.Type, resultDic);
        }

        /// <summary>
        /// 基础参数逻辑
        /// </summary>
        /// <param name="reportNo"></param>
        /// <param name="buyer"></param>
        /// <param name="methods"></param>
        /// <param name=""></param>
        /// <returns></returns>
        public Dictionary<string, string> BasicParamCalculateAsync(
                string reportNo,
                string buyer,
                List<string> methods,
                Dictionary<string, string> resultDic) 
        {
            resultDic["ReportNumber"] = reportNo;
            resultDic["Buyer"] = buyer;           // 第2个字段
            resultDic["CalculateTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");  // 第3个字段
            resultDic["Methods"] = string.Join(new string(' ', 4), methods); // 第4个字段，方法列表以空格分隔
            return resultDic;
        }


        /// <summary>
        /// 计算纤维成分结果
        /// </summary>
        public async Task<Dictionary<string, string>> AnalysisCalculateAsync(List<FiberComponent> components, AnalysisType type,Dictionary<string, string> resultDic)
        {
           if (_components == null) throw new ArgumentNullException(nameof(components));

            // 模拟计算过程

            if (type == AnalysisType.Single) 
            {

            }
            else if (type == AnalysisType.Multiple) 
            {
                //拆分个数
                int splittingCount = components.OfType<SplittingFiberComponent>().Count();
            }
            return resultDic;
        }


        /// <summary>
        /// 生成Word逻辑
        /// </summary>
        public void WorkSheetGenerator(string filePath)
        {
            //验证Result完整性、合规性
            var dataDic = this.Result.Data;

            //将计算完成的Result字典投入WorkSheetTemplate类中进行渲染
        }

        /// <summary>
        /// 结果生成器
        /// </summary>
        /// <returns></returns>
        public AnalysisResult AnalysisResultCalculator(List<FiberComponent> components, RemarkLabel remark) 
        {


            return new AnalysisResult();
        }

        /// <summary>
        /// 领域规则验证
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateComponents()
        {
            if (!_components.Any())
                throw new ArgumentException("至少需要一个成分");

            // 其他业务规则...
        }

    }
}