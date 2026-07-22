/**
 * נקודת הכניסה המרכזית למודלים של האפליקציה (barrel file).
 *
 * כל הממשקים והטיפוסים מיוצאים מכאן כדי לאפשר import נקי:
 * `import { AppointmentResponse, UserResponse } from '@core/models'`
 *
 * המודלים מחולקים לפי דומיין:
 * - auth        – אימות והרשמה
 * - appointment – תורים, בלוקי זמן ואפשרויות מיקום
 * - work-shift  – משמרות עבודה
 * - user        – משתמשים והיסטוריית תורים
 * - settings    – הגדרות מערכת
 * - calendar    – עזרי ממשק לוח שנה שבועי
 */
export * from './auth';
export * from './appointment';
export * from './work-shift';
export * from './user';
export * from './settings';
export * from './calendar';
export * from './chat';
export * from './swap-offer';
