import { ChangeDetectorRef, OnDestroy, Pipe, PipeTransform } from '@angular/core';
import { Subscription } from 'rxjs';
import { I18nService } from './i18n.service';

@Pipe({
  name: 't',
  standalone: true,
  pure: false
})
export class TranslatePipe implements PipeTransform, OnDestroy {
  private subscription: Subscription;

  constructor(
    private readonly i18n: I18nService,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.subscription = this.i18n.language$.subscribe(() => this.cdr.markForCheck());
  }

  transform(key: string, params?: Record<string, string | number | null | undefined>) {
    return this.i18n.translate(key, params);
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }
}
