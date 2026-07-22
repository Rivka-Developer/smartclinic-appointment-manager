/** בקשה להוספת/עדכון משמרת עבודה – נשלחת ב-POST/PUT /workshifts */
export interface WorkShiftRequest {
  /** תאריך המשמרת בפורמט "YYYY-MM-DD" */
  date: string;
  /** שעת תחילת המשמרת בפורמט "HH:mm:ss" */
  startTime: string;
  /** שעת סיום המשמרת בפורמט "HH:mm:ss" */
  endTime: string;
}

/** נתוני משמרת כפי שמוחזרים מהשרת */
export interface WorkShiftResponse {
  /** מזהה ייחודי של המשמרת (GUID) */
  id: string;
  /** תאריך המשמרת בפורמט "YYYY-MM-DD" */
  date: string;
  /** שעת תחילת המשמרת בפורמט "HH:mm:ss" */
  startTime: string;
  /** שעת סיום המשמרת בפורמט "HH:mm:ss" */
  endTime: string;
}
