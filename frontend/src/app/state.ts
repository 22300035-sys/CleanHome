import { createContext } from 'react';
import type { Account, Role, Transport } from '../services/types';

export interface AppContextValue {
  user: Account | null; api: Transport; demo: boolean;
  login(email: string, password: string, role: Role, name?: string, factor?: string): Promise<void>;
  register(email: string, password: string, role: Role, name: string): Promise<boolean>;
  startDemo(role?: Role | 'Admin'): void; logout(): Promise<void>; reloadProfile(): Promise<void>;
  toast(message: string): void;
}
export const AppContext = createContext<AppContextValue | null>(null);

