-- =============================================
-- 纤维分析模块数据库初始化脚本 (更新版)
-- 包含多标准回潮率和定性特征
-- =============================================

-- 1. 删除并重建纤维数据库表（如果需要）
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'fiber_database')
BEGIN
    DROP TABLE fiber_database;
END
GO

-- 创建纤维数据库表
CREATE TABLE fiber_database (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    fiber_name_en NVARCHAR(100) NOT NULL,
    fiber_name_cn NVARCHAR(100),
    category NVARCHAR(50),
    -- 各标准公定回潮率
    moisture_regain_iso DECIMAL(5,2),    -- ISO/EN
    moisture_regain_aatcc DECIMAL(5,2),  -- AATCC
    moisture_regain_can DECIMAL(5,2),    -- CAN/CGSB
    moisture_regain_kor DECIMAL(5,2),    -- KOR
    moisture_regain_gb DECIMAL(5,2),     -- GB
    moisture_regain_cns DECIMAL(5,2),    -- CNS
    moisture_regain_jis DECIMAL(5,2),    -- JIS
    -- 定性特征描述
    qualitative_description NVARCHAR(500),
    -- 状态
    is_active BIT DEFAULT 1,
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME
);
PRINT 'Table fiber_database created successfully';
GO

-- 插入完整纤维数据
INSERT INTO fiber_database (fiber_name_en, fiber_name_cn, category,
    moisture_regain_iso, moisture_regain_aatcc, moisture_regain_can, moisture_regain_kor,
    moisture_regain_gb, moisture_regain_cns, moisture_regain_jis, qualitative_description) VALUES
('Wool', '绵羊毛', 'Natural', 18.25, 13.6, 13.6, 18.25, 15.0, 18.25, 15.0, '表面粗糙，有鳞片，横截面为圆形或近似圆形，溶于次氯酸钠。'),
('Polyamide', '锦纶', 'Synthetic', 5.75, 4.5, 4.5, NULL, 4.5, 6.5, 4.5, '表面光滑，有小黑点，横截面为圆形或近似圆形及各种异形截面，溶于硝酸。'),
('Nylon', '锦纶', 'Synthetic', 5.75, 4.5, 4.5, 4.5, 4.5, 6.5, 4.5, '表面光滑，有小黑点，横截面为圆形或近似圆形及各种异形截面，溶于硝酸。'),
('Acrylic', '腈纶', 'Synthetic', 2.00, 1.5, 1.5, 2.0, 2.0, 2.0, 2.0, '表面光滑，有沟槽或条纹，横截面为圆形、哑铃型或叶状，溶于硝酸。'),
('Viscose', '粘纤', 'Regenerated', 13.00, 11.0, 11.0, NULL, 13.0, 13.0, 11.0, '表面平滑，有清晰条纹，横截面为锯齿形，溶于59.5%硫酸。'),
('Rayon', '粘纤', 'Regenerated', 13.00, 11.0, 11.0, 13.0, 13.0, 13.0, 11.0, '表面平滑，有清晰条纹，横截面为锯齿形，溶于59.5%硫酸。'),
('Cotton', '棉', 'Natural', 8.50, 8.0, 8.0, 8.5, 8.5, 8.5, 8.5, '扁平带状，有天然转曲，有中腔，横截面近似圆形或不规则的腰圆形，溶于70%硫酸。'),
('Polyester', '聚酯纤维', 'Synthetic', 1.50, 0.4, 0.4, 0.4, 0.4, 1.5, 0.4, '表面平滑，有的有小黑点，横截面为圆形或近似圆形及各种异形截面，溶于98%硫酸。'),
('Cashmere', '山羊绒', 'Natural', 18.25, 13.6, 13.6, NULL, 15.0, 18.25, 15.0, '表面光滑，鳞片较薄且包覆较完整，鳞片间距较大，横截面为圆形或近似圆形，溶于次氯酸钠。'),
('Rabbit hair', '兔毛', 'Natural', 18.25, 13.6, 13.6, NULL, 15.0, 18.25, 15.0, '鳞片较小与纤维纵向呈倾斜状，髓腔有单列、双列、多列，横截面为圆形或近似圆形，溶于次氯酸钠。'),
('Mohair', '马海毛', 'Natural', 18.25, 13.6, 13.6, NULL, 14.0, 18.25, 15.0, '鳞片较大有光泽，直径较粗，有的有斑痕，横截面为圆形或近似圆形，有的有髓腔，溶于次氯酸钠。'),
('Alpaca', '羊驼毛', 'Natural', 18.25, 13.6, 13.6, NULL, 15.0, 18.25, 15.0, '鳞片有光泽，有的有通体或间断髓腔，横截面为圆形或近似圆形，溶于次氯酸钠。'),
('Acetate', '醋纤', 'Regenerated', 9.00, 6.5, 6.5, 6.5, 7.0, 9.0, 6.5, '表面光滑有沟槽，横截面为三叶形或不规则锯齿形，溶于硝酸。'),
('vegetable fibres', '植物纤维', 'Natural', 0, 0, 0, NULL, 0, NULL, NULL, '表面光滑有沟槽，横截面为三叶形或不规则锯齿形，溶于硝酸。'),
('Cupro', '铜氨纤维', 'Regenerated', 13.00, 11.0, 11.0, 13.0, 13.0, 13.0, 11.0, '表面平滑，有光泽，横截面为圆形或近似圆形，溶于次氯酸钠。'),
('Hemp', '大麻', 'Natural', 12.00, 12.0, 12.0, 12.0, 12.0, 12.0, 12.0, '纤维直径及形态差异较大，横节不明显，横截面为多边形、扁圆形、腰圆形等，有中腔，溶于70%硫酸。'),
('Jute', '黄麻', 'Natural', 17.00, 13.75, 13.75, 13.75, 14.0, 17.0, 13.75, '有长形条纹，横节不明显，横截面为多边形，有中腔，溶于70%硫酸。'),
('Metal fibre', '金属纤维', 'Synthetic', 2.00, 0.0, 0.0, 0.0, 0.0, NULL, NULL, '边线不直，黑色长杆状，横截面为不规则的长方形或圆形。'),
('Metallized fibres', '金属镀膜纤维', 'Synthetic', 2.00, 0.0, 0.0, NULL, 0.0, NULL, 0.0, '在纤维上涂覆金属的纤维'),
('Metallic', '金属镀膜纤维', 'Synthetic', 0.00, 0.0, 0.0, NULL, 0.0, 2.0, 0.0, '在纤维上涂覆金属的纤维'),
('Metallised fibre', '金属镀膜纤维', 'Synthetic', 2.00, 0.0, NULL, NULL, 0.0, NULL, 0.0, '在纤维上涂覆金属的纤维'),
('Modacrylic', '改性腈纶', 'Synthetic', 2.00, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, '表面有条纹，横截面为不规则的哑铃形，蚕茧型，土豆形等，溶于DMF。'),
('Polyethylene', '乙纶', 'Synthetic', 1.50, 0.0, 0.0, 0.0, 0.0, 1.5, 0.0, '表面平滑，有的带有疤痕，横截面为圆形或近似圆形，不溶于98%硫酸。'),
('Polypropylene', '丙纶', 'Synthetic', 2.00, 0.0, 0.0, 0.0, 0.0, 2.0, 0.0, '表面平滑，有的带有疤痕，横截面为圆形或近似圆形，不溶于98%硫酸。'),
('Animal', '特种动物纤维', 'Natural', 18.25, 13.6, 13.6, NULL, 15.0, 18.25, 15.0, NULL),
('cellulosic fibre', '纤维素纤维', 'Natural', 10.00, 10.0, 10.0, NULL, 10.0, 10.0, 10.0, '天然的纤维素纤维，包括棉、麻等，溶于70%硫酸。'),
('Regenerated cellulose fibre', '再生纤维素纤维', 'Regenerated', 13.00, 11.0, 11.0, NULL, 13.0, 13.0, 11.0, '粘纤、铜氨纤维、莫代尔、莱赛尔的总称，溶于59.5%硫酸。'),
('Olefin', '丙纶', 'Synthetic', 0.00, 0, 0, NULL, 0, NULL, NULL, '表面平滑，有的带有疤痕，横截面为圆形或近似圆形，不溶于98%硫酸。'),
('Paper Yarn', '纤维素材料', 'Natural', 13.75, 13.75, 13.75, NULL, 13.0, 13.0, 11.0, '天然植物'),
('Elastomultiester', '聚酯复合弹性纤维', 'Synthetic', 1.50, 0.4, 0.4, NULL, 0.4, 1.5, 0.4, '纤维多次拉伸到50%后松弛，能快速回复到原长。'),
('Tussah', '柞蚕丝', 'Natural', 11.00, 11.0, 11.0, NULL, 11.0, 11.0, 12.0, '扁平带状，有细微条纹，横截面为细长三角形，溶于次氯酸钠。'),
('Rubber', '二烯类弹性纤维', 'Synthetic', 1.00, 0.0, 0.0, NULL, 0.0, 0.0, 0.0, '纤维被拉伸至原长的三倍后再去除张力时，可迅速地基本回复到原长。'),
('Elastodiene', '二烯类弹性纤维', 'Synthetic', 1.00, 0.0, NULL, NULL, 0.0, NULL, NULL, '纤维被拉伸至原长的三倍后再去除张力时，可迅速地基本回复到原长。'),
('Silk', '桑蚕丝', 'Natural', 11.00, 11.0, 11.0, 11.0, 11.0, 11.0, 12.0, '有光泽，纤维直径和形态有差异，横截面为三角形或多边形，角是圆的，溶于次氯酸钠。'),
('Polyurethane', '聚氨酯', 'Synthetic', NULL, NULL, NULL, 1.0, NULL, NULL, 1.0, NULL),
('Ramie', '苎麻', 'Natural', 8.50, 7.8, 7.8, 12.0, 12.0, 12.0, 12.0, '纤维较粗，有长形条纹及竹状横节，横截面为腰圆形，有中腔，溶于70%硫酸。'),
('Flax', '亚麻', 'Natural', 12.00, 8.75, 8.75, NULL, 12.0, 12.0, 12.0, '纤维较细，有竹状横节，横截面为多边形，有中腔，溶于70%硫酸。'),
('Linen', '亚麻', 'Natural', 12.00, 8.75, 8.75, 12.0, 12.0, NULL, 12.0, '纤维较细，有竹状横节，横截面为多边形，有中腔，溶于70%硫酸。'),
('Modal', '莫代尔', 'Regenerated', 13.00, 11.0, 11.0, 13.0, 13.0, 13.0, 11.0, '表面平滑，有沟槽，横截面为哑铃型，溶于59.5%硫酸。'),
('Lyocell', '莱赛尔', 'Regenerated', 13.00, 11.0, 11.0, 13.0, 13.0, 13.0, 11.0, '表面平滑，有光泽，横截面为圆形或近似圆形，溶于浓盐酸。'),
('Spandex', '氨纶', 'Synthetic', 1.50, 1.3, 1.3, NULL, 1.3, 1.5, NULL, '表面平滑，有的有骨形条纹，纤维被拉伸至原长的三倍后再去除张力时，可迅速地基本回复到原长。'),
('Elastane', '氨纶', 'Synthetic', 1.50, 1.3, 1.3, NULL, 1.3, NULL, NULL, '表面平滑，有的有骨形条纹，纤维被拉伸至原长的三倍后再去除张力时，可迅速地基本回复到原长。');

PRINT 'Fiber data inserted successfully';
GO

-- 验证数据
SELECT COUNT(*) AS total_fibers FROM fiber_database;
SELECT fiber_name_en, fiber_name_cn, moisture_regain_iso, moisture_regain_gb FROM fiber_database ORDER BY fiber_name_en;
GO

PRINT 'Fiber Analysis module database initialization completed!';
