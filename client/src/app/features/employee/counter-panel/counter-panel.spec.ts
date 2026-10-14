import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CounterPanel } from './counter-panel';

describe('CounterPanel', () => {
  let component: CounterPanel;
  let fixture: ComponentFixture<CounterPanel>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CounterPanel],
    }).compileComponents();

    fixture = TestBed.createComponent(CounterPanel);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
