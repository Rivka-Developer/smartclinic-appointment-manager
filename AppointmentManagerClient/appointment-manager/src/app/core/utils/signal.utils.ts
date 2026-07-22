import { DestroyRef, WritableSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';

/** טוען Observable לתוך signal עם ניהול loading אוטומטי.
 *  העברת destroyRef מבטיחה ניקוי אוטומטי כשהקומפוננט נהרס. */
export function loadSignal<T>(
  obs$: Observable<T>,
  target: WritableSignal<T>,
  loading: WritableSignal<boolean>,
  destroyRef?: DestroyRef
): void {
  loading.set(true);
  const source$ = destroyRef ? obs$.pipe(takeUntilDestroyed(destroyRef)) : obs$;
  source$.subscribe({
    next: d => { target.set(d); loading.set(false); },
    error: () => loading.set(false)
  });
}

/** מחזיר פונקציה שמפחיתה מונה ומריצה onDone כשמגיע לאפס */
export function makeCountdown(total: number, onDone: () => void): () => void {
  let count = total;
  return () => { if (--count === 0) onDone(); };
}

/** מבצע ביטול תור עם ניהול loading state.
 *  העברת destroyRef מבטיחה ניקוי אוטומטי כשהקומפוננט נהרס. */
export function performCancel(
  obs$: Observable<void>,
  cancelling: WritableSignal<boolean>,
  onSuccess: () => void,
  onError: () => void,
  destroyRef?: DestroyRef
): void {
  cancelling.set(true);
  const source$ = destroyRef ? obs$.pipe(takeUntilDestroyed(destroyRef)) : obs$;
  source$.subscribe({
    next: () => { cancelling.set(false); onSuccess(); },
    error: () => { cancelling.set(false); onError(); }
  });
}
