import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AlertModal } from '../../../shared/alert-modal/alert-modal/alert-modal';
import { SettingsService } from '../../../core/services/settings-service';
import { DEFAULT_SETTINGS, shiftTime, WorkSettings } from '../../../types/settings';

@Component({
  selector: 'app-settings',
  imports: [FormsModule, AlertModal],
  templateUrl: './settings.html',
})
export class Settings implements OnInit {
  private settingsService = inject(SettingsService);

  protected form = signal<WorkSettings>({ ...DEFAULT_SETTINGS });
  protected error = signal('');
  protected message = signal('');

  protected lateAfter = computed(() => shiftTime(this.form().openTime, this.form().lateGraceMinutes));
  protected earlyBefore = computed(() => shiftTime(this.form().closeTime, -this.form().earlyGraceMinutes));

  ngOnInit(): void {
    this.settingsService.get().subscribe({
      next: (s) => this.form.set(s),
      error: () => this.error.set('Could not load settings'),
    });
  }

  protected set<K extends keyof WorkSettings>(key: K, value: WorkSettings[K]) {
    this.form.update((f) => ({ ...f, [key]: value }));
  }

  protected save() {
    this.settingsService.update(this.form()).subscribe({
      next: (s) => {
        this.form.set(s);
        this.message.set('Settings saved');
      },
      error: (e) => this.error.set(typeof e.error === 'string' ? e.error : 'Could not save settings'),
    });
  }
}
