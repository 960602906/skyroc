using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sys_login_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "请求中提交的登录名，失败且用户不存在时仍保留"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "成功或已匹配失败登录对应的系统用户主键"),
                    is_success = table.Column<bool>(type: "boolean", nullable: false, comment: "登录验证是否成功"),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "失败原因安全摘要，不得包含密码或令牌"),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "请求来源 IP 地址"),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "客户端 User-Agent 摘要"),
                    login_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "登录校验完成时间（UTC）"),
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
                    table.PrimaryKey("PK_sys_login_log", x => x.id);
                },
                comment: "登录日志，记录认证成功或失败的安全审计信息且不保存密码或令牌");

            migrationBuilder.CreateTable(
                name: "sys_notice",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "通知公告标题"),
                    content = table.Column<string>(type: "text", nullable: false, comment: "通知公告纯文本正文，不允许 HTML 标记或脚本"),
                    notice_status = table.Column<int>(type: "integer", nullable: false, comment: "公告状态：0 草稿，1 已发布"),
                    published_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "最近一次发布公告的时间（UTC），草稿为空"),
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
                    table.PrimaryKey("PK_sys_notice", x => x.id);
                    table.CheckConstraint("ck_sys_notice_status", "notice_status BETWEEN 0 AND 1");
                },
                comment: "通知公告，记录后台发布给系统用户的标题、正文和可见状态");

            migrationBuilder.CreateTable(
                name: "sys_service_period",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "运营服务时段名称，同名时段不允许重复"),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false, comment: "服务窗口开始的本地业务时刻，精确到秒"),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false, comment: "服务窗口结束的本地业务时刻，必须晚于开始时刻"),
                    sort_order = table.Column<int>(type: "integer", nullable: false, comment: "服务时段展示和匹配顺序，数值越小越靠前"),
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
                    table.PrimaryKey("PK_sys_service_period", x => x.id);
                    table.CheckConstraint("ck_sys_service_period_sort", "sort_order >= 0");
                    table.CheckConstraint("ck_sys_service_period_time", "end_time > start_time");
                },
                comment: "运营服务时段，维护系统可提供服务或接受下单的日内时间窗口");

            migrationBuilder.CreateTable(
                name: "sys_setting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    setting_key = table.Column<int>(type: "integer", nullable: false, comment: "运营设置稳定键：小程序下单或分拣权重"),
                    setting_value = table.Column<string>(type: "text", nullable: false, comment: "由对应强类型服务校验后的设置值 JSON"),
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
                    table.PrimaryKey("PK_sys_setting", x => x.id);
                    table.CheckConstraint("ck_sys_setting_key", "setting_key BETWEEN 1 AND 2");
                },
                comment: "系统运营设置，以稳定键和值 JSON 保存全局单例配置");

            migrationBuilder.CreateIndex(
                name: "idx_sys_login_log_success_time",
                table: "sys_login_log",
                columns: new[] { "is_success", "login_time" });

            migrationBuilder.CreateIndex(
                name: "idx_sys_login_log_time",
                table: "sys_login_log",
                column: "login_time");

            migrationBuilder.CreateIndex(
                name: "idx_sys_login_log_username_time",
                table: "sys_login_log",
                columns: new[] { "username", "login_time" });

            migrationBuilder.CreateIndex(
                name: "idx_sys_notice_created",
                table: "sys_notice",
                column: "create_time");

            migrationBuilder.CreateIndex(
                name: "idx_sys_notice_status_published",
                table: "sys_notice",
                columns: new[] { "notice_status", "published_time" });

            migrationBuilder.CreateIndex(
                name: "idx_sys_service_period_status_sort",
                table: "sys_service_period",
                columns: new[] { "status", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "uk_sys_service_period_name",
                table: "sys_service_period",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_sys_setting_key",
                table: "sys_setting",
                column: "setting_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sys_login_log");

            migrationBuilder.DropTable(
                name: "sys_notice");

            migrationBuilder.DropTable(
                name: "sys_service_period");

            migrationBuilder.DropTable(
                name: "sys_setting");
        }
    }
}
