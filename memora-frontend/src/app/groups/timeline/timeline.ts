import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { GroupsService, AlbumDto } from '../groups';
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

  mediaSrc(url?: string | null): string | null {
    if (!url) return null;

    const token = localStorage.getItem('token');

    return `${environment.apiUrl}${url}?token=${token}`;
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
}