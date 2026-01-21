import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { GroupsService, GroupDetailDto, MemoryDto } from '../groups';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-group-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './group-detail.html',
  styleUrls: ['./group-detail.css']
})
export class GroupDetailComponent {

  groupId!: string;
  group?: GroupDetailDto;

  items: MemoryDto[] = [];

  // filters
  fType: number | null = null;
  fFrom: string = '';
  fTo: string = '';
  fSearch: string = '';
  fSort: 'newest' | 'oldest' = 'newest';

  // create memory
  showCreate = false;
  cType = 2;
  cHappenedAt = new Date().toISOString().slice(0, 10);
  cTitle = '';
  cQuoteText = '';
  cMediaUrl = '';
  cTags = '';

  constructor(
    private route: ActivatedRoute,
    private groupsService: GroupsService
  ) {}

  ngOnInit() {
    this.groupId = this.route.snapshot.paramMap.get('id')!;

    this.groupsService.groupDetail(this.groupId).subscribe({
      next: (g) => {
        this.group = g;
        this.reload();
      },
      error: (err) => console.error(err)
    });
  }

  reload() {
    this.groupsService.memories(this.groupId, {
      type: this.fType === null ? undefined : this.fType,
      from: this.fFrom ? new Date(this.fFrom).toISOString() : undefined,
      to: this.fTo ? new Date(this.fTo).toISOString() : undefined,
      search: this.fSearch ? this.fSearch : undefined,
      sort: this.fSort,
      page: 1,
      pageSize: 50
    }).subscribe({
      next: (r) => this.items = r.items,
      error: (err) => console.error(err)
    });
  }

  create() {
    const tags = this.cTags.split(',').map(x => x.trim()).filter(Boolean);

    const body = {
      type: this.cType,
      title: this.cTitle || null,
      quoteText: this.cType === 2 ? (this.cQuoteText || null) : null,
      mediaUrl: this.cType !== 2 ? (this.cMediaUrl || null) : null,
      thumbUrl: null,
      happenedAt: new Date(this.cHappenedAt).toISOString(),
      tags
    };

    this.groupsService.createMemory(this.groupId, body).subscribe({
      next: () => {
        this.showCreate = false;
        this.cTitle = '';
        this.cQuoteText = '';
        this.cMediaUrl = '';
        this.cTags = '';
        this.reload();
      },
      error: (err) => console.error(err)
    });
  }

  typeLabel(t: number) {
    return t === 0 ? 'Photo' : t === 1 ? 'Video' : 'Quote';
  }
}
