import type { DescriptionsProps } from 'antd';

import { DEFAULT_DETAIL_DESC_PROPS, DETAIL_EMPTY, displayText, renderEnableStatus } from '@/features/crud';
import {
  toOptions,
  useCompanyOptions,
  useCustomerTagOptions,
  useQuotationOptions,
  useWareOptions
} from '@/service/hooks';

interface CustomerDetailViewProps {
  detail: Api.Customer.Entity;
}

function CustomerDetailView({ detail }: CustomerDetailViewProps) {
  const { t } = useTranslation();

  const { data: companies } = useCompanyOptions();
  const { data: wares } = useWareOptions();
  const { data: quotationOptions } = useQuotationOptions();

  const { data: tags } = useCustomerTagOptions();

  const companyName = toOptions(companies).find(item => item.value === detail.companyId)?.label;
  const wareName = toOptions(wares).find(item => item.value === detail.defaultWareId)?.label;
  const quotationName = quotationOptions?.find(item => item.value === detail.quotationId)?.label;
  const tagNames =
    detail.tagIds
      ?.map(id => toOptions(tags).find(item => item.value === id)?.label)
      .filter(Boolean)
      .join('、') || DETAIL_EMPTY;

  const basicItems: DescriptionsProps['items'] = [
    { children: displayText(detail.name), key: 'name', label: t('page.customer.list.name') },
    { children: displayText(detail.code), key: 'code', label: t('page.customer.list.code') },
    {
      children: displayText(companyName),
      key: 'companyId',
      label: t('page.customer.list.companyId')
    },
    {
      children: displayText(wareName),
      key: 'defaultWareId',
      label: t('page.customer.operate.defaultWareId')
    },
    {
      children: displayText(detail.contactName),
      key: 'contactName',
      label: t('page.customer.list.contactName')
    },
    {
      children: displayText(detail.contactPhone),
      key: 'contactPhone',
      label: t('page.customer.list.contactPhone')
    },
    {
      children: displayText(detail.address),
      key: 'address',
      label: t('page.customer.list.address'),
      span: 2
    },
    {
      children: displayText(quotationName),
      key: 'quotationId',
      label: t('page.customer.operate.quotationId')
    },
    {
      children: tagNames,
      key: 'tagIds',
      label: t('page.customer.operate.tagIds')
    },
    {
      children: renderEnableStatus(detail.status),
      key: 'status',
      label: t('page.customer.list.status')
    }
  ];

  const invoiceItems: DescriptionsProps['items'] = [
    {
      children: displayText(detail.invoiceTitle),
      key: 'invoiceTitle',
      label: t('page.customer.operate.invoiceTitle')
    },
    {
      children: displayText(detail.taxpayerIdentificationNumber),
      key: 'taxpayerIdentificationNumber',
      label: t('page.customer.operate.taxpayerIdentificationNumber')
    },
    {
      children: displayText(detail.invoicePhone),
      key: 'invoicePhone',
      label: t('page.customer.operate.invoicePhone')
    },
    {
      children: displayText(detail.invoiceEmail),
      key: 'invoiceEmail',
      label: t('page.customer.operate.invoiceEmail')
    },
    {
      children: displayText(detail.invoiceAddress),
      key: 'invoiceAddress',
      label: t('page.customer.operate.invoiceAddress'),
      span: 2
    },
    {
      children: displayText(detail.invoiceReceiverName),
      key: 'invoiceReceiverName',
      label: t('page.customer.operate.invoiceReceiverName')
    },
    {
      children: displayText(detail.invoiceReceiverPhone),
      key: 'invoiceReceiverPhone',
      label: t('page.customer.operate.invoiceReceiverPhone')
    },
    {
      children: displayText(detail.invoiceReceiverAddress),
      key: 'invoiceReceiverAddress',
      label: t('page.customer.operate.invoiceReceiverAddress'),
      span: 2
    },
    {
      children: displayText(detail.bankName),
      key: 'bankName',
      label: t('page.customer.operate.bankName')
    },
    {
      children: displayText(detail.bankAccount),
      key: 'bankAccount',
      label: t('page.customer.operate.bankAccount')
    }
  ];

  const businessItems: DescriptionsProps['items'] = [
    {
      children: displayText(detail.legalRepresentative),
      key: 'legalRepresentative',
      label: t('page.customer.operate.legalRepresentative')
    },
    {
      children: displayText(detail.unifiedSocialCreditCode),
      key: 'unifiedSocialCreditCode',
      label: t('page.customer.operate.unifiedSocialCreditCode')
    },
    {
      children: displayText(detail.registeredAddress),
      key: 'registeredAddress',
      label: t('page.customer.operate.registeredAddress'),
      span: 2
    },
    {
      children: displayText(detail.registeredCapital),
      key: 'registeredCapital',
      label: t('page.customer.operate.registeredCapital')
    },
    {
      children: displayText(detail.establishDate),
      key: 'establishDate',
      label: t('page.customer.operate.establishDate')
    },
    {
      children: displayText(detail.businessTerm),
      key: 'businessTerm',
      label: t('page.customer.operate.businessTerm')
    },
    {
      children: displayText(detail.registrationAuthority),
      key: 'registrationAuthority',
      label: t('page.customer.operate.registrationAuthority')
    },
    {
      children: displayText(detail.registrationStatus),
      key: 'registrationStatus',
      label: t('page.customer.operate.registrationStatus')
    },
    {
      children: displayText(detail.businessScope),
      key: 'businessScope',
      label: t('page.customer.operate.businessScope'),
      span: 2
    },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.customer.operate.remark'),
      span: 2
    },
    {
      children: displayText(detail.createTime),
      key: 'createTime',
      label: t('page.customer.list.createTime')
    },
    {
      children: displayText(detail.updateTime),
      key: 'updateTime',
      label: t('page.customer.detail.updateTime')
    }
  ];

  return (
    <>
      <ACard
        className="card-wrapper"
        title={t('page.customer.operate.sectionBasic')}
        variant="borderless"
      >
        <ADescriptions
          {...DEFAULT_DETAIL_DESC_PROPS}
          items={basicItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.customer.operate.sectionInvoice')}
        variant="borderless"
      >
        <ADescriptions
          {...DEFAULT_DETAIL_DESC_PROPS}
          items={invoiceItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.customer.operate.sectionBusiness')}
        variant="borderless"
      >
        <ADescriptions
          {...DEFAULT_DETAIL_DESC_PROPS}
          items={businessItems}
        />
      </ACard>
    </>
  );
}

export default CustomerDetailView;
