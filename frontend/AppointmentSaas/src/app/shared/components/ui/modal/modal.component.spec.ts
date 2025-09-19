import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ModalComponent } from './modal.component';
import { By } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';

describe('ModalComponent', () => {
  let component: ModalComponent;
  let fixture: ComponentFixture<ModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ModalComponent, CommonModule],
    }).compileComponents();

    fixture = TestBed.createComponent(ModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should not be visible when isOpen is false', () => {
    component.isOpen = false;
    fixture.detectChanges();
    const modalOverlay = fixture.debugElement.query(By.css('.app-modal-overlay'));
    expect(modalOverlay).toBeNull();
  });

  it('should be visible when isOpen is true', () => {
    component.isOpen = true;
    fixture.detectChanges();
    const modalOverlay = fixture.debugElement.query(By.css('.app-modal-overlay'));
    expect(modalOverlay).toBeTruthy();
  });

  it('should display the title', () => {
    component.isOpen = true;
    component.title = 'Test Modal Title';
    fixture.detectChanges();
    const titleElement = fixture.debugElement.query(By.css('.app-modal-title')).nativeElement;
    expect(titleElement.textContent).toContain('Test Modal Title');
  });

  it('should emit close event when close button is clicked', () => {
    spyOn(component.close, 'emit');
    component.isOpen = true;
    component.showCloseButton = true;
    fixture.detectChanges();
    const closeButton = fixture.debugElement.query(By.css('.app-modal-close-button')).nativeElement;
    closeButton.click();
    expect(component.close.emit).toHaveBeenCalled();
  });

  it('should emit close event when overlay is clicked and closeOnOverlayClick is true', () => {
    spyOn(component.close, 'emit');
    component.isOpen = true;
    component.closeOnOverlayClick = true;
    fixture.detectChanges();
    const modalOverlay = fixture.debugElement.query(By.css('.app-modal-overlay')).nativeElement;
    modalOverlay.click();
    expect(component.close.emit).toHaveBeenCalled();
  });

  it('should not emit close event when overlay is clicked and closeOnOverlayClick is false', () => {
    spyOn(component.close, 'emit');
    component.isOpen = true;
    component.closeOnOverlayClick = false;
    fixture.detectChanges();
    const modalOverlay = fixture.debugElement.query(By.css('.app-modal-overlay')).nativeElement;
    modalOverlay.click();
    expect(component.close.emit).not.toHaveBeenCalled();
  });

  it('should render ng-content', () => {
    const testContent = '<div>Modal Content</div>';
    TestBed.overrideTemplate(ModalComponent, `
      <app-modal [isOpen]="true">
        ${testContent}
      </app-modal>
    `);
    fixture = TestBed.createComponent(ModalComponent);
    fixture.componentInstance.isOpen = true;
    fixture.detectChanges();
    const modalBody = fixture.debugElement.query(By.css('.app-modal-body')).nativeElement;
    expect(modalBody.textContent).toContain('Modal Content');
  });
});