using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Domain.Model;

public partial class LabDbContextSec : DbContext
{
    public LabDbContextSec()
    {
    }

    public LabDbContextSec(DbContextOptions<LabDbContextSec> options)
        : base(options)
    {
    }

    public virtual DbSet<AdidasMethodItemMap> AdidasMethodItemMaps { get; set; }

    public virtual DbSet<Composition> Compositions { get; set; }

    public virtual DbSet<CustomerService> CustomerServices { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<LabTestInfo> LabTestInfos { get; set; }

    public virtual DbSet<LabTestSchedule> LabTestSchedules { get; set; }

    public virtual DbSet<Menu> Menus { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Standard> Standards { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<WetParameterAatcc> WetParameterAatccs { get; set; }

    public virtual DbSet<WetParameterIso> WetParameterIsos { get; set; }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //    => optionsBuilder.UseSqlServer("NX-limsLabCommandSys");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdidasMethodItemMap>(entity =>
        {
            entity.HasKey(e => e.MethodId);

            entity.ToTable("adidas_method_item_map");

            entity.Property(e => e.MethodId)
                .ValueGeneratedNever()
                .HasColumnName("method_id");
            entity.Property(e => e.MethodCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("method_code");
            entity.Property(e => e.MethodName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("method_name");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type");
        });

        modelBuilder.Entity<Composition>(entity =>
        {
            entity.HasKey(e => e.FiberId);

            entity.ToTable("composition");

            entity.Property(e => e.FiberId)
                .ValueGeneratedNever()
                .HasColumnName("fiber_id");
            entity.Property(e => e.FiberDescripe)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fiber_descripe");
            entity.Property(e => e.FiberName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fiber_name");
            entity.Property(e => e.FiberSource)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fiber_source");
            entity.Property(e => e.FiberType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fiber_type");
        });

        modelBuilder.Entity<CustomerService>(entity =>
        {
            entity.ToTable("customer_service");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CustomerService1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("customer_service");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_feeback");

            entity.ToTable("feedback");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Assignee)
                .HasMaxLength(50)
                .HasColumnName("assignee");
            entity.Property(e => e.CreateTime)
                .HasColumnType("datetime")
                .HasColumnName("create_time");
            entity.Property(e => e.Message)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("message");
            entity.Property(e => e.Status)
                .HasDefaultValue((byte)0)
                .HasColumnName("status");
            entity.Property(e => e.Type)
                .HasDefaultValue((byte)3)
                .HasColumnName("type");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.ItemIndex);

            entity.ToTable("item");

            entity.Property(e => e.ItemIndex)
                .ValueGeneratedNever()
                .HasColumnName("item_index");
            entity.Property(e => e.ItemName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("item_name");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type");
        });

        modelBuilder.Entity<LabTestInfo>(entity =>
        {
            entity.ToTable("lab_test_info");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CustomerService)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("customer_service");
            entity.Property(e => e.Express)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("express");
            entity.Property(e => e.Describe)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("describe");
            entity.Property(e => e.OrderEntryPerson)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("order_entry_person");
            entity.Property(e => e.Remark)
                .HasColumnType("text")
                .HasColumnName("remark");
            entity.Property(e => e.ReportNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("report_number");
            entity.Property(e => e.Reviewer)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("reviewer");
            entity.Property(e => e.ScheduleIndex).HasColumnName("schedule_index");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TestEngineer)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("test_engineer");
            entity.Property(e => e.TestGroup)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("test_group");
            entity.Property(e => e.TestItemNum).HasColumnName("test_item_num");
            entity.Property(e => e.TestSampleNum).HasColumnName("test_sample_num");
            entity.Property(e => e.LastUpdateTime)
    .HasColumnType("datetime")
    .HasColumnName("last_update_time");
        });

        modelBuilder.Entity<LabTestSchedule>(entity =>
        {
            entity.HasKey(e => e.IdSchedule);

            entity.ToTable("lab_test_schedule");

            entity.Property(e => e.IdSchedule)
                .ValueGeneratedNever()
                .HasColumnName("id_schedule");
            entity.Property(e => e.LabOutTime)
                .HasColumnType("datetime")
                .HasColumnName("lab_out_time");
            entity.Property(e => e.OrderInTime)
                .HasColumnType("datetime")
                .HasColumnName("order_in_time");
            entity.Property(e => e.ReportDueDate).HasColumnName("report_due_date");
            entity.Property(e => e.ReviewFinishTime)
                .HasColumnType("datetime")
                .HasColumnName("review_finish_time");
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.ToTable("menu");

            entity.Property(e => e.MenuId)
                .ValueGeneratedNever()
                .HasColumnName("menu_id");
            entity.Property(e => e.ContactBuyer)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("contact_buyer");
            entity.Property(e => e.MenuName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("menu_name");
            entity.Property(e => e.StandardIndex1).HasColumnName("standard_index_1");
            entity.Property(e => e.StandardIndex10).HasColumnName("standard_index_10");
            entity.Property(e => e.StandardIndex100).HasColumnName("standard_index_100");
            entity.Property(e => e.StandardIndex11).HasColumnName("standard_index_11");
            entity.Property(e => e.StandardIndex12).HasColumnName("standard_index_12");
            entity.Property(e => e.StandardIndex13).HasColumnName("standard_index_13");
            entity.Property(e => e.StandardIndex14).HasColumnName("standard_index_14");
            entity.Property(e => e.StandardIndex15).HasColumnName("standard_index_15");
            entity.Property(e => e.StandardIndex16).HasColumnName("standard_index_16");
            entity.Property(e => e.StandardIndex17).HasColumnName("standard_index_17");
            entity.Property(e => e.StandardIndex18).HasColumnName("standard_index_18");
            entity.Property(e => e.StandardIndex19).HasColumnName("standard_index_19");
            entity.Property(e => e.StandardIndex2).HasColumnName("standard_index_2");
            entity.Property(e => e.StandardIndex20).HasColumnName("standard_index_20");
            entity.Property(e => e.StandardIndex21).HasColumnName("standard_index_21");
            entity.Property(e => e.StandardIndex22).HasColumnName("standard_index_22");
            entity.Property(e => e.StandardIndex23).HasColumnName("standard_index_23");
            entity.Property(e => e.StandardIndex24).HasColumnName("standard_index_24");
            entity.Property(e => e.StandardIndex25).HasColumnName("standard_index_25");
            entity.Property(e => e.StandardIndex26).HasColumnName("standard_index_26");
            entity.Property(e => e.StandardIndex27).HasColumnName("standard_index_27");
            entity.Property(e => e.StandardIndex28).HasColumnName("standard_index_28");
            entity.Property(e => e.StandardIndex29).HasColumnName("standard_index_29");
            entity.Property(e => e.StandardIndex3).HasColumnName("standard_index_3");
            entity.Property(e => e.StandardIndex30).HasColumnName("standard_index_30");
            entity.Property(e => e.StandardIndex31).HasColumnName("standard_index_31");
            entity.Property(e => e.StandardIndex32).HasColumnName("standard_index_32");
            entity.Property(e => e.StandardIndex33).HasColumnName("standard_index_33");
            entity.Property(e => e.StandardIndex34).HasColumnName("standard_index_34");
            entity.Property(e => e.StandardIndex35).HasColumnName("standard_index_35");
            entity.Property(e => e.StandardIndex36).HasColumnName("standard_index_36");
            entity.Property(e => e.StandardIndex37).HasColumnName("standard_index_37");
            entity.Property(e => e.StandardIndex38).HasColumnName("standard_index_38");
            entity.Property(e => e.StandardIndex39).HasColumnName("standard_index_39");
            entity.Property(e => e.StandardIndex4).HasColumnName("standard_index_4");
            entity.Property(e => e.StandardIndex40).HasColumnName("standard_index_40");
            entity.Property(e => e.StandardIndex41).HasColumnName("standard_index_41");
            entity.Property(e => e.StandardIndex42).HasColumnName("standard_index_42");
            entity.Property(e => e.StandardIndex43).HasColumnName("standard_index_43");
            entity.Property(e => e.StandardIndex44).HasColumnName("standard_index_44");
            entity.Property(e => e.StandardIndex45).HasColumnName("standard_index_45");
            entity.Property(e => e.StandardIndex46).HasColumnName("standard_index_46");
            entity.Property(e => e.StandardIndex47).HasColumnName("standard_index_47");
            entity.Property(e => e.StandardIndex48).HasColumnName("standard_index_48");
            entity.Property(e => e.StandardIndex49).HasColumnName("standard_index_49");
            entity.Property(e => e.StandardIndex5).HasColumnName("standard_index_5");
            entity.Property(e => e.StandardIndex50).HasColumnName("standard_index_50");
            entity.Property(e => e.StandardIndex51).HasColumnName("standard_index_51");
            entity.Property(e => e.StandardIndex52).HasColumnName("standard_index_52");
            entity.Property(e => e.StandardIndex53).HasColumnName("standard_index_53");
            entity.Property(e => e.StandardIndex54).HasColumnName("standard_index_54");
            entity.Property(e => e.StandardIndex55).HasColumnName("standard_index_55");
            entity.Property(e => e.StandardIndex56).HasColumnName("standard_index_56");
            entity.Property(e => e.StandardIndex57).HasColumnName("standard_index_57");
            entity.Property(e => e.StandardIndex58).HasColumnName("standard_index_58");
            entity.Property(e => e.StandardIndex59).HasColumnName("standard_index_59");
            entity.Property(e => e.StandardIndex6).HasColumnName("standard_index_6");
            entity.Property(e => e.StandardIndex60).HasColumnName("standard_index_60");
            entity.Property(e => e.StandardIndex61).HasColumnName("standard_index_61");
            entity.Property(e => e.StandardIndex62).HasColumnName("standard_index_62");
            entity.Property(e => e.StandardIndex63).HasColumnName("standard_index_63");
            entity.Property(e => e.StandardIndex64).HasColumnName("standard_index_64");
            entity.Property(e => e.StandardIndex65).HasColumnName("standard_index_65");
            entity.Property(e => e.StandardIndex66).HasColumnName("standard_index_66");
            entity.Property(e => e.StandardIndex67).HasColumnName("standard_index_67");
            entity.Property(e => e.StandardIndex68).HasColumnName("standard_index_68");
            entity.Property(e => e.StandardIndex69).HasColumnName("standard_index_69");
            entity.Property(e => e.StandardIndex7).HasColumnName("standard_index_7");
            entity.Property(e => e.StandardIndex70).HasColumnName("standard_index_70");
            entity.Property(e => e.StandardIndex71).HasColumnName("standard_index_71");
            entity.Property(e => e.StandardIndex72).HasColumnName("standard_index_72");
            entity.Property(e => e.StandardIndex73).HasColumnName("standard_index_73");
            entity.Property(e => e.StandardIndex74).HasColumnName("standard_index_74");
            entity.Property(e => e.StandardIndex75).HasColumnName("standard_index_75");
            entity.Property(e => e.StandardIndex76).HasColumnName("standard_index_76");
            entity.Property(e => e.StandardIndex77).HasColumnName("standard_index_77");
            entity.Property(e => e.StandardIndex78).HasColumnName("standard_index_78");
            entity.Property(e => e.StandardIndex79).HasColumnName("standard_index_79");
            entity.Property(e => e.StandardIndex8).HasColumnName("standard_index_8");
            entity.Property(e => e.StandardIndex80).HasColumnName("standard_index_80");
            entity.Property(e => e.StandardIndex81).HasColumnName("standard_index_81");
            entity.Property(e => e.StandardIndex82).HasColumnName("standard_index_82");
            entity.Property(e => e.StandardIndex83).HasColumnName("standard_index_83");
            entity.Property(e => e.StandardIndex84).HasColumnName("standard_index_84");
            entity.Property(e => e.StandardIndex85).HasColumnName("standard_index_85");
            entity.Property(e => e.StandardIndex86).HasColumnName("standard_index_86");
            entity.Property(e => e.StandardIndex87).HasColumnName("standard_index_87");
            entity.Property(e => e.StandardIndex88).HasColumnName("standard_index_88");
            entity.Property(e => e.StandardIndex89).HasColumnName("standard_index_89");
            entity.Property(e => e.StandardIndex9).HasColumnName("standard_index_9");
            entity.Property(e => e.StandardIndex90).HasColumnName("standard_index_90");
            entity.Property(e => e.StandardIndex91).HasColumnName("standard_index_91");
            entity.Property(e => e.StandardIndex92).HasColumnName("standard_index_92");
            entity.Property(e => e.StandardIndex93).HasColumnName("standard_index_93");
            entity.Property(e => e.StandardIndex94).HasColumnName("standard_index_94");
            entity.Property(e => e.StandardIndex95).HasColumnName("standard_index_95");
            entity.Property(e => e.StandardIndex96).HasColumnName("standard_index_96");
            entity.Property(e => e.StandardIndex97).HasColumnName("standard_index_97");
            entity.Property(e => e.StandardIndex98).HasColumnName("standard_index_98");
            entity.Property(e => e.StandardIndex99).HasColumnName("standard_index_99");
            entity.Property(e => e.UploadTime).HasColumnName("upload_time");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionIndex);

            entity.ToTable("permission");

            entity.Property(e => e.PermissionIndex)
                .ValueGeneratedNever()
                .HasColumnName("permission_index");
            entity.Property(e => e.Review)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("false")
                .HasColumnName("review");
            entity.Property(e => e.ReviewPhysics)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("false")
                .HasColumnName("review_physics");
            entity.Property(e => e.ReviewWet)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("false")
                .HasColumnName("review_wet");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("normal")
                .HasColumnName("role");
        });

        modelBuilder.Entity<Standard>(entity =>
        {
            entity.ToTable("standard");

            entity.Property(e => e.StandardId)
                .ValueGeneratedNever()
                .HasColumnName("standard_id");
            entity.Property(e => e.ItemIndex).HasColumnName("item_index");
            entity.Property(e => e.StandardCode)
                .HasMaxLength(255)
                .HasColumnName("standard_code");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("user");

            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("user_id");
            entity.Property(e => e.CreateTime)
                .HasColumnType("datetime")
                .HasColumnName("create_time");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("employee_id");
            entity.Property(e => e.LastLoginIp)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("last_login_ip");
            entity.Property(e => e.LoginFailCount)
                .HasDefaultValue(0)
                .HasColumnName("login_fail_count");
            entity.Property(e => e.NickName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("nick_name");
            entity.Property(e => e.PassWord)
                .HasColumnType("text")
                .HasColumnName("pass_word");
            entity.Property(e => e.PermissionIndex).HasColumnName("permission_index");
            entity.Property(e => e.Status)
                .HasDefaultValue((byte)1)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedTime)
                .HasColumnType("datetime")
                .HasColumnName("updated_time");
            entity.Property(e => e.UserName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("user_name");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.EmployeeId);

            entity.ToTable("user_profile");

            entity.Property(e => e.EmployeeId)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("employee_id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("avatar_url");
            entity.Property(e => e.Birth).HasColumnName("birth");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("gender");
            entity.Property(e => e.IdCard)
                .HasColumnType("text")
                .HasColumnName("id_card");
            entity.Property(e => e.Phone)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.RealName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("real_name");
        });

        modelBuilder.Entity<WetParameterAatcc>(entity =>
        {
            entity.HasKey(e => e.ParamId).HasName("PK_wet_parameter_aatcc_1");

            entity.ToTable("wet_parameter_aatcc");

            entity.Property(e => e.ParamId).HasColumnName("param_id");
            entity.Property(e => e.AfterWash).HasColumnName("after_wash");
            entity.Property(e => e.Bleach)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("bleach");
            entity.Property(e => e.ContactItem)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("contact_item");
            entity.Property(e => e.Cycle)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("cycle");
            entity.Property(e => e.DryCleanProcedure)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("dry_clean_procedure");
            entity.Property(e => e.DryCondition)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("dry_condition");
            entity.Property(e => e.DryProcedure)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("dry_procedure");
            entity.Property(e => e.Iron)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("iron");
            entity.Property(e => e.Program)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("program");
            entity.Property(e => e.ReportNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("report_number");
            entity.Property(e => e.Sensitive)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sensitive");
            entity.Property(e => e.SpecialCareInstruction)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("special_care_instruction");
            entity.Property(e => e.StandardType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("standard_type");
            entity.Property(e => e.SteelBallNum).HasColumnName("steel_ball_num");
            entity.Property(e => e.SteelBallType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("steel_ball_type");
            entity.Property(e => e.Temperature)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("temperature");
            entity.Property(e => e.WashingProcedure)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("washing_procedure");
        });

        modelBuilder.Entity<WetParameterIso>(entity =>
        {
            entity.HasKey(e => e.ParamId).HasName("PK_wet_parameter_iso_1");

            entity.ToTable("wet_parameter_iso");

            entity.Property(e => e.ParamId).HasColumnName("param_id");
            entity.Property(e => e.AfterWash).HasColumnName("after_wash");
            entity.Property(e => e.Ballast)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ballast");
            entity.Property(e => e.Bleach)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("bleach");
            entity.Property(e => e.ContactItem)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("contact_item");
            entity.Property(e => e.DryCleanProcedure)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("dry_clean_procedure");
            entity.Property(e => e.DryProcedure)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("dry_procedure");
            entity.Property(e => e.Iron)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("iron");
            entity.Property(e => e.Program)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("program");
            entity.Property(e => e.ReportNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("report_number");
            entity.Property(e => e.Sensitive)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("sensitive");
            entity.Property(e => e.SpecialCareInstruction)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("special_care_instruction");
            entity.Property(e => e.StandardType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("standard_type");
            entity.Property(e => e.SteelBallNum).HasColumnName("steel_ball_num");
            entity.Property(e => e.SteelBallType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("steel_ball_type");
            entity.Property(e => e.Temperature)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("temperature");
            entity.Property(e => e.WashingProcedure)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("washing_procedure");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
