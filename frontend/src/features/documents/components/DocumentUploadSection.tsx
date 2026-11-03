import { useState } from 'react';
import type { FormEvent } from 'react';
import { useDocumentUpload } from '../useDocumentUpload';
import { getAccessToken } from '../../../services/auth';

const DOCUMENT_TYPES = [
  { value: 'identity_front', label: 'Identidad (frente)' },
  { value: 'identity_back', label: 'Identidad (reverso)' },
  { value: 'portrait', label: 'Retrato' },
  { value: 'address_proof', label: 'Comprobante de domicilio' },
] as const;

export function DocumentUploadSection() {
  const { state, upload } = useDocumentUpload();
  const token = getAccessToken();

  const [file, setFile] = useState<File | null>(null);
  const [documentType, setDocumentType] = useState<string>(
    'identity_front'
  );
  const [customerLocationId, setCustomerLocationId] = useState('');

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!file) {
      return;
    }

    await upload(
      file,
      documentType,
      customerLocationId || null,
      token
    );
  };

  return (
    <section className="worker-panel">
      <h2>Mis documentos</h2>
      <form onSubmit={submit}>
        <label>
          Tipo de documento
          <select
            value={documentType}
            onChange={(event) =>
              setDocumentType(event.target.value)
            }
          >
            {DOCUMENT_TYPES.map((type) => (
              <option
                key={type.value}
                value={type.value}
              >
                {type.label}
              </option>
            ))}
          </select>
        </label>
        {documentType === 'address_proof' ? (
          <label>
            Ubicación de cliente
            <input
              value={customerLocationId}
              onChange={(event) =>
                setCustomerLocationId(event.target.value)
              }
            />
          </label>
        ) : null}
        <label>
          Archivo
          <input
            type="file"
            accept="image/jpeg,image/png,application/pdf"
            onChange={(event) => {
              const selected = event.target.files?.[0] ?? null;

              setFile(selected);
            }}
          />
        </label>
        <button type="submit">Subir documento</button>
      </form>
      {state.kind === 'uploading' ? <p>Subiendo documento…</p> : null}
      {state.kind === 'success' ? (
        <p>Documento subido correctamente.</p>
      ) : null}
      {state.kind === 'error' ? (
        <p role="alert">{state.message}</p>
      ) : null}
    </section>
  );
}
