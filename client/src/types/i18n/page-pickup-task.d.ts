declare namespace App {
  namespace I18n {
    interface PagePickupTask {
      addAfterSale: string;
      afterSaleNo: string;
      assign: string;
      assignTitle: string;
      complete: string;
      completeConfirm: string;
      customerName: string;
      detail: {
        assignedTime: string;
        back: string;
        basicInfo: string;
        completedTime: string;
        contactName: string;
        contactPhone: string;
        driverPhone: string;
        executionInfo: string;
        quantity: string;
        scheduleInfo: string;
        startedTime: string;
      };
      driver: string;
      form: {
        driver: string;
        keyword: string;
        pickupStatus: string;
        remark: string;
      };
      goods: string;
      keyword: string;
      operate: {
        scheduleInfo: string;
        title: string;
      };
      pickupAddress: string;
      pickupStatus: string;
      plannedPickupTime: string;
      remark: string;
      schedule: string;
      start: string;
      startConfirm: string;
      statusCancelled: string;
      statusCompleted: string;
      statusPendingAssign: string;
      statusPendingPickup: string;
      statusPickingUp: string;
      stockInGenerated: string;
      stockInPending: string;
      stockInStatus: string;
      taskNo: string;
      title: string;
    }
  }
}
