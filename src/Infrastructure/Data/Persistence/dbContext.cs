using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class dbContext : DbContext
{
    public dbContext(DbContextOptions<dbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BasicBuyer> BasicBuyers { get; set; }

    public virtual DbSet<BasicBuyerMenu> BasicBuyerMenus { get; set; }

    public virtual DbSet<BasicFormula> BasicFormulas { get; set; }

    public virtual DbSet<BasicItem> BasicItems { get; set; }

    public virtual DbSet<BasicParam> BasicParams { get; set; }

    public virtual DbSet<BasicParamRule> BasicParamRules { get; set; }

    public virtual DbSet<BasicParamStructure> BasicParamStructures { get; set; }

    public virtual DbSet<BasicStandard> BasicStandards { get; set; }

    public virtual DbSet<BasicStandardFamily> BasicStandardFamilies { get; set; }

    public virtual DbSet<Composition> Compositions { get; set; }

    public virtual DbSet<FormulaStandardfamily> FormulaStandardfamilies { get; set; }

    public virtual DbSet<ParamstructureFormula> ParamstructureFormulas { get; set; }

    public virtual DbSet<ParamsturctureStandardfamily> ParamsturctureStandardfamilies { get; set; }

    public virtual DbSet<SampleInfo> SampleInfos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BasicBuyer>(entity =>
        {
            entity.HasKey(e => e.BuyerCode);

            entity.ToTable("basic_buyer");

            entity.Property(e => e.BuyerCode)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("buyer_code");
            entity.Property(e => e.BuyerName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("buyer_name");
            entity.Property(e => e.Country)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("country");
            entity.Property(e => e.IsIndividualTraveler).HasColumnName("is_individual_traveler");
            entity.Property(e => e.Remark)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("remark");
            entity.Property(e => e.SampleStorageDate).HasColumnName("sample_storage_date");
        });

        modelBuilder.Entity<BasicBuyerMenu>(entity =>
        {
            entity.HasKey(e => e.IdMenu);

            entity.ToTable("basic_buyer_menu");

            entity.Property(e => e.IdMenu)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("id_menu");
            entity.Property(e => e.BuyerCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("buyer_code");
            entity.Property(e => e.DisplayGroup)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("display_group");
            entity.Property(e => e.IndexItem)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("index_item");
            entity.Property(e => e.IndexStandardCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("index_standard_code");
            entity.Property(e => e.MenuName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("menu_name");
            entity.Property(e => e.ModifiedName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("modified_name");
            entity.Property(e => e.Requirement)
                .IsUnicode(false)
                .HasColumnName("requirement");
            entity.Property(e => e.TestGroup)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("test_group");
        });

        modelBuilder.Entity<BasicFormula>(entity =>
        {
            entity.HasKey(e => e.FormulaId).HasName("PK_formula");

            entity.ToTable("basic_formula");

            entity.Property(e => e.FormulaId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("formula_id");
            entity.Property(e => e.ConditionFields)
                .HasColumnType("text")
                .HasColumnName("condition_fields");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.EffectiveDate)
                .HasColumnType("datetime")
                .HasColumnName("effective_date");
            entity.Property(e => e.ExpressionTemplate)
                .HasColumnType("text")
                .HasColumnName("expression_template");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.ParamName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("param_name");
            entity.Property(e => e.Version).HasColumnName("version");
        });

        modelBuilder.Entity<BasicItem>(entity =>
        {
            entity.HasKey(e => e.IdItem);

            entity.ToTable("basic_item");

            entity.Property(e => e.IdItem)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("id_item");
            entity.Property(e => e.ItemNameChn)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("item_name_chn");
            entity.Property(e => e.ItemNameEn)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("item_name_en");
            entity.Property(e => e.ItemTypeFir)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("item_type_fir");
            entity.Property(e => e.ItemTypeSec)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("item_type_sec");
        });

        modelBuilder.Entity<BasicParam>(entity =>
        {
            entity.HasKey(e => e.IdParam);

            entity.ToTable("basic_param");

            entity.Property(e => e.IdParam)
                .ValueGeneratedNever()
                .HasColumnName("id_param");
            entity.Property(e => e.IndexItem)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("index_item");
            entity.Property(e => e.IndexSmapleInfo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("index_smaple_info");
            entity.Property(e => e.IndexStandardCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("index_standard_code");
            entity.Property(e => e.Param).HasColumnName("param");
            entity.Property(e => e.TestGroup)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("test_group");
        });

        modelBuilder.Entity<BasicParamRule>(entity =>
        {
            entity.HasKey(e => e.RuleId);

            entity.ToTable("basic_param_rule");

            entity.Property(e => e.RuleId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("rule_id");
            entity.Property(e => e.ConditionPattern)
                .HasColumnType("text")
                .HasColumnName("condition_pattern");
            entity.Property(e => e.DefaultValue)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("default_value");
            entity.Property(e => e.FormulaId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("formula_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.ParamName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("param_name");
            entity.Property(e => e.ParamStructureId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("param_structure_id");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.StandardFamilyCodeId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("standard_family_code_id");
            entity.Property(e => e.StopOnMatch).HasColumnName("stop_on_match");
        });

        modelBuilder.Entity<BasicParamStructure>(entity =>
        {
            entity.HasKey(e => e.ParamStructureId).HasName("PK_basic_param_schema");

            entity.ToTable("basic_param_structure");

            entity.Property(e => e.ParamStructureId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("param_structure_id");
            entity.Property(e => e.AllowedValue)
                .HasColumnType("text")
                .HasColumnName("allowed_value");
            entity.Property(e => e.EffectiveDate)
                .HasColumnType("datetime")
                .HasColumnName("effective_date");
            entity.Property(e => e.ParamName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("param_name");
            entity.Property(e => e.Schema)
                .HasColumnType("text")
                .HasColumnName("schema");
        });

        modelBuilder.Entity<BasicStandard>(entity =>
        {
            entity.HasKey(e => e.IdStandard);

            entity.ToTable("basic_standard");

            entity.Property(e => e.IdStandard)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("id_standard");
            entity.Property(e => e.StandardCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("standard_code");
            entity.Property(e => e.StandardCodeNameChn)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("standard_code_name_chn");
            entity.Property(e => e.StandardCodeNameEn)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("standard_code_name_en");
            entity.Property(e => e.StandardFamilyCodeId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("standard_family_code_id");
            entity.Property(e => e.Status).HasColumnName("status");
        });

        modelBuilder.Entity<BasicStandardFamily>(entity =>
        {
            entity.HasKey(e => e.IdStandardFamily);

            entity.ToTable("basic_standard_family");

            entity.Property(e => e.IdStandardFamily)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("id_standard_family");
            entity.Property(e => e.EffectiveDate)
                .HasColumnType("datetime")
                .HasColumnName("effective_date");
            entity.Property(e => e.StandardFamilyCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("standard_family_code");
            entity.Property(e => e.Version).HasColumnName("version");
        });

        modelBuilder.Entity<Composition>(entity =>
        {
            entity.HasKey(e => e.IdComposition);

            entity.ToTable("composition");

            entity.Property(e => e.IdComposition)
                .ValueGeneratedNever()
                .HasColumnName("id_composition");
            entity.Property(e => e.CompositionNameChn)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("composition_name_chn");
            entity.Property(e => e.CompositionNameEn)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("composition_name_en");
            entity.Property(e => e.PrimaryCategoryChn)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("primary_category_chn");
            entity.Property(e => e.PrimaryCategoryEn)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("primary_category_en");
            entity.Property(e => e.SecondaryClassificationChn)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("secondary_classification_chn");
            entity.Property(e => e.SecondaryClassificationEn)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("secondary_classification_en");
            entity.Property(e => e.TertiaryClassificationChn)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("tertiary_classification_chn");
            entity.Property(e => e.TertiaryClassificationEn)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("tertiary_classification_en");
        });

        modelBuilder.Entity<FormulaStandardfamily>(entity =>
        {
            entity.ToTable("formula_standardfamily");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FormulaId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("formula_id");
            entity.Property(e => e.IdStandardFamily)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("id_standard_family");

            entity.HasOne(d => d.Formula).WithMany(p => p.FormulaStandardfamilies)
                .HasForeignKey(d => d.FormulaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_formula_standardfamily_basic_formula");

            entity.HasOne(d => d.IdStandardFamilyNavigation).WithMany(p => p.FormulaStandardfamilies)
                .HasForeignKey(d => d.IdStandardFamily)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_formula_standardfamily_basic_standard_family");
        });

        modelBuilder.Entity<ParamstructureFormula>(entity =>
        {
            entity.ToTable("paramstructure_formula");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FormulaId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("formula_id");
            entity.Property(e => e.ParamStructureId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("param_structure_id");

            entity.HasOne(d => d.Formula).WithMany(p => p.ParamstructureFormulas)
                .HasForeignKey(d => d.FormulaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_paramstructure_formula_basic_formula");

            entity.HasOne(d => d.ParamStructure).WithMany(p => p.ParamstructureFormulas)
                .HasForeignKey(d => d.ParamStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_paramstructure_formula_basic_param_structure");
        });

        modelBuilder.Entity<ParamsturctureStandardfamily>(entity =>
        {
            entity.ToTable("paramsturcture_standardfamily");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdStandardFamily)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("id_standard_family");
            entity.Property(e => e.ParamStructureId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("param_structure_id");

            entity.HasOne(d => d.IdStandardFamilyNavigation).WithMany(p => p.ParamsturctureStandardfamilies)
                .HasForeignKey(d => d.IdStandardFamily)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_paramsturcture_standardfamily_basic_standard_family");

            entity.HasOne(d => d.ParamStructure).WithMany(p => p.ParamsturctureStandardfamilies)
                .HasForeignKey(d => d.ParamStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_paramsturcture_standardfamily_basic_param_structure");
        });

        modelBuilder.Entity<SampleInfo>(entity =>
        {
            entity.HasKey(e => e.IdSample);

            entity.ToTable("sample_info");

            entity.Property(e => e.IdSample)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("id_sample");
            entity.Property(e => e.ApparelLocation)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("apparel_location");
            entity.Property(e => e.IndexCarelabel).HasColumnName("index_carelabel");
            entity.Property(e => e.IndexComposition).HasColumnName("index_composition");
            entity.Property(e => e.Remark)
                .IsUnicode(false)
                .HasColumnName("remark");
            entity.Property(e => e.ReportNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("report_number");
            entity.Property(e => e.SampleCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sample_code");
            entity.Property(e => e.SampleDescription)
                .IsUnicode(false)
                .HasColumnName("sample_description");
            entity.Property(e => e.Structure)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("structure");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
