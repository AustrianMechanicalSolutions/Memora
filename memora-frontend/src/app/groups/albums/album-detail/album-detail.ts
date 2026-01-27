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
  }

  backToAlbums() {
    this.router.navigate(['/groups', this.groupId, 'albums']);
  }

  loadAlbum() {
    // easiest: load all albums and find the one we need
    this.groupsService.groupAlbums(this.groupId).subscribe({
      next: (albums) => {
        this.album = albums.find(a => a.id === this.albumId);
      },
      error: (err) => console.error(err)
    });
  }

  loadMemories() {
    this.groupsService.memories(this.groupId, {
      albumId: this.albumId,
      sort: 'newest',
      page: 1,
      pageSize: 50
    }).subscribe({
      next: (r) => this.items = r.items,
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
}
