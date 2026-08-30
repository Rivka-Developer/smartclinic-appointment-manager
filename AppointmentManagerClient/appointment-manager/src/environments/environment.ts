// src/environments/environment.ts
// הגדרות סביבת פיתוח (Development).
// apiUrl מצביע לשרת המקומי שרץ על פורט 5225.
export const environment = {
  production: false,
  apiUrl: '/api',  // בפיתוח: proxy מנתב ל-localhost:5225 — Cookie נשלח מאותו origin
  // Client ID מ-Google Cloud Console (OAuth consent screen → Web application)
  googleClientId: '80976086282-1ehspc5m892frnp8f96r66tmp63j8dv3.apps.googleusercontent.com'
};
