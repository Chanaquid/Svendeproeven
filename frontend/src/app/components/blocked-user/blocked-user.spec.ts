import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BlockedUser } from './blocked-user';

describe('BlockedUser', () => {
  let component: BlockedUser;
  let fixture: ComponentFixture<BlockedUser>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BlockedUser],
    }).compileComponents();

    fixture = TestBed.createComponent(BlockedUser);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
