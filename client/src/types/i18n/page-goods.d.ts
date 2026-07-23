declare namespace App {
  namespace I18n {
    interface PageGoods {
      detail: {
        back: string;
        title: string;
        updateTime: string;
      };
      list: {
        brand: string;
        code: string;
        createTime: string;
        defaultSupplierId: string;
        defaultWareId: string;
        form: {
          code: string;
          defaultSupplierId: string;
          defaultWareId: string;
          goodsTypeId: string;
          isOnSale: string;
          name: string;
          status: string;
        };
        goodsTypeId: string;
        isOnSale: string;
        name: string;
        offSale: string;
        onSale: string;
        spec: string;
        status: string;
        title: string;
      };
      operate: {
        addTitle: string;
        baseUnitId: string;
        brand: string;
        code: string;
        defaultSupplierId: string;
        defaultWareId: string;
        description: string;
        editTitle: string;
        form: {
          baseUnitId: string;
          brand: string;
          code: string;
          defaultSupplierId: string;
          defaultWareId: string;
          description: string;
          goodsTypeId: string;
          name: string;
          origin: string;
          remark: string;
          spec: string;
          supplierIds: string;
          taxRate: string;
        };
        goodsTypeId: string;
        isOnSale: string;
        name: string;
        origin: string;
        remark: string;
        sectionBasic: string;
        sectionSale: string;
        sectionSupply: string;
        spec: string;
        status: string;
        supplierIds: string;
        taxRate: string;
      };
      quotation: {
        add: string;
        audit: string;
        audited: string;
        code: string;
        customerIds: string;
        description: string;
        detail: {
          back: string;
          createTime: string;
          title: string;
          updateTime: string;
        };
        edit: string;
        effectiveEnd: string;
        effectiveStart: string;
        form: {
          code: string;
          customerIds: string;
          description: string;
          isAudited: string;
          name: string;
          remark: string;
          status: string;
        };
        isAudited: string;
        manageGoods: string;
        name: string;
        remark: string;
        sectionBasic: string;
        sectionGoods: string;
        sectionStatus: string;
        status: string;
        title: string;
        unaudit: string;
        unaudited: string;
      };
      quotationGoods: {
        add: string;
        edit: string;
        form: {
          goodsId: string;
          goodsUnitId: string;
          isOnSale: string;
          minOrderQuantity: string;
          quotationId: string;
          remark: string;
          unitPrice: string;
        };
        goodsId: string;
        goodsUnitId: string;
        isOnSale: string;
        minOrderQuantity: string;
        quotationId: string;
        remark: string;
        title: string;
        unitPrice: string;
      };
      type: {
        add: string;
        code: string;
        defaultTaxRate: string;
        detail: {
          back: string;
          createTime: string;
          title: string;
          updateTime: string;
        };
        edit: string;
        form: {
          code: string;
          defaultTaxRate: string;
          invoiceGoodsShortName: string;
          name: string;
          parentId: string;
          remark: string;
          sort: string;
          status: string;
          taxCategoryCode: string;
          taxCategoryName: string;
          taxPolicyBasis: string;
        };
        invoiceGoodsShortName: string;
        isTaxExempt: string;
        name: string;
        parentId: string;
        remark: string;
        sectionBasic: string;
        sectionStatus: string;
        sectionTax: string;
        sort: string;
        status: string;
        taxCategoryCode: string;
        taxCategoryName: string;
        taxPolicyBasis: string;
        title: string;
      };
      unit: {
        add: string;
        code: string;
        conversionRate: string;
        detail: {
          back: string;
          createTime: string;
          title: string;
          updateTime: string;
        };
        edit: string;
        form: {
          code: string;
          conversionRate: string;
          goodsId: string;
          name: string;
          remark: string;
          sort: string;
          status: string;
        };
        goodsCode: string;
        goodsId: string;
        isBaseUnit: string;
        name: string;
        remark: string;
        sectionBasic: string;
        sectionStatus: string;
        sort: string;
        status: string;
        title: string;
      };
    }
  }
}
