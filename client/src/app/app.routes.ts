import { Routes } from '@angular/router';
import { Login } from './features/account/login/login';
import { Register } from './features/account/register/register';
import { TakeTicket } from './features/client/take-ticket/take-ticket';
import { ActiveCounters } from './features/display/active-counters/active-counters';
import { authGuard } from './core/guards/auth-guard';
import { roleGuard } from './core/guards/role-guard';
import { guestGuard } from './core/guards/guest-guard';

export const routes: Routes = [
  { path: '', redirectTo: 'take-ticket', pathMatch: 'full' },
  { path: 'login', component: Login, canActivate: [guestGuard] },
  { path: 'register', component: Register },
  { path: 'display', component: ActiveCounters },
  { path: 'take-ticket', component: TakeTicket },

  {
    path: 'take-ticket',
    component: TakeTicket,
    data: { roles: ['Client'] },
  },
  {
    path: 'counter-panel',
    loadComponent: () => import('./features/employee/counter-panel/counter-panel').then((m) => m.CounterPanel),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Employee'] },
  },
  {
    path: 'manage-services',
    loadComponent: () => import('./features/admin/manage-services/manage-services').then((m) => m.ManageServices),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
  },
  {
    path: 'manage-counters',
    loadComponent: () => import('./features/admin/manage-counters/manage-counters').then((m) => m.ManageCounters),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
  },
  {
    path: 'manage-employees',
    loadComponent: () => import('./features/admin/manage-employees/manage-employees').then((m) => m.ManageEmployees),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
  },{
  path: 'queue-overview',
  loadComponent: () => import('./features/admin/queue-overview/queue-overview').then((m) => m.QueueOverview),
  canActivate: [authGuard, roleGuard],
  data: { roles: ['Admin'] },
},
  
  {
  path: 'reports',
  loadComponent: () => import('./features/admin/reports/reports/reports').then((m) => m.Reports),
  canActivate: [authGuard, roleGuard],
  data: { roles: ['Admin'] },
},
{
  path: 'calendar',
  loadComponent: () => import('./features/admin/calendar/calendar/calendar').then((m) => m.Calendar),
  canActivate: [authGuard, roleGuard],
  data: { roles: ['Admin'] },
},
{ path: '**', redirectTo: 'take-ticket', pathMatch: 'full' },
];