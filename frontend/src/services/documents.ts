import { apiGet, apiPostForm } from './api';
import type {
  DocumentUploadResponse,
  UserDocumentResponse,
} from '../types/documents';

export async function getMyDocuments(
  token?: string | null
): Promise<UserDocumentResponse[]> {
  return apiGet<UserDocumentResponse[]>('/api/v1/documents', { token });
}

export async function uploadDocument(input: {
  file: File;
  documentType: string;
  customerLocationId?: string | null;
  token?: string | null;
}): Promise<DocumentUploadResponse> {
  const formData = new FormData();

  formData.append('file', input.file, input.file.name);
  formData.append('documentType', input.documentType);

  if (input.customerLocationId) {
    formData.append('customerLocationId', input.customerLocationId);
  }

  return apiPostForm<DocumentUploadResponse>(
    '/api/v1/documents',
    formData,
    { token: input.token }
  );
}
