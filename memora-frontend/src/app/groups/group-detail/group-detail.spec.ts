import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GroupDetail } from './group-detail';

describe('GroupDetail', () => {
  let component: GroupDetail;
  let fixture: ComponentFixture<GroupDetail>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GroupDetail]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GroupDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
