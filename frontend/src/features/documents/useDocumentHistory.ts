import { useCallback, useState } from 'react';
import { getAccessToken } from '../../services/auth';
import { getMyDocuments } from '../../services/documents';
import type { UserDocumentResponse } from '../../types/documents';

export function useDocumentHistory() {
  const [documents, setDocuments] = useState<UserDocumentResponse[]>([]);
  const token = getAccessToken();

  const load = useCallback(async (): Promise<UserDocumentResponse[]> => {
    const items = await getMyDocuments(token);

    setDocuments(items);

    return items;
  }, [token]);

  return { documents, load };
}
