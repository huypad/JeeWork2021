import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InsertThanhvienComponent } from './insert-thanhvien.component';

describe('InsertThanhvienComponent', () => {
  let component: InsertThanhvienComponent;
  let fixture: ComponentFixture<InsertThanhvienComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ InsertThanhvienComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(InsertThanhvienComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
