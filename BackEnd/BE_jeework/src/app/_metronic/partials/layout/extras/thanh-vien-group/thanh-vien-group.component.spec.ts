import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ThanhVienGroupComponent } from './thanh-vien-group.component';

describe('ThanhVienGroupComponent', () => {
  let component: ThanhVienGroupComponent;
  let fixture: ComponentFixture<ThanhVienGroupComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ThanhVienGroupComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ThanhVienGroupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
