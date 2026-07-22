import {
  Component, Input, Output, EventEmitter,
  ContentChild, TemplateRef, inject, signal, computed, OnInit
, ChangeDetectionStrategy} from '@angular/core';
import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { SpinnerComponent } from '../spinner/spinner.component';
import { HebrewCalendarService } from '../../../core/services/hebrew-calendar.service';
import { buildWeekDays, WeekDayBase, WeekBuiltEvent } from '../../../core/utils/calendar.utils';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-week-calendar',
  standalone: true,
  imports: [DatePipe, NgTemplateOutlet, SpinnerComponent],
  templateUrl: './week-calendar.component.html',
  styleUrls: ['./week-calendar.component.css']
})
export class WeekCalendarComponent implements OnInit {
  private hebrewCalendar = inject(HebrewCalendarService);

  @Input() title       = '';
  @Input() subtitle    = '';
  @Input() loading     = false;
  @Input() hasAnyEvening = false;
  @Input() minWeek     = -Infinity;
  @Input() maxWeek     =  Infinity;
  @Input() spinnerText = 'טוען...';

  @Output() weekBuilt      = new EventEmitter<WeekBuiltEvent>();
  @Output() dayHeaderClick = new EventEmitter<{ day: WeekDayBase; index: number }>();

  @ContentChild('dayHeaderExtra') dayHeaderExtraTpl?: TemplateRef<any>;
  @ContentChild('dayMorning')     dayMorningTpl?:     TemplateRef<any>;
  @ContentChild('dayEvening')     dayEveningTpl?:     TemplateRef<any>;

  weekOffset       = signal(0);
  internalWeekDays = signal<WeekDayBase[]>([]);

  weekLabel = computed(() => {
    const d = this.internalWeekDays();
    if (!d.length) return '';
    const opts: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'long' };
    return `${d[0].date.toLocaleDateString('he-IL', opts)} – ${d[6].date.toLocaleDateString('he-IL', opts)}`;
  });

  hebrewWeekLabel = computed(() => {
    const d = this.internalWeekDays();
    if (!d.length) return '';
    return `${d[0].hebrewDate} – ${d[6].hebrewDate}`;
  });

  canGoPrev = computed(() => this.weekOffset() > this.minWeek);
  canGoNext = computed(() => this.weekOffset() < this.maxWeek);

  ngOnInit(): void {
    this.buildInternalWeek();
  }

  prevWeek(): void {
    this.weekOffset.update(w => w - 1);
    this.buildInternalWeek();
  }

  nextWeek(): void {
    this.weekOffset.update(w => w + 1);
    this.buildInternalWeek();
  }

  onDayHeaderClick(day: WeekDayBase, index: number): void {
    if (!day.isPast && !day.isWeekend) {
      this.dayHeaderClick.emit({ day, index });
    }
  }

  /** קריאה חיצונית מה-parent לאחר שינוי נתונים (ביטול תור, שמירת משמרת וכו') */
  rebuildWeek(): void {
    this.buildInternalWeek();
  }

  private buildInternalWeek(): void {
    const { days, weekStart, weekEnd } = buildWeekDays(this.weekOffset(), this.hebrewCalendar);
    this.internalWeekDays.set(days);
    this.weekBuilt.emit({ weekOffset: this.weekOffset(), days, weekStart, weekEnd });
  }
}
