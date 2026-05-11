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

    public virtual DbSet<AccountInstallment> AccountInstallments { get; set; }

    public virtual DbSet<AccountItem> AccountItems { get; set; }

    public virtual DbSet<AccountItemSerialItem> AccountItemSerialItems { get; set; }

    public virtual DbSet<AccountStatus> AccountStatuses { get; set; }

    public virtual DbSet<AccountTransaction> AccountTransactions { get; set; }

    public virtual DbSet<AccountType> AccountTypes { get; set; }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<Attachment> Attachments { get; set; }

    public virtual DbSet<AttachmentLink> AttachmentLinks { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<ClientCategory> ClientCategories { get; set; }

    public virtual DbSet<ClientStatus> ClientStatuses { get; set; }

    public virtual DbSet<CreditAccount> CreditAccounts { get; set; }

    public virtual DbSet<Embroidery> Embroideries { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeIdentityUser> EmployeeIdentityUsers { get; set; }

    public virtual DbSet<EmployeeSalary> EmployeeSalaries { get; set; }

    public virtual DbSet<EmployeeSchedule> EmployeeSchedules { get; set; }

    public virtual DbSet<EmploymentStatus> EmploymentStatuses { get; set; }

    public virtual DbSet<InventoryBalance> InventoryBalances { get; set; }

    public virtual DbSet<InventoryItemDefinition> InventoryItemDefinitions { get; set; }

    public virtual DbSet<InventoryReservation> InventoryReservations { get; set; }

    public virtual DbSet<InventorySerialItem> InventorySerialItems { get; set; }

    public virtual DbSet<InventoryTransaction> InventoryTransactions { get; set; }

    public virtual DbSet<InventoryTransactionLine> InventoryTransactionLines { get; set; }

    public virtual DbSet<LayawayAccount> LayawayAccounts { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<PriceItemDefinitionEntry> PriceItemDefinitionEntries { get; set; }

    public virtual DbSet<PriceList> PriceLists { get; set; }

    public virtual DbSet<PriceProductEntry> PriceProductEntries { get; set; }

    public virtual DbSet<PriceVisualDefinitionEntry> PriceVisualDefinitionEntries { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCombo> ProductCombos { get; set; }

    public virtual DbSet<ProductComboComponent> ProductComboComponents { get; set; }

    public virtual DbSet<ProductComboImage> ProductComboImages { get; set; }

    public virtual DbSet<ProductComboImageTarget> ProductComboImageTargets { get; set; }

    public virtual DbSet<ProductComboVisualDefinition> ProductComboVisualDefinitions { get; set; }

    public virtual DbSet<ProductComboVisualDefinitionSchool> ProductComboVisualDefinitionSchools { get; set; }

    public virtual DbSet<ProductCost> ProductCosts { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductImageTarget> ProductImageTargets { get; set; }

    public virtual DbSet<ProductProvider> ProductProviders { get; set; }

    public virtual DbSet<ProductVariant> ProductVariants { get; set; }

    public virtual DbSet<ProductVariantSystem> ProductVariantSystems { get; set; }

    public virtual DbSet<ProductVisualDefinition> ProductVisualDefinitions { get; set; }

    public virtual DbSet<ProductVisualDefinitionEmbroidery> ProductVisualDefinitionEmbroideries { get; set; }

    public virtual DbSet<ProductVisualDefinitionProvider> ProductVisualDefinitionProviders { get; set; }

    public virtual DbSet<ProductVisualDefinitionSchool> ProductVisualDefinitionSchools { get; set; }

    public virtual DbSet<Provider> Providers { get; set; }

    public virtual DbSet<ProviderAddress> ProviderAddresses { get; set; }

    public virtual DbSet<ProviderBankAccount> ProviderBankAccounts { get; set; }

    public virtual DbSet<ProviderContact> ProviderContacts { get; set; }

    public virtual DbSet<ProviderPayable> ProviderPayables { get; set; }

    public virtual DbSet<ProviderPayableLine> ProviderPayableLines { get; set; }

    public virtual DbSet<ProviderPayableStatus> ProviderPayableStatuses { get; set; }

    public virtual DbSet<ProviderPayableTransaction> ProviderPayableTransactions { get; set; }

    public virtual DbSet<ProviderPayableType> ProviderPayableTypes { get; set; }

    public virtual DbSet<ProviderPaymentMethod> ProviderPaymentMethods { get; set; }

    public virtual DbSet<ProviderStatus> ProviderStatuses { get; set; }

    public virtual DbSet<PurchaseExpenseType> PurchaseExpenseTypes { get; set; }

    public virtual DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    public virtual DbSet<PurchaseOrderExpense> PurchaseOrderExpenses { get; set; }

    public virtual DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }

    public virtual DbSet<PurchaseOrderStatus> PurchaseOrderStatuses { get; set; }

    public virtual DbSet<PurchaseReceipt> PurchaseReceipts { get; set; }

    public virtual DbSet<PurchaseReceiptLine> PurchaseReceiptLines { get; set; }

    public virtual DbSet<PurchaseReceiptStatus> PurchaseReceiptStatuses { get; set; }

    public virtual DbSet<Sale> Sales { get; set; }

    public virtual DbSet<SaleLineSerialItem> SaleLineSerialItems { get; set; }

    public virtual DbSet<SalePayment> SalePayments { get; set; }

    public virtual DbSet<SaleStatus> SaleStatuses { get; set; }

    public virtual DbSet<SalesLine> SalesLines { get; set; }

    public virtual DbSet<SalesType> SalesTypes { get; set; }

    public virtual DbSet<School> Schools { get; set; }

    public virtual DbSet<SchoolLevel> SchoolLevels { get; set; }

    public virtual DbSet<SchoolVariantLink> SchoolVariantLinks { get; set; }

    public virtual DbSet<Site> Sites { get; set; }

    public virtual DbSet<Size> Sizes { get; set; }

    public virtual DbSet<SizeSystem> SizeSystems { get; set; }

    public virtual DbSet<VCurrentBasePricesItemDefinition> VCurrentBasePricesItemDefinitions { get; set; }

    public virtual DbSet<VCurrentBasePricesProduct> VCurrentBasePricesProducts { get; set; }

    public virtual DbSet<VCurrentBasePricesVisualDefinition> VCurrentBasePricesVisualDefinitions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=semaforo;Username=IOTek_Admin;Password=1234");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("btree_gist")
            .HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("accounts_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.CurrencyCode).HasDefaultValueSql("'MXN'::character varying");
            entity.Property(e => e.OpeningDate).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.AccountStatus).WithMany(p => p.Accounts)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("accounts_account_status_id_fkey");

            entity.HasOne(d => d.AccountType).WithMany(p => p.Accounts)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("accounts_account_type_id_fkey");

            entity.HasOne(d => d.Client).WithMany(p => p.Accounts)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("accounts_client_id_fkey");

            entity.HasOne(d => d.OpenedByEmployee).WithMany(p => p.Accounts)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("accounts_opened_by_employee_id_fkey");

            entity.HasOne(d => d.Site).WithMany(p => p.Accounts)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("accounts_site_id_fkey");
        });

        modelBuilder.Entity<AccountInstallment>(entity =>
        {
            entity.HasKey(e => e.AccountInstallmentId).HasName("account_installments_pkey");

            entity.HasOne(d => d.Account).WithMany(p => p.AccountInstallments).HasConstraintName("account_installments_account_id_fkey");
        });

        modelBuilder.Entity<AccountItem>(entity =>
        {
            entity.HasKey(e => e.AccountItemId).HasName("account_items_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Account).WithMany(p => p.AccountItems).HasConstraintName("account_items_account_id_fkey");

            entity.HasOne(d => d.InventoryItemDefinition).WithMany(p => p.AccountItems)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("account_items_inventory_item_definition_id_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.AccountItems)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("account_items_product_id_fkey");

            entity.HasOne(d => d.ProductVisualDefinition).WithMany(p => p.AccountItems)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("account_items_product_visual_definition_id_fkey");
        });

        modelBuilder.Entity<AccountItemSerialItem>(entity =>
        {
            entity.HasKey(e => new { e.AccountItemId, e.InventorySerialItemId }).HasName("account_item_serial_items_pkey");

            entity.HasOne(d => d.AccountItem).WithMany(p => p.AccountItemSerialItems).HasConstraintName("account_item_serial_items_account_item_id_fkey");

            entity.HasOne(d => d.InventorySerialItem).WithOne(p => p.AccountItemSerialItem)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("account_item_serial_items_inventory_serial_item_id_fkey");
        });

        modelBuilder.Entity<AccountStatus>(entity =>
        {
            entity.HasKey(e => e.AccountStatusId).HasName("account_statuses_pkey");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<AccountTransaction>(entity =>
        {
            entity.HasKey(e => e.AccountTransactionId).HasName("account_transactions_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.TransactionDate).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Account).WithMany(p => p.AccountTransactions).HasConstraintName("account_transactions_account_id_fkey");

            entity.HasOne(d => d.CreatedByEmployee).WithMany(p => p.AccountTransactions)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("account_transactions_created_by_employee_id_fkey");
        });

        modelBuilder.Entity<AccountType>(entity =>
        {
            entity.HasKey(e => e.AccountTypeId).HasName("account_types_pkey");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
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

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("attachments_pkey");

            entity.HasIndex(e => e.Sha256, "ux_attachments_sha256")
                .IsUnique()
                .HasFilter("(sha256 IS NOT NULL)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Sha256).IsFixedLength();
        });

        modelBuilder.Entity<AttachmentLink>(entity =>
        {
            entity.HasKey(e => e.AttachmentLinkId).HasName("attachment_links_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Attachment).WithMany(p => p.AttachmentLinks).HasConstraintName("fk_attachment_links_attachment");
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
            entity.Property(e => e.LastModify).HasDefaultValueSql("now()");

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

        modelBuilder.Entity<CreditAccount>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("credit_accounts_pkey");

            entity.Property(e => e.AccountId).ValueGeneratedNever();

            entity.HasOne(d => d.Account).WithOne(p => p.CreditAccount).HasConstraintName("credit_accounts_account_id_fkey");
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

            entity.HasOne(d => d.EmploymentStatus).WithMany(p => p.Employees)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employees_employment_status_id_fkey");
        });

        modelBuilder.Entity<EmployeeIdentityUser>(entity =>
        {
            entity.HasKey(e => e.AspnetUserId).HasName("employee_identity_users_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.AspnetUser).WithOne(p => p.EmployeeIdentityUser).HasConstraintName("employee_identity_users_aspnet_user_id_fkey");

            entity.HasOne(d => d.Employee).WithOne(p => p.EmployeeIdentityUser).HasConstraintName("employee_identity_users_employee_id_fkey");
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

        modelBuilder.Entity<EmploymentStatus>(entity =>
        {
            entity.HasKey(e => e.EmploymentStatusId).HasName("employment_statuses_pkey");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<InventoryBalance>(entity =>
        {
            entity.HasKey(e => new { e.SiteId, e.InventoryItemDefinitionId }).HasName("inventory_balances_pkey");

            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.InventoryItemDefinition).WithMany(p => p.InventoryBalances)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_balances_inventory_item_definition_id_fkey");

            entity.HasOne(d => d.Site).WithMany(p => p.InventoryBalances)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_balances_site_id_fkey");
        });

        modelBuilder.Entity<InventoryItemDefinition>(entity =>
        {
            entity.HasKey(e => e.InventoryItemDefinitionId).HasName("inventory_item_definitions_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Product).WithMany(p => p.InventoryItemDefinitions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_item_definitions_product_id_fkey");

            entity.HasOne(d => d.ProductVisualDefinition).WithMany(p => p.InventoryItemDefinitions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("inventory_item_definitions_visual_def_fkey");

            entity.HasOne(d => d.Size).WithMany(p => p.InventoryItemDefinitions).HasConstraintName("inventory_item_definitions_size_id_fkey");

            entity.HasMany(d => d.ProductVariants).WithMany(p => p.InventoryItemDefinitions)
                .UsingEntity<Dictionary<string, object>>(
                    "InventoryItemDefinitionVariant",
                    r => r.HasOne<ProductVariant>().WithMany()
                        .HasForeignKey("ProductVariantId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("inventory_item_definition_variants_product_variant_id_fkey"),
                    l => l.HasOne<InventoryItemDefinition>().WithMany()
                        .HasForeignKey("InventoryItemDefinitionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("inventory_item_definition_variants_inventory_item_definition_va"),
                    j =>
                    {
                        j.HasKey("InventoryItemDefinitionId", "ProductVariantId").HasName("inventory_item_definition_variants_pkey");
                        j.ToTable("inventory_item_definition_variants");
                        j.IndexerProperty<int>("InventoryItemDefinitionId").HasColumnName("inventory_item_definition_id");
                        j.IndexerProperty<int>("ProductVariantId").HasColumnName("product_variant_id");
                    });
        });

        modelBuilder.Entity<InventoryReservation>(entity =>
        {
            entity.HasKey(e => e.InventoryReservationId).HasName("inventory_reservations_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.InventoryItemDefinition).WithMany(p => p.InventoryReservations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_reservations_inventory_item_definition_id");

            entity.HasOne(d => d.Site).WithMany(p => p.InventoryReservations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_reservations_site_id_fkey");

            entity.HasMany(d => d.InventorySerialItems).WithMany(p => p.InventoryReservations)
                .UsingEntity<Dictionary<string, object>>(
                    "InventoryReservationSerialItem",
                    r => r.HasOne<InventorySerialItem>().WithMany()
                        .HasForeignKey("InventorySerialItemId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("inventory_reservation_serial_items_inventory_serial_item_id_fke"),
                    l => l.HasOne<InventoryReservation>().WithMany()
                        .HasForeignKey("InventoryReservationId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("inventory_reservation_serial_items_inventory_reservation_id_fke"),
                    j =>
                    {
                        j.HasKey("InventoryReservationId", "InventorySerialItemId").HasName("inventory_reservation_serial_items_pkey");
                        j.ToTable("inventory_reservation_serial_items");
                        j.IndexerProperty<long>("InventoryReservationId").HasColumnName("inventory_reservation_id");
                        j.IndexerProperty<long>("InventorySerialItemId").HasColumnName("inventory_serial_item_id");
                    });
        });

        modelBuilder.Entity<InventorySerialItem>(entity =>
        {
            entity.HasKey(e => e.InventorySerialItemId).HasName("inventory_serial_items_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Status).HasDefaultValue((short)1);

            entity.HasOne(d => d.CurrentSite).WithMany(p => p.InventorySerialItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_serial_items_current_site_id_fkey");

            entity.HasOne(d => d.InventoryItemDefinition).WithMany(p => p.InventorySerialItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_serial_items_inventory_item_definition_id_fkey");
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(e => e.InventoryTransactionId).HasName("inventory_transactions_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.TransactionDate).HasDefaultValueSql("now()");

            entity.HasOne(d => d.PurchaseReceipt).WithMany(p => p.InventoryTransactions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_inventory_transactions_purchase_receipt");
        });

        modelBuilder.Entity<InventoryTransactionLine>(entity =>
        {
            entity.HasKey(e => e.InventoryTransactionLineId).HasName("inventory_transaction_lines_pkey");

            entity.HasOne(d => d.InventoryItemDefinition).WithMany(p => p.InventoryTransactionLines)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_transaction_lines_inventory_item_definition_id_fkey");

            entity.HasOne(d => d.InventoryTransaction).WithMany(p => p.InventoryTransactionLines)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_transaction_lines_inventory_transaction_id_fkey");

            entity.HasOne(d => d.PurchaseReceiptLine).WithMany(p => p.InventoryTransactionLines)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_inventory_transaction_lines_purchase_receipt_line");

            entity.HasOne(d => d.SaleLine).WithMany(p => p.InventoryTransactionLines).HasConstraintName("fk_inventory_transaction_lines_sale_line");

            entity.HasOne(d => d.Site).WithMany(p => p.InventoryTransactionLineSites)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_transaction_lines_site_id_fkey");

            entity.HasOne(d => d.SourceSite).WithMany(p => p.InventoryTransactionLineSourceSites).HasConstraintName("inventory_transaction_lines_source_site_id_fkey");

            entity.HasOne(d => d.TargetSite).WithMany(p => p.InventoryTransactionLineTargetSites).HasConstraintName("nventory_transaction_lines_target_site_id_fkey");

            entity.HasMany(d => d.InventorySerialItems).WithMany(p => p.InventoryTransactionLines)
                .UsingEntity<Dictionary<string, object>>(
                    "InventorySerialItemMove",
                    r => r.HasOne<InventorySerialItem>().WithMany()
                        .HasForeignKey("InventorySerialItemId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("inventory_serial_item_moves_inventory_serial_item_id_fkey"),
                    l => l.HasOne<InventoryTransactionLine>().WithMany()
                        .HasForeignKey("InventoryTransactionLineId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("inventory_serial_item_moves_inventory_transaction_line_id_fkey"),
                    j =>
                    {
                        j.HasKey("InventoryTransactionLineId", "InventorySerialItemId").HasName("inventory_serial_item_moves_pkey");
                        j.ToTable("inventory_serial_item_moves");
                        j.IndexerProperty<long>("InventoryTransactionLineId")
                            .ValueGeneratedOnAdd()
                            .HasColumnName("inventory_transaction_line_id");
                        j.IndexerProperty<long>("InventorySerialItemId")
                            .ValueGeneratedOnAdd()
                            .HasColumnName("inventory_serial_item_id");
                    });
        });

        modelBuilder.Entity<LayawayAccount>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("layaway_accounts_pkey");

            entity.Property(e => e.AccountId).ValueGeneratedNever();

            entity.HasOne(d => d.Account).WithOne(p => p.LayawayAccount).HasConstraintName("layaway_accounts_account_id_fkey");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.PaymentMethodId).HasName("payment_methods_pkey");

            entity.Property(e => e.Active).HasDefaultValue(true);
        });

        modelBuilder.Entity<PriceItemDefinitionEntry>(entity =>
        {
            entity.HasKey(e => e.PriceEntryId).HasName("price_item_definition_entries_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.PriceKind).HasDefaultValueSql("'BASE'::character varying");

            entity.HasOne(d => d.InventoryItemDefinition).WithMany(p => p.PriceItemDefinitionEntries).HasConstraintName("price_item_definition_entries_inventory_item_definition_id_fkey");

            entity.HasOne(d => d.PriceList).WithMany(p => p.PriceItemDefinitionEntries).HasConstraintName("price_item_definition_entries_price_list_id_fkey");
        });

        modelBuilder.Entity<PriceList>(entity =>
        {
            entity.HasKey(e => e.PriceListId).HasName("price_lists_pkey");

            entity.HasIndex(e => e.IsDefault, "ux_price_lists_one_default")
                .IsUnique()
                .HasFilter("(is_default = true)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Currency).HasDefaultValueSql("'MXN'::character varying");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<PriceProductEntry>(entity =>
        {
            entity.HasKey(e => e.PriceEntryId).HasName("price_product_entries_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.PriceKind).HasDefaultValueSql("'BASE'::character varying");

            entity.HasOne(d => d.PriceList).WithMany(p => p.PriceProductEntries).HasConstraintName("price_product_entries_price_list_id_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.PriceProductEntries).HasConstraintName("price_product_entries_product_id_fkey");
        });

        modelBuilder.Entity<PriceVisualDefinitionEntry>(entity =>
        {
            entity.HasKey(e => e.PriceEntryId).HasName("price_visual_definition_entries_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.PriceKind).HasDefaultValueSql("'BASE'::character varying");

            entity.HasOne(d => d.PriceList).WithMany(p => p.PriceVisualDefinitionEntries).HasConstraintName("price_visual_definition_entries_price_list_id_fkey");

            entity.HasOne(d => d.ProductVisualDefinition).WithMany(p => p.PriceVisualDefinitionEntries).HasConstraintName("price_visual_definition_entri_product_visual_definition_id_fkey");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("products_pkey");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Brand).WithMany(p => p.Products).HasConstraintName("products_brand_id_fkey");

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
        });

        modelBuilder.Entity<ProductCombo>(entity =>
        {
            entity.HasKey(e => e.ProductComboId).HasName("product_combos_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<ProductComboComponent>(entity =>
        {
            entity.HasKey(e => e.ProductComboComponentId).HasName("product_combo_components_pkey");

            entity.HasIndex(e => new { e.ProductComboVisualDefinitionId, e.EmbroideryId, e.Placement }, "ux_combo_components_embroidery")
                .IsUnique()
                .HasFilter("((component_type)::text = 'EMBROIDERY'::text)");

            entity.HasIndex(e => new { e.ProductComboVisualDefinitionId, e.ProductId }, "ux_combo_components_product")
                .IsUnique()
                .HasFilter("((component_type)::text = 'PRODUCT'::text)");

            entity.HasIndex(e => new { e.ProductComboVisualDefinitionId, e.ProductVisualDefinitionId }, "ux_combo_components_visualdef")
                .IsUnique()
                .HasFilter("((component_type)::text = 'PRODUCT_VISUAL_DEFINITION'::text)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsRequired).HasDefaultValue(true);
            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Embroidery).WithMany(p => p.ProductComboComponents)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("product_combo_components_embroidery_id_fkey");

            entity.HasOne(d => d.ProductComboVisualDefinition).WithMany(p => p.ProductComboComponents).HasConstraintName("product_combo_components_product_combo_visual_definition_i_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductComboComponents)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("product_combo_components_product_id_fkey");

            entity.HasOne(d => d.ProductVisualDefinition).WithMany(p => p.ProductComboComponents)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("product_combo_components_product_visual_definition_id_fkey");
        });

        modelBuilder.Entity<ProductComboImage>(entity =>
        {
            entity.HasKey(e => e.ProductComboImageId).HasName("product_combo_images_pkey");

            entity.HasIndex(e => new { e.Sha256, e.ImageRole }, "ux_combo_images_sha256_role")
                .IsUnique()
                .HasFilter("(sha256 IS NOT NULL)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.ImageRole).HasDefaultValueSql("'ORIGINAL'::character varying");
            entity.Property(e => e.Sha256).IsFixedLength();
        });

        modelBuilder.Entity<ProductComboImageTarget>(entity =>
        {
            entity.HasKey(e => e.ProductComboImageTargetId).HasName("product_combo_image_targets_pkey");

            entity.HasIndex(e => e.ProductComboVisualDefinitionId, "ux_combo_image_targets_primary")
                .IsUnique()
                .HasFilter("(is_primary = true)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.ProductComboImage).WithMany(p => p.ProductComboImageTargets).HasConstraintName("product_combo_image_targets_product_combo_image_id_fkey");

            entity.HasOne(d => d.ProductComboVisualDefinition).WithOne(p => p.ProductComboImageTarget).HasConstraintName("product_combo_image_targets_product_combo_visual_definitio_fkey");
        });

        modelBuilder.Entity<ProductComboVisualDefinition>(entity =>
        {
            entity.HasKey(e => e.ProductComboVisualDefinitionId).HasName("product_combo_visual_definitions_pkey");

            entity.Property(e => e.ProductComboVisualDefinitionId).HasDefaultValueSql("nextval('product_combo_visual_definiti_product_combo_visual_definiti_seq'::regclass)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.DiscountType).HasDefaultValueSql("'NONE'::character varying");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.PriceList).WithMany(p => p.ProductComboVisualDefinitions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("product_combo_visual_definitions_price_list_id_fkey");

            entity.HasOne(d => d.ProductCombo).WithMany(p => p.ProductComboVisualDefinitions).HasConstraintName("product_combo_visual_definitions_product_combo_id_fkey");
        });

        modelBuilder.Entity<ProductComboVisualDefinitionSchool>(entity =>
        {
            entity.HasKey(e => new { e.ProductComboVisualDefinitionId, e.SchoolId }).HasName("product_combo_visual_definition_schools_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.ProductComboVisualDefinition).WithMany(p => p.ProductComboVisualDefinitionSchools).HasConstraintName("pcvs_combo_visual_definition_id_fkey");

            entity.HasOne(d => d.School).WithMany(p => p.ProductComboVisualDefinitionSchools)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("pcvs_school_id_fkey");
        });

        modelBuilder.Entity<ProductCost>(entity =>
        {
            entity.HasKey(e => e.ProductCostId).HasName("product_costs_pkey");

            entity.ToTable("product_costs", tb => tb.HasComment("costos de los productos"));

            entity.HasOne(d => d.Product).WithMany(p => p.ProductCosts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_costs_product_id_fkey");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ProductImageId).HasName("product_images_pkey");

            entity.HasIndex(e => new { e.Sha256, e.ImageRole }, "ux_product_images_sha256_role")
                .IsUnique()
                .HasFilter("(sha256 IS NOT NULL)");

            entity.HasIndex(e => e.StorageKey, "ux_product_images_storage_key")
                .IsUnique()
                .HasFilter("(storage_key IS NOT NULL)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ImageRole).HasDefaultValueSql("'ORIGINAL'::character varying");
            entity.Property(e => e.Sha256).IsFixedLength();
        });

        modelBuilder.Entity<ProductImageTarget>(entity =>
        {
            entity.HasKey(e => e.ProductImageTargetId).HasName("product_image_targets_pkey");

            entity.HasIndex(e => e.InventoryItemDefinitionId, "ux_product_image_targets_primary_itemdef")
                .IsUnique()
                .HasFilter("(((target_type)::text = 'ITEM_DEFINITION'::text) AND (is_primary = true))");

            entity.HasIndex(e => e.ProductId, "ux_product_image_targets_primary_product")
                .IsUnique()
                .HasFilter("(((target_type)::text = 'PRODUCT'::text) AND (is_primary = true))");

            entity.HasIndex(e => e.ProductVisualDefinitionId, "ux_product_image_targets_primary_visualdef")
                .IsUnique()
                .HasFilter("(((target_type)::text = 'VISUAL_DEFINITION'::text) AND (is_primary = true))");

            entity.HasIndex(e => new { e.ProductImageId, e.InventoryItemDefinitionId }, "ux_product_image_targets_unique_itemdef")
                .IsUnique()
                .HasFilter("((target_type)::text = 'ITEM_DEFINITION'::text)");

            entity.HasIndex(e => new { e.ProductImageId, e.ProductId }, "ux_product_image_targets_unique_product")
                .IsUnique()
                .HasFilter("((target_type)::text = 'PRODUCT'::text)");

            entity.HasIndex(e => new { e.ProductImageId, e.ProductVisualDefinitionId }, "ux_product_image_targets_unique_visualdef")
                .IsUnique()
                .HasFilter("((target_type)::text = 'VISUAL_DEFINITION'::text)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.InventoryItemDefinition).WithOne(p => p.ProductImageTarget)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("product_image_targets_inventory_item_definition_id_fkey");

            entity.HasOne(d => d.Product).WithOne(p => p.ProductImageTarget)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("product_image_targets_product_id_fkey");

            entity.HasOne(d => d.ProductImage).WithMany(p => p.ProductImageTargets).HasConstraintName("product_image_targets_product_image_id_fkey");

            entity.HasOne(d => d.ProductVisualDefinition).WithOne(p => p.ProductImageTarget)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("product_image_targets_visual_def_fkey");
        });

        modelBuilder.Entity<ProductProvider>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.ProviderId }).HasName("pk_product_providers");

            entity.HasIndex(e => e.ProductId, "ux_product_providers_primary")
                .IsUnique()
                .HasFilter("(is_primary = true)");

            entity.HasOne(d => d.Product).WithOne(p => p.ProductProvider).HasConstraintName("fk_product_providers_product");

            entity.HasOne(d => d.Provider).WithMany(p => p.ProductProviders).HasConstraintName("fk_product_providers_provider");
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

        modelBuilder.Entity<ProductVisualDefinition>(entity =>
        {
            entity.HasKey(e => e.ProductVisualDefinitionId).HasName("product_visual_definitions_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.VariantsHash).IsFixedLength();

            entity.HasOne(d => d.Product).WithMany(p => p.ProductVisualDefinitions).HasConstraintName("product_visual_definitions_product_id_fkey");

            entity.HasMany(d => d.ProductVariants).WithMany(p => p.ProductVisualDefinitions)
                .UsingEntity<Dictionary<string, object>>(
                    "ProductVisualDefinitionVariant",
                    r => r.HasOne<ProductVariant>().WithMany()
                        .HasForeignKey("ProductVariantId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("product_visual_definition_variants_product_variant_id_fkey"),
                    l => l.HasOne<ProductVisualDefinition>().WithMany()
                        .HasForeignKey("ProductVisualDefinitionId")
                        .HasConstraintName("product_visual_definition_var_product_visual_definition_id_fkey"),
                    j =>
                    {
                        j.HasKey("ProductVisualDefinitionId", "ProductVariantId").HasName("product_visual_definition_variants_pkey");
                        j.ToTable("product_visual_definition_variants");
                        j.HasIndex(new[] { "ProductVariantId" }, "ix_visual_def_variants_variant");
                        j.IndexerProperty<long>("ProductVisualDefinitionId").HasColumnName("product_visual_definition_id");
                        j.IndexerProperty<int>("ProductVariantId").HasColumnName("product_variant_id");
                    });
        });

        modelBuilder.Entity<ProductVisualDefinitionEmbroidery>(entity =>
        {
            entity.HasKey(e => new { e.ProductVisualDefinitionId, e.EmbroideryId, e.Placement }).HasName("product_visual_definition_embroideries_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsRequired).HasDefaultValue(true);

            entity.HasOne(d => d.Embroidery).WithMany(p => p.ProductVisualDefinitionEmbroideries)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("pvde_embroidery_fkey");

            entity.HasOne(d => d.ProductVisualDefinition).WithMany(p => p.ProductVisualDefinitionEmbroideries).HasConstraintName("pvde_visual_def_fkey");
        });

        modelBuilder.Entity<ProductVisualDefinitionProvider>(entity =>
        {
            entity.HasKey(e => new { e.ProductVisualDefinitionId, e.ProviderId }).HasName("pk_product_visual_definition_providers");

            entity.HasIndex(e => e.ProductVisualDefinitionId, "ux_pvdp_primary")
                .IsUnique()
                .HasFilter("(is_primary = true)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.ProductVisualDefinition).WithOne(p => p.ProductVisualDefinitionProvider).HasConstraintName("fk_pvdp_visual_definition");

            entity.HasOne(d => d.Provider).WithMany(p => p.ProductVisualDefinitionProviders).HasConstraintName("fk_pvdp_provider");
        });

        modelBuilder.Entity<ProductVisualDefinitionSchool>(entity =>
        {
            entity.HasKey(e => new { e.ProductVisualDefinitionId, e.SchoolId }).HasName("product_visual_definition_schools_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.ProductVisualDefinition).WithMany(p => p.ProductVisualDefinitionSchools).HasConstraintName("product_visual_definition_sch_product_visual_definition_id_fkey");

            entity.HasOne(d => d.School).WithMany(p => p.ProductVisualDefinitionSchools).HasConstraintName("product_visual_definition_schools_school_id_fkey");
        });

        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(e => e.ProviderId).HasName("providers_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.ProviderStatus).WithMany(p => p.Providers)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_providers_status");
        });

        modelBuilder.Entity<ProviderAddress>(entity =>
        {
            entity.HasKey(e => e.ProviderAddressId).HasName("provider_addresses_pkey");

            entity.HasIndex(e => e.ProviderId, "ux_provider_addresses_primary")
                .IsUnique()
                .HasFilter("(is_primary = true)");

            entity.Property(e => e.Country).HasDefaultValueSql("'MEXICO'::character varying");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Provider).WithOne(p => p.ProviderAddress).HasConstraintName("fk_provider_addresses_provider");
        });

        modelBuilder.Entity<ProviderBankAccount>(entity =>
        {
            entity.HasKey(e => e.ProviderBankAccountId).HasName("provider_bank_accounts_pkey");

            entity.HasIndex(e => e.ProviderId, "ux_provider_bank_accounts_primary")
                .IsUnique()
                .HasFilter("(is_primary = true)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.CurrencyCode).HasDefaultValueSql("'MXN'::character varying");

            entity.HasOne(d => d.Provider).WithOne(p => p.ProviderBankAccount).HasConstraintName("fk_provider_bank_accounts_provider");
        });

        modelBuilder.Entity<ProviderContact>(entity =>
        {
            entity.HasKey(e => e.ProviderContactId).HasName("provider_contacts_pkey");

            entity.HasIndex(e => e.ProviderId, "ux_provider_contacts_primary")
                .IsUnique()
                .HasFilter("(is_primary = true)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Provider).WithOne(p => p.ProviderContact).HasConstraintName("fk_provider_contacts_provider");
        });

        modelBuilder.Entity<ProviderPayable>(entity =>
        {
            entity.HasKey(e => e.ProviderPayableId).HasName("provider_payables_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.CurrencyCode).HasDefaultValueSql("'MXN'::character varying");
            entity.Property(e => e.DocumentDate).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.OpenedByEmployee).WithMany(p => p.ProviderPayables)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_provider_payables_employee");

            entity.HasOne(d => d.Provider).WithMany(p => p.ProviderPayables)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_provider_payables_provider");

            entity.HasOne(d => d.ProviderPayableStatus).WithMany(p => p.ProviderPayables)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_provider_payables_status");

            entity.HasOne(d => d.ProviderPayableType).WithMany(p => p.ProviderPayables)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_provider_payables_type");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.ProviderPayables)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_provider_payables_purchase_order");

            entity.HasOne(d => d.PurchaseReceipt).WithMany(p => p.ProviderPayables)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_provider_payables_purchase_receipt");

            entity.HasOne(d => d.Site).WithMany(p => p.ProviderPayables)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_provider_payables_site");
        });

        modelBuilder.Entity<ProviderPayableLine>(entity =>
        {
            entity.HasKey(e => e.ProviderPayableLineId).HasName("provider_payable_lines_pkey");

            entity.HasOne(d => d.InventoryItemDefinition).WithMany(p => p.ProviderPayableLines)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_provider_payable_lines_inventory_item_definition");

            entity.HasOne(d => d.Product).WithMany(p => p.ProviderPayableLines)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_provider_payable_lines_product");

            entity.HasOne(d => d.ProductVisualDefinition).WithMany(p => p.ProviderPayableLines)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_provider_payable_lines_visual_definition");

            entity.HasOne(d => d.ProviderPayable).WithMany(p => p.ProviderPayableLines).HasConstraintName("fk_provider_payable_lines_payable");

            entity.HasOne(d => d.PurchaseOrderLine).WithMany(p => p.ProviderPayableLines)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_provider_payable_lines_order_line");

            entity.HasOne(d => d.PurchaseReceiptLine).WithMany(p => p.ProviderPayableLines)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_provider_payable_lines_receipt_line");
        });

        modelBuilder.Entity<ProviderPayableStatus>(entity =>
        {
            entity.HasKey(e => e.ProviderPayableStatusId).HasName("provider_payable_statuses_pkey");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<ProviderPayableTransaction>(entity =>
        {
            entity.HasKey(e => e.ProviderPayableTransactionId).HasName("provider_payable_transactions_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.TransactionDate).HasDefaultValueSql("now()");

            entity.HasOne(d => d.CreatedByEmployee).WithMany(p => p.ProviderPayableTransactions)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_provider_payable_transactions_employee");

            entity.HasOne(d => d.ProviderPayable).WithMany(p => p.ProviderPayableTransactions).HasConstraintName("fk_provider_payable_transactions_payable");

            entity.HasOne(d => d.ProviderPaymentMethod).WithMany(p => p.ProviderPayableTransactions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_provider_payable_transactions_method");
        });

        modelBuilder.Entity<ProviderPayableType>(entity =>
        {
            entity.HasKey(e => e.ProviderPayableTypeId).HasName("provider_payable_types_pkey");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<ProviderPaymentMethod>(entity =>
        {
            entity.HasKey(e => e.ProviderPaymentMethodId).HasName("provider_payment_methods_pkey");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<ProviderStatus>(entity =>
        {
            entity.HasKey(e => e.ProviderStatusId).HasName("provider_statuses_pkey");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<PurchaseExpenseType>(entity =>
        {
            entity.HasKey(e => e.PurchaseExpenseTypeId).HasName("purchase_expense_types_pkey");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.PurchaseOrderId).HasName("purchase_orders_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.CurrencyCode).HasDefaultValueSql("'MXN'::character varying");
            entity.Property(e => e.OrderDate).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.CreatedByEmployee).WithMany(p => p.PurchaseOrders)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_purchase_orders_employee");

            entity.HasOne(d => d.Provider).WithMany(p => p.PurchaseOrders)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_purchase_orders_provider");

            entity.HasOne(d => d.PurchaseOrderStatus).WithMany(p => p.PurchaseOrders)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_purchase_orders_status");

            entity.HasOne(d => d.Site).WithMany(p => p.PurchaseOrders)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_purchase_orders_site");
        });

        modelBuilder.Entity<PurchaseOrderExpense>(entity =>
        {
            entity.HasKey(e => e.PurchaseOrderExpenseId).HasName("purchase_order_expenses_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.CurrencyCode).HasDefaultValueSql("'MXN'::character varying");
            entity.Property(e => e.ExpenseDate).HasDefaultValueSql("now()");

            entity.HasOne(d => d.ProviderPayable).WithMany(p => p.PurchaseOrderExpenses)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_purchase_order_expenses_payable");

            entity.HasOne(d => d.PurchaseExpenseType).WithMany(p => p.PurchaseOrderExpenses)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_purchase_order_expenses_type");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.PurchaseOrderExpenses).HasConstraintName("fk_purchase_order_expenses_order");
        });

        modelBuilder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.HasKey(e => e.PurchaseOrderLineId).HasName("purchase_order_lines_pkey");

            entity.HasOne(d => d.InventoryItemDefinition).WithMany(p => p.PurchaseOrderLines)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_purchase_order_lines_inventory_item_definition");

            entity.HasOne(d => d.Product).WithMany(p => p.PurchaseOrderLines)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_purchase_order_lines_product");

            entity.HasOne(d => d.ProductVisualDefinition).WithMany(p => p.PurchaseOrderLines)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_purchase_order_lines_visual_definition");

            entity.HasOne(d => d.Provider).WithMany(p => p.PurchaseOrderLines)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_purchase_order_lines_provider");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.PurchaseOrderLines).HasConstraintName("fk_purchase_order_lines_order");
        });

        modelBuilder.Entity<PurchaseOrderStatus>(entity =>
        {
            entity.HasKey(e => e.PurchaseOrderStatusId).HasName("purchase_order_statuses_pkey");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<PurchaseReceipt>(entity =>
        {
            entity.HasKey(e => e.PurchaseReceiptId).HasName("purchase_receipts_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.ReceiptDate).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Provider).WithMany(p => p.PurchaseReceipts)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_purchase_receipts_provider");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.PurchaseReceipts)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_purchase_receipts_order");

            entity.HasOne(d => d.PurchaseReceiptStatus).WithMany(p => p.PurchaseReceipts)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_purchase_receipts_status");

            entity.HasOne(d => d.ReceivedByEmployee).WithMany(p => p.PurchaseReceipts)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_purchase_receipts_employee");

            entity.HasOne(d => d.Site).WithMany(p => p.PurchaseReceipts)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_purchase_receipts_site");
        });

        modelBuilder.Entity<PurchaseReceiptLine>(entity =>
        {
            entity.HasKey(e => e.PurchaseReceiptLineId).HasName("purchase_receipt_lines_pkey");

            entity.HasOne(d => d.InventoryItemDefinition).WithMany(p => p.PurchaseReceiptLines)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_purchase_receipt_lines_inventory_item_definition");

            entity.HasOne(d => d.Product).WithMany(p => p.PurchaseReceiptLines)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_purchase_receipt_lines_product");

            entity.HasOne(d => d.ProductVisualDefinition).WithMany(p => p.PurchaseReceiptLines)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_purchase_receipt_lines_visual_definition");

            entity.HasOne(d => d.PurchaseOrderLine).WithMany(p => p.PurchaseReceiptLines)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_purchase_receipt_lines_order_line");

            entity.HasOne(d => d.PurchaseReceipt).WithMany(p => p.PurchaseReceiptLines).HasConstraintName("fk_purchase_receipt_lines_receipt");
        });

        modelBuilder.Entity<PurchaseReceiptStatus>(entity =>
        {
            entity.HasKey(e => e.PurchaseReceiptStatusId).HasName("purchase_receipt_statuses_pkey");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.SaleId).HasName("sales_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.SaleDate).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Account).WithMany(p => p.Sales).HasConstraintName("fk_sales_account");

            entity.HasOne(d => d.Client).WithMany(p => p.Sales).HasConstraintName("fk_sales_client");

            entity.HasOne(d => d.Employee).WithMany(p => p.Sales)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sales_employee");

            entity.HasOne(d => d.SaleStatus).WithMany(p => p.Sales)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sales_sale_status");

            entity.HasOne(d => d.SaleType).WithMany(p => p.Sales)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sales_sale_type");

            entity.HasOne(d => d.Site).WithMany(p => p.Sales)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sales_site");
        });

        modelBuilder.Entity<SaleLineSerialItem>(entity =>
        {
            entity.HasKey(e => new { e.SaleLineId, e.InventorySerialItemId }).HasName("pk_sale_line_serial_items");

            entity.HasOne(d => d.InventorySerialItem).WithOne(p => p.SaleLineSerialItem)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sale_line_serial_items_inventory_serial_item");

            entity.HasOne(d => d.SaleLine).WithMany(p => p.SaleLineSerialItems).HasConstraintName("fk_sale_line_serial_items_sale_line");
        });

        modelBuilder.Entity<SalePayment>(entity =>
        {
            entity.HasKey(e => e.SalePaymentId).HasName("sale_payments_pkey");

            entity.Property(e => e.PaymentDate).HasDefaultValueSql("now()");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.SalePayments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sale_payments_payment_method");

            entity.HasOne(d => d.Sale).WithMany(p => p.SalePayments).HasConstraintName("fk_sale_payments_sale");
        });

        modelBuilder.Entity<SaleStatus>(entity =>
        {
            entity.HasKey(e => e.SaleStatusId).HasName("sale_statuses_pkey");

            entity.Property(e => e.Active).HasDefaultValue(true);
        });

        modelBuilder.Entity<SalesLine>(entity =>
        {
            entity.HasKey(e => e.SaleLineId).HasName("sales_lines_pkey");

            entity.HasOne(d => d.InventoryItemDefinition).WithMany(p => p.SalesLines).HasConstraintName("fk_sales_lines_inventory_item_definition");

            entity.HasOne(d => d.ProductComboVisualDefinition).WithMany(p => p.SalesLines).HasConstraintName("fk_sales_lines_product_combo_visual_definition");

            entity.HasOne(d => d.Product).WithMany(p => p.SalesLines).HasConstraintName("fk_sales_lines_product");

            entity.HasOne(d => d.ProductVisualDefinition).WithMany(p => p.SalesLines).HasConstraintName("fk_sales_lines_product_visual_definition");

            entity.HasOne(d => d.Sale).WithMany(p => p.SalesLines).HasConstraintName("fk_sales_lines_sale");

            entity.HasOne(d => d.Size).WithMany(p => p.SalesLines).HasConstraintName("fk_sales_lines_size");
        });

        modelBuilder.Entity<SalesType>(entity =>
        {
            entity.HasKey(e => e.SaleTypeId).HasName("sales_types_pkey");

            entity.Property(e => e.Active).HasDefaultValue(true);
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

        modelBuilder.Entity<SchoolVariantLink>(entity =>
        {
            entity.HasKey(e => e.SchoolId).HasName("school_variant_links_pkey");

            entity.Property(e => e.SchoolId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.ProductVariant).WithOne(p => p.SchoolVariantLink)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("school_variant_links_variant_fkey");

            entity.HasOne(d => d.School).WithOne(p => p.SchoolVariantLink).HasConstraintName("school_variant_links_school_fkey");
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

        modelBuilder.Entity<VCurrentBasePricesItemDefinition>(entity =>
        {
            entity.ToView("v_current_base_prices_item_definition");
        });

        modelBuilder.Entity<VCurrentBasePricesProduct>(entity =>
        {
            entity.ToView("v_current_base_prices_product");
        });

        modelBuilder.Entity<VCurrentBasePricesVisualDefinition>(entity =>
        {
            entity.ToView("v_current_base_prices_visual_definition");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
