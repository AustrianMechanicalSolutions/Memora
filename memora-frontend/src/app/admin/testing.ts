import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { environment } from '../../environment';

@Component({
  selector: 'app-testing',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './testing.html',
})
export class TestingComponent {
  query: string = '';
  duration: number | null = null;
  result: boolean | null = null;

  constructor(private http: HttpClient) {}

  testSearch() {
    if (!this.query) return;

    const start = performance.now();

    this.http.get<boolean>(`${environment.apiUrl}/api/entities/search`, {
      params: { query: this.query }
    }).subscribe({
      next: (res) => {
        this.duration = Math.round(performance.now() - start);
        this.result = res;
        console.log("API response:", res, typeof res);
      },
      error: (err) => {
        this.duration = Math.round(performance.now() - start);
        this.result = null;
        console.error(err);
      }
    });

  }
}