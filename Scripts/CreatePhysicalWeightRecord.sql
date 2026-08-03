-- ============================================================
-- 物理称重记录表 physical_weight_record
-- 对应实体：src/Infrastructure/Data/Persistence/PhysicalWeightRecord.cs
-- 对应映射：src/Infrastructure/Data/Persistence/dbContext.cs  OnModelCreating
-- 数据库：NX-lims（dbContext 连接串）
-- ============================================================
IF OBJECT_ID(N'dbo.physical_weight_record', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.physical_weight_record
    (
        id               uniqueidentifier NOT NULL CONSTRAINT PK_physical_weight_record PRIMARY KEY, -- Id, 主键, ValueGeneratedNever
        record_index     int              NOT NULL, -- RecordIndex 序号
        test_point       nvarchar(50)     NULL,     -- TestPoint 试样测点
        weight           decimal(10,4)    NOT NULL, -- Weight 重量(g)
        area             decimal(10,4)    NOT NULL, -- Area 面积(cm²)
        g_per_sqm        decimal(10,4)    NOT NULL, -- GPerSqm g/m²
        oz_per_sqyd      decimal(10,4)    NOT NULL, -- OzPerSqyd oz/yd²
        env_temperature  decimal(5,2)     NULL,     -- EnvTemperature 环境温度(℃)
        env_humidity     decimal(5,2)     NULL,     -- EnvHumidity 环境湿度(%)
        test_time        datetime         NOT NULL, -- TestTime 测试时间
        report_number    nvarchar(50)     NULL,     -- ReportNumber 关联报告号
        created_at       datetime         NOT NULL  -- CreatedAt 创建时间
    );
END
GO
