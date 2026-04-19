export interface CreateCategoryDto {
  name: string;
  icon?: string | null;
}

export interface UpdateCategoryDto {
  name: string;
  icon?: string | null;
  isActive: boolean;
}

export interface CategoryDto {
  id: number;
  name: string;
  slug: string;
  icon: string | null;
  isActive: boolean;
  itemCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface CategoryListItemDto {
  id: number;
  name: string;
  icon: string | null;
  slug: string;
  itemCount: number;
}