import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { GroupsService } from '../groups';
import { AuthService } from '../../user/auth.service';
import { map, switchMap } from 'rxjs';

export const adminGuard: CanActivateFn = (route, _state) => {
  const groupsService = inject(GroupsService);
  const auth = inject(AuthService);
  const router = inject(Router);

  const groupId = route.paramMap.get('id')!;

  return auth.currentUser().pipe(
    switchMap(user =>
      groupsService.groupMembers(groupId).pipe(
        map(members => {
          const isAdmin = members.some(
            m => m.userId === user.id && m.role === 'Admin'
          );

          if (isAdmin) return true;

          return router.createUrlTree(['/groups', groupId]);
        })
      )
    )
  );
};