import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export type MemoryType = 'Photo' | 'Video' | 'Quote' | number; // backend uses enum; we accept string/number

export interface GroupDetailDto {
  id: string;
  name: string;
  inviteCode: string;
  memberCount: number;
  createdByUserId: number;
}

export interface GroupStatsDto {
  memoryCount: number;
  albumCount: number;
  createdAt: string;
}

export interface GroupWeeklyContributorDto {
  userId: number;
  displayName: string;
  profileImageUrl?: string | null;
}

export interface GroupWeeklyActivityDto {
  photos: number;
  videos: number;
  quotes: number;
  albums: number;
  contributors: GroupWeeklyContributorDto[];
}

export interface GroupMemberDto {
  userId: number;
  displayName: string;
  role: string;
  profileImageUrl?: string | null;
}

export interface GroupMemberActivityDto {
  userId: number;
  displayName: string;
  role: string;
  joinedAt: string;
  lastPostAt?: string | null;
  profileImageUrl?: string | null;
  totalMemories: number;
  photos: number;
  videos: number;
  quotes: number;
}

export interface MemoryDto {
  id: string;
  groupId: string;
  type: any;
  title?: string | null;
  quoteText?: string | null;
  quoteBy?: string | null;
  mediaUrl?: string | null;
  thumbUrl?: string | null;
  happenedAt: string;
  createdAt: string;
  createdByUserId: number;
  tags?: string[] | null;
  albumId?: string | null;
}

export interface MemoryQuery {
  page: number;
  pageSize: number;
  sort?: 'newest' | 'oldest';
  albumId?: string | null;
  type?: string | number | null;
  from?: string | null; // ISO
  to?: string | null;   // ISO
  search?: string | null;
}

export interface PagedMemories {
  total: number;
  items: MemoryDto[];
}

export interface AlbumDto {
  id: string;
  groupId: string;
  title: string;
  description?: string | null;
  dateStart: string;
  dateEnd?: string | null;
  memoriesCount: number;
}

export interface CreateAlbumRequest {
  title: string;
  description?: string | null;
  dateStart: string;
  dateEnd?: string | null;
}

@Injectable({ providedIn: 'root' })
export class GroupAdminService {
  private base = '/api/groups';

  constructor(private http: HttpClient) {}

  // ===== Group =====
  getGroup(groupId: string): Observable<GroupDetailDto> {
    return this.http.get<GroupDetailDto>(`${this.base}/${groupId}`);
  }

  getStats(groupId: string): Observable<GroupStatsDto> {
    return this.http.get<GroupStatsDto>(`${this.base}/${groupId}/stats`);
  }

  getWeeklyActivity(groupId: string): Observable<GroupWeeklyActivityDto> {
    return this.http.get<GroupWeeklyActivityDto>(`${this.base}/${groupId}/activity/week`);
  }

  getMemberActivity(groupId: string): Observable<GroupMemberActivityDto[]> {
    return this.http.get<GroupMemberActivityDto[]>(`${this.base}/${groupId}/activity/members`);
  }

  // ===== Members =====
  getMembers(groupId: string): Observable<GroupMemberDto[]> {
    return this.http.get<GroupMemberDto[]>(`${this.base}/${groupId}/members`);
  }

  // PLACEHOLDER: Endpoint not in backend yet
  changeMemberRole(groupId: string, userId: number, role: string): Observable<void> {
    console.warn('[PLACEHOLDER] changeMemberRole() needs backend endpoint');
    return new Observable<void>((subscriber) => {
      subscriber.error('PLACEHOLDER: changeMemberRole endpoint missing');
    });
  }

  // PLACEHOLDER: Endpoint not in backend yet
  removeMember(groupId: string, userId: number): Observable<void> {
    console.warn('[PLACEHOLDER] removeMember() needs backend endpoint');
    return new Observable<void>((subscriber) => {
      subscriber.error('PLACEHOLDER: removeMember endpoint missing');
    });
  }

  // ===== Memories =====
  getMemories(groupId: string, q: MemoryQuery): Observable<PagedMemories> {
    let params = new HttpParams()
      .set('page', String(q.page))
      .set('pageSize', String(q.pageSize))
      .set('sort', q.sort ?? 'newest');

    if (q.albumId) params = params.set('albumId', q.albumId);
    if (q.type !== null && q.type !== undefined && q.type !== '') params = params.set('type', String(q.type));
    if (q.from) params = params.set('from', q.from);
    if (q.to) params = params.set('to', q.to);
    if (q.search) params = params.set('search', q.search);

    return this.http.get<PagedMemories>(`${this.base}/${groupId}/memories`, { params });
  }

  // PLACEHOLDER: Endpoint not in backend yet
  deleteMemory(groupId: string, memoryId: string): Observable<void> {
    console.warn('[PLACEHOLDER] deleteMemory() needs backend endpoint');
    return new Observable<void>((subscriber) => {
      subscriber.error('PLACEHOLDER: deleteMemory endpoint missing');
    });
  }

  // ===== Albums =====
  getAlbums(groupId: string): Observable<AlbumDto[]> {
    return this.http.get<AlbumDto[]>(`${this.base}/${groupId}/albums`);
  }

  createAlbum(groupId: string, body: CreateAlbumRequest): Observable<AlbumDto> {
    return this.http.post<AlbumDto>(`${this.base}/${groupId}/albums`, body);
  }
}
