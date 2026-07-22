// src/environments/environment.ts
// הגדרות סביבת פיתוח (Development).
// apiUrl מצביע לשרת המקומי שרץ על פורט 5225.
export const environment = {
  production: false,
  apiUrl: '/api'  // בפיתוח: proxy מנתב ל-localhost:5225 — Cookie נשלח מאותו origin
};
