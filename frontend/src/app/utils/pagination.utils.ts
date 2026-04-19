export function getPageNumbers(currentPage: number, totalPages: number): number[] {
  const pages: number[] = [];

  if (totalPages <= 7) {
    for (let i = 1; i <= totalPages; i++) pages.push(i);
  } else {
    pages.push(1);
    if (currentPage > 3) pages.push(-1);
    for (let i = Math.max(2, currentPage - 1); i <= Math.min(totalPages - 1, currentPage + 1); i++) {
      pages.push(i);
    }
    if (currentPage < totalPages - 2) pages.push(-1);
    pages.push(totalPages);
  }
  return pages;
}

export function getTotalPages(totalCount: number, pageSize: number): number {
  return Math.ceil(totalCount / pageSize);
}