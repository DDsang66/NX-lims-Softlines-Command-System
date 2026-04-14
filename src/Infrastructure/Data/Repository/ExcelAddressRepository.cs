using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class ExcelAddressRepository : IExcelAddressRepository,IScopedDependency
    {
        private readonly LabDbContextSec _db;

        public ExcelAddressRepository(LabDbContextSec db)
        {
            _db = db;
        }

        public async Task<string?> GetFilePathAsync(string repoNum, string buyer, string group, CancellationToken ct)
        {
            // 1. 构建 SQL LIKE 模式
            // 数据库存储格式假设为: RepoNum_Buyer_Group_TimeStamp_Name_sheet.xlsx
            // 我们需要匹配以 "RepoNum_Buyer_Group_" 开头，以 "_sheet.xlsx" 结尾的记录
            // SQL LIKE 语法: % 代表任意多个字符

            // 正确的 Pattern 构造
            string pattern = $"{repoNum}_{buyer}_{group.ToUpper()}_%_%.xlsx";


            // 2. 执行查询
            // EF Core 会将此 SQL 翻译为: WHERE Address LIKE '87.405.26..01_Buyer_PHY_%_sheet.xlsx'
            var filePath = await _db.ExcelAddresses
                .Where(e => e.Status == "Active" && e.Address.EndsWith(".xlsx")) // 简单过滤
                .Where(e => EF.Functions.Like(e.Address, pattern)) // 核心模糊匹配
                .OrderByDescending(e => e.IdExcelAddress) // 如果有多个匹配，取最新的（假设 Id 是自增的）
                .Select(e => e.Address)
                .FirstOrDefaultAsync(ct);

            return filePath;
        }
    }
}
