import { Injectable, signal } from '@angular/core';

export interface PendingBookingClient {
  name: string;
  phone: string;
}

@Injectable({ providedIn: 'root' })
export class AdminBookingStateService {
  pendingClient = signal<PendingBookingClient | null>(null);

  set(name: string, phone: string): void {
    this.pendingClient.set({ name, phone });
  }

  clear(): void {
    this.pendingClient.set(null);
  }
}
