/** נתוני כניסה למערכת – נשלחים ב-POST /auth/login */
export interface LoginRequest {
  email: string;
  password: string;
}

/** נתוני הרשמה למערכת – נשלחים ב-POST /auth/register */
export interface RegisterRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  password: string;
}

/**
 * תשובת השרת לאחר כניסה/הרשמה מוצלחת.
 * ה-JWT נשמר ב-HttpOnly Cookie בלבד (לא נשלח ל-JavaScript).
 */
export interface AuthResponse {
  /** שם המשתמש המחובר לתצוגה בממשק */
  fullName: string;
  /** תפקיד: 'Admin' = מנהלת, 'Client' = לקוחה */
  role: 'Admin' | 'Client';
}
