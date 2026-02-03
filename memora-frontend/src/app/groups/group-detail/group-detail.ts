import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { GroupsService, GroupDetailDto, MemoryDto, AlbumDto, GroupStatsDto, GroupWeeklyActivityDto } from '../groups';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-group-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './group-detail.html',
  styleUrls: ['./group-detail.css']
})
export class GroupDetailComponent {

  groupId!: string;
  group?: GroupDetailDto;

  items: MemoryDto[] = [];

  activeTab: 'timeline' | 'members' = 'timeline';

  members: { userId: string, name: string, role: string }[] = [];
  creatorUserId: string | null = null;

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
  cQuoteBy = '';
  cMediaUrl = '';
  cTags = '';
  selectedFile: File | null = null;
  
  // Tagging
  showMentionPopup = false;
  mentionQuery = '';
  mentionResults: { userId: string; name: string; role: string }[] = [];
  mentionIndex = 0;

  // Albums
  albums: AlbumDto[] = [];
  selectedAlbumId: string | null = null;

  showCreateAlbum = false;
  aTitle = '';
  aDescription = '';
  aDateStart = new Date().toISOString().slice(0, 10);
  aDateEnd = '';

  // Group stats
  groupStats?: GroupStatsDto;
  weeklyActivity?: GroupWeeklyActivityDto;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private groupsService: GroupsService,
  ) {}

  ngOnInit() {
    this.groupId = this.route.snapshot.paramMap.get('id')!;

    this.groupsService.groupDetail(this.groupId).subscribe({
      next: (g) => {
        this.group = g;

        this.creatorUserId = (g as any).createdByUserId;

        this.reload();
        this.loadMembers();
        this.loadStats();
        this.loadActivity();
      },
      error: (err) => console.error(err)
    });



    this.loadAlbums();
  }

  reload() {
    this.groupsService.memories(this.groupId, {
      type: this.fType === null ? undefined : this.fType,
      from: this.fFrom ? new Date(this.fFrom).toISOString() : undefined,
      to: this.fTo ? new Date(this.fTo).toISOString() : undefined,
      search: this.fSearch ? this.fSearch : undefined,
      sort: this.fSort,
      page: 1,
      pageSize: 50,
      albumId: this.selectedAlbumId ?? undefined,
    }).subscribe({
      next: (r) => this.items = r.items,
      error: (err) => console.error(err)
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
    }
  }

  create() {
    const tags = this.cTags.split(',').map(x => x.trim()).filter(Boolean);

    // Quote memory => no file uploaded needed
    if (this.cType == 2) {
      const body = {
        type: this.cType,
        title: this.cTitle || null,
        quoteText: this.cType === 2 ? (this.cQuoteText || null) : null,
        quoteBy: this.cQuoteBy || null,
        mediaUrl: this.cType !== 2 ? (this.cMediaUrl || null) : null,
        thumbUrl: null,
        happenedAt: new Date(this.cHappenedAt).toISOString(),
        albumId: this.selectedAlbumId,
        tags
      };

      this.groupsService.createMemory(this.groupId, body).subscribe({
        next: () => this.afterCreate(),
        error: (err) => console.error(err)
      });

      return;
    }

    // Photo / Video => upload file

    if (!this.selectedFile) {
      alert("Please choose a photo/video file first!");
      return;
    }

    const data = {
      type: this.cType,
      title: this.cTitle || null,
      quoteText: null,
      happenedAt: new Date(this.cHappenedAt).toISOString(),
      albumId: this.selectedAlbumId,
      tags
    };

    this.groupsService.createMemoryWithFile(this.groupId, this.selectedFile, data).subscribe({
      next: () => this.afterCreate(),
      error: (err) => console.error(err)
    });
  }

  private afterCreate() {
    this.showCreate = false;
    this.cTitle = '';
    this.cQuoteText = '';
    this.cQuoteBy = '';
    this.cMediaUrl = '';
    this.cTags = '';
    this.selectedFile = null;
    this.reload();
  }

  typeLabel(t: number) {
    return t === 0 ? 'Photo' : t === 1 ? 'Video' : 'Quote';
  }

  cleanTags(tags: string[] | null | undefined): string[] {
    return (tags ?? [])
      .map(t => (t ?? '').trim())
      .filter(t => t.length > 0);
  }

  loadMembers() {
    this.groupsService.groupMembers(this.groupId).subscribe({
      next: (r) => this.members = r,
      error: (err) => console.error(err)
    });
  }

  loadStats() {
    this.groupsService.groupStats(this.groupId).subscribe({
      next: (r) => this.groupStats = r,
      error: (err) => console.error(err)
    });
  }

  loadActivity() {
    this.groupsService.weeklyActivity(this.groupId).subscribe({
      next: r => this.weeklyActivity = r,
      error: err => console.error(err)
    });
  }

  // Tagging
  private getMentionContext(text: string) {
    const caret = text.length;
    const before = text.slice(0, caret);

    const atPos = before.lastIndexOf('@');
    if (atPos === -1) return null;

    const query = before.slice(atPos + 1);

    if (query.includes(' ')) return null;
    
    return { atPos, query }
  }

  updateMentionPopup() {
    const ctx = this.getMentionContext(this.cQuoteBy || '');
    if (!ctx) {
      this.showMentionPopup = false;
      return;
    }

    this.mentionQuery = ctx.query.toLowerCase();

    const all = this.members ?? [];

    this.mentionResults = all
      .filter(u => u.name.toLowerCase().includes(this.mentionQuery))
      .slice(0, 8);

    this.showMentionPopup = true;
    this.mentionIndex = Math.min(this.mentionIndex, this.mentionResults.length -1);
    if (this.mentionIndex < 0) this.mentionIndex = 0;
  }

  selectMention(u: { name: string }) {
    const ctx = this.getMentionContext(this.cQuoteBy || '');
    if (!ctx) return;

    const beforeAt = this.cQuoteBy.slice(0, ctx.atPos);
    this.cQuoteBy = `${beforeAt}@${u.name}`;
    this.showMentionPopup = false;
  }

  onQuoteByKeydown(event: KeyboardEvent) {
    if (!this.showMentionPopup) return;

    if (event.key == 'ArrowDown') {
      event.preventDefault();
      this.mentionIndex = Math.min(this.mentionIndex + 1, this.mentionResults.length - 1);
    }

    if (event.key == 'ArrowUp') {
      event.preventDefault();
      this.mentionIndex = Math.max(this.mentionIndex - 1, 0);
    }

    if (event.key == 'Enter') {
      event.preventDefault();
      const u = this.mentionResults[this.mentionIndex];
      if (u) this.selectMention(u);
    }

    if (event.key == 'Escape') {
      event.preventDefault();
      this.showMentionPopup = false;
    }
  }

  onQuoteByInput() {
    this.updateMentionPopup();
  }

  closeMentionPopup() {
    this.showMentionPopup = false;
  }

  // Albums
  loadAlbums() {
    this.groupsService.groupAlbums(this.groupId).subscribe({
      next: (r) => this.albums = r,
      error: (err) => console.error(err)
    });
  }

  goToAlbums() {
    this.router.navigate(['/groups', this.groupId, 'albums']);
  }

  // Stats
  timeSince(createdAt: string): string {
    const start = new Date(createdAt);
    const now = new Date();

    let years = now.getFullYear() - start.getFullYear();
    let months = now.getMonth() - start.getMonth();
    let days = now.getDate() - start.getDate();

    if (days < 0) {
      months--;
      const prevMonth = new Date(now.getFullYear(), now.getMonth(), 0);
      days += prevMonth.getDate();
    }

    if (months < 0) {
      years--;
      months += 12;
    }

    if (years > 0) {
      return `${years} year${years === 1 ? '' : 's'}`;
    }

    if (months > 0) {
      return `${months} month${months === 1 ? '' : 's'}`;
    }

    return `${Math.max(days, 1)} day${days === 1 ? '' : 's'}`;
  }
}
