import type { SelectProps } from 'antd';
import { Select } from 'antd';
import type { ReactNode } from 'react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useRemoteOptions } from '@/service/hooks';

type RemoteOptionSelectProps = Omit<
  SelectProps,
  'dropdownRender' | 'filterOption' | 'onDropdownVisibleChange' | 'onSearch' | 'options' | 'showSearch'
> & {
  contextKey?: readonly unknown[];
  limit?: number;
  resource: string;
};

/** 通用远程限量搜索选择器，支持编辑值回填且不会缓存完整业务对象。 */
export default function RemoteOptionSelect({
  contextKey,
  limit = 20,
  loading = false,
  resource,
  value,
  ...selectProps
}: RemoteOptionSelectProps) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [keyword, setKeyword] = useState('');
  const { hasMore, isLoading, options } = useRemoteOptions({
    contextKey,
    keyword,
    limit,
    open,
    resource,
    value
  });

  return (
    <Select
      {...selectProps}
      showSearch
      filterOption={false}
      loading={loading || isLoading}
      open={open}
      options={options}
      value={value}
      dropdownRender={(menu: ReactNode) => (
        <>
          {menu}
          {hasMore && <div className="px-3 py-2 text-xs text-gray-400">{t('common.remoteSelectMore')}</div>}
        </>
      )}
      onSearch={setKeyword}
      onDropdownVisibleChange={(nextOpen: boolean) => {
        setOpen(nextOpen);
        if (!nextOpen) setKeyword('');
      }}
    />
  );
}
