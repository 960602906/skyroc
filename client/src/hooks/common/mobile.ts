import { getIsMobile } from '@/features/app';

export function useMobile() {
  const isMobile = useAppSelector(getIsMobile);

  return isMobile;
}
