using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiPersistenceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_conversation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "会话所属系统用户主键，是所有会话查询的隔离边界"),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "会话列表展示标题，不包含模型推理内容"),
                    conversation_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "会话状态：1 活动，2 用户已删除并等待清理"),
                    last_message_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "最近一条会话消息写入时间（UTC）"),
                    retain_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP + INTERVAL '30 days'", comment: "会话及其消息最早可清理时间（UTC），默认保留 30 天"),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "用户删除会话的时间（UTC），活动会话为空"),
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
                    table.PrimaryKey("PK_ai_conversation", x => x.id);
                    table.UniqueConstraint("ak_ai_conversation_id_user_id", x => new { x.id, x.user_id });
                    table.CheckConstraint("ck_ai_conversation_deleted_time", "conversation_status = 1 OR deleted_time IS NOT NULL");
                    table.CheckConstraint("ck_ai_conversation_status", "conversation_status IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_ai_conversation_sys_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "sys_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "AI 助手会话，按系统用户隔离并控制消息保留期限");

            migrationBuilder.CreateTable(
                name: "mcp_access_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "个人 MCP 令牌所属系统用户主键"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "用户为个人 MCP 令牌设置的用途名称"),
                    prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "用于列表识别和令牌定位的非敏感随机前缀"),
                    token_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false, comment: "使用服务端独立密钥计算的 HMAC-SHA256 十六进制哈希"),
                    scopes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "以空格分隔并按稳定顺序保存的授权范围集合"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "个人 MCP 令牌失效时间（UTC）"),
                    revoked_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "个人 MCP 令牌被用户撤销的时间（UTC）"),
                    last_used_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "个人 MCP 令牌最近一次通过鉴权的时间（UTC）"),
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
                    table.PrimaryKey("PK_mcp_access_token", x => x.id);
                    table.CheckConstraint("ck_mcp_access_token_scopes", "char_length(btrim(scopes)) > 0");
                    table.ForeignKey(
                        name: "FK_mcp_access_token_sys_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "sys_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "个人 MCP 访问令牌元数据，只保存非敏感前缀和 HMAC-SHA256 哈希");

            migrationBuilder.CreateTable(
                name: "ai_action_draft",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "来源 AI 会话主键，外部 MCP 独立生成时为空"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "草稿所属用户主键，也是唯一允许确认和执行的用户"),
                    operation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "能力目录中的稳定 operationId，确认和执行时不得改变"),
                    canonical_arguments_json = table.Column<string>(type: "jsonb", nullable: false, comment: "属性按序且无额外空白的规范化业务参数 JSON"),
                    arguments_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false, comment: "绑定所属用户、operationId 和规范化参数的 SHA-256 哈希"),
                    risk_level = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "草稿风险等级：1 普通写操作，2 高风险操作"),
                    confirmation_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "展示给用户的最小业务变更摘要，不含密钥或完整敏感响应"),
                    draft_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "草稿状态：1 待确认，2 已确认，3 执行中，4 已执行，5 失败，6 过期，7 取消"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP + INTERVAL '30 minutes'", comment: "草稿失效时间（UTC），默认自生成起 30 分钟"),
                    confirmed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "实际确认草稿的系统用户主键，必须与草稿所属用户一致"),
                    confirmed_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "草稿通过人工确认的时间（UTC）"),
                    executed_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "原业务接口执行结束的时间（UTC）"),
                    execution_result_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "可追溯执行结果的非敏感引用，不保存完整工具响应"),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "草稿生成请求的用户级幂等键"),
                    concurrency_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L, comment: "草稿状态流转的乐观并发版本，每次更新必须递增"),
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
                    table.PrimaryKey("PK_ai_action_draft", x => x.id);
                    table.CheckConstraint("ck_ai_action_draft_concurrency_version", "concurrency_version > 0");
                    table.CheckConstraint("ck_ai_action_draft_confirmation", "draft_status IN (1, 6, 7) OR (confirmed_by_user_id IS NOT NULL AND confirmed_time IS NOT NULL)");
                    table.CheckConstraint("ck_ai_action_draft_confirmed_owner", "confirmed_by_user_id IS NULL OR confirmed_by_user_id = user_id");
                    table.CheckConstraint("ck_ai_action_draft_hash", "char_length(arguments_hash) = 64");
                    table.CheckConstraint("ck_ai_action_draft_operation", "position('.' in operation_id) > 1");
                    table.CheckConstraint("ck_ai_action_draft_risk", "risk_level BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_ai_action_draft_status", "draft_status BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_ai_action_draft_ai_conversation_conversation_id_user_id",
                        columns: x => new { x.conversation_id, x.user_id },
                        principalTable: "ai_conversation",
                        principalColumns: new[] { "id", "user_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_action_draft_sys_user_confirmed_by_user_id",
                        column: x => x.confirmed_by_user_id,
                        principalSchema: "public",
                        principalTable: "sys_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_action_draft_sys_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "sys_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "AI 通用写操作草稿，绑定用户、operationId 和规范化参数并等待人工确认");

            migrationBuilder.CreateTable(
                name: "ai_message",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属 AI 会话主键"),
                    role = table.Column<int>(type: "integer", nullable: false, comment: "消息角色：1 系统，2 用户，3 助手，4 工具摘要"),
                    content = table.Column<string>(type: "text", nullable: false, comment: "最终可展示文字，不得包含模型推理或完整敏感工具结果"),
                    sequence = table.Column<long>(type: "bigint", nullable: false, comment: "消息在会话内单调递增的游标序号"),
                    message_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "消息状态：1 生成中，2 已完成，3 失败，4 已取消"),
                    provider_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "生成助手消息所用的统一 Provider 名称"),
                    model_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "生成助手消息所用的模型名称"),
                    tool_call_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "工具调用在当前模型回合中的标识"),
                    tool_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "被调用的白名单工具名称"),
                    tool_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "工具执行状态的稳定文本值"),
                    tool_summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "工具结果的必要脱敏摘要，不保存完整业务响应"),
                    source_references = table.Column<string>(type: "jsonb", nullable: true, comment: "知识来源 JSON 数组，只允许来源 slug 和标题"),
                    completed_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "消息进入完成、失败或取消终态的时间（UTC）"),
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
                    table.PrimaryKey("PK_ai_message", x => x.id);
                    table.CheckConstraint("ck_ai_message_role", "role BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_ai_message_sequence", "sequence > 0");
                    table.CheckConstraint("ck_ai_message_status", "message_status BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_ai_message_ai_conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "ai_conversation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "AI 会话消息，只保存最终文字及脱敏后的工具和来源摘要");

            migrationBuilder.CreateTable(
                name: "ai_order_draft",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "来源 AI 会话主键，外部 MCP 独立生成时为空"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "草稿所属用户主键，也是唯一允许人工确认的用户"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "草稿下单客户主键"),
                    customer_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "草稿生成时的客户名称快照"),
                    customer_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "草稿生成时的客户编码快照"),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "草稿采用的默认报价单主键"),
                    ware_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "草稿指定的履约仓库主键"),
                    order_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "草稿订单业务日期（UTC），用于解析有效价格"),
                    receive_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "客户要求收货时间（UTC）"),
                    contact_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "草稿生成时的联系人姓名快照"),
                    contact_phone_snapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, comment: "草稿生成时的联系人电话快照"),
                    delivery_address_snapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "草稿生成时的配送地址快照"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "用户提供的订单业务备注，不保存无关内部信息"),
                    draft_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "草稿状态：1 待确认，2 已确认，3 已过期，4 已取消"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP + INTERVAL '30 minutes'", comment: "草稿失效时间（UTC），默认自生成起 30 分钟"),
                    confirmed_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "草稿成功创建正式订单的确认时间（UTC）"),
                    sale_order_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "草稿确认后创建的正式销售订单主键"),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "草稿生成请求的用户级幂等键"),
                    concurrency_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L, comment: "草稿状态更新的乐观并发版本，流转时必须递增"),
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
                    table.PrimaryKey("PK_ai_order_draft", x => x.id);
                    table.CheckConstraint("ck_ai_order_draft_concurrency_version", "concurrency_version > 0");
                    table.CheckConstraint("ck_ai_order_draft_status", "draft_status BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_ai_order_draft_ai_conversation_conversation_id_user_id",
                        columns: x => new { x.conversation_id, x.user_id },
                        principalTable: "ai_conversation",
                        principalColumns: new[] { "id", "user_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_order_draft_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_order_draft_quotation_quotation_id",
                        column: x => x.quotation_id,
                        principalTable: "quotation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ai_order_draft_sale_order_sale_order_id",
                        column: x => x.sale_order_id,
                        principalTable: "sale_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ai_order_draft_sys_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "sys_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_order_draft_ware_ware_id",
                        column: x => x.ware_id,
                        principalTable: "ware",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "AI 生成的销售订单草稿，必须由所属用户在有效期内人工确认");

            migrationBuilder.CreateTable(
                name: "ai_order_draft_detail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    ai_order_draft_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属 AI 订单草稿主键"),
                    sort_order = table.Column<int>(type: "integer", nullable: false, comment: "商品行在草稿中的稳定展示顺序，从 1 开始"),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "草稿下单商品主键"),
                    goods_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "草稿生成时的商品名称快照"),
                    goods_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "草稿生成时的商品编码快照"),
                    goods_unit_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "下单数量使用的商品单位主键"),
                    goods_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "草稿生成时的下单单位名称快照"),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, comment: "按下单商品单位计量的业务数量"),
                    base_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, comment: "草稿生成时换算到商品基础单位的数量"),
                    base_unit_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "商品基础单位主键"),
                    base_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "草稿生成时的基础单位名称快照"),
                    unit_conversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, comment: "下单单位换算为基础单位的比例快照"),
                    fixed_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "草稿生成时解析或由用户提供的固定单价"),
                    fixed_goods_unit_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "固定单价对应的计价单位主键"),
                    fixed_goods_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "草稿生成时的计价单位名称快照"),
                    price_source = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "价格来源：1 未解析，2 客户协议价，3 默认报价，4 用户提供"),
                    price_source_record_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "协议价商品或报价商品来源记录主键"),
                    price_source_updated_time_snapshot = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "价格来源记录在草稿生成时的最后更新时间（UTC）"),
                    minimum_order_quantity_snapshot = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true, comment: "价格来源在草稿生成时要求的最小起订数量"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "用户提供的当前草稿商品行业务备注"),
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
                    table.PrimaryKey("PK_ai_order_draft_detail", x => x.id);
                    table.CheckConstraint("ck_ai_order_draft_detail_price_source", "price_source BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_ai_order_draft_detail_prices", "fixed_price >= 0 AND (minimum_order_quantity_snapshot IS NULL OR minimum_order_quantity_snapshot > 0)");
                    table.CheckConstraint("ck_ai_order_draft_detail_quantities", "quantity > 0 AND base_quantity > 0 AND unit_conversion > 0");
                    table.CheckConstraint("ck_ai_order_draft_detail_sort", "sort_order > 0");
                    table.CheckConstraint("ck_ai_order_draft_detail_source_record", "(price_source IN (2, 3) AND price_source_record_id IS NOT NULL) OR (price_source IN (1, 4) AND price_source_record_id IS NULL)");
                    table.ForeignKey(
                        name: "FK_ai_order_draft_detail_ai_order_draft_ai_order_draft_id",
                        column: x => x.ai_order_draft_id,
                        principalTable: "ai_order_draft",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_order_draft_detail_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_order_draft_detail_goods_unit_base_unit_id",
                        column: x => x.base_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ai_order_draft_detail_goods_unit_fixed_goods_unit_id",
                        column: x => x.fixed_goods_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_order_draft_detail_goods_unit_goods_unit_id",
                        column: x => x.goods_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "AI 订单草稿商品明细，保存确认前重新校验所需的价格与单位快照");

            migrationBuilder.CreateIndex(
                name: "idx_ai_action_draft_operation_create_time",
                table: "ai_action_draft",
                columns: new[] { "operation_id", "create_time", "id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "idx_ai_action_draft_status_expires_at",
                table: "ai_action_draft",
                columns: new[] { "draft_status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "idx_ai_action_draft_user_idempotency",
                table: "ai_action_draft",
                columns: new[] { "user_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ai_action_draft_user_status_expires_at",
                table: "ai_action_draft",
                columns: new[] { "user_id", "draft_status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_action_draft_confirmed_by_user_id",
                table: "ai_action_draft",
                column: "confirmed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_action_draft_conversation_id_user_id",
                table: "ai_action_draft",
                columns: new[] { "conversation_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "idx_ai_conversation_status_retain_until",
                table: "ai_conversation",
                columns: new[] { "conversation_status", "retain_until" });

            migrationBuilder.CreateIndex(
                name: "idx_ai_conversation_user_last_message",
                table: "ai_conversation",
                columns: new[] { "user_id", "last_message_time", "id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "idx_ai_message_conversation_sequence",
                table: "ai_message",
                columns: new[] { "conversation_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ai_order_draft_sale_order_id",
                table: "ai_order_draft",
                column: "sale_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ai_order_draft_status_expires_at",
                table: "ai_order_draft",
                columns: new[] { "draft_status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "idx_ai_order_draft_user_idempotency",
                table: "ai_order_draft",
                columns: new[] { "user_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ai_order_draft_user_status_expires_at",
                table: "ai_order_draft",
                columns: new[] { "user_id", "draft_status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_order_draft_conversation_id_user_id",
                table: "ai_order_draft",
                columns: new[] { "conversation_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_order_draft_customer_id",
                table: "ai_order_draft",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_order_draft_quotation_id",
                table: "ai_order_draft",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_order_draft_ware_id",
                table: "ai_order_draft",
                column: "ware_id");

            migrationBuilder.CreateIndex(
                name: "idx_ai_order_draft_detail_base_unit_id",
                table: "ai_order_draft_detail",
                column: "base_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_ai_order_draft_detail_draft_sort",
                table: "ai_order_draft_detail",
                columns: new[] { "ai_order_draft_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ai_order_draft_detail_fixed_unit_id",
                table: "ai_order_draft_detail",
                column: "fixed_goods_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_ai_order_draft_detail_goods_id",
                table: "ai_order_draft_detail",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_ai_order_draft_detail_goods_unit_id",
                table: "ai_order_draft_detail",
                column: "goods_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_mcp_access_token_hash",
                table: "mcp_access_token",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_mcp_access_token_prefix",
                table: "mcp_access_token",
                column: "prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_mcp_access_token_user_create_time",
                table: "mcp_access_token",
                columns: new[] { "user_id", "create_time", "id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "idx_mcp_access_token_user_expires_at",
                table: "mcp_access_token",
                columns: new[] { "user_id", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_action_draft");

            migrationBuilder.DropTable(
                name: "ai_message");

            migrationBuilder.DropTable(
                name: "ai_order_draft_detail");

            migrationBuilder.DropTable(
                name: "mcp_access_token");

            migrationBuilder.DropTable(
                name: "ai_order_draft");

            migrationBuilder.DropTable(
                name: "ai_conversation");
        }
    }
}
