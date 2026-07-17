const SearchActions: FC<{ onReset: () => void; onSearch: () => void }> = memo(({ onReset, onSearch }) => {
  const { t } = useTranslation();

  return (
    <AFlex
      align="center"
      gap={12}
      justify="end"
    >
      <AButton
        icon={<IconIcRoundRefresh />}
        onClick={onReset}
      >
        {t('common.reset')}
      </AButton>
      <AButton
        ghost
        icon={<IconIcRoundSearch />}
        type="primary"
        onClick={onSearch}
      >
        {t('common.search')}
      </AButton>
    </AFlex>
  );
});

export default SearchActions;
