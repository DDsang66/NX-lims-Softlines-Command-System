using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Domain;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Domain.Model.Interface;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;

namespace NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.BuyerRepos
{

    //与数据库交互
    public class PrimarkRepository
    {
        private readonly LabDbContextSec _db;
        private readonly FiberContentHelper _helper;
        public PrimarkRepository(LabDbContextSec db, FiberContentHelper helper)
        {
            _db = db;
            _helper = helper;
        }


        /// <summary>
        /// 获取基础CheckList
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<List<CheckListDto>?> GetCheckListAsync(dynamic input)
        {
            try
            {
                string menuName = input;
                var Menu = await _db.PrimarkMenus.Where(m => m.BuyerTable!.Contains(menuName)).ToListAsync();
                if (Menu == null) return null;

                var checkLists = new List<CheckListDto>();
                foreach (var m in Menu)
                {
                    try
                    {
                        if (m.StandardName != null)
                        {
                            checkLists.Add(new CheckListDto
                            {
                                MenuName = menuName,
                                ItemName = m.ItemName,
                                Standard = m.StandardName,
                                Type = m.Type,
                                Parameter = null
                            });

                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing standard {m.StandardName}: {ex.Message}");
                    }
                }
                checkLists = checkLists.OrderBy(cl => cl.Standard).ToList();

                return checkLists;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($" {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 新建WetParams
        /// </summary>
        /// <param name="input"></param>
        /// <returns> WetParameterIso</returns>
        public async Task<WetParameterIso> CreateWetParamAsync(ParamsInput input) 
        {
            var newParam = new WetParameterIso//构造一个基础对象
            {
                Standard = input.Standard,
                Sensitive = "N",
                ReportNumber = input.OrderNumber!,
                ContactItem = input.ItemName
            };
            await _db.WetParameterIsos.AddAsync(newParam);
            await _db.SaveChangesAsync();
            return newParam!;
        }


        /// <summary>
        /// 获取WetParams
        /// </summary>
        /// <param name="reportNum"></param>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public async Task<WetParameterIso> GetWetParamAsync(string reportNum, string itemName,string sample)
        {
            var Param = await _db.WetParameterIsos
                  .FirstOrDefaultAsync(p => p.ContactItem == itemName && p.ReportNumber == reportNum && p.ContactSample == sample);
            return Param!;
        }


        /// <summary>
        /// 更新WetParam
        /// </summary>
        /// <param name="newParam"></param>
        /// <param name="exitParam"></param>
        public async void UpdateWetParamAsync(WetParameterIso newParam, WetParameterIso exitParam) 
        {
            foreach (var prop in typeof(WetParameterIso).GetProperties())
            {
                if (prop.CanWrite && prop.Name != "ParamId") // 跳过主键字段
                {
                    var value = prop.GetValue(newParam);
                    if (value != null)
                    {
                        prop.SetValue(exitParam, value);
                    }
                }
            }

            await _db.WetParameterIsos.AddAsync(exitParam);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// 新建NormalParams
        /// </summary>
        /// <param name="input"></param>
        /// <returns> WetParameterIso</returns>
        public async Task<NormalParameter> CreateNormalParamAsync(string reprortNum, string itemName, string sample)
        {
            var snowflake = new SnowflakeIdGenerator();
            long snowId = snowflake.NextId();
            var newParam = new NormalParameter//构造一个基础对象
            {
                ParamId = snowId.ToString(),
                ReportNumber = reprortNum,
                ContactItem = itemName,
                ContactSample = sample
            };
            await _db.NormalParameters.AddAsync(newParam);
            await _db.SaveChangesAsync();
            return newParam!;
        }


        /// <summary>
        /// 获取NormaltParam
        /// </summary>
        /// <param name="reportNum"></param>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public async Task<NormalParameter> GetNormalParamAsync(string reportNum, string itemName, string sample)
        {
            var Param = await _db.NormalParameters
                  .FirstOrDefaultAsync(p => p.ContactItem == itemName && p.ReportNumber == reportNum && p.ContactSample == sample);
            return Param!;
        }

        /// <summary>
        /// 更新WetParam
        /// </summary>
        /// <param name="newParam"></param>
        /// <param name="exitParam"></param>
        public async void UpdateNormalParamAsync(string newParam, NormalParameter exitParam)
        {
            exitParam.ExtraParam = newParam;
            await _db.NormalParameters.AddAsync(exitParam);
            await _db.SaveChangesAsync();
        }


        /// <summary>
        /// 根据样品代码、报告编号和购买者信息获取单个样品信息
        /// </summary>
        /// <param name="sampleName">样品代码</param>
        /// <param name="reportNumber">报告编号</param>
        /// <param name="buyer">购买者联系方式</param>
        /// <returns>返回找到的SampleInfo对象，如果未找到则返回null</returns>
        /// <exception cref="ArgumentException">当输入参数为空或null时抛出</exception>
        public async Task<SampleInfo> GetSampleByNameAsync(string sampleName, string reportNumber, string buyer)
        {
            // 参数验证
            if (string.IsNullOrWhiteSpace(sampleName))
                throw new ArgumentException("样品代码不能为空", nameof(sampleName));
            if (string.IsNullOrWhiteSpace(reportNumber))
                throw new ArgumentException("报告编号不能为空", nameof(reportNumber));
            if (string.IsNullOrWhiteSpace(buyer))
                throw new ArgumentException("购买者信息不能为空", nameof(buyer));

            try
            {
                var sampleInfo = await _db.SampleInfos
                    .FirstOrDefaultAsync(s => s.SampleCode == sampleName &&
                                            s.ReportNumber == reportNumber &&
                                            s.ContactBuyer == buyer);

                return sampleInfo;
            }
            catch (Exception ex)
            {
                throw; // 可以选择重新抛出或返回null，取决于业务需求
            }
        }

        /// <summary>
        /// 根据样品代码、报告编号和购买者信息获取单个样品信息
        /// </summary>
        /// <param name="sampleDescObject"></param>
        /// <param name="reportNum"></param>
        /// <param name="buyer"></param>
        /// <returns></returns>
        public async Task<List<SampleInfoDescription>> GetSampleInfoDescription(SampleInfo sampleInfo, string reportNum, string buyer) 
        {
            var sampleDescObj = await _db.SampleInfoDescriptions
                .Where(s => s.SampleId == sampleInfo.IdSample)
                .ToListAsync();
            return sampleDescObj;
        }

        /// <summary>
        /// 保存SampleInfo
        /// </summary>
        /// <param name="sampleDescObject"></param>
        /// <param name="reportNum"></param>
        /// <param name="buyer"></param>
        public async Task SaveSampleInfo(SampleDescObject sampleDescObject, string reportNum, string buyer)
        {
            var snowflake = new SnowflakeIdGenerator();
            long snowId = snowflake.NextId();
            var sampleInfo = new SampleInfo
            {
                IdSample = snowId.ToString(),
                DescriptionId = snowId.ToString(),
                SampleCode = sampleDescObject.sample!,
                ContactBuyer = buyer,
                ReportNumber = reportNum
            };
            _db.SampleInfos.Add(sampleInfo);
            foreach (var item in sampleDescObject.description!)
            {
                snowId = snowflake.NextId();
                if (item.propertyName != null && item.value != null) 
                {
                    var desc = new SampleInfoDescription
                    {
                        IdDescription = snowId.ToString(),
                        SampleId = sampleInfo.IdSample,
                        PropertyName = item.propertyName,
                        PropertyValue = item.value,
                    };
                    _db.SampleInfoDescriptions.Add(desc);
                }

               await _db.SaveChangesAsync();
            }
        }
    }
}
