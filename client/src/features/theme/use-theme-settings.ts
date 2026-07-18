import { getThemeSettings } from './theme-settings-store';

export function useThemeSettings() {
  const themeSettings = useAppSelector(getThemeSettings);

  return themeSettings;
}
