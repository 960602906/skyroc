declare namespace Api {
  namespace SelectionOption {
    type Entity = {
      id: string;
      label: string;
      secondaryText?: null | string;
    };

    type SearchParams = {
      keyword?: string;
      limit?: number;
    };

    type SearchResult = {
      hasMore: boolean;
      items: Entity[];
    };
  }
}
