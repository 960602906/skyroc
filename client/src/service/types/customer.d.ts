declare namespace Api {
  namespace Customer {
    type Entity = Common.CommonRecord<{
      address: string | null;
      bankAccount: string | null;
      bankName: string | null;
      businessScope: string | null;
      businessTerm: string | null;
      code: string;
      companyId: string | null;
      contactName: string | null;
      contactPhone: string | null;
      defaultWareId: string | null;
      establishDate: string | null;
      invoiceAddress: string | null;
      invoiceEmail: string | null;
      invoicePhone: string | null;
      invoiceReceiverAddress: string | null;
      invoiceReceiverName: string | null;
      invoiceReceiverPhone: string | null;
      invoiceTitle: string | null;
      legalRepresentative: string | null;
      name: string;
      quotationId: string | null;
      registeredAddress: string | null;
      registeredCapital: string | null;
      registrationAuthority: string | null;
      registrationStatus: string | null;
      remark: string | null;
      tagIds: string[] | null;
      taxpayerIdentificationNumber: string | null;
      unifiedSocialCreditCode: string | null;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<
        Entity,
        | 'address'
        | 'bankAccount'
        | 'bankName'
        | 'businessScope'
        | 'businessTerm'
        | 'code'
        | 'companyId'
        | 'contactName'
        | 'contactPhone'
        | 'defaultWareId'
        | 'establishDate'
        | 'invoiceAddress'
        | 'invoiceEmail'
        | 'invoicePhone'
        | 'invoiceReceiverAddress'
        | 'invoiceReceiverName'
        | 'invoiceReceiverPhone'
        | 'invoiceTitle'
        | 'legalRepresentative'
        | 'name'
        | 'quotationId'
        | 'registeredAddress'
        | 'registeredCapital'
        | 'registrationAuthority'
        | 'registrationStatus'
        | 'remark'
        | 'status'
        | 'tagIds'
        | 'taxpayerIdentificationNumber'
        | 'unifiedSocialCreditCode'
      >
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = CommonType.RecordNullable<
      Api.Base.SearchParams & {
        companyId?: string | null;
        defaultWareId?: string | null;
        quotationId?: string | null;
        taxpayerIdentificationNumber?: string | null;
        unifiedSocialCreditCode?: string | null;
      }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'code' | 'id' | 'name'>;
  }
}
