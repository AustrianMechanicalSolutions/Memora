import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { GroupsPageComponent } from './groups-page';
import { GroupsService } from '../groups';

describe('GroupsPageComponent', () => {
  let component: GroupsPageComponent;
  let fixture: ComponentFixture<GroupsPageComponent>;
  let mockGroupsService: jasmine.SpyObj<GroupsService>;

  beforeEach(async () => {
    mockGroupsService = jasmine.createSpyObj('GroupsService', ['myGroups', 'createGroup', 'joinGroup']);
    mockGroupsService.myGroups.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [GroupsPageComponent],
      providers: [
        { provide: GroupsService, useValue: mockGroupsService }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GroupsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
