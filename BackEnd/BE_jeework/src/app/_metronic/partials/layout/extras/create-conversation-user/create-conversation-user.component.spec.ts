import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateConversationUserComponent } from './create-conversation-user.component';

describe('CreateConversationUserComponent', () => {
  let component: CreateConversationUserComponent;
  let fixture: ComponentFixture<CreateConversationUserComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CreateConversationUserComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CreateConversationUserComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
