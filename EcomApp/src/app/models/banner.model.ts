export interface Banner {
  id: number;
  title: string;
  subtitle: string;
  bgGradient: string;
  icon: string;
  imageUrl?: string;
  startDate: string;
  durationDays: number;
  sortOrder: number;
  isActive: boolean;
}

export interface CreateBanner {
  title: string;
  subtitle: string;
  bgGradient: string;
  icon: string;
  startDate: string;
  durationDays: number;
  sortOrder: number;
  isActive: boolean;
}

export interface UpdateBanner {
  title: string;
  subtitle: string;
  bgGradient: string;
  icon: string;
  startDate: string;
  durationDays: number;
  sortOrder: number;
  isActive: boolean;
}
