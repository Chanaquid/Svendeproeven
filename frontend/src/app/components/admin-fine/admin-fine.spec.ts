import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminFine } from './admin-fine';

describe('AdminFine', () => {
  let component: AdminFine;
  let fixture: ComponentFixture<AdminFine>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminFine],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminFine);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
