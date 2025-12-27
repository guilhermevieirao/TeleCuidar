using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Entidade base de usuário com campos comuns a todos os tipos
/// Os campos específicos estão em PatientProfile e ProfessionalProfile
/// </summary>
public class User : BaseEntity
{
    // Dados de autenticação
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    
    // Dados básicos de identificação (comuns a todos)
    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    
    // Controle de acesso
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    
    // Verificação de email
    public bool EmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }
    
    // Mudança de email pendente
    public string? PendingEmail { get; set; }
    public string? PendingEmailToken { get; set; }
    public DateTime? PendingEmailTokenExpiry { get; set; }
    
    // Reset de senha
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    
    // Refresh Token para autenticação
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    
    // ============================================
    // Navigation Properties - Perfis específicos
    // ============================================
    public PatientProfile? PatientProfile { get; set; }
    public ProfessionalProfile? ProfessionalProfile { get; set; }
    
    // Navigation Properties - Relacionamentos existentes
    public ICollection<Appointment> AppointmentsAsPatient { get; set; } = new List<Appointment>();
    public ICollection<Appointment> AppointmentsAsProfessional { get; set; } = new List<Appointment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<ScheduleBlock> ScheduleBlocks { get; set; } = new List<ScheduleBlock>();
}
