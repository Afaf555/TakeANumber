import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ManageCounters } from './manage-counters';

describe('ManageCounters', () => {
  let component: ManageCounters;
  let fixture: ComponentFixture<ManageCounters>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManageCounters],
    }).compileComponents();

    fixture = TestBed.createComponent(ManageCounters);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
