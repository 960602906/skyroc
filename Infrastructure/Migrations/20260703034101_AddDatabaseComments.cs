using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "ware",
                comment: "仓库档案，记录仓库联系信息和启用状态");

            migrationBuilder.AlterTable(
                name: "sys_user_role",
                schema: "public",
                comment: "系统用户与角色的多对多关系");

            migrationBuilder.AlterTable(
                name: "sys_user",
                schema: "public",
                comment: "系统用户，记录登录身份、组织归属和个人资料");

            migrationBuilder.AlterTable(
                name: "sys_role_menu",
                schema: "public",
                comment: "角色与菜单权限的多对多关系");

            migrationBuilder.AlterTable(
                name: "sys_role",
                schema: "public",
                comment: "系统角色，维护角色身份和数据权限范围");

            migrationBuilder.AlterTable(
                name: "sys_operation_log",
                comment: "操作日志，记录接口调用结果和运行环境",
                oldComment: "操作日志表");

            migrationBuilder.AlterTable(
                name: "sys_menu_button",
                schema: "public",
                comment: "菜单按钮权限，维护菜单下可授权的操作编码");

            migrationBuilder.AlterTable(
                name: "sys_menu",
                schema: "public",
                comment: "系统菜单，维护前端路由、组件和显示行为");

            migrationBuilder.AlterTable(
                name: "sys_department",
                comment: "系统部门，维护组织层级和负责人信息");

            migrationBuilder.AlterTable(
                name: "supplier",
                comment: "供应商档案，记录供货方联系、银行和税务资料");

            migrationBuilder.AlterTable(
                name: "sale_order_detail",
                comment: "销售订单商品明细，保存商品、单位、价格和验收快照");

            migrationBuilder.AlterTable(
                name: "sale_order",
                comment: "销售订单，记录客户下单、金额和履约状态");

            migrationBuilder.AlterTable(
                name: "quotation_goods",
                comment: "报价单商品明细，记录商品、单位和报价");

            migrationBuilder.AlterTable(
                name: "quotation",
                comment: "销售报价单，记录报价有效期和审核状态");

            migrationBuilder.AlterTable(
                name: "purchaser",
                comment: "采购员档案，关联负责采购的系统用户和部门");

            migrationBuilder.AlterTable(
                name: "purchase_rule_goods",
                comment: "采购规则与适用商品的多对多关系");

            migrationBuilder.AlterTable(
                name: "purchase_rule_customer",
                comment: "采购规则与适用客户的多对多关系");

            migrationBuilder.AlterTable(
                name: "purchase_rule",
                comment: "采购规则，按客户、商品和仓库匹配采购责任方");

            migrationBuilder.AlterTable(
                name: "purchase_plan_order_rel",
                comment: "采购计划明细与来源销售订单明细的关系");

            migrationBuilder.AlterTable(
                name: "purchase_plan_detail",
                comment: "采购计划商品明细，记录需求、计划和已采购数量");

            migrationBuilder.AlterTable(
                name: "purchase_plan",
                comment: "采购计划，记录交期、采购模式和执行状态");

            migrationBuilder.AlterTable(
                name: "order_audit_log",
                comment: "销售订单审核记录，保存每次状态流转轨迹");

            migrationBuilder.AlterTable(
                name: "goods_unit",
                comment: "商品单位，记录单位换算、价格和起订数量");

            migrationBuilder.AlterTable(
                name: "goods_type",
                comment: "商品分类，维护商品层级和税务分类信息");

            migrationBuilder.AlterTable(
                name: "goods_supplier_rel",
                comment: "商品与可供货供应商的多对多关系");

            migrationBuilder.AlterTable(
                name: "goods_image",
                comment: "商品图片，记录商品关联图片及展示顺序");

            migrationBuilder.AlterTable(
                name: "goods",
                comment: "商品档案，记录销售与采购共用的商品基础资料");

            migrationBuilder.AlterTable(
                name: "customer_tag_rel",
                comment: "客户与客户标签的多对多关系");

            migrationBuilder.AlterTable(
                name: "customer_tag",
                comment: "客户标签，用于客户分类、筛选和业务规则匹配");

            migrationBuilder.AlterTable(
                name: "customer_sub_account",
                comment: "客户子账号，记录公司下可登录的客户侧账号");

            migrationBuilder.AlterTable(
                name: "customer_quotation",
                comment: "客户与报价单的多对多关系");

            migrationBuilder.AlterTable(
                name: "customer_protocol_goods",
                comment: "客户协议价商品明细，记录协议商品价格");

            migrationBuilder.AlterTable(
                name: "customer_protocol_customer",
                comment: "客户协议价与适用客户的多对多关系");

            migrationBuilder.AlterTable(
                name: "customer_protocol",
                comment: "客户协议价，维护客户与商品的有效期价格协议");

            migrationBuilder.AlterTable(
                name: "customer",
                comment: "客户档案，记录下单、开票和默认履约信息");

            migrationBuilder.AlterTable(
                name: "company",
                comment: "公司档案，记录客户所属经营主体的基础资料");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "ware",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "ware",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "ware",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "ware",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "sort",
                table: "ware",
                type: "integer",
                nullable: false,
                comment: "同级记录的排序值",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "ware",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "ware",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "业务名称",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "ware",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "ware",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "ware",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_phone",
                table: "ware",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                comment: "业务联系人电话号码",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_name",
                table: "ware",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "业务联系人姓名",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "ware",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "业务唯一编码",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "ware",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                comment: "联系或经营地址",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "ware",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<Guid>(
                name: "role_id",
                schema: "public",
                table: "sys_user_role",
                type: "uuid",
                nullable: false,
                comment: "关联角色主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                schema: "public",
                table: "sys_user_role",
                type: "uuid",
                nullable: false,
                comment: "关联系统用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                schema: "public",
                table: "sys_user",
                type: "varchar(50)",
                nullable: false,
                comment: "用于登录的唯一用户名",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                schema: "public",
                table: "sys_user",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                schema: "public",
                table: "sys_user",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                schema: "public",
                table: "sys_user",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                schema: "public",
                table: "sys_user",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                schema: "public",
                table: "sys_user",
                type: "varchar(20)",
                nullable: true,
                comment: "联系电话",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                schema: "public",
                table: "sys_user",
                type: "varchar(255)",
                nullable: false,
                comment: "不可逆的登录密码哈希",
                oldClrType: typeof(string),
                oldType: "varchar(255)");

            migrationBuilder.AlterColumn<string>(
                name: "nick_name",
                schema: "public",
                table: "sys_user",
                type: "varchar(50)",
                nullable: false,
                comment: "用户显示昵称",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<int>(
                name: "gender",
                schema: "public",
                table: "sys_user",
                type: "integer",
                nullable: false,
                comment: "用户性别编码",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "public",
                table: "sys_user",
                type: "varchar(100)",
                nullable: false,
                comment: "电子邮箱地址",
                oldClrType: typeof(string),
                oldType: "varchar(100)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                schema: "public",
                table: "sys_user",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                schema: "public",
                table: "sys_user",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                schema: "public",
                table: "sys_user",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "DepartmentId",
                schema: "public",
                table: "sys_user",
                type: "uuid",
                nullable: true,
                comment: "所属部门主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "public",
                table: "sys_user",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<Guid>(
                name: "menu_id",
                schema: "public",
                table: "sys_role_menu",
                type: "uuid",
                nullable: false,
                comment: "关联菜单主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "role_id",
                schema: "public",
                table: "sys_role_menu",
                type: "uuid",
                nullable: false,
                comment: "关联角色主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                schema: "public",
                table: "sys_role",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                schema: "public",
                table: "sys_role",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                schema: "public",
                table: "sys_role",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                schema: "public",
                table: "sys_role",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "public",
                table: "sys_role",
                type: "varchar(50)",
                nullable: false,
                comment: "业务名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "desc",
                schema: "public",
                table: "sys_role",
                type: "varchar(200)",
                nullable: true,
                comment: "角色说明",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                schema: "public",
                table: "sys_role",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                schema: "public",
                table: "sys_role",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                schema: "public",
                table: "sys_role",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                schema: "public",
                table: "sys_role",
                type: "varchar(50)",
                nullable: false,
                comment: "业务唯一编码",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "public",
                table: "sys_role",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<string>(
                name: "url",
                table: "sys_operation_log",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                comment: "被调用接口的请求地址",
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "sys_operation_log",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "sys_operation_log",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "sys_operation_log",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "sys_operation_log",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "response_result",
                table: "sys_operation_log",
                type: "text",
                nullable: true,
                comment: "响应结果的脱敏序列化内容",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "request_params",
                table: "sys_operation_log",
                type: "text",
                nullable: true,
                comment: "请求参数的脱敏序列化内容",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "os",
                table: "sys_operation_log",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                comment: "发起操作的客户端操作系统",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "operation_type",
                table: "sys_operation_log",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "操作类型",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "module",
                table: "sys_operation_log",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "操作所属业务模块",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "method",
                table: "sys_operation_log",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                comment: "HTTP 请求方法",
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "location",
                table: "sys_operation_log",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                comment: "仓库或业务地点说明",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_success",
                table: "sys_operation_log",
                type: "boolean",
                nullable: false,
                comment: "操作是否执行成功",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                table: "sys_operation_log",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "发起操作的客户端 IP 地址",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<long>(
                name: "execution_duration",
                table: "sys_operation_log",
                type: "bigint",
                nullable: false,
                comment: "接口执行耗时，单位为毫秒",
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "error_message",
                table: "sys_operation_log",
                type: "text",
                nullable: true,
                comment: "操作失败时记录的错误摘要",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "desc",
                table: "sys_operation_log",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                comment: "角色说明",
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "sys_operation_log",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "sys_operation_log",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "sys_operation_log",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "browser",
                table: "sys_operation_log",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                comment: "发起操作的浏览器信息",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "sys_operation_log",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                schema: "public",
                table: "sys_menu_button",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                schema: "public",
                table: "sys_menu_button",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                schema: "public",
                table: "sys_menu_button",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                schema: "public",
                table: "sys_menu_button",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "menu_id",
                schema: "public",
                table: "sys_menu_button",
                type: "uuid",
                nullable: false,
                comment: "关联菜单主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "所属菜单ID");

            migrationBuilder.AlterColumn<string>(
                name: "desc",
                schema: "public",
                table: "sys_menu_button",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "角色说明",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "按钮描述");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                schema: "public",
                table: "sys_menu_button",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                schema: "public",
                table: "sys_menu_button",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                schema: "public",
                table: "sys_menu_button",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                schema: "public",
                table: "sys_menu_button",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "业务唯一编码",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "按钮编码");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "public",
                table: "sys_menu_button",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                schema: "public",
                table: "sys_menu",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                schema: "public",
                table: "sys_menu",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                schema: "public",
                table: "sys_menu",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: false,
                comment: "菜单显示标题",
                oldClrType: typeof(string),
                oldType: "varchar(100)");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                schema: "public",
                table: "sys_menu",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "redirect",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: true,
                comment: "菜单重定向路径",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "path",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: false,
                comment: "前端路由访问路径",
                oldClrType: typeof(string),
                oldType: "varchar(100)");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_id",
                schema: "public",
                table: "sys_menu",
                type: "uuid",
                nullable: true,
                comment: "上级节点主键；根节点为空",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "order",
                schema: "public",
                table: "sys_menu",
                type: "integer",
                nullable: true,
                comment: "同级记录的显示顺序",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "public",
                table: "sys_menu",
                type: "varchar(50)",
                nullable: false,
                comment: "业务名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<bool>(
                name: "multi_tab",
                schema: "public",
                table: "sys_menu",
                type: "boolean",
                nullable: true,
                comment: "路由是否在多页签中打开",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "menu_type",
                schema: "public",
                table: "sys_menu",
                type: "integer",
                nullable: true,
                comment: "菜单节点类型",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "local_icon",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: true,
                comment: "本地图标资源标识",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "layout",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: true,
                comment: "菜单使用的前端布局标识",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "keep_alive",
                schema: "public",
                table: "sys_menu",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "路由页面是否启用缓存",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "icon_type",
                schema: "public",
                table: "sys_menu",
                type: "integer",
                nullable: true,
                comment: "菜单图标来源类型",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "icon",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: true,
                comment: "菜单图标标识",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "i18nKey",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: true,
                comment: "菜单国际化资源键",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "href",
                schema: "public",
                table: "sys_menu",
                type: "varchar(200)",
                nullable: true,
                comment: "菜单跳转的外部链接",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "hide_in_menu",
                schema: "public",
                table: "sys_menu",
                type: "boolean",
                nullable: false,
                comment: "是否在导航菜单中隐藏",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<int>(
                name: "fixed_index_in_tab",
                schema: "public",
                table: "sys_menu",
                type: "integer",
                nullable: true,
                comment: "页签固定顺序索引",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                schema: "public",
                table: "sys_menu",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                schema: "public",
                table: "sys_menu",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                schema: "public",
                table: "sys_menu",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "constant",
                schema: "public",
                table: "sys_menu",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "路由是否为常量路由",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "component",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: true,
                comment: "前端路由加载的组件路径",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "active_menu",
                schema: "public",
                table: "sys_menu",
                type: "varchar(200)",
                nullable: true,
                comment: "进入路由时默认激活的菜单路径",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "public",
                table: "sys_menu",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "sys_department",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "sys_department",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "sys_department",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "sys_department",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "sort",
                table: "sys_department",
                type: "integer",
                nullable: false,
                comment: "同级记录的排序值",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "sys_department",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "备注");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_id",
                table: "sys_department",
                type: "uuid",
                nullable: true,
                comment: "上级节点主键；根节点为空",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "sys_department",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "业务名称",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "部门名称");

            migrationBuilder.AlterColumn<string>(
                name: "leader_name",
                table: "sys_department",
                type: "text",
                nullable: true,
                comment: "部门负责人名称",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "leader_id",
                table: "sys_department",
                type: "uuid",
                nullable: true,
                comment: "部门负责人用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "sys_department",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "电子邮箱地址",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "邮箱");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "sys_department",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "sys_department",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "sys_department",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "sys_department",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "业务唯一编码",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "部门代码");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "sys_department",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "supplier",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "supplier",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "supplier",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "tax_no",
                table: "supplier",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "纳税人识别号",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "supplier",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "supplier",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "supplier",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                comment: "业务名称",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "supplier",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "supplier",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "supplier",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_phone",
                table: "supplier",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                comment: "业务联系人电话号码",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_name",
                table: "supplier",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "业务联系人姓名",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "supplier",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "业务唯一编码",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "bank_name",
                table: "supplier",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "开户银行名称",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "bank_account",
                table: "supplier",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "银行账号",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "supplier",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                comment: "联系或经营地址",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "supplier",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "sale_order_detail",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "sale_order_detail",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "sale_order_detail",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_conversion",
                table: "sale_order_detail",
                type: "numeric(18,6)",
                nullable: false,
                comment: "下单单位换算为基础单位的比例",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "total_price",
                table: "sale_order_detail",
                type: "numeric(18,4)",
                nullable: false,
                comment: "数量与单价计算后的总金额",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "sale_order_detail",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "sale_order_id",
                table: "sale_order_detail",
                type: "uuid",
                nullable: false,
                comment: "来源销售订单主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "sale_order_detail",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "sale_order_detail",
                type: "numeric(18,6)",
                nullable: false,
                comment: "业务数量，按当前商品单位计量",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");

            migrationBuilder.AlterColumn<string>(
                name: "inner_remark",
                table: "sale_order_detail",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "仅内部人员可见的备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "has_purchase_plan",
                table: "sale_order_detail",
                type: "boolean",
                nullable: false,
                comment: "是否已生成采购计划",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "goods_unit_name_snapshot",
                table: "sale_order_detail",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "下单时的商品单位名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_unit_id",
                table: "sale_order_detail",
                type: "uuid",
                nullable: false,
                comment: "下单商品单位主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "goods_type_name_snapshot",
                table: "sale_order_detail",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "业务发生时的商品分类名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "goods_name_snapshot",
                table: "sale_order_detail",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                comment: "业务发生时的商品名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "goods_image_snapshot",
                table: "sale_order_detail",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务发生时的商品图片地址快照",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "sale_order_detail",
                type: "uuid",
                nullable: false,
                comment: "关联商品主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "goods_description_snapshot",
                table: "sale_order_detail",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                comment: "业务发生时的商品描述快照",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "goods_code_snapshot",
                table: "sale_order_detail",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "业务发生时的商品编码快照",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "fixed_price",
                table: "sale_order_detail",
                type: "numeric(18,4)",
                nullable: false,
                comment: "订单商品固定单价",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<string>(
                name: "fixed_goods_unit_name_snapshot",
                table: "sale_order_detail",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "下单时的计价单位名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "fixed_goods_unit_id",
                table: "sale_order_detail",
                type: "uuid",
                nullable: true,
                comment: "计价单位主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "customer_check_status",
                table: "sale_order_detail",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "客户验收状态",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "customer_check_price",
                table: "sale_order_detail",
                type: "numeric(18,4)",
                nullable: true,
                comment: "客户验收确认金额",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "customer_check_base_quantity",
                table: "sale_order_detail",
                type: "numeric(18,6)",
                nullable: true,
                comment: "客户验收数量，按基础单位计量",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "sale_order_detail",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "sale_order_detail",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "sale_order_detail",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "base_unit_name_snapshot",
                table: "sale_order_detail",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "业务发生时的基础单位名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "base_unit_id",
                table: "sale_order_detail",
                type: "uuid",
                nullable: true,
                comment: "商品基础单位主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "base_quantity",
                table: "sale_order_detail",
                type: "numeric(18,6)",
                nullable: false,
                comment: "按商品基础单位换算后的数量",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "sale_order_detail",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<Guid>(
                name: "ware_id",
                table: "sale_order",
                type: "uuid",
                nullable: true,
                comment: "关联仓库主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "sale_order",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "update_status",
                table: "sale_order",
                type: "boolean",
                nullable: false,
                comment: "订单审核通过后是否发生过修改",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "sale_order",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "sale_order",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "sale_order",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "settlement_price",
                table: "sale_order",
                type: "numeric(18,4)",
                nullable: false,
                comment: "订单最终结算金额",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<int>(
                name: "return_status",
                table: "sale_order",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "订单回单状态",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "sale_order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "receive_date",
                table: "sale_order",
                type: "timestamp with time zone",
                nullable: true,
                comment: "客户要求收货时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "quotation_id",
                table: "sale_order",
                type: "uuid",
                nullable: true,
                comment: "销售报价单主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "print_status",
                table: "sale_order",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "订单打印状态",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "out_storage_status",
                table: "sale_order",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "销售出库单生成状态",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "out_date",
                table: "sale_order",
                type: "timestamp with time zone",
                nullable: true,
                comment: "计划或实际出库时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "order_status",
                table: "sale_order",
                type: "integer",
                nullable: false,
                defaultValue: -1,
                comment: "销售订单当前业务状态",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: -1);

            migrationBuilder.AlterColumn<decimal>(
                name: "order_price",
                table: "sale_order",
                type: "numeric(18,4)",
                nullable: false,
                comment: "订单销售总金额",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<string>(
                name: "order_no",
                table: "sale_order",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "销售订单业务编号",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "order_date",
                table: "sale_order",
                type: "timestamp with time zone",
                nullable: false,
                comment: "客户下单时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "inner_remark",
                table: "sale_order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "仅内部人员可见的备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "has_purchase_plan",
                table: "sale_order",
                type: "boolean",
                nullable: false,
                comment: "是否已生成采购计划",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "has_out_sale",
                table: "sale_order",
                type: "boolean",
                nullable: false,
                comment: "是否已生成销售出库单",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "delivery_address_snapshot",
                table: "sale_order",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                comment: "下单时的配送地址快照",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "customer_name_snapshot",
                table: "sale_order",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                comment: "业务发生时的客户名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "sale_order",
                type: "uuid",
                nullable: false,
                comment: "关联客户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "customer_code_snapshot",
                table: "sale_order",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "下单时的客户编码快照",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "sale_order",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "sale_order",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "sale_order",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_phone_snapshot",
                table: "sale_order",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                comment: "业务发生时的联系人电话快照",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_name_snapshot",
                table: "sale_order",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "业务发生时的联系人姓名快照",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "sale_order",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "quotation_goods",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "quotation_goods",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "quotation_goods",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "quotation_goods",
                type: "numeric(18,4)",
                nullable: false,
                comment: "当前商品单位对应的单价",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "quotation_goods",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "quotation_goods",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "quotation_id",
                table: "quotation_goods",
                type: "uuid",
                nullable: false,
                comment: "销售报价单主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "min_order_quantity",
                table: "quotation_goods",
                type: "numeric(18,4)",
                nullable: true,
                comment: "最小下单数量，按当前商品单位计量",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_on_sale",
                table: "quotation_goods",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "商品是否允许上架销售",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_unit_id",
                table: "quotation_goods",
                type: "uuid",
                nullable: false,
                comment: "下单商品单位主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "quotation_goods",
                type: "uuid",
                nullable: false,
                comment: "关联商品主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "quotation_goods",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "quotation_goods",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "quotation_goods",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "quotation_goods",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "quotation",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "quotation",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "quotation",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "quotation",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "quotation",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                comment: "业务名称",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<bool>(
                name: "is_audited",
                table: "quotation",
                type: "boolean",
                nullable: false,
                comment: "报价单是否已审核通过",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<DateTime>(
                name: "effective_start",
                table: "quotation",
                type: "timestamp with time zone",
                nullable: true,
                comment: "价格或协议有效期开始时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "effective_end",
                table: "quotation",
                type: "timestamp with time zone",
                nullable: true,
                comment: "价格或协议有效期结束时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "quotation",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                comment: "业务描述",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "quotation",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "quotation",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "quotation",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "quotation",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "业务唯一编码",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "quotation",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "purchaser",
                type: "uuid",
                nullable: true,
                comment: "关联系统用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "purchaser",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "purchaser",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "purchaser",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "purchaser",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "purchaser",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "purchaser",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                comment: "联系电话",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "purchaser",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "业务名称",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "department_id",
                table: "purchaser",
                type: "uuid",
                nullable: true,
                comment: "所属部门主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "purchaser",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "purchaser",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "purchaser",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "purchaser",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "业务唯一编码",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "purchaser",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "purchase_rule_goods",
                type: "uuid",
                nullable: false,
                comment: "关联商品主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "purchase_rule_id",
                table: "purchase_rule_goods",
                type: "uuid",
                nullable: false,
                comment: "采购规则主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "purchase_rule_customer",
                type: "uuid",
                nullable: false,
                comment: "关联客户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "purchase_rule_id",
                table: "purchase_rule_customer",
                type: "uuid",
                nullable: false,
                comment: "采购规则主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ware_id",
                table: "purchase_rule",
                type: "uuid",
                nullable: true,
                comment: "关联仓库主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "purchase_rule",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "purchase_rule",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "purchase_rule",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "supplier_id",
                table: "purchase_rule",
                type: "uuid",
                nullable: true,
                comment: "关联供应商主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "purchase_rule",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "purchase_rule",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "purchaser_id",
                table: "purchase_rule",
                type: "uuid",
                nullable: true,
                comment: "负责采购的采购员主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "purchase_pattern",
                table: "purchase_rule",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                comment: "采购模式：供应商直供或市场自采",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "purchase_rule",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                comment: "业务名称",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_type_id",
                table: "purchase_rule",
                type: "uuid",
                nullable: true,
                comment: "商品分类主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "purchase_rule",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "purchase_rule",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "purchase_rule",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "purchase_rule",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "业务唯一编码",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "purchase_rule",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "purchase_plan_order_rel",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "purchase_plan_order_rel",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "purchase_plan_order_rel",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "purchase_plan_order_rel",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "sale_order_id",
                table: "purchase_plan_order_rel",
                type: "uuid",
                nullable: false,
                comment: "来源销售订单主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "sale_order_detail_id",
                table: "purchase_plan_order_rel",
                type: "uuid",
                nullable: false,
                comment: "来源销售订单商品明细主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "required_quantity",
                table: "purchase_plan_order_rel",
                type: "numeric(18,6)",
                nullable: false,
                comment: "来源订单需求数量，按采购单位计量",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");

            migrationBuilder.AlterColumn<Guid>(
                name: "purchase_plan_detail_id",
                table: "purchase_plan_order_rel",
                type: "uuid",
                nullable: false,
                comment: "采购计划商品明细主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "purchase_plan_order_rel",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "purchase_plan_order_rel",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "purchase_plan_order_rel",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "purchase_plan_order_rel",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "purchase_plan_detail",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "purchase_plan_detail",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "purchase_plan_detail",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "purchase_plan_detail",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "required_quantity",
                table: "purchase_plan_detail",
                type: "numeric(18,6)",
                nullable: false,
                comment: "来源订单需求数量，按采购单位计量",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "purchase_plan_detail",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "purchased_quantity",
                table: "purchase_plan_detail",
                type: "numeric(18,6)",
                nullable: false,
                comment: "已生成采购单的数量，按采购单位计量",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");

            migrationBuilder.AlterColumn<string>(
                name: "purchase_unit_name_snapshot",
                table: "purchase_plan_detail",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "计划生成时的采购单位名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "purchase_unit_id",
                table: "purchase_plan_detail",
                type: "uuid",
                nullable: false,
                comment: "采购计量单位主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "purchase_plan_id",
                table: "purchase_plan_detail",
                type: "uuid",
                nullable: false,
                comment: "采购计划主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "planned_quantity",
                table: "purchase_plan_detail",
                type: "numeric(18,6)",
                nullable: false,
                comment: "计划采购数量，按采购单位计量",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");

            migrationBuilder.AlterColumn<string>(
                name: "goods_name_snapshot",
                table: "purchase_plan_detail",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                comment: "业务发生时的商品名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "purchase_plan_detail",
                type: "uuid",
                nullable: false,
                comment: "关联商品主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "goods_code_snapshot",
                table: "purchase_plan_detail",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "业务发生时的商品编码快照",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "purchase_plan_detail",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "purchase_plan_detail",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "purchase_plan_detail",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "purchase_plan_detail",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "purchase_plan",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "purchase_plan",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "purchase_plan",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "supplier_name_snapshot",
                table: "purchase_plan",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                comment: "计划生成时的供应商名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "supplier_id",
                table: "purchase_plan",
                type: "uuid",
                nullable: true,
                comment: "关联供应商主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "purchase_plan",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "purchase_plan",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "purchaser_name_snapshot",
                table: "purchase_plan",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                comment: "计划生成时的采购员名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "purchaser_id",
                table: "purchase_plan",
                type: "uuid",
                nullable: true,
                comment: "负责采购的采购员主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "purchase_status",
                table: "purchase_plan",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                comment: "采购单生成进度状态",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "purchase_pattern",
                table: "purchase_plan",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                comment: "采购模式：供应商直供或市场自采",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "plan_no",
                table: "purchase_plan",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "采购计划业务编号",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "plan_date",
                table: "purchase_plan",
                type: "timestamp with time zone",
                nullable: false,
                comment: "计划采购交期（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "purchase_plan",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "purchase_plan",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "purchase_plan",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "purchase_plan",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "order_audit_log",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "order_audit_log",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "order_audit_log",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "order_audit_log",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "sale_order_id",
                table: "order_audit_log",
                type: "uuid",
                nullable: false,
                comment: "来源销售订单主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "order_audit_log",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "previous_status",
                table: "order_audit_log",
                type: "integer",
                nullable: false,
                comment: "审核动作发生前的订单状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "current_status",
                table: "order_audit_log",
                type: "integer",
                nullable: false,
                comment: "审核动作完成后的订单状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "order_audit_log",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "order_audit_log",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "order_audit_log",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "audit_user_name_snapshot",
                table: "order_audit_log",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "审核时的用户名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<Guid>(
                name: "audit_user_id",
                table: "order_audit_log",
                type: "uuid",
                nullable: true,
                comment: "执行审核的系统用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "audit_time",
                table: "order_audit_log",
                type: "timestamp with time zone",
                nullable: false,
                comment: "审核动作发生时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "action",
                table: "order_audit_log",
                type: "integer",
                nullable: false,
                comment: "审核动作类型",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "order_audit_log",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "goods_unit",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "goods_unit",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "goods_unit",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "goods_unit",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "sort",
                table: "goods_unit",
                type: "integer",
                nullable: false,
                comment: "同级记录的排序值",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "goods_unit",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "goods_unit",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "业务名称",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<bool>(
                name: "is_base_unit",
                table: "goods_unit",
                type: "boolean",
                nullable: false,
                comment: "是否为商品基础计量单位",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "goods_unit",
                type: "uuid",
                nullable: false,
                comment: "关联商品主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "goods_unit",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "goods_unit",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "goods_unit",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "conversion_rate",
                table: "goods_unit",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 1m,
                comment: "当前单位换算为基础单位的比例",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldDefaultValue: 1m);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "goods_unit",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "业务唯一编码",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "goods_unit",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "goods_type",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "goods_type",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "goods_type",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "tax_policy_basis",
                table: "goods_type",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "税收优惠政策依据",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "tax_category_name",
                table: "goods_type",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "税收分类名称",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "tax_category_code",
                table: "goods_type",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                comment: "税收分类编码",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "goods_type",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "sort",
                table: "goods_type",
                type: "integer",
                nullable: false,
                comment: "同级记录的排序值",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "goods_type",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_id",
                table: "goods_type",
                type: "uuid",
                nullable: true,
                comment: "上级节点主键；根节点为空",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "goods_type",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "业务名称",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "is_tax_exempt",
                table: "goods_type",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "商品分类是否免税",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "invoice_goods_short_name",
                table: "goods_type",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "发票商品简称",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "image_url",
                table: "goods_type",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "商品图片访问地址",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "default_tax_rate",
                table: "goods_type",
                type: "numeric(8,4)",
                nullable: true,
                comment: "分类默认税率",
                oldClrType: typeof(decimal),
                oldType: "numeric(8,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "goods_type",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "goods_type",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "goods_type",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "goods_type",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "业务唯一编码",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "goods_type",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<bool>(
                name: "is_default",
                table: "goods_supplier_rel",
                type: "boolean",
                nullable: false,
                comment: "是否为默认关系或默认配置",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<Guid>(
                name: "supplier_id",
                table: "goods_supplier_rel",
                type: "uuid",
                nullable: false,
                comment: "关联供应商主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "goods_supplier_rel",
                type: "uuid",
                nullable: false,
                comment: "关联商品主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "url",
                table: "goods_image",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "被调用接口的请求地址",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "goods_image",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "goods_image",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "goods_image",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "goods_image",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "sort",
                table: "goods_image",
                type: "integer",
                nullable: false,
                comment: "同级记录的排序值",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<bool>(
                name: "is_primary",
                table: "goods_image",
                type: "boolean",
                nullable: false,
                comment: "是否为主要供货关系",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "goods_image",
                type: "uuid",
                nullable: false,
                comment: "关联商品主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "file_name",
                table: "goods_image",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "图片或附件文件名",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "goods_image",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "goods_image",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "goods_image",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "goods_image",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "goods",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "goods",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "goods",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "tax_rate",
                table: "goods",
                type: "numeric(8,4)",
                nullable: true,
                comment: "适用税率",
                oldClrType: typeof(decimal),
                oldType: "numeric(8,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "goods",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "spec",
                table: "goods",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "商品规格型号",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "goods",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "origin",
                table: "goods",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "商品产地",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "goods",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                comment: "业务名称",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<bool>(
                name: "is_on_sale",
                table: "goods",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "商品是否允许上架销售",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_type_id",
                table: "goods",
                type: "uuid",
                nullable: false,
                comment: "商品分类主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "goods",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                comment: "业务描述",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "default_ware_id",
                table: "goods",
                type: "uuid",
                nullable: true,
                comment: "默认履约仓库主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "default_supplier_id",
                table: "goods",
                type: "uuid",
                nullable: true,
                comment: "商品默认供应商主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "goods",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "goods",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "goods",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "goods",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "业务唯一编码",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "brand",
                table: "goods",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "商品品牌",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "base_unit_id",
                table: "goods",
                type: "uuid",
                nullable: true,
                comment: "商品基础单位主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "goods",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_tag_id",
                table: "customer_tag_rel",
                type: "uuid",
                nullable: false,
                comment: "客户标签主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "customer_tag_rel",
                type: "uuid",
                nullable: false,
                comment: "关联客户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "customer_tag",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "customer_tag",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "customer_tag",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "customer_tag",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "sort",
                table: "customer_tag",
                type: "integer",
                nullable: false,
                comment: "同级记录的排序值",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "customer_tag",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_id",
                table: "customer_tag",
                type: "uuid",
                nullable: true,
                comment: "上级节点主键；根节点为空",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "customer_tag",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "业务名称",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "customer_tag",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "customer_tag",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "customer_tag",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "customer_tag",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "业务唯一编码",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "customer_tag",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "customer_sub_account",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "用于登录的唯一用户名",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "customer_sub_account",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "customer_sub_account",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "customer_sub_account",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "customer_sub_account",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "customer_sub_account",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "customer_sub_account",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                comment: "联系电话",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "customer_sub_account",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                comment: "不可逆的登录密码哈希",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "nick_name",
                table: "customer_sub_account",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "用户显示昵称",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "customer_sub_account",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "电子邮箱地址",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "customer_sub_account",
                type: "uuid",
                nullable: true,
                comment: "关联客户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "customer_sub_account",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "customer_sub_account",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "customer_sub_account",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "company_id",
                table: "customer_sub_account",
                type: "uuid",
                nullable: false,
                comment: "所属公司主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "customer_sub_account",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<bool>(
                name: "is_default",
                table: "customer_quotation",
                type: "boolean",
                nullable: false,
                comment: "是否为默认关系或默认配置",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<DateTime>(
                name: "effective_start",
                table: "customer_quotation",
                type: "timestamp with time zone",
                nullable: true,
                comment: "价格或协议有效期开始时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "effective_end",
                table: "customer_quotation",
                type: "timestamp with time zone",
                nullable: true,
                comment: "价格或协议有效期结束时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "quotation_id",
                table: "customer_quotation",
                type: "uuid",
                nullable: false,
                comment: "销售报价单主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "customer_quotation",
                type: "uuid",
                nullable: false,
                comment: "关联客户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "customer_protocol_goods",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "customer_protocol_goods",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "customer_protocol_goods",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "customer_protocol_goods",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "customer_protocol_goods",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "protocol_price",
                table: "customer_protocol_goods",
                type: "numeric(18,4)",
                nullable: false,
                comment: "协议约定的商品单价",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "min_order_quantity",
                table: "customer_protocol_goods",
                type: "numeric(18,4)",
                nullable: true,
                comment: "最小下单数量，按当前商品单位计量",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_unit_id",
                table: "customer_protocol_goods",
                type: "uuid",
                nullable: false,
                comment: "下单商品单位主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "customer_protocol_goods",
                type: "uuid",
                nullable: false,
                comment: "关联商品主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_protocol_id",
                table: "customer_protocol_goods",
                type: "uuid",
                nullable: false,
                comment: "客户协议价主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "customer_protocol_goods",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "customer_protocol_goods",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "customer_protocol_goods",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "customer_protocol_goods",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "customer_protocol_customer",
                type: "uuid",
                nullable: false,
                comment: "关联客户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_protocol_id",
                table: "customer_protocol_customer",
                type: "uuid",
                nullable: false,
                comment: "客户协议价主键",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "customer_protocol",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "customer_protocol",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "customer_protocol",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "customer_protocol",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "customer_protocol",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "quotation_id",
                table: "customer_protocol",
                type: "uuid",
                nullable: true,
                comment: "销售报价单主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "customer_protocol",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                comment: "业务名称",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<DateTime>(
                name: "effective_start",
                table: "customer_protocol",
                type: "timestamp with time zone",
                nullable: false,
                comment: "价格或协议有效期开始时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "effective_end",
                table: "customer_protocol",
                type: "timestamp with time zone",
                nullable: true,
                comment: "价格或协议有效期结束时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "customer_protocol",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "customer_protocol",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "customer_protocol",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "customer_protocol",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "业务唯一编码",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "customer_protocol",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "customer",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "customer",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "customer",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "unified_social_credit_code",
                table: "customer",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                comment: "企业统一社会信用代码",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "taxpayer_identification_number",
                table: "customer",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                comment: "客户纳税人识别号",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "customer",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "customer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registration_status",
                table: "customer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "企业工商登记状态",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registration_authority",
                table: "customer",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "企业登记机关",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registered_capital",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "企业登记注册资本",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registered_address",
                table: "customer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "企业工商注册地址",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "quotation_id",
                table: "customer",
                type: "uuid",
                nullable: true,
                comment: "销售报价单主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "customer",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                comment: "业务名称",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "legal_representative",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "企业法定代表人",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "invoice_title",
                table: "customer",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "发票抬头",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "invoice_receiver_phone",
                table: "customer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "发票收件人电话",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "invoice_receiver_name",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "发票收件人姓名",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "invoice_receiver_address",
                table: "customer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "发票收件地址",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "invoice_phone",
                table: "customer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "开票联系电话",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "invoice_email",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "发票接收邮箱",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "invoice_address",
                table: "customer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "开票登记地址",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "establish_date",
                table: "customer",
                type: "timestamp with time zone",
                nullable: true,
                comment: "企业成立日期",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "default_ware_id",
                table: "customer",
                type: "uuid",
                nullable: true,
                comment: "默认履约仓库主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "customer",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "customer",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "customer",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_phone",
                table: "customer",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                comment: "业务联系人电话号码",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_name",
                table: "customer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "业务联系人姓名",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "company_id",
                table: "customer",
                type: "uuid",
                nullable: true,
                comment: "所属公司主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "customer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "业务唯一编码",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "business_term",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "企业登记的营业期限",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "business_scope",
                table: "customer",
                type: "text",
                nullable: true,
                comment: "企业登记的经营范围",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "bank_name",
                table: "customer",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "开户银行名称",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "bank_account",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "银行账号",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "customer",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                comment: "联系或经营地址",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "customer",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "company",
                type: "timestamp with time zone",
                nullable: true,
                comment: "记录最后修改时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "company",
                type: "varchar(50)",
                nullable: true,
                comment: "最后修改记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "company",
                type: "uuid",
                nullable: true,
                comment: "最后修改记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "company",
                type: "integer",
                nullable: false,
                comment: "记录启用状态",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "company",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "业务备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "company",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                comment: "业务名称",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "company",
                type: "timestamp with time zone",
                nullable: false,
                comment: "记录创建时间（UTC）",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "company",
                type: "varchar(50)",
                nullable: true,
                comment: "创建记录的用户名称",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "company",
                type: "uuid",
                nullable: true,
                comment: "创建记录的用户主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_phone",
                table: "company",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                comment: "业务联系人电话号码",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_name",
                table: "company",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "业务联系人姓名",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "company",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "业务唯一编码",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "company",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                comment: "联系或经营地址",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "company",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "记录主键",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "ware",
                oldComment: "仓库档案，记录仓库联系信息和启用状态");

            migrationBuilder.AlterTable(
                name: "sys_user_role",
                schema: "public",
                oldComment: "系统用户与角色的多对多关系");

            migrationBuilder.AlterTable(
                name: "sys_user",
                schema: "public",
                oldComment: "系统用户，记录登录身份、组织归属和个人资料");

            migrationBuilder.AlterTable(
                name: "sys_role_menu",
                schema: "public",
                oldComment: "角色与菜单权限的多对多关系");

            migrationBuilder.AlterTable(
                name: "sys_role",
                schema: "public",
                oldComment: "系统角色，维护角色身份和数据权限范围");

            migrationBuilder.AlterTable(
                name: "sys_operation_log",
                comment: "操作日志表",
                oldComment: "操作日志，记录接口调用结果和运行环境");

            migrationBuilder.AlterTable(
                name: "sys_menu_button",
                schema: "public",
                oldComment: "菜单按钮权限，维护菜单下可授权的操作编码");

            migrationBuilder.AlterTable(
                name: "sys_menu",
                schema: "public",
                oldComment: "系统菜单，维护前端路由、组件和显示行为");

            migrationBuilder.AlterTable(
                name: "sys_department",
                oldComment: "系统部门，维护组织层级和负责人信息");

            migrationBuilder.AlterTable(
                name: "supplier",
                oldComment: "供应商档案，记录供货方联系、银行和税务资料");

            migrationBuilder.AlterTable(
                name: "sale_order_detail",
                oldComment: "销售订单商品明细，保存商品、单位、价格和验收快照");

            migrationBuilder.AlterTable(
                name: "sale_order",
                oldComment: "销售订单，记录客户下单、金额和履约状态");

            migrationBuilder.AlterTable(
                name: "quotation_goods",
                oldComment: "报价单商品明细，记录商品、单位和报价");

            migrationBuilder.AlterTable(
                name: "quotation",
                oldComment: "销售报价单，记录报价有效期和审核状态");

            migrationBuilder.AlterTable(
                name: "purchaser",
                oldComment: "采购员档案，关联负责采购的系统用户和部门");

            migrationBuilder.AlterTable(
                name: "purchase_rule_goods",
                oldComment: "采购规则与适用商品的多对多关系");

            migrationBuilder.AlterTable(
                name: "purchase_rule_customer",
                oldComment: "采购规则与适用客户的多对多关系");

            migrationBuilder.AlterTable(
                name: "purchase_rule",
                oldComment: "采购规则，按客户、商品和仓库匹配采购责任方");

            migrationBuilder.AlterTable(
                name: "purchase_plan_order_rel",
                oldComment: "采购计划明细与来源销售订单明细的关系");

            migrationBuilder.AlterTable(
                name: "purchase_plan_detail",
                oldComment: "采购计划商品明细，记录需求、计划和已采购数量");

            migrationBuilder.AlterTable(
                name: "purchase_plan",
                oldComment: "采购计划，记录交期、采购模式和执行状态");

            migrationBuilder.AlterTable(
                name: "order_audit_log",
                oldComment: "销售订单审核记录，保存每次状态流转轨迹");

            migrationBuilder.AlterTable(
                name: "goods_unit",
                oldComment: "商品单位，记录单位换算、价格和起订数量");

            migrationBuilder.AlterTable(
                name: "goods_type",
                oldComment: "商品分类，维护商品层级和税务分类信息");

            migrationBuilder.AlterTable(
                name: "goods_supplier_rel",
                oldComment: "商品与可供货供应商的多对多关系");

            migrationBuilder.AlterTable(
                name: "goods_image",
                oldComment: "商品图片，记录商品关联图片及展示顺序");

            migrationBuilder.AlterTable(
                name: "goods",
                oldComment: "商品档案，记录销售与采购共用的商品基础资料");

            migrationBuilder.AlterTable(
                name: "customer_tag_rel",
                oldComment: "客户与客户标签的多对多关系");

            migrationBuilder.AlterTable(
                name: "customer_tag",
                oldComment: "客户标签，用于客户分类、筛选和业务规则匹配");

            migrationBuilder.AlterTable(
                name: "customer_sub_account",
                oldComment: "客户子账号，记录公司下可登录的客户侧账号");

            migrationBuilder.AlterTable(
                name: "customer_quotation",
                oldComment: "客户与报价单的多对多关系");

            migrationBuilder.AlterTable(
                name: "customer_protocol_goods",
                oldComment: "客户协议价商品明细，记录协议商品价格");

            migrationBuilder.AlterTable(
                name: "customer_protocol_customer",
                oldComment: "客户协议价与适用客户的多对多关系");

            migrationBuilder.AlterTable(
                name: "customer_protocol",
                oldComment: "客户协议价，维护客户与商品的有效期价格协议");

            migrationBuilder.AlterTable(
                name: "customer",
                oldComment: "客户档案，记录下单、开票和默认履约信息");

            migrationBuilder.AlterTable(
                name: "company",
                oldComment: "公司档案，记录客户所属经营主体的基础资料");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "ware",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "ware",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "ware",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "ware",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<int>(
                name: "sort",
                table: "ware",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "同级记录的排序值");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "ware",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "ware",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "业务名称");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "ware",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "ware",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "ware",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "contact_phone",
                table: "ware",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "业务联系人电话号码");

            migrationBuilder.AlterColumn<string>(
                name: "contact_name",
                table: "ware",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "业务联系人姓名");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "ware",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "业务唯一编码");

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "ware",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true,
                oldComment: "联系或经营地址");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "ware",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "role_id",
                schema: "public",
                table: "sys_user_role",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联角色主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                schema: "public",
                table: "sys_user_role",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联系统用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                schema: "public",
                table: "sys_user",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldComment: "用于登录的唯一用户名");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                schema: "public",
                table: "sys_user",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                schema: "public",
                table: "sys_user",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                schema: "public",
                table: "sys_user",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                schema: "public",
                table: "sys_user",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                schema: "public",
                table: "sys_user",
                type: "varchar(20)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldNullable: true,
                oldComment: "联系电话");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                schema: "public",
                table: "sys_user",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldComment: "不可逆的登录密码哈希");

            migrationBuilder.AlterColumn<string>(
                name: "nick_name",
                schema: "public",
                table: "sys_user",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldComment: "用户显示昵称");

            migrationBuilder.AlterColumn<int>(
                name: "gender",
                schema: "public",
                table: "sys_user",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "用户性别编码");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "public",
                table: "sys_user",
                type: "varchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldComment: "电子邮箱地址");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                schema: "public",
                table: "sys_user",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                schema: "public",
                table: "sys_user",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                schema: "public",
                table: "sys_user",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "DepartmentId",
                schema: "public",
                table: "sys_user",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "所属部门主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "public",
                table: "sys_user",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "menu_id",
                schema: "public",
                table: "sys_role_menu",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联菜单主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "role_id",
                schema: "public",
                table: "sys_role_menu",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联角色主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                schema: "public",
                table: "sys_role",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                schema: "public",
                table: "sys_role",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                schema: "public",
                table: "sys_role",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                schema: "public",
                table: "sys_role",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "public",
                table: "sys_role",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldComment: "业务名称");

            migrationBuilder.AlterColumn<string>(
                name: "desc",
                schema: "public",
                table: "sys_role",
                type: "varchar(200)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldNullable: true,
                oldComment: "角色说明");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                schema: "public",
                table: "sys_role",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                schema: "public",
                table: "sys_role",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                schema: "public",
                table: "sys_role",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                schema: "public",
                table: "sys_role",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldComment: "业务唯一编码");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "public",
                table: "sys_role",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<string>(
                name: "url",
                table: "sys_operation_log",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldComment: "被调用接口的请求地址");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "sys_operation_log",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "sys_operation_log",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "sys_operation_log",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "sys_operation_log",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "response_result",
                table: "sys_operation_log",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "响应结果的脱敏序列化内容");

            migrationBuilder.AlterColumn<string>(
                name: "request_params",
                table: "sys_operation_log",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "请求参数的脱敏序列化内容");

            migrationBuilder.AlterColumn<string>(
                name: "os",
                table: "sys_operation_log",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true,
                oldComment: "发起操作的客户端操作系统");

            migrationBuilder.AlterColumn<string>(
                name: "operation_type",
                table: "sys_operation_log",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "操作类型");

            migrationBuilder.AlterColumn<string>(
                name: "module",
                table: "sys_operation_log",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "操作所属业务模块");

            migrationBuilder.AlterColumn<string>(
                name: "method",
                table: "sys_operation_log",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldComment: "HTTP 请求方法");

            migrationBuilder.AlterColumn<string>(
                name: "location",
                table: "sys_operation_log",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true,
                oldComment: "仓库或业务地点说明");

            migrationBuilder.AlterColumn<bool>(
                name: "is_success",
                table: "sys_operation_log",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "操作是否执行成功");

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                table: "sys_operation_log",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "发起操作的客户端 IP 地址");

            migrationBuilder.AlterColumn<long>(
                name: "execution_duration",
                table: "sys_operation_log",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldComment: "接口执行耗时，单位为毫秒");

            migrationBuilder.AlterColumn<string>(
                name: "error_message",
                table: "sys_operation_log",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "操作失败时记录的错误摘要");

            migrationBuilder.AlterColumn<string>(
                name: "desc",
                table: "sys_operation_log",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldComment: "角色说明");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "sys_operation_log",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "sys_operation_log",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "sys_operation_log",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "browser",
                table: "sys_operation_log",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true,
                oldComment: "发起操作的浏览器信息");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "sys_operation_log",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                schema: "public",
                table: "sys_menu_button",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                schema: "public",
                table: "sys_menu_button",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                schema: "public",
                table: "sys_menu_button",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                schema: "public",
                table: "sys_menu_button",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<Guid>(
                name: "menu_id",
                schema: "public",
                table: "sys_menu_button",
                type: "uuid",
                nullable: false,
                comment: "所属菜单ID",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联菜单主键");

            migrationBuilder.AlterColumn<string>(
                name: "desc",
                schema: "public",
                table: "sys_menu_button",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "按钮描述",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "角色说明");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                schema: "public",
                table: "sys_menu_button",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                schema: "public",
                table: "sys_menu_button",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                schema: "public",
                table: "sys_menu_button",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                schema: "public",
                table: "sys_menu_button",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "按钮编码",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "业务唯一编码");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "public",
                table: "sys_menu_button",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                schema: "public",
                table: "sys_menu",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                schema: "public",
                table: "sys_menu",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                schema: "public",
                table: "sys_menu",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldComment: "菜单显示标题");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                schema: "public",
                table: "sys_menu",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "redirect",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true,
                oldComment: "菜单重定向路径");

            migrationBuilder.AlterColumn<string>(
                name: "path",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldComment: "前端路由访问路径");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_id",
                schema: "public",
                table: "sys_menu",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "上级节点主键；根节点为空");

            migrationBuilder.AlterColumn<int>(
                name: "order",
                schema: "public",
                table: "sys_menu",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "同级记录的显示顺序");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "public",
                table: "sys_menu",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldComment: "业务名称");

            migrationBuilder.AlterColumn<bool>(
                name: "multi_tab",
                schema: "public",
                table: "sys_menu",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true,
                oldComment: "路由是否在多页签中打开");

            migrationBuilder.AlterColumn<int>(
                name: "menu_type",
                schema: "public",
                table: "sys_menu",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "菜单节点类型");

            migrationBuilder.AlterColumn<string>(
                name: "local_icon",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true,
                oldComment: "本地图标资源标识");

            migrationBuilder.AlterColumn<string>(
                name: "layout",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true,
                oldComment: "菜单使用的前端布局标识");

            migrationBuilder.AlterColumn<bool>(
                name: "keep_alive",
                schema: "public",
                table: "sys_menu",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "路由页面是否启用缓存");

            migrationBuilder.AlterColumn<int>(
                name: "icon_type",
                schema: "public",
                table: "sys_menu",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "菜单图标来源类型");

            migrationBuilder.AlterColumn<string>(
                name: "icon",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true,
                oldComment: "菜单图标标识");

            migrationBuilder.AlterColumn<string>(
                name: "i18nKey",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true,
                oldComment: "菜单国际化资源键");

            migrationBuilder.AlterColumn<string>(
                name: "href",
                schema: "public",
                table: "sys_menu",
                type: "varchar(200)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldNullable: true,
                oldComment: "菜单跳转的外部链接");

            migrationBuilder.AlterColumn<bool>(
                name: "hide_in_menu",
                schema: "public",
                table: "sys_menu",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "是否在导航菜单中隐藏");

            migrationBuilder.AlterColumn<int>(
                name: "fixed_index_in_tab",
                schema: "public",
                table: "sys_menu",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "页签固定顺序索引");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                schema: "public",
                table: "sys_menu",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                schema: "public",
                table: "sys_menu",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                schema: "public",
                table: "sys_menu",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<bool>(
                name: "constant",
                schema: "public",
                table: "sys_menu",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "路由是否为常量路由");

            migrationBuilder.AlterColumn<string>(
                name: "component",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldNullable: true,
                oldComment: "前端路由加载的组件路径");

            migrationBuilder.AlterColumn<string>(
                name: "active_menu",
                schema: "public",
                table: "sys_menu",
                type: "varchar(200)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldNullable: true,
                oldComment: "进入路由时默认激活的菜单路径");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "public",
                table: "sys_menu",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "sys_department",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "sys_department",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "sys_department",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "sys_department",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<int>(
                name: "sort",
                table: "sys_department",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "同级记录的排序值");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "sys_department",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "备注",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_id",
                table: "sys_department",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "上级节点主键；根节点为空");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "sys_department",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "部门名称",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "业务名称");

            migrationBuilder.AlterColumn<string>(
                name: "leader_name",
                table: "sys_department",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "部门负责人名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "leader_id",
                table: "sys_department",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "部门负责人用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "sys_department",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "邮箱",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "电子邮箱地址");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "sys_department",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "sys_department",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "sys_department",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "sys_department",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "部门代码",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "业务唯一编码");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "sys_department",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "supplier",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "supplier",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "supplier",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "tax_no",
                table: "supplier",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "纳税人识别号");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "supplier",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "supplier",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "supplier",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldComment: "业务名称");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "supplier",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "supplier",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "supplier",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "contact_phone",
                table: "supplier",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "业务联系人电话号码");

            migrationBuilder.AlterColumn<string>(
                name: "contact_name",
                table: "supplier",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "业务联系人姓名");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "supplier",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "业务唯一编码");

            migrationBuilder.AlterColumn<string>(
                name: "bank_name",
                table: "supplier",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "开户银行名称");

            migrationBuilder.AlterColumn<string>(
                name: "bank_account",
                table: "supplier",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "银行账号");

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "supplier",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true,
                oldComment: "联系或经营地址");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "supplier",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "sale_order_detail",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "sale_order_detail",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "sale_order_detail",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_conversion",
                table: "sale_order_detail",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldComment: "下单单位换算为基础单位的比例");

            migrationBuilder.AlterColumn<decimal>(
                name: "total_price",
                table: "sale_order_detail",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldComment: "数量与单价计算后的总金额");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "sale_order_detail",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<Guid>(
                name: "sale_order_id",
                table: "sale_order_detail",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "来源销售订单主键");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "sale_order_detail",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "sale_order_detail",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldComment: "业务数量，按当前商品单位计量");

            migrationBuilder.AlterColumn<string>(
                name: "inner_remark",
                table: "sale_order_detail",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "仅内部人员可见的备注");

            migrationBuilder.AlterColumn<bool>(
                name: "has_purchase_plan",
                table: "sale_order_detail",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "是否已生成采购计划");

            migrationBuilder.AlterColumn<string>(
                name: "goods_unit_name_snapshot",
                table: "sale_order_detail",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "下单时的商品单位名称快照");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_unit_id",
                table: "sale_order_detail",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "下单商品单位主键");

            migrationBuilder.AlterColumn<string>(
                name: "goods_type_name_snapshot",
                table: "sale_order_detail",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "业务发生时的商品分类名称快照");

            migrationBuilder.AlterColumn<string>(
                name: "goods_name_snapshot",
                table: "sale_order_detail",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldComment: "业务发生时的商品名称快照");

            migrationBuilder.AlterColumn<string>(
                name: "goods_image_snapshot",
                table: "sale_order_detail",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务发生时的商品图片地址快照");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "sale_order_detail",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联商品主键");

            migrationBuilder.AlterColumn<string>(
                name: "goods_description_snapshot",
                table: "sale_order_detail",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true,
                oldComment: "业务发生时的商品描述快照");

            migrationBuilder.AlterColumn<string>(
                name: "goods_code_snapshot",
                table: "sale_order_detail",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "业务发生时的商品编码快照");

            migrationBuilder.AlterColumn<decimal>(
                name: "fixed_price",
                table: "sale_order_detail",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldComment: "订单商品固定单价");

            migrationBuilder.AlterColumn<string>(
                name: "fixed_goods_unit_name_snapshot",
                table: "sale_order_detail",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "下单时的计价单位名称快照");

            migrationBuilder.AlterColumn<Guid>(
                name: "fixed_goods_unit_id",
                table: "sale_order_detail",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "计价单位主键");

            migrationBuilder.AlterColumn<int>(
                name: "customer_check_status",
                table: "sale_order_detail",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0,
                oldComment: "客户验收状态");

            migrationBuilder.AlterColumn<decimal>(
                name: "customer_check_price",
                table: "sale_order_detail",
                type: "numeric(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true,
                oldComment: "客户验收确认金额");

            migrationBuilder.AlterColumn<decimal>(
                name: "customer_check_base_quantity",
                table: "sale_order_detail",
                type: "numeric(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldNullable: true,
                oldComment: "客户验收数量，按基础单位计量");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "sale_order_detail",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "sale_order_detail",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "sale_order_detail",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "base_unit_name_snapshot",
                table: "sale_order_detail",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "业务发生时的基础单位名称快照");

            migrationBuilder.AlterColumn<Guid>(
                name: "base_unit_id",
                table: "sale_order_detail",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "商品基础单位主键");

            migrationBuilder.AlterColumn<decimal>(
                name: "base_quantity",
                table: "sale_order_detail",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldComment: "按商品基础单位换算后的数量");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "sale_order_detail",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "ware_id",
                table: "sale_order",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "关联仓库主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "sale_order",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<bool>(
                name: "update_status",
                table: "sale_order",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "订单审核通过后是否发生过修改");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "sale_order",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "sale_order",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "sale_order",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<decimal>(
                name: "settlement_price",
                table: "sale_order",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldComment: "订单最终结算金额");

            migrationBuilder.AlterColumn<int>(
                name: "return_status",
                table: "sale_order",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0,
                oldComment: "订单回单状态");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "sale_order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<DateTime>(
                name: "receive_date",
                table: "sale_order",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "客户要求收货时间（UTC）");

            migrationBuilder.AlterColumn<Guid>(
                name: "quotation_id",
                table: "sale_order",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "销售报价单主键");

            migrationBuilder.AlterColumn<int>(
                name: "print_status",
                table: "sale_order",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0,
                oldComment: "订单打印状态");

            migrationBuilder.AlterColumn<int>(
                name: "out_storage_status",
                table: "sale_order",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0,
                oldComment: "销售出库单生成状态");

            migrationBuilder.AlterColumn<DateTime>(
                name: "out_date",
                table: "sale_order",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "计划或实际出库时间（UTC）");

            migrationBuilder.AlterColumn<int>(
                name: "order_status",
                table: "sale_order",
                type: "integer",
                nullable: false,
                defaultValue: -1,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: -1,
                oldComment: "销售订单当前业务状态");

            migrationBuilder.AlterColumn<decimal>(
                name: "order_price",
                table: "sale_order",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldComment: "订单销售总金额");

            migrationBuilder.AlterColumn<string>(
                name: "order_no",
                table: "sale_order",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "销售订单业务编号");

            migrationBuilder.AlterColumn<DateTime>(
                name: "order_date",
                table: "sale_order",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "客户下单时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "inner_remark",
                table: "sale_order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "仅内部人员可见的备注");

            migrationBuilder.AlterColumn<bool>(
                name: "has_purchase_plan",
                table: "sale_order",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "是否已生成采购计划");

            migrationBuilder.AlterColumn<bool>(
                name: "has_out_sale",
                table: "sale_order",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "是否已生成销售出库单");

            migrationBuilder.AlterColumn<string>(
                name: "delivery_address_snapshot",
                table: "sale_order",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true,
                oldComment: "下单时的配送地址快照");

            migrationBuilder.AlterColumn<string>(
                name: "customer_name_snapshot",
                table: "sale_order",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldComment: "业务发生时的客户名称快照");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "sale_order",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联客户主键");

            migrationBuilder.AlterColumn<string>(
                name: "customer_code_snapshot",
                table: "sale_order",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "下单时的客户编码快照");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "sale_order",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "sale_order",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "sale_order",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "contact_phone_snapshot",
                table: "sale_order",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "业务发生时的联系人电话快照");

            migrationBuilder.AlterColumn<string>(
                name: "contact_name_snapshot",
                table: "sale_order",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "业务发生时的联系人姓名快照");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "sale_order",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "quotation_goods",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "quotation_goods",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "quotation_goods",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "quotation_goods",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldComment: "当前商品单位对应的单价");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "quotation_goods",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "quotation_goods",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<Guid>(
                name: "quotation_id",
                table: "quotation_goods",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "销售报价单主键");

            migrationBuilder.AlterColumn<decimal>(
                name: "min_order_quantity",
                table: "quotation_goods",
                type: "numeric(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true,
                oldComment: "最小下单数量，按当前商品单位计量");

            migrationBuilder.AlterColumn<bool>(
                name: "is_on_sale",
                table: "quotation_goods",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true,
                oldComment: "商品是否允许上架销售");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_unit_id",
                table: "quotation_goods",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "下单商品单位主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "quotation_goods",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联商品主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "quotation_goods",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "quotation_goods",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "quotation_goods",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "quotation_goods",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "quotation",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "quotation",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "quotation",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "quotation",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "quotation",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldComment: "业务名称");

            migrationBuilder.AlterColumn<bool>(
                name: "is_audited",
                table: "quotation",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "报价单是否已审核通过");

            migrationBuilder.AlterColumn<DateTime>(
                name: "effective_start",
                table: "quotation",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "价格或协议有效期开始时间（UTC）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "effective_end",
                table: "quotation",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "价格或协议有效期结束时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "quotation",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true,
                oldComment: "业务描述");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "quotation",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "quotation",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "quotation",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "quotation",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "业务唯一编码");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "quotation",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "purchaser",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "关联系统用户主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "purchaser",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "purchaser",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "purchaser",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "purchaser",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "purchaser",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "purchaser",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "联系电话");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "purchaser",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "业务名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "department_id",
                table: "purchaser",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "所属部门主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "purchaser",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "purchaser",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "purchaser",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "purchaser",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "业务唯一编码");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "purchaser",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "purchase_rule_goods",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联商品主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "purchase_rule_id",
                table: "purchase_rule_goods",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "采购规则主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "purchase_rule_customer",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联客户主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "purchase_rule_id",
                table: "purchase_rule_customer",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "采购规则主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "ware_id",
                table: "purchase_rule",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "关联仓库主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "purchase_rule",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "purchase_rule",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "purchase_rule",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "supplier_id",
                table: "purchase_rule",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "关联供应商主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "purchase_rule",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "purchase_rule",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<Guid>(
                name: "purchaser_id",
                table: "purchase_rule",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "负责采购的采购员主键");

            migrationBuilder.AlterColumn<int>(
                name: "purchase_pattern",
                table: "purchase_rule",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1,
                oldComment: "采购模式：供应商直供或市场自采");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "purchase_rule",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldComment: "业务名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_type_id",
                table: "purchase_rule",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "商品分类主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "purchase_rule",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "purchase_rule",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "purchase_rule",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "purchase_rule",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "业务唯一编码");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "purchase_rule",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "purchase_plan_order_rel",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "purchase_plan_order_rel",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "purchase_plan_order_rel",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "purchase_plan_order_rel",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<Guid>(
                name: "sale_order_id",
                table: "purchase_plan_order_rel",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "来源销售订单主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "sale_order_detail_id",
                table: "purchase_plan_order_rel",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "来源销售订单商品明细主键");

            migrationBuilder.AlterColumn<decimal>(
                name: "required_quantity",
                table: "purchase_plan_order_rel",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldComment: "来源订单需求数量，按采购单位计量");

            migrationBuilder.AlterColumn<Guid>(
                name: "purchase_plan_detail_id",
                table: "purchase_plan_order_rel",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "采购计划商品明细主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "purchase_plan_order_rel",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "purchase_plan_order_rel",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "purchase_plan_order_rel",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "purchase_plan_order_rel",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "purchase_plan_detail",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "purchase_plan_detail",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "purchase_plan_detail",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "purchase_plan_detail",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<decimal>(
                name: "required_quantity",
                table: "purchase_plan_detail",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldComment: "来源订单需求数量，按采购单位计量");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "purchase_plan_detail",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<decimal>(
                name: "purchased_quantity",
                table: "purchase_plan_detail",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldComment: "已生成采购单的数量，按采购单位计量");

            migrationBuilder.AlterColumn<string>(
                name: "purchase_unit_name_snapshot",
                table: "purchase_plan_detail",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "计划生成时的采购单位名称快照");

            migrationBuilder.AlterColumn<Guid>(
                name: "purchase_unit_id",
                table: "purchase_plan_detail",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "采购计量单位主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "purchase_plan_id",
                table: "purchase_plan_detail",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "采购计划主键");

            migrationBuilder.AlterColumn<decimal>(
                name: "planned_quantity",
                table: "purchase_plan_detail",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldComment: "计划采购数量，按采购单位计量");

            migrationBuilder.AlterColumn<string>(
                name: "goods_name_snapshot",
                table: "purchase_plan_detail",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldComment: "业务发生时的商品名称快照");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "purchase_plan_detail",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联商品主键");

            migrationBuilder.AlterColumn<string>(
                name: "goods_code_snapshot",
                table: "purchase_plan_detail",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "业务发生时的商品编码快照");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "purchase_plan_detail",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "purchase_plan_detail",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "purchase_plan_detail",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "purchase_plan_detail",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "purchase_plan",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "purchase_plan",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "purchase_plan",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "supplier_name_snapshot",
                table: "purchase_plan",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true,
                oldComment: "计划生成时的供应商名称快照");

            migrationBuilder.AlterColumn<Guid>(
                name: "supplier_id",
                table: "purchase_plan",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "关联供应商主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "purchase_plan",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "purchase_plan",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<string>(
                name: "purchaser_name_snapshot",
                table: "purchase_plan",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true,
                oldComment: "计划生成时的采购员名称快照");

            migrationBuilder.AlterColumn<Guid>(
                name: "purchaser_id",
                table: "purchase_plan",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "负责采购的采购员主键");

            migrationBuilder.AlterColumn<int>(
                name: "purchase_status",
                table: "purchase_plan",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1,
                oldComment: "采购单生成进度状态");

            migrationBuilder.AlterColumn<int>(
                name: "purchase_pattern",
                table: "purchase_plan",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1,
                oldComment: "采购模式：供应商直供或市场自采");

            migrationBuilder.AlterColumn<string>(
                name: "plan_no",
                table: "purchase_plan",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "采购计划业务编号");

            migrationBuilder.AlterColumn<DateTime>(
                name: "plan_date",
                table: "purchase_plan",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "计划采购交期（UTC）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "purchase_plan",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "purchase_plan",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "purchase_plan",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "purchase_plan",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "order_audit_log",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "order_audit_log",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "order_audit_log",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "order_audit_log",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<Guid>(
                name: "sale_order_id",
                table: "order_audit_log",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "来源销售订单主键");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "order_audit_log",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<int>(
                name: "previous_status",
                table: "order_audit_log",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "审核动作发生前的订单状态");

            migrationBuilder.AlterColumn<int>(
                name: "current_status",
                table: "order_audit_log",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "审核动作完成后的订单状态");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "order_audit_log",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "order_audit_log",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "order_audit_log",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "audit_user_name_snapshot",
                table: "order_audit_log",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "审核时的用户名称快照");

            migrationBuilder.AlterColumn<Guid>(
                name: "audit_user_id",
                table: "order_audit_log",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "执行审核的系统用户主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "audit_time",
                table: "order_audit_log",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "审核动作发生时间（UTC）");

            migrationBuilder.AlterColumn<int>(
                name: "action",
                table: "order_audit_log",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "审核动作类型");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "order_audit_log",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "goods_unit",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "goods_unit",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "goods_unit",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "goods_unit",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<int>(
                name: "sort",
                table: "goods_unit",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "同级记录的排序值");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "goods_unit",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "goods_unit",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "业务名称");

            migrationBuilder.AlterColumn<bool>(
                name: "is_base_unit",
                table: "goods_unit",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "是否为商品基础计量单位");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "goods_unit",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联商品主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "goods_unit",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "goods_unit",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "goods_unit",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<decimal>(
                name: "conversion_rate",
                table: "goods_unit",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 1m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldDefaultValue: 1m,
                oldComment: "当前单位换算为基础单位的比例");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "goods_unit",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "业务唯一编码");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "goods_unit",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "goods_type",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "goods_type",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "goods_type",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "tax_policy_basis",
                table: "goods_type",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "税收优惠政策依据");

            migrationBuilder.AlterColumn<string>(
                name: "tax_category_name",
                table: "goods_type",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "税收分类名称");

            migrationBuilder.AlterColumn<string>(
                name: "tax_category_code",
                table: "goods_type",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "税收分类编码");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "goods_type",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<int>(
                name: "sort",
                table: "goods_type",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "同级记录的排序值");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "goods_type",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_id",
                table: "goods_type",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "上级节点主键；根节点为空");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "goods_type",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "业务名称");

            migrationBuilder.AlterColumn<bool>(
                name: "is_tax_exempt",
                table: "goods_type",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "商品分类是否免税");

            migrationBuilder.AlterColumn<string>(
                name: "invoice_goods_short_name",
                table: "goods_type",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "发票商品简称");

            migrationBuilder.AlterColumn<string>(
                name: "image_url",
                table: "goods_type",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "商品图片访问地址");

            migrationBuilder.AlterColumn<decimal>(
                name: "default_tax_rate",
                table: "goods_type",
                type: "numeric(8,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(8,4)",
                oldNullable: true,
                oldComment: "分类默认税率");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "goods_type",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "goods_type",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "goods_type",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "goods_type",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "业务唯一编码");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "goods_type",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<bool>(
                name: "is_default",
                table: "goods_supplier_rel",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "是否为默认关系或默认配置");

            migrationBuilder.AlterColumn<Guid>(
                name: "supplier_id",
                table: "goods_supplier_rel",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联供应商主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "goods_supplier_rel",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联商品主键");

            migrationBuilder.AlterColumn<string>(
                name: "url",
                table: "goods_image",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "被调用接口的请求地址");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "goods_image",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "goods_image",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "goods_image",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "goods_image",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<int>(
                name: "sort",
                table: "goods_image",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "同级记录的排序值");

            migrationBuilder.AlterColumn<bool>(
                name: "is_primary",
                table: "goods_image",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "是否为主要供货关系");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "goods_image",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联商品主键");

            migrationBuilder.AlterColumn<string>(
                name: "file_name",
                table: "goods_image",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "图片或附件文件名");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "goods_image",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "goods_image",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "goods_image",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "goods_image",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "goods",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "goods",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "goods",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<decimal>(
                name: "tax_rate",
                table: "goods",
                type: "numeric(8,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(8,4)",
                oldNullable: true,
                oldComment: "适用税率");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "goods",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "spec",
                table: "goods",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "商品规格型号");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "goods",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<string>(
                name: "origin",
                table: "goods",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "商品产地");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "goods",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldComment: "业务名称");

            migrationBuilder.AlterColumn<bool>(
                name: "is_on_sale",
                table: "goods",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true,
                oldComment: "商品是否允许上架销售");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_type_id",
                table: "goods",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "商品分类主键");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "goods",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true,
                oldComment: "业务描述");

            migrationBuilder.AlterColumn<Guid>(
                name: "default_ware_id",
                table: "goods",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "默认履约仓库主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "default_supplier_id",
                table: "goods",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "商品默认供应商主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "goods",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "goods",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "goods",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "goods",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "业务唯一编码");

            migrationBuilder.AlterColumn<string>(
                name: "brand",
                table: "goods",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "商品品牌");

            migrationBuilder.AlterColumn<Guid>(
                name: "base_unit_id",
                table: "goods",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "商品基础单位主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "goods",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_tag_id",
                table: "customer_tag_rel",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "客户标签主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "customer_tag_rel",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联客户主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "customer_tag",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "customer_tag",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "customer_tag",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "customer_tag",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<int>(
                name: "sort",
                table: "customer_tag",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "同级记录的排序值");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "customer_tag",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_id",
                table: "customer_tag",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "上级节点主键；根节点为空");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "customer_tag",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "业务名称");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "customer_tag",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "customer_tag",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "customer_tag",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "customer_tag",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "业务唯一编码");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "customer_tag",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "customer_sub_account",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "用于登录的唯一用户名");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "customer_sub_account",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "customer_sub_account",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "customer_sub_account",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "customer_sub_account",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "customer_sub_account",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "customer_sub_account",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "联系电话");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "customer_sub_account",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true,
                oldComment: "不可逆的登录密码哈希");

            migrationBuilder.AlterColumn<string>(
                name: "nick_name",
                table: "customer_sub_account",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "用户显示昵称");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "customer_sub_account",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "电子邮箱地址");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "customer_sub_account",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "关联客户主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "customer_sub_account",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "customer_sub_account",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "customer_sub_account",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "company_id",
                table: "customer_sub_account",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "所属公司主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "customer_sub_account",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<bool>(
                name: "is_default",
                table: "customer_quotation",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "是否为默认关系或默认配置");

            migrationBuilder.AlterColumn<DateTime>(
                name: "effective_start",
                table: "customer_quotation",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "价格或协议有效期开始时间（UTC）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "effective_end",
                table: "customer_quotation",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "价格或协议有效期结束时间（UTC）");

            migrationBuilder.AlterColumn<Guid>(
                name: "quotation_id",
                table: "customer_quotation",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "销售报价单主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "customer_quotation",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联客户主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "customer_protocol_goods",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "customer_protocol_goods",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "customer_protocol_goods",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "customer_protocol_goods",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "customer_protocol_goods",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<decimal>(
                name: "protocol_price",
                table: "customer_protocol_goods",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldComment: "协议约定的商品单价");

            migrationBuilder.AlterColumn<decimal>(
                name: "min_order_quantity",
                table: "customer_protocol_goods",
                type: "numeric(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true,
                oldComment: "最小下单数量，按当前商品单位计量");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_unit_id",
                table: "customer_protocol_goods",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "下单商品单位主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "goods_id",
                table: "customer_protocol_goods",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联商品主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_protocol_id",
                table: "customer_protocol_goods",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "客户协议价主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "customer_protocol_goods",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "customer_protocol_goods",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "customer_protocol_goods",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "customer_protocol_goods",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "customer_protocol_customer",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "关联客户主键");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_protocol_id",
                table: "customer_protocol_customer",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "客户协议价主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "customer_protocol",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "customer_protocol",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "customer_protocol",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "customer_protocol",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "customer_protocol",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<Guid>(
                name: "quotation_id",
                table: "customer_protocol",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "销售报价单主键");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "customer_protocol",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldComment: "业务名称");

            migrationBuilder.AlterColumn<DateTime>(
                name: "effective_start",
                table: "customer_protocol",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "价格或协议有效期开始时间（UTC）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "effective_end",
                table: "customer_protocol",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "价格或协议有效期结束时间（UTC）");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "customer_protocol",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "customer_protocol",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "customer_protocol",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "customer_protocol",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "业务唯一编码");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "customer_protocol",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "customer",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "customer",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "customer",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "unified_social_credit_code",
                table: "customer",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "企业统一社会信用代码");

            migrationBuilder.AlterColumn<string>(
                name: "taxpayer_identification_number",
                table: "customer",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "客户纳税人识别号");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "customer",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "customer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<string>(
                name: "registration_status",
                table: "customer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "企业工商登记状态");

            migrationBuilder.AlterColumn<string>(
                name: "registration_authority",
                table: "customer",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "企业登记机关");

            migrationBuilder.AlterColumn<string>(
                name: "registered_capital",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "企业登记注册资本");

            migrationBuilder.AlterColumn<string>(
                name: "registered_address",
                table: "customer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "企业工商注册地址");

            migrationBuilder.AlterColumn<Guid>(
                name: "quotation_id",
                table: "customer",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "销售报价单主键");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "customer",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldComment: "业务名称");

            migrationBuilder.AlterColumn<string>(
                name: "legal_representative",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "企业法定代表人");

            migrationBuilder.AlterColumn<string>(
                name: "invoice_title",
                table: "customer",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "发票抬头");

            migrationBuilder.AlterColumn<string>(
                name: "invoice_receiver_phone",
                table: "customer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "发票收件人电话");

            migrationBuilder.AlterColumn<string>(
                name: "invoice_receiver_name",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "发票收件人姓名");

            migrationBuilder.AlterColumn<string>(
                name: "invoice_receiver_address",
                table: "customer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "发票收件地址");

            migrationBuilder.AlterColumn<string>(
                name: "invoice_phone",
                table: "customer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "开票联系电话");

            migrationBuilder.AlterColumn<string>(
                name: "invoice_email",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "发票接收邮箱");

            migrationBuilder.AlterColumn<string>(
                name: "invoice_address",
                table: "customer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "开票登记地址");

            migrationBuilder.AlterColumn<DateTime>(
                name: "establish_date",
                table: "customer",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "企业成立日期");

            migrationBuilder.AlterColumn<Guid>(
                name: "default_ware_id",
                table: "customer",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "默认履约仓库主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "customer",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "customer",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "customer",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "contact_phone",
                table: "customer",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "业务联系人电话号码");

            migrationBuilder.AlterColumn<string>(
                name: "contact_name",
                table: "customer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "业务联系人姓名");

            migrationBuilder.AlterColumn<Guid>(
                name: "company_id",
                table: "customer",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "所属公司主键");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "customer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "业务唯一编码");

            migrationBuilder.AlterColumn<string>(
                name: "business_term",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "企业登记的营业期限");

            migrationBuilder.AlterColumn<string>(
                name: "business_scope",
                table: "customer",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "企业登记的经营范围");

            migrationBuilder.AlterColumn<string>(
                name: "bank_name",
                table: "customer",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "开户银行名称");

            migrationBuilder.AlterColumn<string>(
                name: "bank_account",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "银行账号");

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "customer",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true,
                oldComment: "联系或经营地址");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "customer",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "company",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "记录最后修改时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "update_name",
                table: "company",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "最后修改记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "update_by",
                table: "company",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "最后修改记录的用户主键");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "company",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "记录启用状态");

            migrationBuilder.AlterColumn<string>(
                name: "remark",
                table: "company",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "业务备注");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "company",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldComment: "业务名称");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "company",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "记录创建时间（UTC）");

            migrationBuilder.AlterColumn<string>(
                name: "create_name",
                table: "company",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true,
                oldComment: "创建记录的用户名称");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "company",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "创建记录的用户主键");

            migrationBuilder.AlterColumn<string>(
                name: "contact_phone",
                table: "company",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "业务联系人电话号码");

            migrationBuilder.AlterColumn<string>(
                name: "contact_name",
                table: "company",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "业务联系人姓名");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "company",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "业务唯一编码");

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "company",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true,
                oldComment: "联系或经营地址");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "company",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()",
                oldComment: "记录主键");
        }
    }
}
