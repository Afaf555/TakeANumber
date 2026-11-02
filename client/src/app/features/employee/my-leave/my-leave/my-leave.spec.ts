import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MyLeave } from './my-leave';

describe('MyLeave', () => {
  let component: MyLeave;
  let fixture: ComponentFixture<MyLeave>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MyLeave],
    }).compileComponents();

    fixture = TestBed.createComponent(MyLeave);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
