declare namespace Api {
  namespace Base {
    type SearchParams = CommonType.RecordNullable<
      Pick<Common.CommonRecord, 'status'> &
        Common.CommonSearchParams & {
          code?: string | null;
          name?: string | null;
        }
    >;

    type ToggleStatusParams = {
      id: string;
      status?: Api.Common.EnableStatus | null;
    };

    type ToggleBooleanParams = { id: string };
  }
}
