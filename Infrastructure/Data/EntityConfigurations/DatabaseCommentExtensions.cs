using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 为 PostgreSQL 模型统一配置可落库的表和列注释。
/// </summary>
public static class DatabaseCommentExtensions
{
    private static readonly IReadOnlyDictionary<Type, string> TableComments = new Dictionary<Type, string>
    {
        [typeof(Company)] = "公司档案，记录客户所属经营主体的基础资料",
        [typeof(Customer)] = "客户档案，记录下单、开票和默认履约信息",
        [typeof(CustomerSubAccount)] = "客户子账号，记录公司下可登录的客户侧账号",
        [typeof(CustomerTag)] = "客户标签，用于客户分类、筛选和业务规则匹配",
        [typeof(CustomerTagRelation)] = "客户与客户标签的多对多关系",
        [typeof(Department)] = "系统部门，维护组织层级和负责人信息",
        [typeof(Goods)] = "商品档案，记录销售与采购共用的商品基础资料",
        [typeof(GoodsImage)] = "商品图片，记录商品关联图片及展示顺序",
        [typeof(GoodsSupplierRelation)] = "商品与可供货供应商的多对多关系",
        [typeof(GoodsType)] = "商品分类，维护商品层级和税务分类信息",
        [typeof(GoodsUnit)] = "商品单位，记录单位换算、价格和起订数量",
        [typeof(Menu)] = "系统菜单，维护前端路由、组件和显示行为",
        [typeof(MenuButton)] = "菜单按钮权限，维护菜单下可授权的操作编码",
        [typeof(OperationLog)] = "操作日志，记录接口调用结果和运行环境",
        [typeof(OrderAuditLog)] = "销售订单审核记录，保存每次状态流转轨迹",
        [typeof(SaleOrder)] = "销售订单，记录客户下单、金额和履约状态",
        [typeof(SaleOrderDetail)] = "销售订单商品明细，保存商品、单位、价格和验收快照",
        [typeof(CustomerProtocol)] = "客户协议价，维护客户与商品的有效期价格协议",
        [typeof(CustomerProtocolCustomer)] = "客户协议价与适用客户的多对多关系",
        [typeof(CustomerProtocolGoods)] = "客户协议价商品明细，记录协议商品价格",
        [typeof(CustomerQuotation)] = "客户与报价单的多对多关系",
        [typeof(Quotation)] = "销售报价单，记录报价有效期和审核状态",
        [typeof(QuotationGoods)] = "报价单商品明细，记录商品、单位和报价",
        [typeof(Purchaser)] = "采购员档案，关联负责采购的系统用户和部门",
        [typeof(PurchaseOrder)] = "采购单，记录供货方、采购责任人、预计到货和执行状态",
        [typeof(PurchaseOrderDetail)] = "采购单商品明细，记录商品、单位、数量、价格和生产日期快照",
        [typeof(PurchaseOrderPlanRelation)] = "采购单明细与来源采购计划明细的数量关联",
        [typeof(PurchasePlan)] = "采购计划，记录交期、采购模式和执行状态",
        [typeof(PurchasePlanDetail)] = "采购计划商品明细，记录需求、计划和已采购数量",
        [typeof(PurchasePlanOrderRelation)] = "采购计划明细与来源销售订单明细的关系",
        [typeof(PurchaseRule)] = "采购规则，按客户、商品和仓库匹配采购责任方",
        [typeof(PurchaseRuleCustomer)] = "采购规则与适用客户的多对多关系",
        [typeof(PurchaseRuleGoods)] = "采购规则与适用商品的多对多关系",
        [typeof(Supplier)] = "供应商档案，记录供货方联系、银行和税务资料",
        [typeof(Role)] = "系统角色，维护角色身份和数据权限范围",
        [typeof(RoleMenu)] = "角色与菜单权限的多对多关系",
        [typeof(User)] = "系统用户，记录登录身份、组织归属和个人资料",
        [typeof(UserRole)] = "系统用户与角色的多对多关系",
        [typeof(Ware)] = "仓库档案，记录仓库联系信息和启用状态"
    };

    private static readonly IReadOnlyDictionary<string, string> PropertyComments = new Dictionary<string, string>
    {
        ["Action"] = "审核动作类型",
        ["ActiveMenu"] = "进入路由时默认激活的菜单路径",
        ["Address"] = "联系或经营地址",
        ["AllocatedQuantity"] = "采购单从来源计划占用的数量，按采购单位计量",
        ["AuditTime"] = "审核动作发生时间（UTC）",
        ["AuditUserId"] = "执行审核的系统用户主键",
        ["AuditUserNameSnapshot"] = "审核时的用户名称快照",
        ["BankAccount"] = "银行账号",
        ["BankName"] = "开户银行名称",
        ["BaseQuantity"] = "按商品基础单位换算后的数量",
        ["BaseUnitId"] = "商品基础单位主键",
        ["BaseUnitNameSnapshot"] = "业务发生时的基础单位名称快照",
        ["Brand"] = "商品品牌",
        ["Browser"] = "发起操作的浏览器信息",
        ["BusinessScope"] = "企业登记的经营范围",
        ["BusinessStatus"] = "采购单执行状态：草稿、已完成或已取消",
        ["BusinessTerm"] = "企业登记的营业期限",
        ["Code"] = "业务唯一编码",
        ["CompanyId"] = "所属公司主键",
        ["Component"] = "前端路由加载的组件路径",
        ["Constant"] = "路由是否为常量路由",
        ["ContactName"] = "业务联系人姓名",
        ["ContactNameSnapshot"] = "业务发生时的联系人姓名快照",
        ["ContactPhone"] = "业务联系人电话号码",
        ["ContactPhoneSnapshot"] = "业务发生时的联系人电话快照",
        ["ConversionRate"] = "当前单位换算为基础单位的比例",
        ["CreateBy"] = "创建记录的用户主键",
        ["CreateName"] = "创建记录的用户名称",
        ["CreateTime"] = "记录创建时间（UTC）",
        ["CurrentStatus"] = "审核动作完成后的订单状态",
        ["CustomerCheckBaseQuantity"] = "客户验收数量，按基础单位计量",
        ["CustomerCheckPrice"] = "客户验收确认金额",
        ["CustomerCheckStatus"] = "客户验收状态",
        ["CustomerCodeSnapshot"] = "下单时的客户编码快照",
        ["CustomerId"] = "关联客户主键",
        ["CustomerNameSnapshot"] = "业务发生时的客户名称快照",
        ["CustomerProtocolId"] = "客户协议价主键",
        ["CustomerTagId"] = "客户标签主键",
        ["DefaultSupplierId"] = "商品默认供应商主键",
        ["DefaultTaxRate"] = "分类默认税率",
        ["DefaultWareId"] = "默认履约仓库主键",
        ["DeliveryAddressSnapshot"] = "下单时的配送地址快照",
        ["DepartmentId"] = "所属部门主键",
        ["Desc"] = "角色说明",
        ["Description"] = "业务描述",
        ["EffectiveEnd"] = "价格或协议有效期结束时间（UTC）",
        ["EffectiveStart"] = "价格或协议有效期开始时间（UTC）",
        ["Email"] = "电子邮箱地址",
        ["ErrorMessage"] = "操作失败时记录的错误摘要",
        ["EstablishDate"] = "企业成立日期",
        ["ExecutionDuration"] = "接口执行耗时，单位为毫秒",
        ["FileName"] = "图片或附件文件名",
        ["FixedGoodsUnitId"] = "计价单位主键",
        ["FixedGoodsUnitNameSnapshot"] = "下单时的计价单位名称快照",
        ["FixedIndexInTab"] = "页签固定顺序索引",
        ["FixedPrice"] = "订单商品固定单价",
        ["Gender"] = "用户性别编码",
        ["GoodsCodeSnapshot"] = "业务发生时的商品编码快照",
        ["GoodsDescriptionSnapshot"] = "业务发生时的商品描述快照",
        ["GoodsId"] = "关联商品主键",
        ["GoodsImageSnapshot"] = "业务发生时的商品图片地址快照",
        ["GoodsInfoSnapshot"] = "采购发生时序列化保存的商品详情快照",
        ["GoodsNameSnapshot"] = "业务发生时的商品名称快照",
        ["GoodsTypeId"] = "商品分类主键",
        ["GoodsTypeNameSnapshot"] = "业务发生时的商品分类名称快照",
        ["GoodsUnitId"] = "下单商品单位主键",
        ["GoodsUnitNameSnapshot"] = "下单时的商品单位名称快照",
        ["HasOutSale"] = "是否已生成销售出库单",
        ["HasPurchasePlan"] = "是否已生成采购计划",
        ["HideInMenu"] = "是否在导航菜单中隐藏",
        ["Href"] = "菜单跳转的外部链接",
        ["I18NKey"] = "菜单国际化资源键",
        ["Icon"] = "菜单图标标识",
        ["IconType"] = "菜单图标来源类型",
        ["Id"] = "记录主键",
        ["ImageUrl"] = "商品图片访问地址",
        ["InnerRemark"] = "仅内部人员可见的备注",
        ["InvoiceAddress"] = "开票登记地址",
        ["InvoiceEmail"] = "发票接收邮箱",
        ["InvoiceGoodsShortName"] = "发票商品简称",
        ["InvoicePhone"] = "开票联系电话",
        ["InvoiceReceiverAddress"] = "发票收件地址",
        ["InvoiceReceiverName"] = "发票收件人姓名",
        ["InvoiceReceiverPhone"] = "发票收件人电话",
        ["InvoiceTitle"] = "发票抬头",
        ["IpAddress"] = "发起操作的客户端 IP 地址",
        ["IsAudited"] = "报价单是否已审核通过",
        ["IsBaseUnit"] = "是否为商品基础计量单位",
        ["IsDefault"] = "是否为默认关系或默认配置",
        ["IsOnSale"] = "商品是否允许上架销售",
        ["IsPrimary"] = "是否为主要供货关系",
        ["IsSuccess"] = "操作是否执行成功",
        ["IsTaxExempt"] = "商品分类是否免税",
        ["KeepAlive"] = "路由页面是否启用缓存",
        ["Layout"] = "菜单使用的前端布局标识",
        ["LeaderId"] = "部门负责人用户主键",
        ["LeaderName"] = "部门负责人名称",
        ["LegalRepresentative"] = "企业法定代表人",
        ["LocalIcon"] = "本地图标资源标识",
        ["Location"] = "仓库或业务地点说明",
        ["MenuId"] = "关联菜单主键",
        ["MenuType"] = "菜单节点类型",
        ["Method"] = "HTTP 请求方法",
        ["MinOrderQuantity"] = "最小下单数量，按当前商品单位计量",
        ["Module"] = "操作所属业务模块",
        ["MultiTab"] = "路由是否在多页签中打开",
        ["Name"] = "业务名称",
        ["NickName"] = "用户显示昵称",
        ["OperationType"] = "操作类型",
        ["Order"] = "同级记录的显示顺序",
        ["OrderDate"] = "客户下单时间（UTC）",
        ["OrderNo"] = "销售订单业务编号",
        ["OrderPrice"] = "订单销售总金额",
        ["OrderStatus"] = "销售订单当前业务状态",
        ["Origin"] = "商品产地",
        ["Os"] = "发起操作的客户端操作系统",
        ["OutDate"] = "计划或实际出库时间（UTC）",
        ["OutStorageStatus"] = "销售出库单生成状态",
        ["ParentId"] = "上级节点主键；根节点为空",
        ["PasswordHash"] = "不可逆的登录密码哈希",
        ["Path"] = "前端路由访问路径",
        ["Phone"] = "联系电话",
        ["PlanDate"] = "计划采购交期（UTC）",
        ["PlannedQuantity"] = "计划采购数量，按采购单位计量",
        ["PlanNo"] = "采购计划业务编号",
        ["PreviousStatus"] = "审核动作发生前的订单状态",
        ["PrintStatus"] = "订单打印状态",
        ["ProductDate"] = "采购商品生产日期，仅记录自然日",
        ["ProtocolPrice"] = "协议约定的商品单价",
        ["PurchasedQuantity"] = "已生成采购单的数量，按采购单位计量",
        ["PurchasePattern"] = "采购模式：供应商直供或市场自采",
        ["PurchaseNo"] = "采购单业务唯一编号",
        ["PurchaseOrderDetailId"] = "采购单商品明细主键",
        ["PurchaseOrderId"] = "采购单主键",
        ["PurchasePlanDetailId"] = "采购计划商品明细主键",
        ["PurchasePlanId"] = "采购计划主键",
        ["PurchasePrice"] = "采购单价，按系统业务币种计量",
        ["PurchaseQuantity"] = "采购数量，按采购单位计量",
        ["PurchaserId"] = "负责采购的采购员主键",
        ["PurchaserNameSnapshot"] = "采购业务发生时的采购员名称快照",
        ["PurchaseRuleId"] = "采购规则主键",
        ["PurchaseStatus"] = "采购单生成进度状态",
        ["PurchaseTotalPrice"] = "采购数量与采购单价计算后的金额快照",
        ["PurchaseUnitId"] = "采购计量单位主键",
        ["PurchaseUnitNameSnapshot"] = "采购业务发生时的采购单位名称快照",
        ["Quantity"] = "业务数量，按当前商品单位计量",
        ["QuotationId"] = "销售报价单主键",
        ["ReceiveDate"] = "客户要求收货时间（UTC）",
        ["ReceiveTime"] = "采购单预计到货时间（UTC）",
        ["Redirect"] = "菜单重定向路径",
        ["RegisteredAddress"] = "企业工商注册地址",
        ["RegisteredCapital"] = "企业登记注册资本",
        ["RegistrationAuthority"] = "企业登记机关",
        ["RegistrationStatus"] = "企业工商登记状态",
        ["Remark"] = "业务备注",
        ["RequestParams"] = "请求参数的脱敏序列化内容",
        ["RequiredQuantity"] = "需求数量，按采购单位计量",
        ["ResponseResult"] = "响应结果的脱敏序列化内容",
        ["ReturnStatus"] = "订单回单状态",
        ["RoleId"] = "关联角色主键",
        ["SaleOrderDetailId"] = "来源销售订单商品明细主键",
        ["SaleOrderId"] = "来源销售订单主键",
        ["SettlementPrice"] = "订单最终结算金额",
        ["Sort"] = "同级记录的排序值",
        ["Spec"] = "商品规格型号",
        ["Status"] = "记录启用状态",
        ["SupplierId"] = "关联供应商主键",
        ["SupplierContactNameSnapshot"] = "采购发生时的供应商联系人姓名快照",
        ["SupplierContactPhoneSnapshot"] = "采购发生时的供应商联系人电话快照",
        ["SupplierNameSnapshot"] = "采购业务发生时的供应商名称快照",
        ["TaxCategoryCode"] = "税收分类编码",
        ["TaxCategoryName"] = "税收分类名称",
        ["TaxNo"] = "纳税人识别号",
        ["TaxpayerIdentificationNumber"] = "客户纳税人识别号",
        ["TaxPolicyBasis"] = "税收优惠政策依据",
        ["TaxRate"] = "适用税率",
        ["Title"] = "菜单显示标题",
        ["TotalPrice"] = "数量与单价计算后的总金额",
        ["UnifiedSocialCreditCode"] = "企业统一社会信用代码",
        ["UnitConversion"] = "下单单位换算为基础单位的比例",
        ["UnitPrice"] = "当前商品单位对应的单价",
        ["UpdateBy"] = "最后修改记录的用户主键",
        ["UpdateName"] = "最后修改记录的用户名称",
        ["UpdateStatus"] = "订单审核通过后是否发生过修改",
        ["UpdateTime"] = "记录最后修改时间（UTC）",
        ["Url"] = "被调用接口的请求地址",
        ["UserId"] = "关联系统用户主键",
        ["Username"] = "用于登录的唯一用户名",
        ["WareId"] = "关联仓库主键"
    };

    /// <summary>
    /// 将注释目录应用到模型中的每张表和每个持久化字段。
    /// </summary>
    /// <param name="modelBuilder">待配置的 EF Core 模型构建器。</param>
    /// <exception cref="InvalidOperationException">实体或字段没有登记业务注释时抛出。</exception>
    public static void ApplyDatabaseComments(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!TableComments.TryGetValue(entityType.ClrType, out var tableComment))
            {
                throw new InvalidOperationException($"数据库实体 {entityType.ClrType.Name} 缺少表注释。");
            }

            var entityBuilder = modelBuilder.Entity(entityType.ClrType);
            entityBuilder.ToTable(tableBuilder => tableBuilder.HasComment(tableComment));

            foreach (var property in entityType.GetProperties())
            {
                if (!PropertyComments.TryGetValue(property.Name, out var propertyComment))
                {
                    throw new InvalidOperationException(
                        $"数据库实体 {entityType.ClrType.Name} 的字段 {property.Name} 缺少列注释。");
                }

                entityBuilder.Property(property.Name).HasComment(propertyComment);
            }
        }
    }
}
