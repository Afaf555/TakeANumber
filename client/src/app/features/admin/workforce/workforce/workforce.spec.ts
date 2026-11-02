import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Workforce } from './workforce';

describe('Workforce', () => {
  let component: Workforce;
  let fixture: ComponentFixture<Workforce>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Workforce],
    }).compileComponents();

    fixture = TestBed.createComponent(Workforce);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
