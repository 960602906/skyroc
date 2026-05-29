using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseDataTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "company",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    contact_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customer_tag",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort = table.Column<int>(type: "integer", nullable: false),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_tag", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_tag_customer_tag_parent_id",
                        column: x => x.parent_id,
                        principalTable: "customer_tag",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "goods_type",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort = table.Column<int>(type: "integer", nullable: false),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_type", x => x.id);
                    table.ForeignKey(
                        name: "FK_goods_type_goods_type_parent_id",
                        column: x => x.parent_id,
                        principalTable: "goods_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "purchaser",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchaser", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchaser_sys_department_department_id",
                        column: x => x.department_id,
                        principalTable: "sys_department",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_purchaser_sys_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "sys_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "quotation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    effective_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_audited = table.Column<bool>(type: "boolean", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    contact_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bank_account = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tax_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ware",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    contact_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    sort = table.Column<int>(type: "integer", nullable: false),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ware", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customer_protocol",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    effective_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_protocol", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_protocol_quotation_quotation_id",
                        column: x => x.quotation_id,
                        principalTable: "quotation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "customer",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_ware_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_customer_quotation_quotation_id",
                        column: x => x.quotation_id,
                        principalTable: "quotation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_customer_ware_default_ware_id",
                        column: x => x.default_ware_id,
                        principalTable: "ware",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "purchase_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purchaser_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ware_id = table.Column<Guid>(type: "uuid", nullable: true),
                    goods_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purchase_pattern = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_rule", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_rule_goods_type_goods_type_id",
                        column: x => x.goods_type_id,
                        principalTable: "goods_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_purchase_rule_purchaser_purchaser_id",
                        column: x => x.purchaser_id,
                        principalTable: "purchaser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_purchase_rule_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_purchase_rule_ware_ware_id",
                        column: x => x.ware_id,
                        principalTable: "ware",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "customer_protocol_customer",
                columns: table => new
                {
                    customer_protocol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_protocol_customer", x => new { x.customer_protocol_id, x.customer_id });
                    table.ForeignKey(
                        name: "FK_customer_protocol_customer_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customer_protocol_customer_customer_protocol_customer_proto~",
                        column: x => x.customer_protocol_id,
                        principalTable: "customer_protocol",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_quotation",
                columns: table => new
                {
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    effective_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_quotation", x => new { x.customer_id, x.quotation_id });
                    table.ForeignKey(
                        name: "FK_customer_quotation_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customer_quotation_quotation_quotation_id",
                        column: x => x.quotation_id,
                        principalTable: "quotation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_sub_account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nick_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_sub_account", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_sub_account_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customer_sub_account_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "customer_tag_rel",
                columns: table => new
                {
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_tag_rel", x => new { x.customer_id, x.customer_tag_id });
                    table.ForeignKey(
                        name: "FK_customer_tag_rel_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customer_tag_rel_customer_tag_customer_tag_id",
                        column: x => x.customer_tag_id,
                        principalTable: "customer_tag",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_rule_customer",
                columns: table => new
                {
                    purchase_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_rule_customer", x => new { x.purchase_rule_id, x.customer_id });
                    table.ForeignKey(
                        name: "FK_purchase_rule_customer_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_rule_customer_purchase_rule_purchase_rule_id",
                        column: x => x.purchase_rule_id,
                        principalTable: "purchase_rule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_protocol_goods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    customer_protocol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protocol_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    min_order_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_protocol_goods", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_protocol_goods_customer_protocol_customer_protocol~",
                        column: x => x.customer_protocol_id,
                        principalTable: "customer_protocol",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    goods_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_ware_id = table.Column<Guid>(type: "uuid", nullable: true),
                    spec = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    origin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tax_rate = table.Column<decimal>(type: "numeric(8,4)", nullable: true),
                    is_on_sale = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods", x => x.id);
                    table.ForeignKey(
                        name: "FK_goods_goods_type_goods_type_id",
                        column: x => x.goods_type_id,
                        principalTable: "goods_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_supplier_default_supplier_id",
                        column: x => x.default_supplier_id,
                        principalTable: "supplier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_goods_ware_default_ware_id",
                        column: x => x.default_ware_id,
                        principalTable: "ware",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "goods_image",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sort = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_image", x => x.id);
                    table.ForeignKey(
                        name: "FK_goods_image_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goods_supplier_rel",
                columns: table => new
                {
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_supplier_rel", x => new { x.goods_id, x.supplier_id });
                    table.ForeignKey(
                        name: "FK_goods_supplier_rel_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_goods_supplier_rel_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "goods_unit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    conversion_rate = table.Column<decimal>(type: "numeric(18,6)", nullable: false, defaultValue: 1m),
                    is_base_unit = table.Column<bool>(type: "boolean", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_unit", x => x.id);
                    table.ForeignKey(
                        name: "FK_goods_unit_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_rule_goods",
                columns: table => new
                {
                    purchase_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_rule_goods", x => new { x.purchase_rule_id, x.goods_id });
                    table.ForeignKey(
                        name: "FK_purchase_rule_goods_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_rule_goods_purchase_rule_purchase_rule_id",
                        column: x => x.purchase_rule_id,
                        principalTable: "purchase_rule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotation_goods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    min_order_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    is_on_sale = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_goods", x => x.id);
                    table.ForeignKey(
                        name: "FK_quotation_goods_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotation_goods_goods_unit_goods_unit_id",
                        column: x => x.goods_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotation_goods_quotation_quotation_id",
                        column: x => x.quotation_id,
                        principalTable: "quotation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_company_code",
                table: "company",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_company_name",
                table: "company",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_customer_code",
                table: "customer",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_customer_company_id",
                table: "customer",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_default_ware_id",
                table: "customer",
                column: "default_ware_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_name",
                table: "customer",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_customer_quotation_id",
                table: "customer",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_protocol_code",
                table: "customer_protocol",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_customer_protocol_quotation_id",
                table: "customer_protocol",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_protocol_customer_customer_id",
                table: "customer_protocol_customer",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_protocol_goods_goods_id",
                table: "customer_protocol_goods",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_protocol_goods_protocol_id",
                table: "customer_protocol_goods",
                column: "customer_protocol_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_protocol_goods_unique_goods_unit",
                table: "customer_protocol_goods",
                columns: new[] { "customer_protocol_id", "goods_id", "goods_unit_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_customer_protocol_goods_unit_id",
                table: "customer_protocol_goods",
                column: "goods_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_quotation_quotation_id",
                table: "customer_quotation",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_sub_account_company_id",
                table: "customer_sub_account",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_sub_account_customer_id",
                table: "customer_sub_account",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_sub_account_username",
                table: "customer_sub_account",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_customer_tag_code",
                table: "customer_tag",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_customer_tag_parent_id",
                table: "customer_tag",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_tag_rel_tag_id",
                table: "customer_tag_rel",
                column: "customer_tag_id");

            migrationBuilder.CreateIndex(
                name: "idx_goods_code",
                table: "goods",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_goods_default_supplier_id",
                table: "goods",
                column: "default_supplier_id");

            migrationBuilder.CreateIndex(
                name: "idx_goods_default_ware_id",
                table: "goods",
                column: "default_ware_id");

            migrationBuilder.CreateIndex(
                name: "idx_goods_type_id",
                table: "goods",
                column: "goods_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_goods_base_unit_id",
                table: "goods",
                column: "base_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_goods_image_goods_id",
                table: "goods_image",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_goods_supplier_rel_supplier_id",
                table: "goods_supplier_rel",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "idx_goods_type_code",
                table: "goods_type",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_goods_type_parent_id",
                table: "goods_type",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "idx_goods_unit_goods_id",
                table: "goods_unit",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_goods_unit_goods_id_name",
                table: "goods_unit",
                columns: new[] { "goods_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_purchase_rule_code",
                table: "purchase_rule",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_purchase_rule_goods_type_id",
                table: "purchase_rule",
                column: "goods_type_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_rule_purchaser_id",
                table: "purchase_rule",
                column: "purchaser_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_rule_supplier_id",
                table: "purchase_rule",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_rule_ware_id",
                table: "purchase_rule",
                column: "ware_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_rule_customer_customer_id",
                table: "purchase_rule_customer",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_rule_goods_goods_id",
                table: "purchase_rule_goods",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchaser_code",
                table: "purchaser",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_purchaser_department_id",
                table: "purchaser",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchaser_user_id",
                table: "purchaser",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_quotation_code",
                table: "quotation",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_quotation_name",
                table: "quotation",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_quotation_goods_goods_id",
                table: "quotation_goods",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_quotation_goods_quotation_id",
                table: "quotation_goods",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "idx_quotation_goods_unique_goods_unit",
                table: "quotation_goods",
                columns: new[] { "quotation_id", "goods_id", "goods_unit_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_quotation_goods_unit_id",
                table: "quotation_goods",
                column: "goods_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_supplier_code",
                table: "supplier",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_supplier_name",
                table: "supplier",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_ware_code",
                table: "ware",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ware_name",
                table: "ware",
                column: "name");

            migrationBuilder.AddForeignKey(
                name: "FK_customer_protocol_goods_goods_goods_id",
                table: "customer_protocol_goods",
                column: "goods_id",
                principalTable: "goods",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_protocol_goods_goods_unit_goods_unit_id",
                table: "customer_protocol_goods",
                column: "goods_unit_id",
                principalTable: "goods_unit",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_goods_goods_unit_base_unit_id",
                table: "goods",
                column: "base_unit_id",
                principalTable: "goods_unit",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_goods_ware_default_ware_id",
                table: "goods");

            migrationBuilder.DropForeignKey(
                name: "FK_goods_unit_goods_goods_id",
                table: "goods_unit");

            migrationBuilder.DropTable(
                name: "customer_protocol_customer");

            migrationBuilder.DropTable(
                name: "customer_protocol_goods");

            migrationBuilder.DropTable(
                name: "customer_quotation");

            migrationBuilder.DropTable(
                name: "customer_sub_account");

            migrationBuilder.DropTable(
                name: "customer_tag_rel");

            migrationBuilder.DropTable(
                name: "goods_image");

            migrationBuilder.DropTable(
                name: "goods_supplier_rel");

            migrationBuilder.DropTable(
                name: "purchase_rule_customer");

            migrationBuilder.DropTable(
                name: "purchase_rule_goods");

            migrationBuilder.DropTable(
                name: "quotation_goods");

            migrationBuilder.DropTable(
                name: "customer_protocol");

            migrationBuilder.DropTable(
                name: "customer_tag");

            migrationBuilder.DropTable(
                name: "customer");

            migrationBuilder.DropTable(
                name: "purchase_rule");

            migrationBuilder.DropTable(
                name: "company");

            migrationBuilder.DropTable(
                name: "quotation");

            migrationBuilder.DropTable(
                name: "purchaser");

            migrationBuilder.DropTable(
                name: "ware");

            migrationBuilder.DropTable(
                name: "goods");

            migrationBuilder.DropTable(
                name: "goods_type");

            migrationBuilder.DropTable(
                name: "goods_unit");

            migrationBuilder.DropTable(
                name: "supplier");
        }
    }
}
