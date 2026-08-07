-- ============================================================
-- 物理称重记录表 physical_weight_record — 增量脚本(新增测试类型扩展列)
-- 支持三种物理测试: 面积克重(area) / 长度克重(length) / 条重(piece)
-- 新增列: test_type, length_cm, piece_count, g_per_m, oz_per_yd, g_per_piece, lb_per_dozen
-- 对应映射：src/Infrastructure/Data/Persistence/dbContext.cs  OnModelCreating
-- 数据库：NX-lims(dbContext 连接串)
-- 执行方式：手动在 SQL Server 对 NX-lims 库执行
-- ============================================================
IF COL_LENGTH(N'dbo.physical_weight_record', N'test_type') IS NULL
BEGIN
    ALTER TABLE dbo.physical_weight_record ADD test_type nvarchar(20) NULL; -- TestType 测试类型
END
GO

IF COL_LENGTH(N'dbo.physical_weight_record', N'length_cm') IS NULL
BEGIN
    ALTER TABLE dbo.physical_weight_record ADD length_cm decimal(10,4) NULL; -- LengthCm 试样长度(cm)
END
GO

IF COL_LENGTH(N'dbo.physical_weight_record', N'piece_count') IS NULL
BEGIN
    ALTER TABLE dbo.physical_weight_record ADD piece_count int NULL; -- PieceCount 条数
END
GO

IF COL_LENGTH(N'dbo.physical_weight_record', N'g_per_m') IS NULL
BEGIN
    ALTER TABLE dbo.physical_weight_record ADD g_per_m decimal(10,4) NOT NULL DEFAULT 0; -- GPerM 长度克重 g/m
END
GO

IF COL_LENGTH(N'dbo.physical_weight_record', N'oz_per_yd') IS NULL
BEGIN
    ALTER TABLE dbo.physical_weight_record ADD oz_per_yd decimal(10,4) NOT NULL DEFAULT 0; -- OzPerYd 长度克重 oz/yd
END
GO

IF COL_LENGTH(N'dbo.physical_weight_record', N'g_per_piece') IS NULL
BEGIN
    ALTER TABLE dbo.physical_weight_record ADD g_per_piece decimal(10,4) NOT NULL DEFAULT 0; -- GPerPiece 条重 g/piece
END
GO

IF COL_LENGTH(N'dbo.physical_weight_record', N'lb_per_dozen') IS NULL
BEGIN
    ALTER TABLE dbo.physical_weight_record ADD lb_per_dozen decimal(10,4) NOT NULL DEFAULT 0; -- LbPerDozen 条重 lb/dozen
END
GO
