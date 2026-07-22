# SmartClinic

מערכת ניהול תורים Full-Stack לעסקים/קליניקות, עם תמיכה בשני תפקידים — לקוח ומנהל.

## טכנולוגיות
- **Backend:** ASP.NET Core 8, Clean Architecture, EF Core, SQL Server, Hangfire
- **Frontend:** Angular 21 (Standalone Components + Signals)
- **Auth:** JWT
- **שפה:** ממשק בעברית מלאה (RTL)

## תכונות עיקריות
- ניהול תורים ומשמרות עבודה
- לוח שנה עברי למנהל
- תזכורות אימייל אוטומטיות (Hangfire)
- דוח יומי למנהל
- הרשאות לפי תפקיד (Client / Admin)

## הרצה מקומית

### Backend
```powershell
cd AppointmentManager
dotnet run --project AppointmentManager.Api
```
Swagger: https://localhost:7001/swagger

### Frontend
```powershell
cd AppointmentManagerClient/appointment-manager
npm install
ng serve
```
http://localhost:4200
תעתיק את כל זה (בלי שורת הגדר העליונה/תחתונה עם ה-````) לתוך README.md, שמור, ואז תריץ:


git add README.md
git commit -m "Update README"
git push
