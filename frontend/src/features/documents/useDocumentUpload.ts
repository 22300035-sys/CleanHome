import { useCallback, useState } from 'react';
import { uploadDocument } from '../../services/documents';
import { ApiError } from '../../services/api';
import type { DocumentUploadResponse } from '../../types/documents';

export type DocumentUploadState =
  | { kind: 'idle' }
  | { kind: 'uploading' }
  | { kind: 'success'; document: DocumentUploadResponse }
  | { kind: 'error'; status: number; message: string };

export function useDocumentUpload() {
  const [state, setState] = useState<DocumentUploadState>({
    kind: 'idle',
  });

  const upload = useCallback(
    async (
      file: File,
      documentType: string,
      customerLocationId?: string | null,
      token?: string | null
    ) => {
      setState({ kind: 'uploading' });

      try {
        const uploadedDocument = await uploadDocument({
          file,
          documentType,
          customerLocationId: customerLocationId ?? null,
          token: token ?? null,
        });

        setState({
          kind: 'success',
          document: uploadedDocument,
        });

        return uploadedDocument;
      } catch (error) {
        const status =
          error instanceof ApiError
            ? error.status
            : 0;

        const message =
          error instanceof Error
            ? error.message
            : 'No fue posible subir el documento.';

        setState({
          kind: 'error',
          status,
          message,
        });
      }
    },
    []
  );

  return { state, upload };
}
