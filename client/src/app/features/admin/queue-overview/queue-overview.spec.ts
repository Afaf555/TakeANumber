import { ComponentFixture, TestBed } from '@angular/core/testing';
import { QueueOverview } from './queue-overview';

describe('QueueOverview', () => {
  let component: QueueOverview;
  let fixture: ComponentFixture<QueueOverview>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [QueueOverview],
    }).compileComponents();

    fixture = TestBed.createComponent(QueueOverview);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
