// src/environments/environment.prod.ts
// הגדרות סביבת ייצור (Production) - Render.
// אם שם השירות ב-Render שונה מ-"smartclinic-api" (למשל השם היה תפוס), יש לעדכן כאן
// וגם את AllowedOrigins__0 / render.yaml בהתאם.
export const environment = {
  production: true,
  apiUrl: 'https://smartclinic-api.onrender.com/api',
  // Client ID מ-Google Cloud Console (OAuth consent screen → Web application)
  googleClientId: '80976086282-1ehspc5m892frnp8f96r66tmp63j8dv3.apps.googleusercontent.com'
};
