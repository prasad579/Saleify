import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiHealthService {
  private http = inject(HttpClient);
  online = signal(true);
  checked = signal(false);

  check() {
    this.http.get(`${environment.apiUrl}/health`).subscribe({
      next: () => {
        this.online.set(true);
        this.checked.set(true);
      },
      error: () => {
        this.online.set(false);
        this.checked.set(true);
      }
    });
  }
}
