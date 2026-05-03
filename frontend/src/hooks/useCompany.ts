import { useQuery } from '@tanstack/react-query';
import { apiService } from '../services/api';

export const COMPANY_QUERY_KEY = ['company', 'me'] as const;

export const useCompany = () =>
  useQuery({
    queryKey: COMPANY_QUERY_KEY,
    queryFn: () => apiService.getMyCompany(),
    staleTime: 5 * 60 * 1000,  // treat data as fresh for 5 min
    gcTime: 10 * 60 * 1000,    // keep in cache for 10 min after unmount
    retry: 1,
  });
