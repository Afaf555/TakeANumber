import { Routes } from '@angular/router';
import { Login } from './features/account/login/login';
import { Register } from './features/account/register/register';
import { TakeTicket } from './features/client/take-ticket/take-ticket';
import { CounterPanel } from './features/employee/counter-panel/counter-panel';
import { ManageServices } from './features/admin/manage-services/manage-services';
import { ManageCounters } from './features/admin/manage-counters/manage-counters';
import { ManageEmployees } from './features/admin/manage-employees/manage-employees';
import { ActiveCounters } from './features/display/active-counters/active-counters';
import { authGuard } from './core/guards/auth-guard';
import { roleGuard } from './core/guards/role-guard';
import { guestGuard } from './core/guards/guest-guard';
import { QueueOverview } from './features/admin/queue-overview/queue-overview';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: Login, canActivate: [guestGuard] },
  { path: 'register', component: Register, canActivate: [guestGuard] },
  { path: 'display', component: ActiveCounters },
  {
    path: 'take-ticket',
    component: TakeTicket,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Client'] },
  },
  {
    path: 'counter-panel',
    component: CounterPanel,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Employee'] },
  },
  {
    path: 'manage-services',
    component: ManageServices,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
  },
  {
    path: 'manage-counters',
    component: ManageCounters,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
  },
  {
    path: 'manage-employees',
    component: ManageEmployees,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
  },{
  path: 'queue-overview',
  component: QueueOverview,
  canActivate: [authGuard, roleGuard],
  data: { roles: ['Admin'] },
},
  { path: '**', redirectTo: 'login', pathMatch: 'full' },
];