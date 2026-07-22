import {
  Component, Input, Output, EventEmitter,
  signal, inject, ChangeDetectionStrategy, OnInit, computed
} from '@angular/core';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { WorkShiftsService } from '../../../core/services/work-shifts.service';
import { SnackService } from '../../../core/services/snack.service';
import { toLocalDateStr } from '../../../core/utils/calendar.utils';

interface TemplateShift {
  startTime: string; // "HH:mm"
  endTime: string;   // "HH:mm"
}

interface DayTemplate {
  dayOfWeek: number; // 0=ראשון ... 6=שבת
  enabled: boolean;
  shifts: TemplateShift[];
}

const DAY_LABELS = ['ראשון', 'שני', 'שלישי', 'רביעי', 'חמישי', 'שישי', 'שבת'];
const STORAGE_KEY = 'smartclinic_weekly_template';

const DEFAULT_TEMPLATE: DayTemplate[] = [
  { dayOfWeek: 0, enabled: true, shifts: [{ startTime: '20:00', endTime: '23:00' }] },
  { dayOfWeek: 1, enabled: true, shifts: [{ startTime: '10:30', endTime: '14:00' }, { startTime: '20:00', endTime: '23:00' }] },
  { dayOfWeek: 2, enabled: true, shifts: [{ startTime: '20:00', endTime: '23:00' }] },
  { dayOfWeek: 3, enabled: true, shifts: [{ startTime: '10:30', endTime: '14:00' }, { startTime: '20:00', endTime: '23:00' }] },
  { dayOfWeek: 4, enabled: true, shifts: [{ startTime: '20:00', endTime: '23:00' }] },
];

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-weekly-template',
  standalone: true,
  imports: [],
  templateUrl: './weekly-template.component.html',
  styleUrls: ['./weekly-template.component.css']
})
export class WeeklyTemplateComponent implements OnInit {
  @Input() weekStart: Date | null = null;
  @Output() applied = new EventEmitter<void>();

  private shiftsService = inject(WorkShiftsService);
  private snack         = inject(SnackService);

  readonly dayLabels = DAY_LABELS;

  panelOpen = signal(false);
  editMode  = signal(false);
  applying  = signal(false);
  template = signal<DayTemplate[]>([]);
  draft    = signal<DayTemplate[]>([]);

  readonly activeDays = computed(() => this.template().filter(d => d.enabled));

  ngOnInit(): void {
    this.template.set(this.loadTemplate());
  }

  private clone(t: DayTemplate[]): DayTemplate[] {
    return t.map(d => ({ ...d, shifts: d.shifts.map(s => ({ ...s })) }));
  }

  private loadTemplate(): DayTemplate[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as DayTemplate[];
        if (Array.isArray(parsed)) {
          const workDays = parsed.filter(d => d.dayOfWeek <= 4);
          if (workDays.length === 5) return workDays;
        }
      }
    } catch { /* ignore */ }
    return this.clone(DEFAULT_TEMPLATE);
  }

  togglePanel(): void {
    const opening = !this.panelOpen();
    this.panelOpen.set(opening);
    if (!opening) this.editMode.set(false);
  }

  openEdit(): void {
    this.draft.set(this.clone(this.template()));
    this.editMode.set(true);
  }

  cancelEdit(): void {
    this.editMode.set(false);
  }

  resetToDefault(): void {
    this.draft.set(this.clone(DEFAULT_TEMPLATE));
  }

  toggleDay(index: number): void {
    this.draft.update(d => {
      const copy = this.clone(d);
      copy[index].enabled = !copy[index].enabled;
      return copy;
    });
  }

  addShift(dayIndex: number): void {
    this.draft.update(d => {
      const copy = this.clone(d);
      copy[dayIndex].shifts.push({ startTime: '09:00', endTime: '12:00' });
      return copy;
    });
  }

  removeShift(dayIndex: number, shiftIndex: number): void {
    this.draft.update(d => {
      const copy = this.clone(d);
      copy[dayIndex].shifts.splice(shiftIndex, 1);
      return copy;
    });
  }

  updateTime(dayIndex: number, shiftIndex: number, field: 'startTime' | 'endTime', value: string): void {
    this.draft.update(d => {
      const copy = this.clone(d);
      copy[dayIndex].shifts[shiftIndex][field] = value;
      return copy;
    });
  }

  saveTemplate(): void {
    const saved = this.clone(this.draft());
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(saved)); } catch { /* ignore */ }
    this.template.set(saved);
    this.editMode.set(false);
    this.snack.success('תבנית שמורה');
  }

  applyToWeek(): void {
    const start = this.weekStart;
    if (!start) return;

    const template = this.template();
    const requests: Observable<{ message: string } | null>[] = [];

    for (let i = 0; i < 7; i++) {
      const date = new Date(start);
      date.setDate(start.getDate() + i);
      const dow = date.getDay();
      const dayT = template.find(d => d.dayOfWeek === dow);
      if (!dayT?.enabled || !dayT.shifts.length) continue;

      const dateStr = toLocalDateStr(date);
      for (const shift of dayT.shifts) {
        requests.push(
          this.shiftsService.add({
            date: dateStr,
            startTime: shift.startTime + ':00',
            endTime:   shift.endTime   + ':00'
          }).pipe(catchError(() => of(null)))
        );
      }
    }

    if (!requests.length) {
      this.snack.info('אין ימים פעילים בתבנית');
      return;
    }

    this.applying.set(true);
    forkJoin(requests).subscribe(results => {
      this.applying.set(false);
      const ok  = results.filter(r => r !== null).length;
      const err = results.length - ok;
      if      (err === 0) this.snack.success(`${ok} משמרות נוספו בהצלחה`);
      else if (ok  === 0) this.snack.error('לא נוספו משמרות – ייתכן שכבר קיימות');
      else                this.snack.info(`${ok} נוספו, ${err} כבר קיימות או נכשלו`);
      this.applied.emit();
    });
  }
}
