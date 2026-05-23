export interface Category {
  id: number;
  name: string;
  icon?: string;
}

export interface CreateCategory {
  name: string;
  icon?: string;
}

export interface UpdateCategory {
  name: string;
  icon?: string;
}
