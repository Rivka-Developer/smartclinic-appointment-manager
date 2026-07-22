import { AppointmentResponse } from './appointment';
import { WorkShiftResponse } from './work-shift';

/**
 * תא בלוח השנה השבועי של המנהלת.
 * כל תא מייצג יחידת זמן אחת (למשל 5 דקות) ויכול להיות פנוי, תפוס, buffer, או סגור.
 */
export interface CalendarSlot {
  /** הזמן המדויק שהתא מייצג */
  time: Date;
  /**
   * סוג התא:
   * - `free`   = פנוי לקביעת תור
   * - `busy`   = תפוס (יש תור)
   * - `buffer` = זמן buffer בין תורים
   * - `closed` = מחוץ לשעות הפעילות
   */
  type: 'free' | 'busy' | 'buffer' | 'closed';
  /** נתוני התור – קיים רק כאשר `type === 'busy'` */
  appointment?: AppointmentResponse;
  /** תחילת הבלוק הפנוי שאליו שייך התא (לשימוש בהדגשת בלוקים) */
  blockStart?: Date;
  /** סוף הבלוק הפנוי שאליו שייך התא */
  blockEnd?: Date;
}

/** נתוני יום שלם בלוח השנה השבועי */
export interface WeekDay {
  /** אובייקט תאריך של היום */
  date: Date;
  /** שם היום בעברית (למשל "ראשון") */
  label: string;
  /** משמרות העבודה של אותו יום */
  shifts: WorkShiftResponse[];
  /** כל תאי הזמן של אותו יום */
  slots: CalendarSlot[];
}
