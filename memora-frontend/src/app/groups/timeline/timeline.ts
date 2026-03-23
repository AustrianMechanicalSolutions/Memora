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
        });

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
}