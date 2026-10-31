import { SharedService } from "@/services/shared.service";
import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";

export const permissionGuard: CanActivateFn = (route) => {

  const router = inject(Router);
  const _sharedService = inject(SharedService);

  const requiredPermissions: string[] = route.data['permissions'] ?? [];

  if (_sharedService.isSuperAdmin()) {
    return true;
  }
  if (requiredPermissions.some((permission) => _sharedService.permissions.includes(permission))) {
    return true;
  }

  const uri = 'auth/access'
  router.navigate([uri]);
  return false;
};
