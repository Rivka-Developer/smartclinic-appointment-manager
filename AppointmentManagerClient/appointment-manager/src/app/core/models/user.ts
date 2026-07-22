import { AppointmentResponse } from './appointment';

/** נתוני לקוח בסיסיים לתצוגה ברשימת הלקוחות */
export interface UserResponse {
  /** מזהה ייחודי של המשתמש (GUID) */
  id: string;
  /** שם מלא */
  fullName: string;
  /** מספר טלפון */
  phoneNumber: string;
  /** כתובת מייל */
  email: string;
  /** סה"כ תורים שנקבעו בעבר */
  totalAppointments: number;
}

/** נתוני לקוח כולל היסטוריית התורים המלאה שלו */
export interface UserHistoryResponse {
  /** מזהה ייחודי של המשתמש (GUID) */
  id: string;
  /** שם מלא */
  fullName: string;
  /** מספר טלפון */
  phoneNumber: string;
  /** כתובת מייל */
  email: string;
  /** רשימת כל התורים של המשתמש (עבר + עתיד) */
  appointments: AppointmentResponse[];
}
