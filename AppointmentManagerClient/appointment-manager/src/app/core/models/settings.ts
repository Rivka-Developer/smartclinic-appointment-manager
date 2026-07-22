/**
 * הגדרות המערכת שנשלחות ומתקבלות מ-/settings.
 * מגדירות את שעות הפעילות ומגבלות הזמן לתורים.
 */
export interface SystemSettingsDto {
  /** שעת תחילת משמרת בוקר (פורמט "HH:mm:ss") */
  morningStartTime: string;
  /** שעת תחילת משמרת ערב – קו ההפרדה בלוח השנה */
  eveningStartTime: string;
  /** משך מקסימלי לתור במשמרת בוקר (דקות) */
  morningMaxDuration: number;
  /** משך מקסימלי לתור במשמרת ערב (דקות) */
  eveningMaxDuration: number;
  /** זמן מאגר (buffer) בין תורים (דקות) */
  bufferTime: number;
  /** מרווח מינימלי שנוצר בין תורים שיחשב לגיטימי (דקות) */
  minGapSize: number;
}
