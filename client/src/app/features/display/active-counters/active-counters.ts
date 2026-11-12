import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, OnDestroy, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AccountService } from '../../../core/services/account-service';
import { DisplayService } from '../../../core/services/display-service';
import { ActiveCounter } from '../../../types/display';

type Called = { code: string; counterName: string; serviceName: string | null };


@Component({
  selector: 'app-active-counters',
  imports: [DatePipe, RouterLink],
  templateUrl: './active-counters.html',
  styleUrl: './active-counters.css',
})
export class ActiveCounters implements OnDestroy {
  protected displayService = inject(DisplayService);
  protected accountService = inject(AccountService);

  protected now = signal(Date.now());
  private clock = setInterval(() => this.now.set(Date.now()), 10000);

  protected justCalled = signal<Called | null>(null);
  protected callKey = signal(0); 
  private previous = new Map<number, string | null>();
  private firstRun = true;

  protected exitLink = computed(() => {
    const user = this.accountService.currentUser();
    return user ? this.accountService.homeFor(user.role) : '/take-ticket';
  });

  private serviceOf = computed(() => {
    const map = new Map<string, string>();
    for (const q of this.displayService.queues()) for (const s of q.serving) map.set(s.code, q.serviceName);
    return map;
  });

  constructor() {
    
    effect(() => {
      const counters = this.displayService.counters();
      untracked(() => this.detectCalls(counters));
    });
  }

  private detectCalls(counters: ActiveCounter[]) {
    let called: ActiveCounter | null = null;
    for (const c of counters) {
      const before = this.previous.get(c.counterId);
      if (!this.firstRun && c.currentTicketCode && before !== c.currentTicketCode) called = c;
      this.previous.set(c.counterId, c.currentTicketCode);
    }
    if (this.firstRun && counters.length) {
     
      called = [...counters]
        .filter((c) => c.currentTicketNumber != null)
        .sort((a, b) => (b.currentTicketNumber ?? 0) - (a.currentTicketNumber ?? 0))[0] ?? null;
      this.firstRun = false;
      if (called) this.justCalled.set(this.toCalled(called));
      return;
    }
    if (called) {
      this.justCalled.set(this.toCalled(called));
      this.callKey.update((k) => k + 1);

    } else {
     
    }
  }

  private toCalled(c: ActiveCounter): Called {
    const code = c.currentTicketCode ?? '';
    return { code, counterName: c.counterName, serviceName: this.serviceOf().get(code) ?? null };
  }

  protected serviceName = computed(() => {
    const j = this.justCalled();
    return j ? (j.serviceName ?? this.serviceOf().get(j.code) ?? '') : '';
  });

 
  protected isLatest(c: ActiveCounter) {
    return false;
  }

  protected cardClass(c: ActiveCounter) {
    if (c.isOnBreak) return 'border border-warning/50 bg-warning/5';
    if (this.isLatest(c)) return 'border-2 border-primary bg-base-100';
    return 'border border-base-300 bg-base-100';
  }

  private numberOf(code: string) {
    return Number(code.split('-').pop()) || 0;
  }

 
  ngOnDestroy() {
    clearInterval(this.clock);
  }
}
