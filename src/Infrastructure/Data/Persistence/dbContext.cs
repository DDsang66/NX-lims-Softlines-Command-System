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

    public virtual DbSet<BasicMenuItem> BasicMenuItems { get; set; }

    public virtual DbSet<BasicParam> BasicParams { get; set; }

    public virtual DbSet<BasicParamRule> BasicParamRules { get; set; }

    public virtual DbSet<BasicParamStructure> BasicParamStructures { get; set; }

    public virtual DbSet<BasicStandard> BasicStandards { get; set; }

    public virtual DbSet<BasicStandardFamily> BasicStandardFamilies { get; set; }

    public virtual DbSet<CheckList> CheckLists { get; set; }

    public virtual DbSet<CheckListItem> CheckListItems { get; set; }

    public virtual DbSet<Composition> Compositions { get; set; }

    public virtual DbSet<ConditionPool> ConditionPools { get; set; }

    public virtual DbSet<FormulaStandardfamily> FormulaStandardfamilies { get; set; }

    public virtual DbSet<OutboxEntry> OutboxEntries { get; set; }

    public virtual DbSet<ParamsturctureStandardfamily> ParamsturctureStandardfamilies { get; set; }

    public virtual DbSet<PhysicalWeightRecordPo> PhysicalWeightRecords { get; set; }

    public virtual DbSet<ProcessedEvent> ProcessedEvents { get; set; }

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
            entity.HasKey(e => e.MenuId);

            entity.ToTable("basic_buyer_menu");

            entity.Property(e => e.MenuId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("menu_id");
            entity.Property(e => e.BuyerCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("buyer_code");
            entity.Property(e => e.MenuName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("menu_name");
            entity.Property(e => e.Remark)
                .HasColumnType("text")
                .HasColumnName("remark");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UploadTime)
                .HasColumnType("datetime")
                .HasColumnName("upload_time");
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
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.IsFeasible).HasColumnName("is_feasible");
            entity.Property(e => e.ItemNameChn)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("item_name_chn");
            entity.Property(e => e.ItemNameEn)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("item_name_en");
            entity.Property(e => e.ParamRequireDenfinition)
                .HasColumnType("text")
                .HasColumnName("param_require_denfinition");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TestGroup).HasColumnName("test_group");
        });

        modelBuilder.Entity<BasicMenuItem>(entity =>
        {
            entity.ToTable("basic_menu_item");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.BuyerModifiedGroup)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("buyer_modified_group");
            entity.Property(e => e.BuyerModifiedTestItem)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("buyer_modified_test_item");
            entity.Property(e => e.BuyerModifiedTestMethod)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("buyer_modified_test_method");
            entity.Property(e => e.BuyerOwnName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("buyer_own_name");
            entity.Property(e => e.MenuId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("menu_id");
            entity.Property(e => e.Requirement)
                .HasColumnType("text")
                .HasColumnName("requirement");
            entity.Property(e => e.StandardId)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("standard_id");
            entity.Property(e => e.TestItemId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("test_item_id");
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
            entity.Property(e => e.FormulaId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("formula_id");
            entity.Property(e => e.ParamName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("param_name");
            entity.Property(e => e.Schema)
                .HasColumnType("text")
                .HasColumnName("schema");
            entity.Property(e => e.Status).HasColumnName("status");
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

        modelBuilder.Entity<CheckList>(entity =>
        {
            entity.ToTable("check_list");

            entity.Property(e => e.CheckListId)
                .ValueGeneratedNever()
                .HasColumnName("check_list_id");
            entity.Property(e => e.CreatedTime)
                .HasColumnType("datetime")
                .HasColumnName("created_time");
            entity.Property(e => e.OrderId)
                .HasMaxLength(50)
                .HasColumnName("order_id");
            entity.Property(e => e.Remark)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("remark");
            entity.Property(e => e.Status).HasColumnName("status");
        });

        modelBuilder.Entity<CheckListItem>(entity =>
        {
            entity.ToTable("check_list_item");

            entity.Property(e => e.CheckListItemId)
                .ValueGeneratedNever()
                .HasColumnName("check_list_item_id");
            entity.Property(e => e.BuyerModifiedTestItem)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("buyer_modified_test_item");
            entity.Property(e => e.BuyerModifiedTestStandard)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("buyer_modified_test_standard");
            entity.Property(e => e.CheckListId).HasColumnName("check_list_id");
            entity.Property(e => e.Requirement)
                .HasColumnType("text")
                .HasColumnName("requirement");
            entity.Property(e => e.Samples)
                .HasColumnType("text")
                .HasColumnName("samples");
            entity.Property(e => e.StandardId)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("standard_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TestGroup).HasColumnName("test_group");
            entity.Property(e => e.TestItemId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("test_item_id");
            entity.Property(e => e.TestPointParams)
                .HasColumnType("text")
                .HasColumnName("test_point_params");
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

        modelBuilder.Entity<ConditionPool>(entity =>
        {
            entity.ToTable("condition_pool");

            entity.Property(e => e.ConditionPoolId)
                .ValueGeneratedNever()
                .HasColumnName("condition_pool_id");
            entity.Property(e => e.CheckListId).HasColumnName("check_list_id");
            entity.Property(e => e.Conditions)
                .HasColumnType("text")
                .HasColumnName("conditions");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TestPoints)
                .HasColumnType("text")
                .HasColumnName("test_points");
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

        modelBuilder.Entity<OutboxEntry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OutboxEn__3214EC07A5FC2690");

            entity.ToTable("outbox_entry");

            entity.HasIndex(e => e.AggregateRootId, "IX_OutboxEntry_AggregateRootId");

            entity.HasIndex(e => e.EventId, "IX_OutboxEntry_EventId").IsUnique();

            entity.HasIndex(e => new { e.Published, e.OccurredOn }, "IX_OutboxEntry_Published_OccurredOn");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.AggregateRootId)
                .HasMaxLength(200)
                .HasColumnName("aggregate_root_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.DeadLettered).HasColumnName("dead_lettered");
            entity.Property(e => e.Error).HasColumnName("error");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.EventType)
                .HasMaxLength(500)
                .HasColumnName("event_type");
            entity.Property(e => e.OccurredOn).HasColumnName("occurred_on");
            entity.Property(e => e.Payload).HasColumnName("payload");
            entity.Property(e => e.Published).HasColumnName("published");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.RetryCount).HasColumnName("retry_count");
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

        modelBuilder.Entity<PhysicalWeightRecordPo>(entity =>
        {
            entity.ToTable("physical_weight_record");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Area)
                .HasColumnType("decimal(10, 4)")
                .HasColumnName("area");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.EnvHumidity)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("env_humidity");
            entity.Property(e => e.EnvTemperature)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("env_temperature");
            entity.Property(e => e.Gsm)
                .HasColumnType("decimal(10, 4)")
                .HasColumnName("g_per_sqm");
            entity.Property(e => e.Oz)
                .HasColumnType("decimal(10, 4)")
                .HasColumnName("oz_per_sqyd");
            entity.Property(e => e.TestType)
                .HasMaxLength(20)
                .HasColumnName("test_type");
            entity.Property(e => e.LengthCm)
                .HasColumnType("decimal(10, 4)")
                .HasColumnName("length_cm");
            entity.Property(e => e.PieceCount).HasColumnName("piece_count");
            entity.Property(e => e.GPerM)
                .HasColumnType("decimal(10, 4)")
                .HasColumnName("g_per_m");
            entity.Property(e => e.OzPerYd)
                .HasColumnType("decimal(10, 4)")
                .HasColumnName("oz_per_yd");
            entity.Property(e => e.GPerPiece)
                .HasColumnType("decimal(10, 4)")
                .HasColumnName("g_per_piece");
            entity.Property(e => e.LbPerDozen)
                .HasColumnType("decimal(10, 4)")
                .HasColumnName("lb_per_dozen");
            entity.Property(e => e.RecordIndex).HasColumnName("record_index");
            entity.Property(e => e.ReportNumber)
                .HasMaxLength(50)
                .HasColumnName("report_number");
            entity.Property(e => e.SampleId)
                .HasMaxLength(50)
                .HasColumnName("sample_id");
            entity.Property(e => e.TestPoint)
                .HasMaxLength(50)
                .HasColumnName("test_point");
            entity.Property(e => e.TestTime)
                .HasColumnType("datetime")
                .HasColumnName("test_time");
            entity.Property(e => e.Weight)
                .HasColumnType("decimal(10, 4)")
                .HasColumnName("weight");
            entity.HasIndex(e => e.ReportNumber, "IX_physical_weight_record_report_number");
        });

        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__processe__3214EC07B0EC46CB");

            entity.ToTable("processed_event");

            entity.HasIndex(e => e.EventId, "UQ_ProcessedEvent_EventId").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.ProcessedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("processed_at");
        });

        modelBuilder.Entity<SampleInfo>(entity =>
        {
            entity.HasKey(e => e.IdSample);

            entity.ToTable("sample_info");

            entity.Property(e => e.IdSample)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("id_sample");
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
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
