import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminDispute } from './admin-dispute';

describe('AdminDispute', () => {
  let component: AdminDispute;
  let fixture: ComponentFixture<AdminDispute>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminDispute],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminDispute);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
