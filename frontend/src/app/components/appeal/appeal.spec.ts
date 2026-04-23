import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Appeal } from './appeal';

describe('Appeal', () => {
  let component: Appeal;
  let fixture: ComponentFixture<Appeal>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Appeal],
    }).compileComponents();

    fixture = TestBed.createComponent(Appeal);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
