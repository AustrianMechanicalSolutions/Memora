import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GroupsService, AlbumDto, MemoryDto } from '../../groups';

@Component({
  selector: 'app-album-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './album-detail.html',
  styleUrls: ['./album-detail.css']
})
export class AlbumDetailComponent {
  groupId!: string;
  albumId!: string;

  album?: AlbumDto;
  items: MemoryDto[] = [];

  newType: number = 0;
  newTitle = '';
  newQuoteText = '';
  newDate = new Date().toISOString().slice(0, 10);
  selectedFile?: File;

  // Mentioning
  members: { userId: string, name: string; role: string }[] = [];
  newQuoteBy = '';
  showMentionPopup = false;
  mentionQuery = '';
  mentionResults: { userId: string, name: string; role: string }[] = [];
  mentionIndex = 0;

  // Adding a memory
  showAddMemoryModal = false;
  addStep: 'choose' | 'media' | 'quote' = 'choose';
  mediaType: 'photo' | 'video' = 'photo';
  previewUrl: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private groupsService: GroupsService
  ) {}

  ngOnInit() {
    this.groupId = this.route.snapshot.paramMap.get('id')!;
    this.albumId = this.route.snapshot.paramMap.get('albumId')!;

    this.loadAlbum();
    this.loadMemories();
    this.loadMembers();
  }

  backToAlbums() {
    this.router.navigate(['/groups', this.groupId, 'albums']);
  }

  loadAlbum() {
    if (this.albumId == 'all') {
      this.album = {
        id: 'all',
        groupId: this.groupId,
        title: 'All Memories',
        description: 'Photos, videos & quotes — everything in one place',
        dateStart: new Date(0).toISOString(),
        dateEnd: null,
        memoryCount: 0
      };
      return;
    }
    
    this.groupsService.groupAlbums(this.groupId).subscribe({
      next: (albums) => {
        this.album = albums.find(a => a.id === this.albumId);
      },
      error: (err) => console.error(err)
    });
  }

  loadMemories() {
    const query: any = {
      sort: 'newest',
      page: 1,
      pageSize: 50
    }

    if (this.albumId != 'all') query.albumId = this.albumId;
    
    this.groupsService.memories(this.groupId, query).subscribe({
      next: (r) => this.items = r.items,
      error: (err) => console.error(err)
    });
  }

  loadMembers() {
    this.groupsService.groupMembers(this.groupId).subscribe({
      next: (r) => this.members = r,
      error: (err) => console.error(err)
    });
  }

  typeLabel(t: number) {
    return t === 0 ? 'Photo' : t === 1 ? 'Video' : 'Quote';
  }

  cleanTags(tags: string[] | null | undefined): string[] {
    return (tags ?? [])
      .map(t => (t ?? '').trim())
      .filter(t => t.length > 0);
  }

  createMemory() {
    const baseData: any = {
      type: this.newType,
      title: this.newTitle || null,
      quoteText: this.newType === 2 ? this.newQuoteText : null,
      quoteBy: this.newType === 2 ? (this.newQuoteBy || null) : null,
      happenedAt: new Date(this.newDate).toISOString(),
      tags: [],
      albumId: this.albumId !== 'all' ? this.albumId : null
    };

    if (this.newType === 2) {
      // Quote (no file)
      this.groupsService.createMemory(this.groupId, baseData).subscribe({
        next: () => this.afterCreate(),
        error: err => console.error(err)
      });
    } else {
      // Photo or video
      if (!this.selectedFile) {
        alert('Please select a file');
        return;
      }

      this.groupsService
        .createMemoryWithFile(this.groupId, this.selectedFile, baseData)
        .subscribe({
          next: () => this.afterCreate(),
          error: err => console.error(err)
        });
    }
  }

  afterCreate() {
    this.newTitle = '';
    this.newQuoteText = '';
    this.selectedFile = undefined;
    this.previewUrl = null;
    this.loadMemories();
    this.newQuoteBy = '';
    this.showMentionPopup = false;
  }

  // Mentioning
  private getMentionContext(text: string) {
    const caret = text.length;
    const before = text.slice(0, caret);

    const atPos = before.lastIndexOf('@');
    if (atPos === -1) return null;

    const query = before.slice(atPos + 1);
    if (query.includes(' ')) return null;

    return { atPos, query };
  }

  updateMentionPopup() {
    const ctx = this.getMentionContext(this.newQuoteBy || '');
    if (!ctx) {
      this.showMentionPopup = false;
      return;
    }

    this.mentionQuery = ctx.query.toLowerCase();

    this.mentionResults = this.members
      .filter(u => u.name.toLowerCase().includes(this.mentionQuery))
      .slice(0, 8);

    this.showMentionPopup = true;
    this.mentionIndex = Math.min(
      this.mentionIndex,
      this.mentionResults.length - 1
    );
    if (this.mentionIndex < 0) this.mentionIndex = 0;
  }

  selectMention(u: { name: string }) {
    const ctx = this.getMentionContext(this.newQuoteBy || '');
    if (!ctx) return;

    const beforeAt = this.newQuoteBy.slice(0, ctx.atPos);
    this.newQuoteBy = `${beforeAt}@${u.name}`;
    this.showMentionPopup = false;
  }

  onQuoteByKeydown(event: KeyboardEvent) {
    if (!this.showMentionPopup) return;

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.mentionIndex = Math.min(
        this.mentionIndex + 1,
        this.mentionResults.length - 1
      );
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.mentionIndex = Math.max(this.mentionIndex - 1, 0);
    }

    if (event.key === 'Enter') {
      event.preventDefault();
      const u = this.mentionResults[this.mentionIndex];
      if (u) this.selectMention(u);
    }

    if (event.key === 'Escape') {
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

  openAddMemory() {
    this.showAddMemoryModal = true;
    this.addStep = 'choose';
  }

  onFileSelected(e: any) {
    const file = e.target.files?.[0];
    if (!file) return;

    this.selectedFile = file;
    this.previewUrl = URL.createObjectURL(file);  
  }

  submitMedia() {
    if (!this.selectedFile) {
      alert('Please select a file');
      return;
    }

    this.newType = this.mediaType === 'photo' ? 0 : 1;

    this.createMemory();
    this.showAddMemoryModal = false;
  }

  submitQuote() {
    this.newType = 2;

    if (!this.newQuoteText) {
      alert('Please write a quote');
      return;
    }

    this.createMemory();
    this.showAddMemoryModal = false;
  }

  isImage(url: string | null | undefined): boolean {
    if (!url) return false;
    return /\.(jpg|jpeg|png|gif|webp)$/i.test(url);
  }

  isVideo(url: string | null | undefined): boolean {
    if (!url) return false;
    return /\.(mp4|webm|mov)$/i.test(url);
  }
}
