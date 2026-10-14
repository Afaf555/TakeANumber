import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ManageServices } from './manage-services';

describe('ManageServices', () => {
  let component: ManageServices;
  let fixture: ComponentFixture<ManageServices>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManageServices],
    }).compileComponents();

    fixture = TestBed.createComponent(ManageServices);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
