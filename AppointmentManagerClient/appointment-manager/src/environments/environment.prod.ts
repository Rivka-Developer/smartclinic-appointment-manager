// src/environments/environment.prod.ts
// הגדרות סביבת ייצור (Production) - Render.
// שם השירות "smartclinic-api" היה תפוס ב-Render, אז הוא קיבל סיומת אקראית בפועל.
export const environment = {
  production: true,
  apiUrl: 'https://smartclinic-api-hasv.onrender.com/api',
  // Client ID מ-Google Cloud Console (OAuth consent screen → Web application)
  googleClientId: '80976086282-1ehspc5m892frnp8f96r66tmp63j8dv3.apps.googleusercontent.com'
};
