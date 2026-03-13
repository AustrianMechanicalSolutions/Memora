import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { GroupAdminService, MemoryDto, PagedMemories } from '../../services/admin';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-group-admin-memories',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule
  ],
  templateUrl: './memories.html',
  styleUrls: ['./memories.css']
})
export class MemoriesComponent implements OnInit, OnDestroy {
  groupId!: string;

  loading = true;
  error?: string;

  total = 0;
  items: MemoryDto[] = [];

  query = {
    page: 1,
    pageSize: 20,
    sort: 'newest' as 'newest' | 'oldest',
    search: '',
    from: '',
    to: '',
    type: '',
    albumId: ''
  };

  private sub = new Subscription();

  constructor(private route: ActivatedRoute, private service: GroupAdminService) {}

  ngOnInit(): void {
    this.groupId = this.route.parent?.snapshot.paramMap.get('id')!;
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = undefined;

    this.sub.add(
      this.service.getMemories(this.groupId, {
        page: this.query.page,
        pageSize: this.query.pageSize,
        sort: this.query.sort,
        search: this.query.search?.trim() || null,
        from: this.query.from || null,
        to: this.query.to || null,
        type: this.query.type || null,
        albumId: this.query.albumId || null,
      }).subscribe({
        next: (res: PagedMemories) => {
          this.total = res.total;
          this.items = res.items;
          this.loading = false;
        },
        error: (err) => {
          console.error(err);
          this.error = 'Konnte Erinnerungen nicht laden.';
          this.loading = false;
        }
      })
    );
  }

  applyFilters(): void {
    this.query.page = 1;
    this.load();
  }

  prev(): void {
    if (this.query.page <= 1) return;
    this.query.page--;
    this.load();
  }

  next(): void {
    const maxPage = Math.ceil(this.total / this.query.pageSize);
    if (this.query.page >= maxPage) return;
    this.query.page++;
    this.load();
  }

  delete(m: MemoryDto): void {
    alert('PLACEHOLDER: Delete-Memory Endpoint fehlt im Backend.');
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }
}
