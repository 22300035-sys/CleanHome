export interface DocumentUploadResponse {
  id: string;
  documentType: string;
  status: string;
  uploadedAt: string;
}

export interface UserDocumentResponse {
  id: string;
  documentType: string;
  securityStatus: string;
  moderationStatus: string;
  reviewStatus: string;
  uploadedAt: string;
}
