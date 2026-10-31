import { SharedService } from "@/services/shared.service";
import { UserService } from "@/services/user.service";
import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { firstValueFrom } from "rxjs";


export const authGuard: CanActivateFn = async (route, state) => {

  const router = inject(Router);
  const _userService = inject(UserService);
  const _sharedService = inject(SharedService);

  try {
    const res = await firstValueFrom(_userService.getMe());
    const roles = res.data.roles ?? [];
    const permissions = [... new Set(roles.flatMap((roles)=> roles.permissions.map((permissions) => permissions.key)))];

    _sharedService.setCurrentUser(res.data)
    _sharedService.setRoles(roles.map((roles) => roles.name))
    _sharedService.setPermissions(permissions)

  } catch (error) {
    const uri = `auth/login`
    router.navigate([uri], {
      queryParams: {
        redirect_uri: state.url
      }
    })
    return false;
  }

  return true;
};