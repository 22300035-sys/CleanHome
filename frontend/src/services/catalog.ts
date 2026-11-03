import { apiGet } from './api';
import type { ServiceCategoryDto } from '../types/catalog';

export async function getServiceCategories(): Promise<
  ServiceCategoryDto[]
> {
  return apiGet<ServiceCategoryDto[]>('/api/v1/service-categories');
}
