using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Helpers;

public static class TestDataSeeder
{
    public static async Task<User> CreateAdminUserAsync(IServiceProvider serviceProvider, string email = "admin@test.com")
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = new User
        {
            Email = email,
            PasswordHash = "$2a$11$K7TqZWL3dIZ.fP3nzSYzZ.wLX8HE5LCB2xvGm3v3Q6YQxNvK1ZS6y", // Password: Test@123
            Name = "Admin",
            LastName = "User",
            Cpf = "12345678909",
            Role = UserRole.ADMIN,
            Status = UserStatus.Active,
            EmailVerified = true
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        return user;
    }

    public static async Task<User> CreateProfessionalUserAsync(
        IServiceProvider serviceProvider, 
        string email = "professional@test.com",
        Guid? specialtyId = null)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = new User
        {
            Email = email,
            PasswordHash = "$2a$11$K7TqZWL3dIZ.fP3nzSYzZ.wLX8HE5LCB2xvGm3v3Q6YQxNvK1ZS6y", // Password: Test@123
            Name = "Dr. João",
            LastName = "Silva",
            Cpf = "52998224725",
            Role = UserRole.PROFESSIONAL,
            Status = UserStatus.Active,
            EmailVerified = true
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        // Criar perfil profissional
        var professionalProfile = new ProfessionalProfile
        {
            UserId = user.Id,
            SpecialtyId = specialtyId,
            Crm = "123456"
        };
        context.ProfessionalProfiles.Add(professionalProfile);
        await context.SaveChangesAsync();
        
        return user;
    }

    public static async Task<User> CreatePatientUserAsync(
        IServiceProvider serviceProvider, 
        string email = "patient@test.com",
        string cpf = "11144477735")
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = new User
        {
            Email = email,
            PasswordHash = "$2a$11$K7TqZWL3dIZ.fP3nzSYzZ.wLX8HE5LCB2xvGm3v3Q6YQxNvK1ZS6y", // Password: Test@123
            Name = "Maria",
            LastName = "Santos",
            Cpf = cpf,
            Role = UserRole.PATIENT,
            Status = UserStatus.Active,
            EmailVerified = true
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        // Criar perfil de paciente
        var patientProfile = new PatientProfile
        {
            UserId = user.Id,
            BirthDate = new DateTime(1990, 1, 15),
            Gender = "F"
        };
        context.PatientProfiles.Add(patientProfile);
        await context.SaveChangesAsync();
        
        return user;
    }

    public static async Task<Specialty> CreateSpecialtyAsync(
        IServiceProvider serviceProvider, 
        string name = "Cardiologia")
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var specialty = new Specialty
        {
            Name = name,
            Description = $"Especialidade de {name}",
            Status = SpecialtyStatus.Active
        };
        
        context.Specialties.Add(specialty);
        await context.SaveChangesAsync();
        
        return specialty;
    }

    public static async Task<Appointment> CreateAppointmentAsync(
        IServiceProvider serviceProvider,
        Guid patientId,
        Guid professionalId,
        Guid specialtyId,
        DateTime? date = null,
        AppointmentStatus status = AppointmentStatus.Scheduled)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var appointment = new Appointment
        {
            PatientId = patientId,
            ProfessionalId = professionalId,
            SpecialtyId = specialtyId,
            Date = date ?? DateTime.UtcNow.AddDays(1).Date,
            Time = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            Type = AppointmentType.Common,
            Status = status
        };
        
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();
        
        return appointment;
    }

    public static async Task<Schedule> CreateScheduleAsync(
        IServiceProvider serviceProvider,
        Guid professionalId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var schedule = new Schedule
        {
            ProfessionalId = professionalId,
            GlobalConfigJson = "{\"slotDuration\":30,\"breakTime\":10}",
            DaysConfigJson = "[{\"dayOfWeek\":1,\"startTime\":\"08:00\",\"endTime\":\"18:00\"}]",
            ValidityStartDate = DateTime.UtcNow.Date,
            IsActive = true
        };
        
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();
        
        return schedule;
    }

    public static async Task ClearDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        context.Appointments.RemoveRange(context.Appointments);
        context.Schedules.RemoveRange(context.Schedules);
        context.ScheduleBlocks.RemoveRange(context.ScheduleBlocks);
        context.AuditLogs.RemoveRange(context.AuditLogs);
        context.Notifications.RemoveRange(context.Notifications);
        context.Attachments.RemoveRange(context.Attachments);
        context.Invites.RemoveRange(context.Invites);
        context.ProfessionalProfiles.RemoveRange(context.ProfessionalProfiles);
        context.PatientProfiles.RemoveRange(context.PatientProfiles);
        context.Users.RemoveRange(context.Users);
        context.Specialties.RemoveRange(context.Specialties);
        
        await context.SaveChangesAsync();
    }
}
