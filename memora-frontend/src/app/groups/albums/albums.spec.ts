import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GroupAlbumsComponent } from './albums';

describe('Albums', () => {
  let component: GroupAlbumsComponent;
  let fixture: ComponentFixture<GroupAlbumsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GroupAlbumsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GroupAlbumsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
