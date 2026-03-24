import { Component, HostListener, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { GroupsService, AlbumDto } from '../groups';
import { ElementRef, QueryList, ViewChildren } from '@angular/core';
import { environment } from '../../../environment';

@Component({
  selector: 'app-group-timeline',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './timeline.html', 
  styleUrls: ['./timeline.css']
})
export class TimelineComponent implements OnInit {
  groupId!: string;
  albums: AlbumDto[] = [];
  loading = true;

  groupMode: 'year' | 'month' = 'year';

  // Story mode
  @ViewChildren('timelineItem') items!: QueryList<ElementRef>;

  storyMode = false;
  private storyIndex = 0;
  private storyTimeout: any;

  activeStoryIndex: number | null = null;
  activeMemoryIndex = 0;
  private memoryInterval: any;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private groupsService: GroupsService
  ) {}

  ngOnInit() {
    this.groupId = this.route.snapshot.paramMap.get('id')!;
    this.loadAlbums();
  }

  loadAlbums() {
    this.groupsService.groupAlbums(this.groupId).subscribe({
      next: (albums) => {
        this.albums = albums.sort(
          (a, b) =>
            new Date(a.dateStart).getTime() -
            new Date(b.dateStart).getTime()
        );

        // assign random memory as cover
        this.albums.forEach(a => {
          this.groupsService.memories(this.groupId, {
            albumId: a.id,
            pageSize: 10 // get some to randomize
          }).subscribe(r => {
            const media = r.items
              .filter(m => m.mediaUrl); // only items with media

            if (media.length > 0) {
              const random =
                media[Math.floor(Math.random() * media.length)];

              (a as any).coverUrl = this.mediaSrc(random.mediaUrl);
            }
          });

          this.groupsService.loadTopMemory(this.groupId, a);

          this.groupsService
            .getAlbumPreviewMemories(this.groupId, a.id)
            .subscribe(memories => {
              a.previewMemories = memories.map(m => ({
                ...m,
                mediaUrl: m.mediaUrl
              }));
            });
        });

        this.determineGrouping();

        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.loading = false;
      }
    });
  }

  openAlbum(album: AlbumDto) {
    this.router.navigate(['/groups', this.groupId, 'albums', album.id]);
  }

  mediaSrc(url?: string | null): string | undefined {
    if (!url) return undefined;

    const token = localStorage.getItem('token');

    // Sometimes token is appended
    if (url.includes('token=')) {
      return url;
    }

    const separator = url.includes('?') ? '&' : '?';

    if (url.startsWith('http')) {
      return `${url}${separator}token=${token}`;
    }

    return `${environment.apiUrl}${url}${separator}token=${token}`;
  }

  determineGrouping() {
    if (this.albums.length === 0) return;

    const dates = this.albums.map(a => new Date(a.dateStart));
    const min = new Date(Math.min(...dates.map(d => d.getTime())));
    const max = new Date(Math.max(...dates.map(d => d.getTime())));

    const diffYears = max.getFullYear() - min.getFullYear();

    if (diffYears >= 2) {
      this.groupMode = 'year';
    } else {
      this.groupMode = 'month';
    }
  }

  getTimeKey(album: AlbumDto): string {
    const d = new Date(album.dateStart);

    if (this.groupMode === 'year') {
      return `${d.getFullYear()}`;
    }

    return `${d.getFullYear()}-${d.getMonth()}`;
  }

  isTimeChange(i: number): boolean {
    if (i === 0) return true;

    return (
      this.getTimeKey(this.albums[i]) !==
      this.getTimeKey(this.albums[i - 1])
    );
  }

  formatTimeLabel(album: AlbumDto): string {
    const d = new Date(album.dateStart);

    if (this.groupMode === 'year') {
      return `${d.getFullYear()}`;
    }

    return d.toLocaleString('default', {
      month: 'long',
      year: 'numeric'
    });
  }

  isImageFromType(m: any): boolean {
    return m?.type === 0;
  }

  isVideoFromType(m: any): boolean {
    return m?.type === 1;
  }

  isVideo(url: string | null | undefined): boolean {
    if (!url) return false;

    return /\.(mp4|webm|mov)$/i.test(url);
  }

  // Story mode
  toggleStoryMode() {
    if (this.storyMode) {
      this.stopStory();
      this.storyMode = false;
    } else {
      this.storyMode = true;
      this.startStory();
    }
  }

  startStory() {
    this.storyIndex = 0;
    this.playNext();
  }

  stopStory() {
    this.storyMode = false;

    clearTimeout(this.storyTimeout);
    clearInterval(this.memoryInterval);

    this.storyIndex = 0;
    this.activeStoryIndex = null;
    this.activeMemoryIndex = 0;

    this.clearHighlights();
  }

  private getScrollParent(el: HTMLElement): HTMLElement | Window {
    let parent = el.parentElement;

    while (parent) {
      const style = getComputedStyle(parent);
      const overflowY = style.overflowY;

      if (
        (overflowY === 'auto' || overflowY === 'scroll') &&
        parent.scrollHeight > parent.clientHeight
      ) {
        return parent;
      }

      parent = parent.parentElement;
    }

    return window;
  }

  scrollToElement(el: HTMLElement) {
    el.scrollIntoView({
      behavior: 'smooth',
      block: 'start'
    });

    setTimeout(() => {
      window.scrollBy({
        top: -window.innerHeight * 0.25,
        behavior: 'smooth'
      });
    }, 300);
  }

  playNext() {
    if (!this.storyMode) return;

    const elements = this.items.toArray();

    if (this.storyIndex >= elements.length) {
      this.stopStory();
      return;
    }

    const el = elements[this.storyIndex].nativeElement;
    const album = this.albums[this.storyIndex];

    this.activeStoryIndex = this.storyIndex;
    this.activeMemoryIndex = 0;

    // WAIT for DOM update + animation
    setTimeout(() => {
      this.scrollToElement(el);
    }, 100); // key fix

    this.startMemoryPlayback(album);

    this.storyIndex++;

    this.storyTimeout = setTimeout(() => {
      if (!this.storyMode) return;

      this.stopMemoryPlayback();
      this.playNext();
    }, 5000);
  }

  highlight(el: HTMLElement) {
  this.clearHighlights();
    el.classList.add('active-story');
  }

  clearHighlights() {
    this.items.forEach(i =>
      i.nativeElement.classList.remove('active-story')
    );
  }

  startMemoryPlayback(album: AlbumDto) {
    const memories = album.previewMemories?.length
      ? album.previewMemories
      : album.topMemory
        ? [album.topMemory]
        : [];

    if (memories.length === 0) return;

    const totalDuration = 5000; // total per album
    const perMemory = Math.max(totalDuration / memories.length, 1500);

    this.activeMemoryIndex = 0;

    this.memoryInterval = setInterval(() => {
      this.activeMemoryIndex++;

      // stop at last memory (no looping)
      if (this.activeMemoryIndex >= memories.length) {
        clearInterval(this.memoryInterval);
      }
    }, perMemory);
  }

  stopMemoryPlayback() {
    clearInterval(this.memoryInterval);
  }

  @HostListener('mouseenter')
  pause() {
    if (this.storyMode) clearTimeout(this.storyTimeout);
  }

  @HostListener('mouseleave')
  resume() {
    if (this.storyMode) this.playNext();
  }

  @HostListener('document:keydown.escape')
  onEsc() {
    if (this.storyMode) {
      this.stopStory();
      this.storyMode = false;
    }
  }
}