import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GroupsService, AlbumDto } from '../groups';
import { TranslatePipe } from '../../translation/translate.pipe';
import { I18nService } from '../../translation/i18n.service';

@Component({
  selector: 'app-albums',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, TranslatePipe],
  templateUrl: './albums.html',
  styleUrls: ['./albums.css']
})
export class GroupAlbumsComponent {
  groupId!: string;
  albums: AlbumDto[] = [];

  // create album form
  showCreate = false;
  aTitle = '';
  aDescription = '';
  aDateStart = new Date().toISOString().slice(0, 10);
  aDateEnd = '';
  selectedAlbumId: string | null = null;
  allMemoriesCount = 0;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private groupsService: GroupsService,
    private i18n: I18nService
  ) {}

  ngOnInit() {
    this.groupId = this.route.snapshot.paramMap.get('id')!;

    this.route.queryParamMap.subscribe(params => {
      const a = params.get('albumId');
      this.selectedAlbumId = a;
      this.reload();
    });
    this.reload();
  }

  reload() {
    this.groupsService.groupAlbums(this.groupId).subscribe({
      next: (albums) => {
        this.albums = albums;
      },
      error: (err) => console.error(err)
    });

    this.groupsService.memories(this.groupId, {
      page: 1,
      pageSize: 1
    }).subscribe({
      next: (r) => {
        this.allMemoriesCount = r.total;
      },
      error: (err) => console.error(err)
    });
  }

  createAlbum() {
    if (!this.aTitle.trim()) {
      alert(this.i18n.translate('admin.titleRequired'));
      return;
    }

    const body = {
      title: this.aTitle.trim(),
      description: this.aDescription?.trim() || null,
      dateStart: new Date(this.aDateStart).toISOString(),
      dateEnd: this.aDateEnd ? new Date(this.aDateEnd).toISOString() : null
    };

    this.groupsService.createAlbum(this.groupId, body).subscribe({
      next: () => {
        this.showCreate = false;
        this.aTitle = '';
        this.aDescription = '';
        this.aDateStart = new Date().toISOString().slice(0, 10);
        this.aDateEnd = '';
        this.reload();
      },
      error: (err) => console.error(err)
    });
  }

  backToGroup() {
    this.router.navigate(['/groups', this.groupId]);
  }

  openAlbum(albumId: string) {
    this.router.navigate(['/groups', this.groupId, 'albums', albumId]);
  }

  get albumsWithAll(): AlbumDto[] {
    const allAlbum: AlbumDto = {
      id: 'all',
      groupId: this.groupId,
      title: this.i18n.translate('albums.allMemories'),
      description: this.i18n.translate('albums.collections'),
      dateStart: null as any,
      dateEnd: null,
      memoryCount: this.allMemoriesCount
    };

    return [allAlbum, ...this.albums];
  }
}
