import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SupportChat } from './support-chat';

describe('SupportChat', () => {
  let component: SupportChat;
  let fixture: ComponentFixture<SupportChat>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SupportChat],
    }).compileComponents();

    fixture = TestBed.createComponent(SupportChat);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
