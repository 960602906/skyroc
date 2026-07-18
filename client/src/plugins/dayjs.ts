import { extend } from 'dayjs';
import customParseFormat from 'dayjs/plugin/customParseFormat';
import localeData from 'dayjs/plugin/localeData';
import utc from 'dayjs/plugin/utc';

import { setDayjsLocale } from '../locales/dayjs';

export function setupDayjs() {
  extend(localeData);
  extend(utc);
  extend(customParseFormat);

  setDayjsLocale();
}
