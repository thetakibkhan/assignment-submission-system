type SearchableItem = {
  status?: string;
  title?: string;
};

export function filterDashboardItems<T extends SearchableItem>(
  items: readonly T[],
  searchText: string,
  status: string,
): T[] {
  const normalizedSearchText = searchText.trim().toLocaleLowerCase();

  return items.filter((item) => {
    const matchesSearch = normalizedSearchText.length === 0
      || (item.title ?? "").toLocaleLowerCase().includes(normalizedSearchText);
    const matchesStatus = status === "All" || item.status === status;
    return matchesSearch && matchesStatus;
  });
}
