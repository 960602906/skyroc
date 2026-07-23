const pickupTask: App.I18n.Schema['translation']['page']['pickupTask'] = {
  addAfterSale: 'Create After-sale',
  afterSaleNo: 'After-sale No.',
  assign: 'Assign Driver',
  assignTitle: 'Schedule Pickup · {{taskNo}}',
  complete: 'Complete Pickup',
  completeConfirm: 'Complete pickup task {{taskNo}}? It can then be used for sales-return stock-in.',
  customerName: 'Customer',
  detail: {
    assignedTime: 'Assigned At',
    back: 'Back to List',
    basicInfo: 'Task & Source',
    completedTime: 'Completed At',
    contactName: 'Contact',
    contactPhone: 'Contact Phone',
    driverPhone: 'Driver Phone',
    executionInfo: 'Fulfillment',
    quantity: 'Pickup Quantity',
    scheduleInfo: 'Scheduling',
    startedTime: 'Started At'
  },
  driver: 'Pickup Driver',
  form: {
    driver: 'Select an active driver',
    keyword: 'Enter task no., after-sale no., customer, or goods',
    pickupStatus: 'Select pickup status',
    remark: 'Enter scheduling remark (optional)'
  },
  goods: 'Goods to Pick Up',
  keyword: 'Keyword',
  operate: {
    scheduleInfo: 'Scheduling Details',
    title: 'Schedule Pickup · {{taskNo}}'
  },
  pickupAddress: 'Pickup Address / Contact',
  pickupStatus: 'Pickup Status',
  plannedPickupTime: 'Planned Pickup Time',
  remark: 'Scheduling Remark',
  schedule: 'Schedule Pickup',
  start: 'Start Pickup',
  startConfirm: 'Start pickup task {{taskNo}}?',
  statusCancelled: 'Cancelled',
  statusCompleted: 'Completed',
  statusPendingAssign: 'Pending Assignment',
  statusPendingPickup: 'Pending Pickup',
  statusPickingUp: 'Picking Up',
  stockInGenerated: 'Stock-in Created',
  stockInPending: 'Pending Stock-in',
  stockInStatus: 'Return Stock-in',
  taskNo: 'Pickup Task No.',
  title: 'Pickup Tasks'
};

export default pickupTask;
