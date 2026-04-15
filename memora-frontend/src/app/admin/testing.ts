import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-search-test',
  templateUrl: './search-test.component.html',
})
export class TestingComponent {
  query: string = '';
  duration: number | null = null;
  result: boolean | null = null;

  constructor(private http: HttpClient) {}

  testSearch() {
    if (!this.query) return;

    const start = performance.now();

    this.http
      .get<boolean>(`/api/entities/search`, {
        params: { query: this.query }
      })
      .subscribe({
        next: (res) => {
          const end = performance.now();
          this.duration = Math.round(end - start);
          this.result = res;
        },
        error: (err) => {
          const end = performance.now();
          this.duration = Math.round(end - start);
          console.error('Error:', err);
        }
      });
  }
}