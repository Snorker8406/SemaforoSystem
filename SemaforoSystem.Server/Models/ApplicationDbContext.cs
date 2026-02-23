using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SemaforoSystem.Server.Models;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<AccountPayment> AccountPayments { get; set; }

    public virtual DbSet<AccountStatus> AccountStatuses { get; set; }

    public virtual DbSet<AccountType> AccountTypes { get; set; }

    public virtual DbSet<Archive> Archives { get; set; }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<ClientCategory> ClientCategories { get; set; }

    public virtual DbSet<ClientStatus> ClientStatuses { get; set; }

    public virtual DbSet<Embroidery> Embroideries { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeSalary> EmployeeSalaries { get; set; }

    public virtual DbSet<EmployeeSchedule> EmployeeSchedules { get; set; }

    public virtual DbSet<File> Files { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCombo> ProductCombos { get; set; }

    public virtual DbSet<ProductComboDetail> ProductComboDetails { get; set; }

    public virtual DbSet<ProductCost> ProductCosts { get; set; }

    public virtual DbSet<ProductPicture> ProductPictures { get; set; }

    public virtual DbSet<ProductPrice> ProductPrices { get; set; }

    public virtual DbSet<ProductProvider> ProductProviders { get; set; }

    public virtual DbSet<ProductVariant> ProductVariants { get; set; }

    public virtual DbSet<ProductVariantSystem> ProductVariantSystems { get; set; }

    public virtual DbSet<Provider> Providers { get; set; }

    public virtual DbSet<ProviderAccount> ProviderAccounts { get; set; }

    public virtual DbSet<ProviderAccountPayment> ProviderAccountPayments { get; set; }

    public virtual DbSet<Sale> Sales { get; set; }

    public virtual DbSet<SalesDetail> SalesDetails { get; set; }

    public virtual DbSet<SalesType> SalesTypes { get; set; }

    public virtual DbSet<School> Schools { get; set; }

    public virtual DbSet<SchoolLevel> SchoolLevels { get; set; }

    public virtual DbSet<Site> Sites { get; set; }

    public virtual DbSet<Size> Sizes { get; set; }

    public virtual DbSet<SizeSystem> SizeSystems { get; set; }

    public virtual DbSet<Stock> Stocks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("accounts_pkey");

            entity.Property(e => e.OpeningDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.AccountStatus).WithMany(p => p.Accounts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("accounts_account_status_id_fkey");

            entity.HasOne(d => d.AccountType).WithMany(p => p.Accounts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("accounts_account_type_id_fkey");

            entity.HasOne(d => d.Client).WithMany(p => p.Accounts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("accounts_client_id_fkey");

            entity.HasOne(d => d.Employee).WithMany(p => p.Accounts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("accounts_employee_id_fkey");

            entity.HasOne(d => d.Sale).WithMany(p => p.Accounts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("accounts_sale_id_fkey");

            entity.HasOne(d => d.Site).WithMany(p => p.Accounts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("accounts_site_id_fkey");
        });

        modelBuilder.Entity<AccountPayment>(entity =>
        {
            entity.HasKey(e => e.AccountPaymentId).HasName("account_payments_pkey");

            entity.Property(e => e.PaymentDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Account).WithMany(p => p.AccountPayments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("account_payments_account_id_fkey");

            entity.HasOne(d => d.Employee).WithMany(p => p.AccountPayments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("account_payments_employee_id_fkey");
        });

        modelBuilder.Entity<AccountStatus>(entity =>
        {
            entity.HasKey(e => e.AccountStatusId).HasName("account_status_pkey");
        });

        modelBuilder.Entity<AccountType>(entity =>
        {
            entity.HasKey(e => e.AccountTypeId).HasName("account_types_pkey");
        });

        modelBuilder.Entity<Archive>(entity =>
        {
            entity.HasKey(e => e.ArchiveId).HasName("archives_pkey");
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("AspNetUserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("attendance_pkey");

            entity.Property(e => e.AttendanceId).ValueGeneratedNever();

            entity.HasOne(d => d.Employee).WithMany(p => p.Attendances)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("attendance_employee_id_fkey");
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandId).HasName("brands_pkey");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("categories_pkey");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory).HasConstraintName("category_parent_category_id_fkey");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("clients_pkey");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.ClientCategory).WithMany(p => p.Clients)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("clients_client_category_id_fkey");

            entity.HasOne(d => d.ClientStatus).WithMany(p => p.Clients)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("clients_client_status_id_fkey");

            entity.HasOne(d => d.Employee).WithMany(p => p.Clients)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("clients_employee_id_fkey");
        });

        modelBuilder.Entity<ClientCategory>(entity =>
        {
            entity.HasKey(e => e.ClientCategoryId).HasName("client_categories_pkey");
        });

        modelBuilder.Entity<ClientStatus>(entity =>
        {
            entity.HasKey(e => e.ClientStatusId).HasName("client_status_pkey");

            entity.Property(e => e.ClientStatusId).ValueGeneratedNever();
        });

        modelBuilder.Entity<Embroidery>(entity =>
        {
            entity.HasKey(e => e.EmbroideryId).HasName("embroideries_pkey");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.School).WithMany(p => p.Embroideries).HasConstraintName("embroideries_school_id_fkey");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("employees_pkey");

            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.CreateDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<EmployeeSalary>(entity =>
        {
            entity.HasKey(e => e.EmployeeSalaryId).HasName("employee_salary_pkey");

            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeSalaries)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("employee_salary_employee_id_fkey");
        });

        modelBuilder.Entity<EmployeeSchedule>(entity =>
        {
            entity.HasKey(e => e.EmployeeScheduleId).HasName("employee_schedule_pkey");

            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeSchedules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("employee_schedule_employee_id_fkey");
        });

        modelBuilder.Entity<File>(entity =>
        {
            entity.HasKey(e => e.FileId).HasName("files_pkey");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Account).WithMany(p => p.Files).HasConstraintName("files_account_id_fkey");

            entity.HasOne(d => d.Archive).WithMany(p => p.Files)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("files_archive_id_fkey");

            entity.HasOne(d => d.Client).WithMany(p => p.Files).HasConstraintName("files_client_id_fkey");

            entity.HasOne(d => d.Employee).WithMany(p => p.Files).HasConstraintName("files_employee_id_fkey");

            entity.HasOne(d => d.ProviderAccount).WithMany(p => p.Files).HasConstraintName("files_provider_account_id_fkey");

            entity.HasOne(d => d.ProviderAccountPayment).WithMany(p => p.Files).HasConstraintName("files_provider_account_payment_id_fkey");

            entity.HasOne(d => d.Provider).WithMany(p => p.Files).HasConstraintName("files_provider_id_fkey");

            entity.HasOne(d => d.School).WithMany(p => p.Files).HasConstraintName("files_school_id_fkey");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("products_pkey");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Brand).WithMany(p => p.Products).HasConstraintName("products_brand_id_fkey");

            entity.HasOne(d => d.ProductPicture).WithMany(p => p.Products).HasConstraintName("products_product_picture_id_fkey");

            entity.HasOne(d => d.SizeSystem).WithMany(p => p.Products).HasConstraintName("products_size_system_id_fkey");

            entity.HasMany(d => d.Categories).WithMany(p => p.Products)
                .UsingEntity<Dictionary<string, object>>(
                    "ProductCategory",
                    r => r.HasOne<Category>().WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("product_category_category_id_fkey"),
                    l => l.HasOne<Product>().WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("product_category_product_id_fkey"),
                    j =>
                    {
                        j.HasKey("ProductId", "CategoryId").HasName("product_category_pkey");
                        j.ToTable("product_category");
                        j.IndexerProperty<int>("ProductId").HasColumnName("product_id");
                        j.IndexerProperty<int>("CategoryId").HasColumnName("category_id");
                    });

            entity.HasMany(d => d.ProductVariantSystems).WithMany(p => p.Products)
                .UsingEntity<Dictionary<string, object>>(
                    "ProductsProductVariantSystem",
                    r => r.HasOne<ProductVariantSystem>().WithMany()
                        .HasForeignKey("ProductVariantSystemId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("products_product_variant_system_product_variant_system_id_fkey"),
                    l => l.HasOne<Product>().WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("products_product_variant_system_product_id_fkey"),
                    j =>
                    {
                        j.HasKey("ProductId", "ProductVariantSystemId").HasName("products_product_variant_system_pkey");
                        j.ToTable("products_product_variant_system");
                        j.IndexerProperty<int>("ProductId").HasColumnName("product_id");
                        j.IndexerProperty<int>("ProductVariantSystemId").HasColumnName("product_variant_system_id");
                    });

            entity.HasMany(d => d.Schools).WithMany(p => p.Products)
                .UsingEntity<Dictionary<string, object>>(
                    "ProductSchool",
                    r => r.HasOne<School>().WithMany()
                        .HasForeignKey("SchoolId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("product_schools_school_id_fkey"),
                    l => l.HasOne<Product>().WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("product_schools_product_id_fkey"),
                    j =>
                    {
                        j.HasKey("ProductId", "SchoolId").HasName("product_schools_pkey");
                        j.ToTable("product_schools");
                        j.IndexerProperty<int>("ProductId").HasColumnName("product_id");
                        j.IndexerProperty<int>("SchoolId").HasColumnName("school_id");
                    });
        });

        modelBuilder.Entity<ProductCombo>(entity =>
        {
            entity.HasKey(e => e.ProductComboId).HasName("product_combos_pkey");
        });

        modelBuilder.Entity<ProductComboDetail>(entity =>
        {
            entity.HasKey(e => e.ProductComboDetailId).HasName("product_combo_details_pkey");

            entity.HasOne(d => d.Embroidery).WithMany(p => p.ProductComboDetails).HasConstraintName("product_combo_details_embroidery_id_fkey");

            entity.HasOne(d => d.ProductCombo).WithMany(p => p.ProductComboDetails)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_combo_details_product_combo_id_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductComboDetails).HasConstraintName("product_combo_details_product_id_fkey");
        });

        modelBuilder.Entity<ProductCost>(entity =>
        {
            entity.HasKey(e => e.ProductCostId).HasName("product_costs_pkey");

            entity.ToTable("product_costs", tb => tb.HasComment("costos de los productos"));

            entity.HasOne(d => d.Product).WithMany(p => p.ProductCosts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_costs_product_id_fkey");
        });

        modelBuilder.Entity<ProductPicture>(entity =>
        {
            entity.HasKey(e => e.ProductPictureId).HasName("product_pictures_pkey");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("CURRENT_DATE");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductPictures)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_pictures_product_id_fkey");
        });

        modelBuilder.Entity<ProductPrice>(entity =>
        {
            entity.HasKey(e => e.PriceId).HasName("product_prices_pkey");

            entity.Property(e => e.PriceId).ValueGeneratedNever();

            entity.HasOne(d => d.ProductCombo).WithMany(p => p.ProductPrices).HasConstraintName("product_prices_product_combo_id_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductPrices)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_prices_product_id_fkey");

            entity.HasOne(d => d.Size).WithMany(p => p.ProductPrices).HasConstraintName("product_prices_size_id_fkey");

            entity.HasOne(d => d.Variant).WithMany(p => p.ProductPrices).HasConstraintName("product_prices_variant_id_fkey");
        });

        modelBuilder.Entity<ProductProvider>(entity =>
        {
            entity.HasOne(d => d.Product).WithMany()
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_providers_product_id_fkey");

            entity.HasOne(d => d.Provider).WithMany()
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_providers_provider_id_fkey");
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.ProductVariantId).HasName("product_variants_pkey");

            entity.Property(e => e.ProductVariantId).HasDefaultValueSql("nextval('product_variant_product_variant_id_seq'::regclass)");

            entity.HasOne(d => d.ProductVariantSystem).WithMany(p => p.ProductVariants)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_variants_product_variant_system_id_fkey");
        });

        modelBuilder.Entity<ProductVariantSystem>(entity =>
        {
            entity.HasKey(e => e.ProductVariantId).HasName("product_variant_system_pkey");
        });

        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(e => e.ProviderId).HasName("providers_pkey");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<ProviderAccount>(entity =>
        {
            entity.HasKey(e => e.ProviderAccountId).HasName("provider_account_pkey");

            entity.Property(e => e.OpeningDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Employee).WithMany(p => p.ProviderAccounts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("provider_account_employee_id_fkey");

            entity.HasOne(d => d.Provider).WithMany(p => p.ProviderAccounts).HasConstraintName("provider_account_provider_id_fkey");
        });

        modelBuilder.Entity<ProviderAccountPayment>(entity =>
        {
            entity.HasKey(e => e.ProviderAccountPaymentId).HasName("provider_account_payments_pkey");

            entity.Property(e => e.PaymentDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Employee).WithMany(p => p.ProviderAccountPayments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("provider_account_payments_employee_id_fkey");

            entity.HasOne(d => d.ProviderAccount).WithMany(p => p.ProviderAccountPayments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("provider_account_payments_provider_account_id_fkey");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.SaleId).HasName("sales_pkey");

            entity.Property(e => e.SaleDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Client).WithMany(p => p.Sales).HasConstraintName("sales_client_id_fkey");

            entity.HasOne(d => d.Employee).WithMany(p => p.Sales)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sales_employee_id_fkey");

            entity.HasOne(d => d.SaleType).WithMany(p => p.Sales)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sales_sale_type_id_fkey");

            entity.HasOne(d => d.Site).WithMany(p => p.Sales)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sales_site_id_fkey");
        });

        modelBuilder.Entity<SalesDetail>(entity =>
        {
            entity.HasKey(e => e.SaleDetailId).HasName("sales_details_pkey");

            entity.HasOne(d => d.Product).WithMany(p => p.SalesDetails)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sales_details_product_id_fkey");

            entity.HasOne(d => d.Sale).WithMany(p => p.SalesDetails)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sales_details_sale_id_fkey");

            entity.HasOne(d => d.Size).WithMany(p => p.SalesDetails).HasConstraintName("sales_details_size_id_fkey");
        });

        modelBuilder.Entity<SalesType>(entity =>
        {
            entity.HasKey(e => e.SaleTypeId).HasName("sales_types_pkey");
        });

        modelBuilder.Entity<School>(entity =>
        {
            entity.HasKey(e => e.SchoolId).HasName("schools_pkey");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.SchoolLevel).WithMany(p => p.Schools)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("schools_school_level_id_fkey");
        });

        modelBuilder.Entity<SchoolLevel>(entity =>
        {
            entity.HasKey(e => e.SchoolLevelId).HasName("school_levels_pkey");
        });

        modelBuilder.Entity<Site>(entity =>
        {
            entity.HasKey(e => e.SiteId).HasName("sites_pkey");
        });

        modelBuilder.Entity<Size>(entity =>
        {
            entity.HasKey(e => e.SizeId).HasName("sizes_pkey");

            entity.HasOne(d => d.SizeSystem).WithMany(p => p.Sizes).HasConstraintName("size_system_size_system_id_fkey");
        });

        modelBuilder.Entity<SizeSystem>(entity =>
        {
            entity.HasKey(e => e.SizeSystemId).HasName("size_system_pkey");
        });

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.StockId).HasName("stock_pkey");

            entity.Property(e => e.Barcode).HasDefaultValueSql("'100'::character varying");
            entity.Property(e => e.CreateDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Product).WithMany(p => p.Stocks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_product_id_fkey");

            entity.HasOne(d => d.SaleDetail).WithMany(p => p.Stocks).HasConstraintName("stock_sale_detail_id_fkey");

            entity.HasOne(d => d.Site).WithMany(p => p.Stocks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_site_id_fkey");

            entity.HasOne(d => d.Size).WithMany(p => p.Stocks).HasConstraintName("stock_size_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
