import {
  ActivatedRouteSnapshot,
  DetachedRouteHandle,
  RouteReuseStrategy,
} from '@angular/router';

export class CachedRouteReuseStrategy implements RouteReuseStrategy {
  private handles = new Map<string, DetachedRouteHandle>();
  private readonly noCache = ['login', 'register'];

  private key(route: ActivatedRouteSnapshot) {
    return route.routeConfig?.path ?? '';
  }

  private cacheable(route: ActivatedRouteSnapshot) {
    const path = route.routeConfig?.path;
    return !!path && path !== '**' && !this.noCache.includes(path);
  }

  shouldDetach(route: ActivatedRouteSnapshot): boolean {
    return this.cacheable(route);
  }

  store(route: ActivatedRouteSnapshot, handle: DetachedRouteHandle | null): void {
    if (handle) this.handles.set(this.key(route), handle);
    else this.handles.delete(this.key(route));
  }

  shouldAttach(route: ActivatedRouteSnapshot): boolean {
    return this.cacheable(route) && this.handles.has(this.key(route));
  }

  retrieve(route: ActivatedRouteSnapshot): DetachedRouteHandle | null {
    return this.handles.get(this.key(route)) ?? null;
  }

  shouldReuseRoute(future: ActivatedRouteSnapshot, curr: ActivatedRouteSnapshot): boolean {
    return future.routeConfig === curr.routeConfig;
  }

  clear() {
    for (const handle of this.handles.values()) (handle as any).componentRef?.destroy();
    this.handles.clear();
  }
}
