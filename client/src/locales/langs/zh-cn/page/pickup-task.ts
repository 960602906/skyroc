const pickupTask: App.I18n.Schema['translation']['page']['pickupTask'] = {
  addAfterSale: '新增售后',
  afterSaleNo: '售后单号',
  assign: '分配司机',
  assignTitle: '安排取货 · {{taskNo}}',
  complete: '完成取货',
  completeConfirm: '确认完成取货任务 {{taskNo}}？完成后可作为销售退货入库来源。',
  customerName: '客户名称',
  detail: {
    assignedTime: '分配时间',
    back: '返回列表',
    basicInfo: '任务与来源',
    completedTime: '完成时间',
    contactName: '联系人',
    contactPhone: '联系电话',
    driverPhone: '司机电话',
    executionInfo: '履约信息',
    quantity: '取货数量',
    scheduleInfo: '调度信息',
    startedTime: '开始时间'
  },
  driver: '取货司机',
  form: {
    driver: '请选择启用司机',
    keyword: '请输入任务号/售后单号/客户/商品',
    pickupStatus: '请选择取货状态',
    remark: '请输入调度备注（可选）'
  },
  goods: '取货商品',
  keyword: '关键字',
  operate: {
    scheduleInfo: '安排信息',
    title: '安排取货 · {{taskNo}}'
  },
  pickupAddress: '取货地址/联系人',
  pickupStatus: '取货状态',
  plannedPickupTime: '计划取货时间',
  remark: '调度备注',
  schedule: '安排取货',
  start: '开始取货',
  startConfirm: '确认开始执行取货任务 {{taskNo}}？',
  statusCancelled: '已取消',
  statusCompleted: '已完成',
  statusPendingAssign: '待分配',
  statusPendingPickup: '待取货',
  statusPickingUp: '取货中',
  stockInGenerated: '已生成退货入库',
  stockInPending: '待退货入库',
  stockInStatus: '退货入库',
  taskNo: '取货任务号',
  title: '取货任务'
};

export default pickupTask;
