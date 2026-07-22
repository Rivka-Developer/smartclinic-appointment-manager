// =====================================
// קובץ: ApplicationDbContext.cs
// שכבה: Infrastructure (תשתית)
// תפקיד: מגדיר את "גשר" בין הקוד לבסיס הנתונים SQL Server.
//         DbContext = מחלקת Entity Framework Core שמנהלת חיבור לבסיס הנתונים,
//         מעקב אחרי שינויים, ומיפוי ישויות לטבלאות.
//         OnModelCreating = מאפשר הגדרת מבנה הטבלאות, קשרים, ואינדקסים.
// =====================================

using Microsoft.EntityFrameworkCore;
using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain;

namespace AppointmentManager.Infrastructure
{
    /// <summary>
    /// DbContext של האפליקציה - מנהל את כל הגישה לבסיס הנתונים.
    /// יורש מ-DbContext של Entity Framework Core.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// קונסטרקטור - מקבל את הגדרות החיבור מה-DI Container.
        /// DbContextOptions = הגדרות חיבור (Connection String, Provider וכו').
        /// ": base(options)" = מעביר את ההגדרות למחלקת האב (DbContext).
        /// </summary>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // --- הגדרת הטבלאות (DbSets) ---
        // כל DbSet<T> מייצג טבלה בבסיס הנתונים.
        // EF Core ממפה: DbSet<User> ↔ טבלת "Users" ב-SQL

        /// <summary>טבלת המשתמשים - Users</summary>
        public DbSet<User> Users { get; set; } = default!;

        /// <summary>טבלת התורים - Appointments</summary>
        public DbSet<Appointment> Appointments { get; set; } = default!;

        /// <summary>טבלת משמרות העבודה - WorkShifts</summary>
        public DbSet<WorkShift> WorkShifts { get; set; } = default!;

        /// <summary>טבלת הגדרות המערכת - Settings</summary>
        public DbSet<SystemSettings> Settings { get; set; } = default!;

        /// <summary>טבלת הצעות החלפת תורים - SwapOffers</summary>
        public DbSet<SwapOffer> SwapOffers { get; set; } = default!;

        /// <summary>
        /// נקרא אוטומטית על ידי EF Core בעת בניית המודל.
        /// כאן מגדירים: מפתחות ראשיים, קשרים, אינדקסים, ואילוצים.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // חובה לקרוא ל-base לאתחול הגדרות ברירת המחדל של EF
            base.OnModelCreating(modelBuilder);

            // --- הגדרות טבלת Users ---
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id); // מפתח ראשי (Primary Key) - שדה Id

                entity.Property(u => u.Email)
                    .IsRequired()          // שדה חובה - NOT NULL ב-SQL
                    .HasMaxLength(150);    // מקסימום 150 תווים - VARCHAR(150)

                entity.HasIndex(u => u.Email)
                    .IsUnique();           // אינדקס ייחודי - מונע אימיילים כפולים

                entity.Property(u => u.FullName)
                    .IsRequired()          // שדה חובה
                    .HasMaxLength(100);    // מקסימום 100 תווים
            });

            // --- הגדרות טבלת Appointments ---
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.HasIndex(a => a.StartTime);
                entity.HasIndex(a => a.ClientId);
                entity.HasIndex(a => a.Status);

                // מניעת הזמנות כפולות: שני תורים פעילים (Scheduled=0) לא יכולים להתחיל באותו זמן
                entity.HasIndex(a => new { a.StartTime, a.Status })
                      .IsUnique()
                      .HasFilter("[Status] = 0");


                entity.HasOne(a => a.Client)
                      .WithMany(u => u.Appointments)
                      .HasForeignKey(a => a.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // --- הגדרות טבלת WorkShifts ---
            modelBuilder.Entity<WorkShift>(entity =>
            {
                entity.HasKey(ws => ws.Id);

                entity.Property(ws => ws.Date).IsRequired();
                entity.HasIndex(ws => ws.Date); // מאיץ שאילתות לפי תאריך
            });

            // --- הגדרות טבלת SystemSettings ---
            modelBuilder.Entity<SystemSettings>(entity =>
            {
                entity.HasKey(ss => ss.Id); // מפתח ראשי

                entity.Property(ss => ss.BusinessName)
                    .HasMaxLength(100);      // שם עסק - עד 100 תווים
            });

            // --- הגדרות טבלת SwapOffers ---
            modelBuilder.Entity<SwapOffer>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.HasOne(o => o.Appointment)
                      .WithMany()
                      .HasForeignKey(o => o.AppointmentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.OfferedByClient)
                      .WithMany()
                      .HasForeignKey(o => o.OfferedByClientId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.AcceptedByClient)
                      .WithMany()
                      .HasForeignKey(o => o.AcceptedByClientId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);

                // אינדקס חלקי ייחודי: תור אחד יכול לקבל הצעה Active אחת בלבד
                entity.HasIndex(o => new { o.AppointmentId, o.Status })
                      .IsUnique()
                      .HasFilter("[Status] = 0");

                entity.HasIndex(o => o.Status);
            });
        }
    }
}
