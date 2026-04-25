import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DirectChat } from './direct-chat';

describe('DirectChat', () => {
  let component: DirectChat;
  let fixture: ComponentFixture<DirectChat>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DirectChat],
    }).compileComponents();

    fixture = TestBed.createComponent(DirectChat);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
