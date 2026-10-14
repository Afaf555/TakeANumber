import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActiveCounters } from './active-counters';

describe('ActiveCounters', () => {
  let component: ActiveCounters;
  let fixture: ComponentFixture<ActiveCounters>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ActiveCounters],
    }).compileComponents();

    fixture = TestBed.createComponent(ActiveCounters);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
