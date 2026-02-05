import { ComponentFixture, TestBed } from '@angular/core/testing';

<<<<<<< HEAD
import { GroupDetailComponent } from './group-detail';

describe('GroupDetail', () => {
  let component: GroupDetailComponent;
  let fixture: ComponentFixture<GroupDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GroupDetailComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GroupDetailComponent);
=======
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
>>>>>>> origin/main
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
