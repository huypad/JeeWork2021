import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateConvesationGroupComponent } from './create-convesation-group.component';

describe('CreateConvesationGroupComponent', () => {
  let component: CreateConvesationGroupComponent;
  let fixture: ComponentFixture<CreateConvesationGroupComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CreateConvesationGroupComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CreateConvesationGroupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
