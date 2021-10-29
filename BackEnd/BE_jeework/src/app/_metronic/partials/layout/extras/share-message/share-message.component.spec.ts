import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShareMessageComponent } from './share-message.component';

describe('ShareMessageComponent', () => {
  let component: ShareMessageComponent;
  let fixture: ComponentFixture<ShareMessageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ShareMessageComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ShareMessageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
