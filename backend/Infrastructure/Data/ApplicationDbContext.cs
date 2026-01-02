using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<PatientProfile> PatientProfiles { get; set; }
    public DbSet<ProfessionalProfile> ProfessionalProfiles { get; set; }
    public DbSet<Specialty> Specialties { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<Invite> Invites { get; set; }
    public DbSet<ScheduleBlock> ScheduleBlocks { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<MedicalCertificate> MedicalCertificates { get; set; }
    public DbSet<DigitalCertificate> DigitalCertificates { get; set; }
    public DbSet<ExamRequest> ExamRequests { get; set; }
    public DbSet<MedicalReport> MedicalReports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Cpf).IsUnique();
            entity.HasIndex(e => e.Phone).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Cpf).IsRequired().HasMaxLength(14);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Avatar).HasMaxLength(500);

            entity.HasMany(e => e.AppointmentsAsPatient)
                .WithOne(a => a.Patient)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.AppointmentsAsProfessional)
                .WithOne(a => a.Professional)
                .HasForeignKey(a => a.ProfessionalId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Relacionamento 1:1 com PatientProfile
            entity.HasOne(e => e.PatientProfile)
                .WithOne(p => p.User)
                .HasForeignKey<PatientProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Relacionamento 1:1 com ProfessionalProfile
            entity.HasOne(e => e.ProfessionalProfile)
                .WithOne(p => p.User)
                .HasForeignKey<ProfessionalProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // PatientProfile Configuration
        modelBuilder.Entity<PatientProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.Cns);
            entity.Property(e => e.Cns).HasMaxLength(15);
            entity.Property(e => e.SocialName).HasMaxLength(200);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.MotherName).HasMaxLength(200);
            entity.Property(e => e.FatherName).HasMaxLength(200);
            entity.Property(e => e.Nationality).HasMaxLength(100);
            entity.Property(e => e.ZipCode).HasMaxLength(10);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(2);
        });
        
        // ProfessionalProfile Configuration
        modelBuilder.Entity<ProfessionalProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.Crm);
            entity.Property(e => e.Crm).HasMaxLength(20);
            entity.Property(e => e.Cbo).HasMaxLength(10);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.Nationality).HasMaxLength(100);
            entity.Property(e => e.ZipCode).HasMaxLength(10);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(2);
            
            // Relacionamento com Specialty
            entity.HasOne(e => e.Specialty)
                .WithMany(s => s.Professionals)
                .HasForeignKey(e => e.SpecialtyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Specialty Configuration
        modelBuilder.Entity<Specialty>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
        });

        // Appointment Configuration
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Observation).HasMaxLength(2000);
            entity.Property(e => e.MeetLink).HasMaxLength(500);

            entity.HasOne(e => e.Specialty)
                .WithMany(s => s.Appointments)
                .HasForeignKey(e => e.SpecialtyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Notification Configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Link).HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Schedule Configuration
        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Professional)
                .WithMany(u => u.Schedules)
                .HasForeignKey(e => e.ProfessionalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog Configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);

            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Attachment Configuration
        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.FileType).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.Appointment)
                .WithMany(a => a.Attachments)
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Invite Configuration
        modelBuilder.Entity<Invite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.Property(e => e.Email).HasMaxLength(255); // Email não é mais obrigatório para links genéricos
            entity.Property(e => e.Token).IsRequired().HasMaxLength(50);
            
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ScheduleBlock Configuration
        modelBuilder.Entity<ScheduleBlock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);

            entity.HasOne(e => e.Professional)
                .WithMany(u => u.ScheduleBlocks)
                .HasForeignKey(e => e.ProfessionalId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Approver)
                .WithMany()
                .HasForeignKey(e => e.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Prescription Configuration
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ItemsJson).IsRequired();
            entity.Property(e => e.DigitalSignature).HasMaxLength(10000);
            entity.Property(e => e.CertificateThumbprint).HasMaxLength(100);
            entity.Property(e => e.CertificateSubject).HasMaxLength(500);
            entity.Property(e => e.DocumentHash).HasMaxLength(100);
            entity.HasIndex(e => e.DocumentHash);

            entity.HasOne(e => e.Appointment)
                .WithMany()
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Professional)
                .WithMany()
                .HasForeignKey(e => e.ProfessionalId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MedicalCertificate Configuration
        modelBuilder.Entity<MedicalCertificate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tipo).IsRequired();
            entity.Property(e => e.Conteudo).IsRequired();
            entity.Property(e => e.Cid).HasMaxLength(20);
            entity.Property(e => e.DigitalSignature).HasMaxLength(10000);
            entity.Property(e => e.CertificateThumbprint).HasMaxLength(100);
            entity.Property(e => e.CertificateSubject).HasMaxLength(500);
            entity.Property(e => e.DocumentHash).HasMaxLength(100);
            entity.HasIndex(e => e.DocumentHash);
            entity.HasIndex(e => e.AppointmentId);

            entity.HasOne(e => e.Appointment)
                .WithMany()
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Professional)
                .WithMany()
                .HasForeignKey(e => e.ProfessionalId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // DigitalCertificate Configuration
        modelBuilder.Entity<DigitalCertificate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Issuer).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Thumbprint).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CpfFromCertificate).HasMaxLength(14);
            entity.Property(e => e.NameFromCertificate).HasMaxLength(300);
            entity.Property(e => e.EncryptedPfxBase64).IsRequired();
            entity.Property(e => e.EncryptionIV).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Thumbprint);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.DigitalCertificates)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // ExamRequest Configuration
        modelBuilder.Entity<ExamRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NomeExame).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CodigoExame).HasMaxLength(50);
            entity.Property(e => e.Categoria).IsRequired();
            entity.Property(e => e.Prioridade).IsRequired();
            entity.Property(e => e.IndicacaoClinica).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.HipoteseDiagnostica).HasMaxLength(500);
            entity.Property(e => e.Cid).HasMaxLength(20);
            entity.Property(e => e.Observacoes).HasMaxLength(2000);
            entity.Property(e => e.InstrucoesPreparo).HasMaxLength(2000);
            entity.Property(e => e.DigitalSignature).HasMaxLength(10000);
            entity.Property(e => e.CertificateThumbprint).HasMaxLength(100);
            entity.Property(e => e.CertificateSubject).HasMaxLength(500);
            entity.Property(e => e.DocumentHash).HasMaxLength(100);
            entity.HasIndex(e => e.DocumentHash);
            entity.HasIndex(e => e.AppointmentId);

            entity.HasOne(e => e.Appointment)
                .WithMany()
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Professional)
                .WithMany()
                .HasForeignKey(e => e.ProfessionalId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // MedicalReport Configuration
        modelBuilder.Entity<MedicalReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tipo).IsRequired();
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(500);
            entity.Property(e => e.HistoricoClinico).HasMaxLength(5000);
            entity.Property(e => e.ExameFisico).HasMaxLength(5000);
            entity.Property(e => e.ExamesComplementares).HasMaxLength(5000);
            entity.Property(e => e.HipoteseDiagnostica).HasMaxLength(500);
            entity.Property(e => e.Cid).HasMaxLength(20);
            entity.Property(e => e.Conclusao).IsRequired().HasMaxLength(5000);
            entity.Property(e => e.Recomendacoes).HasMaxLength(3000);
            entity.Property(e => e.Observacoes).HasMaxLength(2000);
            entity.Property(e => e.DigitalSignature).HasMaxLength(10000);
            entity.Property(e => e.CertificateThumbprint).HasMaxLength(100);
            entity.Property(e => e.CertificateSubject).HasMaxLength(500);
            entity.Property(e => e.DocumentHash).HasMaxLength(100);
            entity.HasIndex(e => e.DocumentHash);
            entity.HasIndex(e => e.AppointmentId);

            entity.HasOne(e => e.Appointment)
                .WithMany()
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Professional)
                .WithMany()
                .HasForeignKey(e => e.ProfessionalId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Domain.Common.BaseEntity entity)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
