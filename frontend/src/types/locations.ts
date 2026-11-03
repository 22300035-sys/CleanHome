export interface CustomerLocationRequest {
  label: string;
  street: string;
  exteriorNumber: string;
  interiorNumber?: string | null;
  neighborhood: string;
  postalCode: string;
  city: string;
  state: string;
  reference?: string | null;
}

export interface CustomerLocationResponse {
  id: string;
  label: string;
  street: string;
  exteriorNumber: string;
  interiorNumber?: string | null;
  neighborhood: string;
  postalCode: string;
  city: string;
  state: string;
  reference?: string | null;
  verificationStatus: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}
