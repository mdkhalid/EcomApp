/** Mirrors the AdminSettingsController GET response and SettingsCatalog descriptor. */
export interface AdminSetting {
  key: string;
  group: string;
  description: string;
  isSensitive: boolean;
  value: string | null;
  defaultValue: string;
  type?: string;
  isPublic?: boolean;
}

/** Shape sent to PUT /api/admin/settings */
export interface SettingUpdate {
  key: string;
  value: string | null;
}
