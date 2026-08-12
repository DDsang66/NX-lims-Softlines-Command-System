-- 如果表已存在则先删除 (可选，请根据实际情况谨慎操作)
 DROP TABLE IF EXISTS dbo.template;

CREATE TABLE dbo.template
(
    -- 主键，varchar(25)，非空
    template_id      VARCHAR(75)    NOT NULL,

    -- 模板名称，对应 C# string (默认 string.Empty)
    template_name    NVARCHAR(255)  NOT NULL,

    -- 模板的url，对应 C# string (默认 string.Empty)
    -- 使用 NVARCHAR 以支持可能包含中文或特殊字符的 URL 路径
    template_url     NVARCHAR(1024) NOT NULL,

    -- 地区站点，对应 C# Site 枚举 (默认 Site.NB)
    -- 使用 TINYINT 存储枚举值，并添加注释说明含义
    site             TINYINT        NOT NULL, -- 0: NB (请根据实际枚举整数值调整注释)

    -- 当前datasheet状态，对应 C# Status 枚举 (默认 Status.Draft)
    -- 使用 TINYINT 存储枚举值
    status           TINYINT        NOT NULL, -- 0: Draft, 1: Published (请根据实际枚举整数值调整)

    -- 当前模板版本，对应 C# int (默认 1)
    version          INT            NOT NULL,

    -- 变更时间，对应 C# DateTime (默认 DateTime.Now)
    -- DATETIME2 是 SQL Server 推荐的高精度时间类型
    update_at        DATETIME2      NOT NULL,

    -- 新增：模板文件类型，对应 C# TemplateFileType 枚举 (默认 Docx)
    -- 使用 TINYINT 存储枚举值
    file_type        TINYINT        NOT NULL, -- 0: Docx, 1: Excel (请根据实际枚举整数值调整)

    -- 新增：业务子分类文件夹名称，对应 C# string (如 Common_FLAM, Common_PHY 等)
    business_category NVARCHAR(128) NOT NULL,

    -- 主键约束
    CONSTRAINT PK_template PRIMARY KEY (template_id)
);

-- 为表和字段添加详细的中文说明 (SQL Server 扩展属性，方便数据库文档化)
EXEC sp_addextendedproperty 
    @name = N'MS_Description', @value = N'模板聚合根表', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'template';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', @value = N'模板唯一标识', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'template', 
    @level2type = N'COLUMN', @level2name = N'template_id';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', @value = N'模板名称', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'template', 
    @level2type = N'COLUMN', @level2name = N'template_name';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', @value = N'模板的URL地址', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'template', 
    @level2type = N'COLUMN', @level2name = N'template_url';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', @value = N'地区站点枚举 (例如: 0=NB)', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'template', 
    @level2type = N'COLUMN', @level2name = N'site';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', @value = N'当前datasheet状态枚举 (例如: 0=Draft, 1=Published)', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'template', 
    @level2type = N'COLUMN', @level2name = N'status';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', @value = N'当前模板版本', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'template', 
    @level2type = N'COLUMN', @level2name = N'version';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', @value = N'变更时间', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'template', 
    @level2type = N'COLUMN', @level2name = N'update_at';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', @value = N'模板文件类型枚举 (0=Docx, 1=Excel)', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'template', 
    @level2type = N'COLUMN', @level2name = N'file_type';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', @value = N'业务子分类文件夹名称 (如 Common_FLAM, Common_PHY 等)', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'template', 
    @level2type = N'COLUMN', @level2name = N'business_category';
