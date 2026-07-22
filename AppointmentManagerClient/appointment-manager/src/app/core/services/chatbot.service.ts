import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ChatRequest, ChatResponse } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ChatbotService {
  private http = inject(HttpClient);
  private readonly BASE = `${environment.apiUrl}/chatbot`;

  send(request: ChatRequest): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(this.BASE, request);
  }
}
