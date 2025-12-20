using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid ProfessionalId { get; set; }
    public Guid SpecialtyId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public TimeSpan? EndTime { get; set; }
    public AppointmentType Type { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Observation { get; set; }
    public string? MeetLink { get; set; }
    public string? PreConsultationJson { get; set; } // Store PreConsultationForm as JSON
    public string? BiometricsJson { get; set; } // Store BiometricsData as JSON
    public string? AttachmentsChatJson { get; set; } // Store AttachmentMessage[] as JSON
    
    // Navigation Properties
    public User Patient { get; set; } = null!;
    public User Professional { get; set; } = null!;
    public Specialty Specialty { get; set; } = null!;
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
