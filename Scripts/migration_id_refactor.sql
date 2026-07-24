-- ============================================================================
-- ID 重构 Migration: bigint→uniqueidentifier, uniqueidentifier→nvarchar
-- ⚠️ 执行前务必备份数据库！
-- ============================================================================
-- 影响范围: LabDbContextSec 库的 lab_test_info 表 + NX-lims 库的 check_list 表
-- 日期: 2026-07-24
-- ============================================================================

-- ==================== LabDbContextSec: NX-lims Lab Command Sys ====================

BEGIN TRANSACTION;
GO

-- ============================================================================
-- 1. lab_test_info.id: bigint → uniqueidentifier (PK)
-- ============================================================================

ALTER TABLE lab_test_info ADD id_new UNIQUEIDENTIFIER;
GO
UPDATE lab_test_info SET id_new = NEWID();
GO
ALTER TABLE lab_test_info ALTER COLUMN id_new UNIQUEIDENTIFIER NOT NULL;
GO
ALTER TABLE lab_test_info DROP CONSTRAINT PK_lab_test_info;
GO
ALTER TABLE lab_test_info DROP COLUMN id;
GO
EXEC sp_rename 'lab_test_info.id_new', 'id', 'COLUMN';
GO
ALTER TABLE lab_test_info ADD CONSTRAINT PK_lab_test_info PRIMARY KEY (id);
GO

-- ============================================================================
-- 2. lab_test_info.order_id: uniqueidentifier → nvarchar(50)
-- ============================================================================

ALTER TABLE lab_test_info ADD order_id_new NVARCHAR(50);
GO
UPDATE lab_test_info SET order_id_new = ISNULL(report_number, '');
GO
ALTER TABLE lab_test_info DROP COLUMN order_id;
GO
EXEC sp_rename 'lab_test_info.order_id_new', 'order_id', 'COLUMN';
GO

COMMIT;
GO

-- ==================== dbContext: NX-lims ====================

USE [NX-lims];
GO

BEGIN TRANSACTION;
GO

-- ============================================================================
-- 3. check_list.order_id: uniqueidentifier → nvarchar(50)
-- ============================================================================

ALTER TABLE check_list ADD order_id_new NVARCHAR(50);
GO
UPDATE check_list SET order_id_new = ISNULL(CAST(order_id AS NVARCHAR(50)), '');
GO
ALTER TABLE check_list DROP COLUMN order_id;
GO
EXEC sp_rename 'check_list.order_id_new', 'order_id', 'COLUMN';
GO

COMMIT;
GO

-- ============================================================================
-- ROLLBACK;  -- 失败时执行回滚
-- ============================================================================
