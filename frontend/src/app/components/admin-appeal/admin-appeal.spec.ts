import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminAppeal } from './admin-appeal';

describe('AdminAppeal', () => {
  let component: AdminAppeal;
  let fixture: ComponentFixture<AdminAppeal>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminAppeal],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminAppeal);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
