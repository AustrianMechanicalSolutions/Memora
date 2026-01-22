import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface GroupListItemDto {
  id: string;
  name: string;
  memberCount: number;
}

export interface GroupDetailDto {
  id: string;
  name: string;
  inviteCode: string;
  memberCount: number;
}

export interface MemoryDto {
  id: string;
  groupId: string;
  type: number; // 0 Photo, 1 Video, 2 Quote
  title?: string;
  quoteText?: string;
  mediaUrl?: string;
  thumbUrl?: string;
  happenedAt: string;
  createdAt: string;
  createdByUserId: string;
  tags: string[];
}

export interface MemoryQuery {
  type?: number;
  from?: string;
  to?: string;
  search?: string;
  sort?: 'newest' | 'oldest';
  page?: number;
  pageSize?: number;
}

export interface CreateGroupRequest {
  name: string;
}

@Injectable({
  providedIn: 'root'
})
export class GroupsService {
  private baseUrl = 'http://localhost:5000/api/groups'; 
  // change to your backend URL/port

  constructor(private http: HttpClient) {}

  myGroups(): Observable<GroupListItemDto[]> {
    return this.http.get<GroupListItemDto[]>(this.baseUrl);
  }

  groupDetail(groupId: string): Observable<GroupDetailDto> {
    return this.http.get<GroupDetailDto>(`${this.baseUrl}/${groupId}`);
  }

  memories(groupId: string, query: MemoryQuery) {
    let params = new HttpParams();
    Object.entries(query).forEach(([k, v]) => {
      if (v !== undefined && v !== null && v !== '') {
        params = params.set(k, String(v));
      }
    });

    return this.http.get<{ total: number; items: MemoryDto[] }>(
      `${this.baseUrl}/${groupId}/memories`,
      { params }
    );
  }

  createMemory(groupId: string, body: any) {
    return this.http.post(`${this.baseUrl}/${groupId}/memories`, body);
  }

  createMemoryWithFile(groupId: string, file: File, data: any) {
    const formData = new FormData();

    formData.append("type", String(data.type));
    formData.append("title", data.title ?? "");
    formData.append("quoteText", data.quoteText ?? "");
    formData.append("happenedAt", data.happenedAt);
    for (const tag of (data.tags ?? [])) {
      formData.append("tags", tag);
    }

    formData.append("file", file);

    return this.http.post(`${this.baseUrl}/${groupId}/memories`, formData);
  }

  createGroup(name: string) {
    return this.http.post<GroupDetailDto>(this.baseUrl, { name });
  }
}
