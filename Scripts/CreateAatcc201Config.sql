-- ============================================================
-- AATCC 201 校准参数表 aatcc201_config
-- 对应实体：src/Infrastructure/Data/Persistence/Aatcc201Config.cs
-- 对应映射：src/Infrastructure/Data/Persistence/dbContext.cs  OnModelCreating
-- 数据库：NX-lims（dbContext 连接串）
-- 说明：AATCC 201 加热板干燥速率测试仪的唯一持久化对象（校准参数，单行配置）。
--       测试记录不落结构化库，报告以文件存 wwwroot/DocxModel/SaveDocx/DryingRate/。
-- 种子数据：从旧软件 mdb 导入现场当前值（决策9）
--   (201config: temp_hw1/2, temp_board1/2, wind1/2, slope_point, flat_point,
--    slope_dg_no, slope_continue_no, slope_continue_temp)
--   (201testerconfig: set_temp, temp_board1_x, temp_board2_x, p, i, d)
-- ============================================================
IF OBJECT_ID(N'dbo.aatcc201_config', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.aatcc201_config
    (
        id                  uniqueidentifier NOT NULL CONSTRAINT PK_aatcc201_config PRIMARY KEY, -- Id, 主键, ValueGeneratedNever
        machine_no          nvarchar(50)     NOT NULL, -- MachineNo 设备编号
        temp_hw1            decimal(10,4)    NOT NULL, -- TempHw1 表面温度1偏置(℃)
        temp_hw2            decimal(10,4)    NOT NULL, -- TempHw2 表面温度2偏置(℃)
        temp_board1         decimal(10,4)    NOT NULL, -- TempBoard1 加热板温度1偏置(℃)
        temp_board2         decimal(10,4)    NOT NULL, -- TempBoard2 加热板温度2偏置(℃)
        wind1               decimal(10,4)    NOT NULL, -- Wind1 风速1偏置(m/s)
        wind2               decimal(10,4)    NOT NULL, -- Wind2 风速2偏置(m/s)
        slope_point         int              NOT NULL, -- SlopePoint 斜坡段点数
        flat_point          int              NOT NULL, -- FlatPoint 平缓段点数
        slope_dg_no         int              NOT NULL, -- SlopeDgNo 斜坡判定序号
        slope_continue_no   int              NOT NULL, -- SlopeContinueNo 斜坡持续点数
        slope_continue_temp decimal(10,4)    NOT NULL, -- SlopeContinueTemp 斜坡持续温差(℃)
        set_temp            decimal(10,4)    NOT NULL, -- SetTemp 设定温度(℃)
        temp_board1_x       decimal(10,4)    NOT NULL, -- TempBoard1X 加热板1修正(℃)
        temp_board2_x       decimal(10,4)    NOT NULL, -- TempBoard2X 加热板2修正(℃)
        p                   decimal(10,4)    NOT NULL, -- P 比例系数
        i                   decimal(10,4)    NOT NULL, -- I 积分系数
        d                   decimal(10,4)    NOT NULL, -- D 微分系数
        updated_at          datetime         NOT NULL, -- UpdatedAt 更新时间
        updated_by          nvarchar(50)     NULL      -- UpdatedBy 更新人
    );
END
GO

-- 种子数据：旧 mdb 现场当前值（幂等，已有数据则跳过）
IF NOT EXISTS (SELECT 1 FROM dbo.aatcc201_config)
BEGIN
    INSERT INTO dbo.aatcc201_config
        (id, machine_no, temp_hw1, temp_hw2, temp_board1, temp_board2, wind1, wind2,
         slope_point, flat_point, slope_dg_no, slope_continue_no, slope_continue_temp,
         set_temp, temp_board1_x, temp_board2_x, p, i, d, updated_at, updated_by)
    VALUES
        (NEWID(), N'AATCC201-01',
         0, 0, 0, 0, 5, -52,
         20, 25, 160, 80, 3,
         37.0, -0.5, 0, 50, 40, 0,
         GETDATE(), N'system');
END
GO

-- 存量行迁移：旧种子 set_temp=370 / temp_board1_x=-5 是设备 0.1℃ 单位，
-- 而本表列语义为 ℃（前端保存/回读都按 ℃），一次性换算成 ℃。
-- 守卫 set_temp > 100：℃ 语义的设定温度（加热板干燥 30~80℃）不可能 >100，只命中旧设备单位种子。
IF EXISTS (SELECT 1 FROM dbo.aatcc201_config WHERE set_temp > 100)
BEGIN
    UPDATE dbo.aatcc201_config
       SET set_temp      = set_temp / 10.0,
           temp_board1_x = temp_board1_x / 10.0,
           temp_board2_x = temp_board2_x / 10.0
     WHERE set_temp > 100;
END
GO
