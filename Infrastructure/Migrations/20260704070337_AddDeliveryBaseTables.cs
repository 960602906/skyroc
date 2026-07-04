using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryBaseTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "carrier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务名称"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "业务唯一编码"),
                    contact_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "业务联系人姓名"),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "业务联系人电话号码"),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true, comment: "联系或经营地址"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "业务备注"),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "记录创建时间（UTC）"),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true, comment: "创建记录的用户主键"),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true, comment: "创建记录的用户名称"),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "记录最后修改时间（UTC）"),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true, comment: "最后修改记录的用户主键"),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true, comment: "最后修改记录的用户名称"),
                    status = table.Column<int>(type: "integer", nullable: false, comment: "记录启用状态")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carrier", x => x.id);
                },
                comment: "承运商档案，记录负责配送履约的第三方物流公司资料");

            migrationBuilder.CreateTable(
                name: "delivery_route",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务名称"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "业务唯一编码"),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "配送路线描述，说明覆盖区域或配送顺序"),
                    sort = table.Column<int>(type: "integer", nullable: false, comment: "同级记录的排序值"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "业务备注"),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "记录创建时间（UTC）"),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true, comment: "创建记录的用户主键"),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true, comment: "创建记录的用户名称"),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "记录最后修改时间（UTC）"),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true, comment: "最后修改记录的用户主键"),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true, comment: "最后修改记录的用户名称"),
                    status = table.Column<int>(type: "integer", nullable: false, comment: "记录启用状态")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delivery_route", x => x.id);
                },
                comment: "配送路线档案，维护配送分区路线及排序");

            migrationBuilder.CreateTable(
                name: "driver",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "业务名称"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "业务唯一编码"),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "联系电话"),
                    carrier_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "所属承运商主键"),
                    plate_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "司机车牌号"),
                    license_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "司机驾驶证号"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "业务备注"),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "记录创建时间（UTC）"),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true, comment: "创建记录的用户主键"),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true, comment: "创建记录的用户名称"),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "记录最后修改时间（UTC）"),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true, comment: "最后修改记录的用户主键"),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true, comment: "最后修改记录的用户名称"),
                    status = table.Column<int>(type: "integer", nullable: false, comment: "记录启用状态")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_driver", x => x.id);
                    table.ForeignKey(
                        name: "FK_driver_carrier_carrier_id",
                        column: x => x.carrier_id,
                        principalTable: "carrier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "司机档案，记录执行配送任务的司机及其所属承运商");

            migrationBuilder.CreateTable(
                name: "customer_route",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联配送路线主键"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联客户主键"),
                    sort = table.Column<int>(type: "integer", nullable: false, comment: "同级记录的排序值"),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "记录创建时间（UTC）"),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true, comment: "创建记录的用户主键"),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true, comment: "创建记录的用户名称"),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "记录最后修改时间（UTC）"),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true, comment: "最后修改记录的用户主键"),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true, comment: "最后修改记录的用户名称"),
                    status = table.Column<int>(type: "integer", nullable: false, comment: "记录启用状态")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_route", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_route_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_route_delivery_route_route_id",
                        column: x => x.route_id,
                        principalTable: "delivery_route",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "客户与配送路线的关系，记录客户归属路线和路线内配送顺序");

            migrationBuilder.CreateTable(
                name: "delivery_exception",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    exception_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "配送异常业务唯一编号"),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "关联司机主键"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "关联客户主键"),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "配送异常描述，说明配送过程中发生的具体问题"),
                    handle_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "配送异常处理状态：待处理或已处理"),
                    handle_remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "配送异常处理说明"),
                    handle_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "配送异常处理完成时间（UTC）"),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "记录创建时间（UTC）"),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true, comment: "创建记录的用户主键"),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true, comment: "创建记录的用户名称"),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "记录最后修改时间（UTC）"),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true, comment: "最后修改记录的用户主键"),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true, comment: "最后修改记录的用户名称"),
                    status = table.Column<int>(type: "integer", nullable: false, comment: "记录启用状态")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delivery_exception", x => x.id);
                    table.ForeignKey(
                        name: "FK_delivery_exception_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_exception_driver_driver_id",
                        column: x => x.driver_id,
                        principalTable: "driver",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "配送异常，记录配送过程中上报的异常及处理状态");

            migrationBuilder.CreateIndex(
                name: "idx_carrier_code",
                table: "carrier",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_carrier_name",
                table: "carrier",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_customer_route_customer_id",
                table: "customer_route",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_route_route_customer",
                table: "customer_route",
                columns: new[] { "route_id", "customer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_delivery_exception_customer_id",
                table: "delivery_exception",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_exception_driver_id",
                table: "delivery_exception",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_exception_no",
                table: "delivery_exception",
                column: "exception_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_delivery_route_code",
                table: "delivery_route",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_delivery_route_name",
                table: "delivery_route",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_driver_carrier_id",
                table: "driver",
                column: "carrier_id");

            migrationBuilder.CreateIndex(
                name: "idx_driver_code",
                table: "driver",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_route");

            migrationBuilder.DropTable(
                name: "delivery_exception");

            migrationBuilder.DropTable(
                name: "delivery_route");

            migrationBuilder.DropTable(
                name: "driver");

            migrationBuilder.DropTable(
                name: "carrier");
        }
    }
}
