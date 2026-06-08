import { environment } from '../../environments/environment';

export const API_BASE = environment.apiUrl;
export const API_URL = `${API_BASE}/api`;

export function getFullImageUrl(path: string): string {
  return `${API_BASE}${path}`;
}
