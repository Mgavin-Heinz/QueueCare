using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QueueCare.Models;

namespace QueueCare.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ClinicDay> ClinicDays { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<QueueEntry> QueueEntries { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLog { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ClinicDay>()
                .HasIndex(c => new { c.Date, c.ServiceId })
                .IsUnique();

            builder.Entity<Patient>()
                .HasIndex(p => p.IdNumber)
                .IsUnique();

            builder.Entity<Patient>()
                .HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<Patient>(p => p.UserId);

            builder.Entity<QueueEntry>()
                .HasOne(q => q.Appointment)
                .WithOne()
                .HasForeignKey<QueueEntry>(q => q.AppointmentId);

            builder.Entity<Appointment>()
                .Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Entity<QueueEntry>()
                .Property(q => q.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Entity<ClinicDay>()
                 .HasOne(c => c.Service)
                 .WithMany()
                 .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
                .HasOne(a => a.ClinicDay)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<QueueEntry>()
                .HasOne(q => q.Patient)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<QueueEntry>()
                .HasOne(q => q.ClinicDay)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .HasOne(n => n.Patient)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AuditLog>()
                .HasOne(l => l.User)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }


}
