import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { GroupsService, AlbumDto, MemoryDto, AlbumPersonDto, CommentDto } from '../../groups';
import { TranslatePipe } from '../../../translate.pipe';
import { I18nService } from '../../../i18n.service';
import { environment } from '../../../../environment';

@Component({
  selector: 'app-album-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, TranslatePipe],
  templateUrl: './album-detail.html',
  styleUrls: ['./album-detail.css']
})
export class AlbumDetailComponent {
  private readonly backendOrigin = `${window.location.protocol}//${window.location.hostname}:5000`;
  groupId!: string;
  albumId!: string;

  album?: AlbumDto;
  items: MemoryDto[] = [];

  // Memory viewer + comments
  showMemoryModal = false;
  activeMemory: MemoryDto | null = null;
  comments: CommentDto[] = [];
  topLevelComments: CommentDto[] = [];
  replyMap: { [key: string]: CommentDto[] } = {};
  commentText = '';
  replyTo: CommentDto | null = null;
  commentsLoading = false;

  newType: number = 0;
  newTitle = '';
  newQuoteText = '';
  newDate = new Date().toISOString().slice(0, 10);
  selectedFile?: File;

  // Mentioning
  members: { userId: string, name: string; role: string; avatarUrl: string; }[] = [];
  memberById: { [key: string]: { name: string; avatarUrl?: string | null } } = {};
  activeUploader: { name: string; avatarUrl?: string | null } | null = null;
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
  failedMedia = new Set<string>();

  // Adding people
  albumPeople: AlbumPersonDto[] = [];
  canEditAlbum = false;
  showAddPersonModal = false;
  personQuery = '';
  personResults: { userId: string; name: string; role: string; avatarUrl?: string | null }[] = [];

  // Security
  imageSrcMap = new Map<string, string>();
  loadingSet = new Set<string>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private groupsService: GroupsService,
    private i18n: I18nService,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.groupId = this.route.snapshot.paramMap.get('id')!;
    this.albumId = this.route.snapshot.paramMap.get('albumId')!;

    this.loadAlbum();
    this.loadAlbumPeople();
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
        title: this.i18n.translate('albums.allMemories'),
        description: this.i18n.translate('albums.collections'),
        dateStart: new Date(0).toISOString(),
        dateEnd: null,
        memoryCount: 0
      };
      return;
    }
    
    this.groupsService.groupAlbums(this.groupId).subscribe({
      next: (albums) => {
        this.album = albums.find(a => a.id === this.albumId);

        this.canEditAlbum = true;
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
      next: (r) => {
        this.items = r.items;

        this.items.forEach(m => {
          if (m.type === 0 || m.type === 1) {
            this.loadMedia(m.mediaUrl);
          }
        });
      },
      error: (err) => console.error(err)
    });
  }

  loadMembers() {
    this.groupsService.groupMembers(this.groupId).subscribe({
      next: (r) => {
        this.members = r;
        this.memberById = r.reduce((acc, m) => {
          acc[m.userId] = { name: m.name, avatarUrl: m.avatarUrl ?? null };
          return acc;
        }, {} as { [key: string]: { name: string; avatarUrl?: string | null } });
        this.updateActiveUploader();
      },
      error: (err) => console.error(err)
    });
  }

  loadMedia(url?: string | null) {
    if (!url || this.imageSrcMap.has(url) || this.loadingSet.has(url)) return;

    this.loadingSet.add(url);

    const fullUrl = environment.apiUrl + url;

    this.http.get(fullUrl, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const objectUrl = URL.createObjectURL(blob);
        this.imageSrcMap.set(url, objectUrl);
        this.loadingSet.delete(url);
      },
      error: (err) => {
        console.error('Media load failed', err);
        this.loadingSet.delete(url);
      }
    });
  }

  openMemory(m: MemoryDto) {
    this.activeMemory = m;
    this.updateActiveUploader();
    this.showMemoryModal = true;
    this.commentText = '';
    this.replyTo = null;
    this.loadComments();
  }

  closeMemory() {
    this.showMemoryModal = false;
    this.activeMemory = null;
    this.activeUploader = null;
    this.comments = [];
    this.topLevelComments = [];
    this.replyMap = {};
    this.commentText = '';
    this.replyTo = null;
    this.commentsLoading = false;
  }

  private updateActiveUploader() {
    if (!this.activeMemory) {
      this.activeUploader = null;
      return;
    }

    const found = this.memberById[this.activeMemory.createdByUserId];
    this.activeUploader = found
      ? { name: found.name, avatarUrl: found.avatarUrl ?? null }
      : { name: this.i18n.translate('album.unknownUser'), avatarUrl: null };
  }

  loadComments() {
    if (!this.activeMemory) return;

    this.commentsLoading = true;
    this.groupsService.memoryComments(this.groupId, this.activeMemory.id).subscribe({
      next: (r) => {
        this.comments = r;
        this.rebuildCommentTree();
        this.commentsLoading = false;
        this.activeMemory!.commentCount = this.comments.length;
      },
      error: (err) => {
        console.error(err);
        this.commentsLoading = false;
      }
    });
  }

  private rebuildCommentTree() {
    const top: CommentDto[] = [];
    const map: { [key: string]: CommentDto[] } = {};
    const byId = new Map<string, CommentDto>();

    for (const c of this.comments) {
      byId.set(c.id, c);
    }

    const rootIdFor = (comment: CommentDto): string => {
      let current: CommentDto = comment;
      const seen = new Set<string>();

      while (current.parentCommentId && byId.has(current.parentCommentId)) {
        if (seen.has(current.parentCommentId)) break;
        seen.add(current.parentCommentId);
        current = byId.get(current.parentCommentId)!;
      }

      return current.id;
    };

    for (const c of this.comments) {
      if (c.parentCommentId) {
        const rootId = rootIdFor(c);
        if (!map[rootId]) map[rootId] = [];
        map[rootId].push(c);
      } else {
        top.push(c);
      }
    }

    const byDate = (a: CommentDto, b: CommentDto) =>
      new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();

    top.sort(byDate);
    Object.keys(map).forEach(k => map[k].sort(byDate));

    this.topLevelComments = top;
    this.replyMap = map;
  }

  submitComment() {
    if (!this.activeMemory) return;

    const content = (this.commentText || '').trim();
    if (!content) return;

    this.groupsService.addComment(this.groupId, this.activeMemory.id, {
      content,
      parentCommentId: this.replyTo?.id ?? null
    }).subscribe({
      next: (comment) => {
        this.comments = [...this.comments, comment];
        this.rebuildCommentTree();
        this.commentText = '';
        this.replyTo = null;
        this.activeMemory!.commentCount = (this.activeMemory!.commentCount || 0) + 1;
      },
      error: (err) => console.error(err)
    });
  }

  setReply(target: CommentDto) {
    this.replyTo = target;
  }

  cancelReply() {
    this.replyTo = null;
  }

  toggleMemoryLike(m: MemoryDto, event?: Event) {
    if (event) event.stopPropagation();

    if (m.isLiked) {
      this.groupsService.unlikeMemory(this.groupId, m.id).subscribe({
        next: () => {
          m.isLiked = false;
          m.likeCount = Math.max(0, (m.likeCount || 0) - 1);
        },
        error: (err) => console.error(err)
      });
    } else {
      this.groupsService.likeMemory(this.groupId, m.id).subscribe({
        next: () => {
          m.isLiked = true;
          m.likeCount = (m.likeCount || 0) + 1;
        },
        error: (err) => console.error(err)
      });
    }
  }

  toggleCommentLike(c: CommentDto, event?: Event) {
    if (event) event.stopPropagation();

    if (c.isLiked) {
      this.groupsService.unlikeComment(this.groupId, c.id).subscribe({
        next: () => {
          c.isLiked = false;
          c.likeCount = Math.max(0, (c.likeCount || 0) - 1);
        },
        error: (err) => console.error(err)
      });
    } else {
      this.groupsService.likeComment(this.groupId, c.id).subscribe({
        next: () => {
          c.isLiked = true;
          c.likeCount = (c.likeCount || 0) + 1;
        },
        error: (err) => console.error(err)
      });
    }
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
        alert(this.i18n.translate('album.selectFile'));
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
      alert(this.i18n.translate('album.selectFile'));
      return;
    }

    this.newType = this.mediaType === 'photo' ? 0 : 1;

    this.createMemory();
    this.showAddMemoryModal = false;
  }

  submitQuote() {
    this.newType = 2;

    if (!this.newQuoteText) {
      alert(this.i18n.translate('album.writeQuote'));
      return;
    }

    this.createMemory();
    this.showAddMemoryModal = false;
  }

  isImage(url: string | null | undefined): boolean {
    if (!url) return false;
    const result = /\.(jpg|jpeg|png|gif|webp)$/i.test(url);
    console.log("Image? " + result + "    " + url)
    return result;
  }

  isVideo(url: string | null | undefined): boolean {
    if (!url) return false;
    return /\.(mp4|webm|mov)$/i.test(url);
  }

  mediaSrc(url?: string | null): string | null {
    if (!url) return null;

    const token = localStorage.getItem('token');

    return `${environment.apiUrl}${url}?token=${token}`;
  }

  mediaFailed(url: string | null | undefined): boolean {
    return !!url && this.failedMedia.has(url);
  }

  onMediaError(url: string | null | undefined) {
    if (url) this.failedMedia.add(url);
  }

  // People in Album
  loadAlbumPeople() {
    if (this.albumId === 'all') return;

    this.groupsService.albumPeople(this.groupId, this.albumId)
      .subscribe(r => this.albumPeople = r);
  }

  openAddPerson() {
    this.showAddPersonModal = true;
    this.personQuery = '';
    this.updatePersonResults();
  }

  closeAddPerson() {
    this.showAddPersonModal = false;
  }

  updatePersonResults() {
    const q = (this.personQuery || '').trim().toLowerCase();

    // Only show group members not already in albumPeople
    const already = new Set(this.albumPeople.map(p => p.userId));

    this.personResults = this.members
      .filter(m => !already.has(m.userId))
      .filter(m => !q || m.name.toLowerCase().includes(q))
      .slice(0, 20);
  }

  addPersonToAlbum(userId: string) {
    if (this.albumId === 'all') return;

    this.groupsService.addAlbumPerson(this.groupId, this.albumId, userId).subscribe({
      next: () => {
        this.personResults = this.personResults.filter(
          u => u.userId !== userId
        );

        const added = this.members.find(m => m.userId === userId);
        if (added) {
          this.albumPeople = [
            ...this.albumPeople,
            {
              userId: added.userId,
              name: added.name,
              role: added.role,
              avatarUrl: added.avatarUrl ?? null
            }
          ];
        }

        this.loadAlbumPeople();
      },
      error: err => console.error(err)
    });
  }

  removePerson(userId: string) {
    this.groupsService
      .removeAlbumPerson(this.groupId, this.albumId, userId)
      .subscribe({
        next: () => {
          this.albumPeople = this.albumPeople.filter(
            p => p.userId !== userId
          );

          this.updatePersonResults();
        },
        error: err => console.error(err)
      });
  }
}
