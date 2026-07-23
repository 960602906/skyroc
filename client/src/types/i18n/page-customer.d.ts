declare namespace App {
  namespace I18n {
    interface PageCustomer {
      company: {
        addCompany: string;
        address: string;
        code: string;
        contactName: string;
        contactPhone: string;
        createTime: string;
        detail: {
          back: string;
          createTime: string;
          title: string;
          updateTime: string;
        };
        editCompany: string;
        form: {
          address: string;
          code: string;
          contactName: string;
          contactPhone: string;
          name: string;
          remark: string;
          status: string;
        };
        name: string;
        remark: string;
        sectionBasic: string;
        sectionStatus: string;
        status: string;
        title: string;
      };
      detail: {
        back: string;
        title: string;
        updateTime: string;
      };
      list: {
        addCustomer: string;
        address: string;
        code: string;
        companyId: string;
        contactName: string;
        contactPhone: string;
        createTime: string;
        editCustomer: string;
        form: {
          address: string;
          code: string;
          companyId: string;
          contactName: string;
          contactPhone: string;
          name: string;
          status: string;
        };
        name: string;
        status: string;
        title: string;
      };
      operate: {
        addTitle: string;
        bankAccount: string;
        bankName: string;
        businessScope: string;
        businessTerm: string;
        defaultWareId: string;
        editTitle: string;
        establishDate: string;
        form: {
          bankAccount: string;
          bankName: string;
          businessScope: string;
          businessTerm: string;
          defaultWareId: string;
          establishDate: string;
          invoiceAddress: string;
          invoiceEmail: string;
          invoicePhone: string;
          invoiceReceiverAddress: string;
          invoiceReceiverName: string;
          invoiceReceiverPhone: string;
          invoiceTitle: string;
          legalRepresentative: string;
          quotationId: string;
          registeredAddress: string;
          registeredCapital: string;
          registrationAuthority: string;
          registrationStatus: string;
          remark: string;
          tagIds: string;
          taxpayerIdentificationNumber: string;
          unifiedSocialCreditCode: string;
        };
        invoiceAddress: string;
        invoiceEmail: string;
        invoicePhone: string;
        invoiceReceiverAddress: string;
        invoiceReceiverName: string;
        invoiceReceiverPhone: string;
        invoiceTitle: string;
        legalRepresentative: string;
        quotationId: string;
        registeredAddress: string;
        registeredCapital: string;
        registrationAuthority: string;
        registrationStatus: string;
        remark: string;
        sectionBasic: string;
        sectionBusiness: string;
        sectionInvoice: string;
        tagIds: string;
        taxpayerIdentificationNumber: string;
        unifiedSocialCreditCode: string;
      };
      protocol: {
        addProtocol: string;
        code: string;
        createTime: string;
        customerIds: string;
        detail: {
          back: string;
          createTime: string;
          title: string;
          updateTime: string;
        };
        editProtocol: string;
        effectiveEnd: string;
        effectiveStart: string;
        form: {
          code: string;
          customerIds: string;
          name: string;
          quotationId: string;
          remark: string;
          status: string;
        };
        manageGoods: string;
        name: string;
        quotationId: string;
        remark: string;
        sectionBasic: string;
        sectionGoods: string;
        sectionStatus: string;
        status: string;
        title: string;
      };
      protocolGoods: {
        add: string;
        addProtocolGoods: string;
        createTime: string;
        customerProtocolId: string;
        editProtocolGoods: string;
        form: {
          customerProtocolId: string;
          goodsId: string;
          goodsUnitId: string;
          minOrderQuantity: string;
          protocolPrice: string;
          remark: string;
        };
        goodsId: string;
        goodsUnitId: string;
        minOrderQuantity: string;
        protocolPrice: string;
        remark: string;
        status: string;
        title: string;
      };
      subAccount: {
        addSubAccount: string;
        companyId: string;
        createTime: string;
        customerId: string;
        detail: {
          back: string;
          createTime: string;
          title: string;
          updateTime: string;
        };
        editSubAccount: string;
        email: string;
        form: {
          companyId: string;
          customerId: string;
          email: string;
          nickName: string;
          phone: string;
          remark: string;
          status: string;
          username: string;
        };
        nickName: string;
        phone: string;
        remark: string;
        sectionBasic: string;
        sectionStatus: string;
        status: string;
        title: string;
        username: string;
      };
      tag: {
        addTag: string;
        code: string;
        createTime: string;
        detail: {
          back: string;
          createTime: string;
          title: string;
          updateTime: string;
        };
        editTag: string;
        form: {
          code: string;
          name: string;
          parentId: string;
          remark: string;
          sort: string;
          status: string;
        };
        name: string;
        parentId: string;
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
