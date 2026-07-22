// src/app/core/services/swap-offers.service.ts
// שירות לוח העברת תורים.

import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdminAcceptOfferRequest, AdminSwapOfferResponse, CreateSwapOfferRequest, SwapOfferResponse, SwapOfferStatus } from '../models';
import { environment } from '../../../environments/environment';
import { loadSignal } from '../utils/signal.utils';

@Injectable({ providedIn: 'root' })
export class SwapOffersService {
  private http = inject(HttpClient);

  private readonly BASE = `${environment.apiUrl}/swap-offers`;

  readonly activeOffers = signal<SwapOfferResponse[]>([]);
  readonly loading      = signal(false);

  loadActiveOffers(): void {
    loadSignal(this.getActiveOffers(), this.activeOffers, this.loading);
  }

  getActiveOffers(): Observable<SwapOfferResponse[]> {
    return this.http.get<SwapOfferResponse[]>(this.BASE);
  }

  createOffer(req: CreateSwapOfferRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(this.BASE, req);
  }

  acceptOffer(id: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.BASE}/${id}/accept`, {});
  }

  cancelOffer(id: string): Observable<void> {
    return this.http.delete<void>(`${this.BASE}/${id}`);
  }

  // ── Admin ───────────────────────────────────────────────────────────────────

  readonly adminOffers  = signal<AdminSwapOfferResponse[]>([]);
  readonly adminLoading = signal(false);

  loadAdminOffers(status?: SwapOfferStatus): void {
    const params = status !== undefined ? `?status=${status}` : '';
    loadSignal(
      this.http.get<AdminSwapOfferResponse[]>(`${this.BASE}/admin${params}`),
      this.adminOffers,
      this.adminLoading
    );
  }

  adminCreateOffer(appointmentId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.BASE}/admin/create`, { appointmentId });
  }

  adminAcceptOffer(offerId: string, req: AdminAcceptOfferRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.BASE}/${offerId}/admin-accept`, req);
  }

  adminCancelOffer(offerId: string): Observable<void> {
    return this.http.delete<void>(`${this.BASE}/${offerId}/admin`);
  }
}
