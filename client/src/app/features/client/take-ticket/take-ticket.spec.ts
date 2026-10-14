import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TakeTicket } from './take-ticket';

describe('TakeTicket', () => {
  let component: TakeTicket;
  let fixture: ComponentFixture<TakeTicket>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TakeTicket],
    }).compileComponents();

    fixture = TestBed.createComponent(TakeTicket);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
