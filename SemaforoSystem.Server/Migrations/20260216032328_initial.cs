using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SemaforoSystem.Server.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_status",
                columns: table => new
                {
                    account_status_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("account_status_pkey", x => x.account_status_id);
                });

            migrationBuilder.CreateTable(
                name: "account_types",
                columns: table => new
                {
                    account_type_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("account_types_pkey", x => x.account_type_id);
                });

            migrationBuilder.CreateTable(
                name: "archives",
                columns: table => new
                {
                    archive_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    data = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("archives_pkey", x => x.archive_id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    brand_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    supplier_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("brands_pkey", x => x.brand_id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    create_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    enabled = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("categories_pkey", x => x.category_id);
                });

            migrationBuilder.CreateTable(
                name: "client_categories",
                columns: table => new
                {
                    client_category_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_categories_pkey", x => x.client_category_id);
                });

            migrationBuilder.CreateTable(
                name: "client_status",
                columns: table => new
                {
                    client_status_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_status_pkey", x => x.client_status_id);
                });

            migrationBuilder.CreateTable(
                name: "product_combos",
                columns: table => new
                {
                    product_combo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_combos_pkey", x => x.product_combo_id);
                });

            migrationBuilder.CreateTable(
                name: "providers",
                columns: table => new
                {
                    provider_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    phone = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    contact_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    cellphone = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    bank_accounts = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    whatsapp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    website = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    image = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("providers_pkey", x => x.provider_id);
                });

            migrationBuilder.CreateTable(
                name: "sales_types",
                columns: table => new
                {
                    sale_type_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sales_types_pkey", x => x.sale_type_id);
                });

            migrationBuilder.CreateTable(
                name: "school_levels",
                columns: table => new
                {
                    school_level_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("school_levels_pkey", x => x.school_level_id);
                });

            migrationBuilder.CreateTable(
                name: "sites",
                columns: table => new
                {
                    site_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    logo = table.Column<byte[]>(type: "bytea", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    image = table.Column<byte[]>(type: "bytea", nullable: true),
                    location = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sites_pkey", x => x.site_id);
                });

            migrationBuilder.CreateTable(
                name: "sizes",
                columns: table => new
                {
                    size_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sizes_pkey", x => x.size_id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    employee_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    appuser_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    first_last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    second_last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    birthdate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    create_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    photo = table.Column<byte[]>(type: "bytea", nullable: true),
                    address = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cellphone = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    whatsapp = table.Column<bool>(type: "boolean", nullable: true),
                    phone = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    facebook = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    facebook_profile_image = table.Column<byte[]>(type: "bytea", nullable: true),
                    health_info = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    marital_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("employees_pkey", x => x.employee_id);
                    table.ForeignKey(
                        name: "employees_AspNetUsers_fkey",
                        column: x => x.appuser_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "schools",
                columns: table => new
                {
                    school_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    school_level_id = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ciudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    principal_info = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    logo = table.Column<byte[]>(type: "bytea", nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    photo = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("schools_pkey", x => x.school_id);
                    table.ForeignKey(
                        name: "schools_school_level_id_fkey",
                        column: x => x.school_level_id,
                        principalTable: "school_levels",
                        principalColumn: "school_level_id");
                });

            migrationBuilder.CreateTable(
                name: "attendance",
                columns: table => new
                {
                    attendance_id = table.Column<int>(type: "integer", nullable: false),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    weekday = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    stop_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("attendance_pkey", x => x.attendance_id);
                    table.ForeignKey(
                        name: "attendance_employee_id_fkey",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id");
                });

            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    client_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    client_status_id = table.Column<int>(type: "integer", nullable: false),
                    client_category_id = table.Column<int>(type: "integer", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_modify = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_modified_by = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    last_name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    last_name_mother = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    gender = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    account_days_limit = table.Column<int>(type: "integer", nullable: true),
                    account_amount_limit = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    cellphone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    whatsapp = table.Column<bool>(type: "boolean", nullable: false),
                    facebook = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    facebook_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    profile_image = table.Column<byte[]>(type: "bytea", nullable: true),
                    comments = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("clients_pkey", x => x.client_id);
                    table.ForeignKey(
                        name: "clients_client_category_id_fkey",
                        column: x => x.client_category_id,
                        principalTable: "client_categories",
                        principalColumn: "client_category_id");
                    table.ForeignKey(
                        name: "clients_client_status_id_fkey",
                        column: x => x.client_status_id,
                        principalTable: "client_status",
                        principalColumn: "client_status_id");
                    table.ForeignKey(
                        name: "clients_employee_id_fkey",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id");
                });

            migrationBuilder.CreateTable(
                name: "employee_salary",
                columns: table => new
                {
                    employee_salary_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    weekday = table.Column<int>(type: "integer", nullable: true),
                    salary_day = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    salary_hour = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("employee_salary_pkey", x => x.employee_salary_id);
                    table.ForeignKey(
                        name: "employee_salary_employee_id_fkey",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id");
                });

            migrationBuilder.CreateTable(
                name: "employee_schedule",
                columns: table => new
                {
                    employee_schedule_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    weekday = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    stop_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    break_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("employee_schedule_pkey", x => x.employee_schedule_id);
                    table.ForeignKey(
                        name: "employee_schedule_employee_id_fkey",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id");
                });

            migrationBuilder.CreateTable(
                name: "provider_account",
                columns: table => new
                {
                    provider_account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    provider_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    opening_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    settlement_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    balance = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("provider_account_pkey", x => x.provider_account_id);
                    table.ForeignKey(
                        name: "provider_account_employee_id_fkey",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "provider_account_provider_id_fkey",
                        column: x => x.provider_id,
                        principalTable: "providers",
                        principalColumn: "provider_id");
                });

            migrationBuilder.CreateTable(
                name: "embroideries",
                columns: table => new
                {
                    embroidery_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    school_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    emb_file = table.Column<byte[]>(type: "bytea", nullable: true),
                    dst_file = table.Column<byte[]>(type: "bytea", nullable: true),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    stiches = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    color_secuence = table.Column<string>(type: "text", nullable: true),
                    price = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    image_design = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    image = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("embroideries_pkey", x => x.embroidery_id);
                    table.ForeignKey(
                        name: "embroideries_school_id_fkey",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "school_id");
                });

            migrationBuilder.CreateTable(
                name: "sales",
                columns: table => new
                {
                    sale_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    client_id = table.Column<int>(type: "integer", nullable: true),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    sale_type_id = table.Column<int>(type: "integer", nullable: false),
                    sale_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    notes = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    client_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    total = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sales_pkey", x => x.sale_id);
                    table.ForeignKey(
                        name: "sales_client_id_fkey",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "client_id");
                    table.ForeignKey(
                        name: "sales_employee_id_fkey",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "sales_sale_type_id_fkey",
                        column: x => x.sale_type_id,
                        principalTable: "sales_types",
                        principalColumn: "sale_type_id");
                    table.ForeignKey(
                        name: "sales_site_id_fkey",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "site_id");
                });

            migrationBuilder.CreateTable(
                name: "provider_account_payments",
                columns: table => new
                {
                    provider_account_payment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    provider_account_id = table.Column<int>(type: "integer", nullable: false),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    payment_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("provider_account_payments_pkey", x => x.provider_account_payment_id);
                    table.ForeignKey(
                        name: "provider_account_payments_employee_id_fkey",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "provider_account_payments_provider_account_id_fkey",
                        column: x => x.provider_account_id,
                        principalTable: "provider_account",
                        principalColumn: "provider_account_id");
                });

            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    account_type_id = table.Column<int>(type: "integer", nullable: false),
                    sale_id = table.Column<int>(type: "integer", nullable: false),
                    account_status_id = table.Column<int>(type: "integer", nullable: false),
                    opening_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    settlement_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    balance = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("accounts_pkey", x => x.account_id);
                    table.ForeignKey(
                        name: "accounts_account_status_id_fkey",
                        column: x => x.account_status_id,
                        principalTable: "account_status",
                        principalColumn: "account_status_id");
                    table.ForeignKey(
                        name: "accounts_account_type_id_fkey",
                        column: x => x.account_type_id,
                        principalTable: "account_types",
                        principalColumn: "account_type_id");
                    table.ForeignKey(
                        name: "accounts_client_id_fkey",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "client_id");
                    table.ForeignKey(
                        name: "accounts_employee_id_fkey",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "accounts_sale_id_fkey",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "sale_id");
                    table.ForeignKey(
                        name: "accounts_site_id_fkey",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "site_id");
                });

            migrationBuilder.CreateTable(
                name: "account_payments",
                columns: table => new
                {
                    account_payment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    payment_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("account_payments_pkey", x => x.account_payment_id);
                    table.ForeignKey(
                        name: "account_payments_account_id_fkey",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "account_payments_employee_id_fkey",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id");
                });

            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    file_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_id = table.Column<int>(type: "integer", nullable: true),
                    employee_id = table.Column<int>(type: "integer", nullable: true),
                    provider_id = table.Column<int>(type: "integer", nullable: true),
                    school_id = table.Column<int>(type: "integer", nullable: true),
                    account_id = table.Column<int>(type: "integer", nullable: true),
                    provider_account_id = table.Column<int>(type: "integer", nullable: true),
                    provider_account_payment_id = table.Column<int>(type: "integer", nullable: true),
                    archive_id = table.Column<int>(type: "integer", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    file_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    content_type = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    field_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    size = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("files_pkey", x => x.file_id);
                    table.ForeignKey(
                        name: "files_account_id_fkey",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "files_archive_id_fkey",
                        column: x => x.archive_id,
                        principalTable: "archives",
                        principalColumn: "archive_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "files_client_id_fkey",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "client_id");
                    table.ForeignKey(
                        name: "files_employee_id_fkey",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "files_provider_account_id_fkey",
                        column: x => x.provider_account_id,
                        principalTable: "provider_account",
                        principalColumn: "provider_account_id");
                    table.ForeignKey(
                        name: "files_provider_account_payment_id_fkey",
                        column: x => x.provider_account_payment_id,
                        principalTable: "provider_account_payments",
                        principalColumn: "provider_account_payment_id");
                    table.ForeignKey(
                        name: "files_provider_id_fkey",
                        column: x => x.provider_id,
                        principalTable: "providers",
                        principalColumn: "provider_id");
                    table.ForeignKey(
                        name: "files_school_id_fkey",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "school_id");
                });

            migrationBuilder.CreateTable(
                name: "product_category",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_category_pkey", x => new { x.product_id, x.category_id });
                    table.ForeignKey(
                        name: "product_category_category_id_fkey",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "category_id");
                });

            migrationBuilder.CreateTable(
                name: "product_combo_details",
                columns: table => new
                {
                    product_combo_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_combo_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: true),
                    embroidery_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_combo_details_pkey", x => x.product_combo_detail_id);
                    table.ForeignKey(
                        name: "product_combo_details_embroidery_id_fkey",
                        column: x => x.embroidery_id,
                        principalTable: "embroideries",
                        principalColumn: "embroidery_id");
                    table.ForeignKey(
                        name: "product_combo_details_product_combo_id_fkey",
                        column: x => x.product_combo_id,
                        principalTable: "product_combos",
                        principalColumn: "product_combo_id");
                });

            migrationBuilder.CreateTable(
                name: "product_pictures",
                columns: table => new
                {
                    product_picture_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    picture = table.Column<byte[]>(type: "bytea", nullable: true),
                    create_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_pictures_pkey", x => x.product_picture_id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    brand_id = table.Column<int>(type: "integer", nullable: true),
                    product_picture_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    comments = table.Column<string>(type: "text", nullable: true),
                    serial_count = table.Column<long>(type: "bigint", nullable: true),
                    serialize = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("products_pkey", x => x.product_id);
                    table.ForeignKey(
                        name: "products_brand_id_fkey",
                        column: x => x.brand_id,
                        principalTable: "brands",
                        principalColumn: "brand_id");
                    table.ForeignKey(
                        name: "products_product_picture_id_fkey",
                        column: x => x.product_picture_id,
                        principalTable: "product_pictures",
                        principalColumn: "product_picture_id");
                });

            migrationBuilder.CreateTable(
                name: "product_prices",
                columns: table => new
                {
                    price_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    size_id = table.Column<int>(type: "integer", nullable: true),
                    price = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_prices_pkey", x => x.price_id);
                    table.ForeignKey(
                        name: "product_prices_product_id_fkey",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id");
                    table.ForeignKey(
                        name: "product_prices_size_id_fkey",
                        column: x => x.size_id,
                        principalTable: "sizes",
                        principalColumn: "size_id");
                });

            migrationBuilder.CreateTable(
                name: "product_providers",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    provider_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "product_providers_product_id_fkey",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id");
                    table.ForeignKey(
                        name: "product_providers_provider_id_fkey",
                        column: x => x.provider_id,
                        principalTable: "providers",
                        principalColumn: "provider_id");
                });

            migrationBuilder.CreateTable(
                name: "product_schools",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    school_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_schools_pkey", x => new { x.product_id, x.school_id });
                    table.ForeignKey(
                        name: "product_schools_product_id_fkey",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id");
                    table.ForeignKey(
                        name: "product_schools_school_id_fkey",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "school_id");
                });

            migrationBuilder.CreateTable(
                name: "sales_details",
                columns: table => new
                {
                    sale_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    size_id = table.Column<int>(type: "integer", nullable: true),
                    price = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    special_price = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    delivered = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sales_details_pkey", x => x.sale_detail_id);
                    table.ForeignKey(
                        name: "sales_details_product_id_fkey",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id");
                    table.ForeignKey(
                        name: "sales_details_sale_id_fkey",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "sale_id");
                    table.ForeignKey(
                        name: "sales_details_size_id_fkey",
                        column: x => x.size_id,
                        principalTable: "sizes",
                        principalColumn: "size_id");
                });

            migrationBuilder.CreateTable(
                name: "stock",
                columns: table => new
                {
                    stock_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    size_id = table.Column<int>(type: "integer", nullable: true),
                    sale_detail_id = table.Column<int>(type: "integer", nullable: true),
                    price_special = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    create_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    serial_number = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: true),
                    barcode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false, defaultValueSql: "'100'::character varying")
                },
                constraints: table =>
                {
                    table.PrimaryKey("stock_pkey", x => x.stock_id);
                    table.ForeignKey(
                        name: "stock_product_id_fkey",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id");
                    table.ForeignKey(
                        name: "stock_sale_detail_id_fkey",
                        column: x => x.sale_detail_id,
                        principalTable: "sales_details",
                        principalColumn: "sale_detail_id");
                    table.ForeignKey(
                        name: "stock_site_id_fkey",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "site_id");
                    table.ForeignKey(
                        name: "stock_size_id_fkey",
                        column: x => x.size_id,
                        principalTable: "sizes",
                        principalColumn: "size_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_payments_account_id",
                table: "account_payments",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_payments_employee_id",
                table: "account_payments",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_account_status_id",
                table: "accounts",
                column: "account_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_account_type_id",
                table: "accounts",
                column: "account_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_client_id",
                table: "accounts",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_employee_id",
                table: "accounts",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_sale_id",
                table: "accounts",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_site_id",
                table: "accounts",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_employee_id",
                table: "attendance",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_clients_client_category_id",
                table: "clients",
                column: "client_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_clients_client_status_id",
                table: "clients",
                column: "client_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_clients_employee_id",
                table: "clients",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_embroideries_school_id",
                table: "embroideries",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_salary_employee_id",
                table: "employee_salary",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_schedule_employee_id",
                table: "employee_schedule",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_appuser_id",
                table: "employees",
                column: "appuser_id");

            migrationBuilder.CreateIndex(
                name: "IX_files_account_id",
                table: "files",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_files_archive_id",
                table: "files",
                column: "archive_id");

            migrationBuilder.CreateIndex(
                name: "IX_files_client_id",
                table: "files",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_files_employee_id",
                table: "files",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_files_provider_account_id",
                table: "files",
                column: "provider_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_files_provider_account_payment_id",
                table: "files",
                column: "provider_account_payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_files_provider_id",
                table: "files",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_files_school_id",
                table: "files",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_category_category_id",
                table: "product_category",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_combo_details_embroidery_id",
                table: "product_combo_details",
                column: "embroidery_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_combo_details_product_combo_id",
                table: "product_combo_details",
                column: "product_combo_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_combo_details_product_id",
                table: "product_combo_details",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_pictures_product_id",
                table: "product_pictures",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_prices_product_id",
                table: "product_prices",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_prices_size_id",
                table: "product_prices",
                column: "size_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_providers_product_id",
                table: "product_providers",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_providers_provider_id",
                table: "product_providers",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_schools_school_id",
                table: "product_schools",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_brand_id",
                table: "products",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_product_picture_id",
                table: "products",
                column: "product_picture_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_account_employee_id",
                table: "provider_account",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_account_provider_id",
                table: "provider_account",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_account_payments_employee_id",
                table: "provider_account_payments",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_account_payments_provider_account_id",
                table: "provider_account_payments",
                column: "provider_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_client_id",
                table: "sales",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_employee_id",
                table: "sales",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_sale_type_id",
                table: "sales",
                column: "sale_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_site_id",
                table: "sales",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_details_product_id",
                table: "sales_details",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_details_sale_id",
                table: "sales_details",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_details_size_id",
                table: "sales_details",
                column: "size_id");

            migrationBuilder.CreateIndex(
                name: "IX_schools_school_level_id",
                table: "schools",
                column: "school_level_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_product_id",
                table: "stock",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_sale_detail_id",
                table: "stock",
                column: "sale_detail_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_site_id",
                table: "stock",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_size_id",
                table: "stock",
                column: "size_id");

            migrationBuilder.AddForeignKey(
                name: "product_category_product_id_fkey",
                table: "product_category",
                column: "product_id",
                principalTable: "products",
                principalColumn: "product_id");

            migrationBuilder.AddForeignKey(
                name: "product_combo_details_product_id_fkey",
                table: "product_combo_details",
                column: "product_id",
                principalTable: "products",
                principalColumn: "product_id");

            migrationBuilder.AddForeignKey(
                name: "product_pictures_product_id_fkey",
                table: "product_pictures",
                column: "product_id",
                principalTable: "products",
                principalColumn: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "product_pictures_product_id_fkey",
                table: "product_pictures");

            migrationBuilder.DropTable(
                name: "account_payments");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "attendance");

            migrationBuilder.DropTable(
                name: "employee_salary");

            migrationBuilder.DropTable(
                name: "employee_schedule");

            migrationBuilder.DropTable(
                name: "files");

            migrationBuilder.DropTable(
                name: "product_category");

            migrationBuilder.DropTable(
                name: "product_combo_details");

            migrationBuilder.DropTable(
                name: "product_prices");

            migrationBuilder.DropTable(
                name: "product_providers");

            migrationBuilder.DropTable(
                name: "product_schools");

            migrationBuilder.DropTable(
                name: "stock");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "accounts");

            migrationBuilder.DropTable(
                name: "archives");

            migrationBuilder.DropTable(
                name: "provider_account_payments");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "embroideries");

            migrationBuilder.DropTable(
                name: "product_combos");

            migrationBuilder.DropTable(
                name: "sales_details");

            migrationBuilder.DropTable(
                name: "account_status");

            migrationBuilder.DropTable(
                name: "account_types");

            migrationBuilder.DropTable(
                name: "provider_account");

            migrationBuilder.DropTable(
                name: "schools");

            migrationBuilder.DropTable(
                name: "sales");

            migrationBuilder.DropTable(
                name: "sizes");

            migrationBuilder.DropTable(
                name: "providers");

            migrationBuilder.DropTable(
                name: "school_levels");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropTable(
                name: "sales_types");

            migrationBuilder.DropTable(
                name: "sites");

            migrationBuilder.DropTable(
                name: "client_categories");

            migrationBuilder.DropTable(
                name: "client_status");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "brands");

            migrationBuilder.DropTable(
                name: "product_pictures");
        }
    }
}
