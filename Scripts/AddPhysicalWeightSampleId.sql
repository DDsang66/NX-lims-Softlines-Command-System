-- ============================================================
-- 物理称重记录表 physical_weight_record — 增量脚本
-- 1. 新增 sample_id 列(试样编号, 修复数据丢失)
-- 2. 新增 report_number 索引(支撑按报告号查询)
-- 对应映射：src/Infrastructure/Data/Persistence/dbContext.cs  OnModelCreating
-- 数据库：NX-lims(dbContext 连接串)
-- 执行方式：手动在 SQL Server 对 NX-lims 库执行
-- ============================================================
IF COL_LENGTH(N'dbo.physical_weight_record', N'sample_id') IS NULL
BEGIN
    ALTER TABLE dbo.physical_weight_record
        ADD sample_id nvarchar(50) NULL; -- SampleId 试样编号
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_physical_weight_record_report_number'
                 AND object_id = OBJECT_ID(N'dbo.physical_weight_record'))
BEGIN
    CREATE INDEX IX_physical_weight_record_report_number
        ON dbo.physical_weight_record (report_number);
END
GO
