// =====================================
// קובץ: MappingProfile.cs
// שכבה: Application
// תפקיד: מגדיר את כללי המיפוי בין ישויות הדומיין ל-DTOs ולהיפך.
//         AutoMapper = ספריה שמבצעת העתקת נתונים בין אובייקטים אוטומטית לפי הכללים שנגדיר.
//         בלי AutoMapper היה צריך לכתוב בצורה ידנית:
//           response.Id = appointment.Id;
//           response.StartTime = appointment.StartTime; וכו'...
//         Profile = מחלקה בסיסית של AutoMapper שמכילה הגדרות מיפוי.
// =====================================

using AppointmentManager.Application.DTOs;
using AppointmentManager.Application.DTOs.Auth;
using AppointmentManager.Domain.Entities;
using AutoMapper;

namespace AppointmentManager.Application
{
    /// <summary>
    /// מגדיר את כללי המיפוי האוטומטי בין מחלקות.
    /// הכלל הבסיסי: שדות עם שמות זהים מועתקים אוטומטית.
    /// שדות עם שמות שונים מוגדרים ידנית ב-ForMember.
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // --- מיפוי בקשת תור לישות תור ---
            // AppointmentRequest (DTO) → Appointment (Domain Entity)
            // שדות זהים: StartTime, DurationMinutes (מועתקים אוטומטית)
            CreateMap<AppointmentRequest, Appointment>();

            // --- מיפוי ישות תור לתגובת תור ---
            // Appointment (Domain Entity) → AppointmentResponse (DTO)
            CreateMap<Appointment, AppointmentResponse>()
                // EndTime לא קיים ב-Appointment ישירות, מחושב: StartTime + DurationMinutes
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.StartTime.AddMinutes(src.DurationMinutes)))
                // ClientName נלקח מ-Client?.FullName (? = בטוח גם אם Client הוא null)
                .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => src.Client != null ? src.Client.FullName : string.Empty))
                // ClientPhone נלקח מ-Client?.PhoneNumber
                .ForMember(dest => dest.ClientPhone, opt => opt.MapFrom(src => src.Client != null ? src.Client.PhoneNumber : string.Empty));

            // --- מיפוי TimeBlock ל-TimeBlockDto ---
            // TimeBlock (מבנה פנימי) → TimeBlockDto (DTO לשליחה לחוץ)
            CreateMap<TimeBlock, TimeBlockDto>();

            // --- מיפוי בקשת משמרת לישות משמרת ---
            CreateMap<WorkShiftRequest, WorkShift>();

            // --- מיפוי ישות משמרת לתגובת משמרת ---
            // שדות זהים: Id, Date, StartTime, EndTime (מועתקים אוטומטית)
            CreateMap<WorkShift, WorkShiftResponse>();

            // --- מיפוי הגדרות ל-DTO ---
            CreateMap<SystemSettings, SystemSettingsDto>();

            // --- מיפוי DTO להגדרות (לעדכון) ---
            CreateMap<SystemSettingsDto, SystemSettings>()
                // Id לא נשלח מה-Frontend - מתעלמים ממנו כדי לא לאפס אותו
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // --- מיפוי ישות משתמש לתגובת משתמש ---
            CreateMap<User, UserResponse>()
                // Role הוא enum, ממיר ל-string: UserRole.Admin → "Admin"
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
                // TotalAppointments = ספירת התורים. בודק null לפני ספירה
                .ForMember(dest => dest.TotalAppointments, opt => opt.MapFrom(src => src.Appointments != null ? src.Appointments.Count() : 0));

            // --- מיפוי משתמש להיסטוריה ---
            // ממיר User → UserHistoryResponse (כולל Appointments)
            CreateMap<User, UserHistoryResponse>();

            // --- מיפוי בקשת הרשמה למשתמש ---
            CreateMap<RegisterRequest, User>();
        }
    }
}
