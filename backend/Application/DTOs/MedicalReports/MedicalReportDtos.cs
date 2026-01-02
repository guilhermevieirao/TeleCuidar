namespace Application.DTOs.MedicalReports;

/// <summary>
/// DTO para retorno de laudo médico
/// </summary>
public class MedicalReportDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid ProfessionalId { get; set; }
    public Guid PatientId { get; set; }
    
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public DateTime DataEmissao { get; set; }
    
    public string? HistoricoClinico { get; set; }
    public string? ExameFisico { get; set; }
    public string? ExamesComplementares { get; set; }
    public string? HipoteseDiagnostica { get; set; }
    public string? Cid { get; set; }
    public string Conclusao { get; set; } = string.Empty;
    public string? Recomendacoes { get; set; }
    public string? Observacoes { get; set; }
    
    // Dados do profissional
    public string? ProfessionalName { get; set; }
    public string? ProfessionalCrm { get; set; }
    public string? ProfessionalUf { get; set; }
    public string? ProfessionalSpecialty { get; set; }
    
    // Dados do paciente
    public string? PatientName { get; set; }
    public string? PatientCpf { get; set; }
    public string? PatientCns { get; set; }
    public DateTime? PatientBirthDate { get; set; }
    
    // Assinatura digital
    public bool IsSigned { get; set; }
    public string? CertificateSubject { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? DocumentHash { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO para criação de laudo médico
/// </summary>
public class CreateMedicalReportDto
{
    public Guid AppointmentId { get; set; }
    public string Tipo { get; set; } = "Exame";
    public string Titulo { get; set; } = string.Empty;
    public DateTime? DataEmissao { get; set; }
    
    public string? HistoricoClinico { get; set; }
    public string? ExameFisico { get; set; }
    public string? ExamesComplementares { get; set; }
    public string? HipoteseDiagnostica { get; set; }
    public string? Cid { get; set; }
    public string Conclusao { get; set; } = string.Empty;
    public string? Recomendacoes { get; set; }
    public string? Observacoes { get; set; }
}

/// <summary>
/// DTO para atualização de laudo médico
/// </summary>
public class UpdateMedicalReportDto
{
    public string? Tipo { get; set; }
    public string? Titulo { get; set; }
    public DateTime? DataEmissao { get; set; }
    
    public string? HistoricoClinico { get; set; }
    public string? ExameFisico { get; set; }
    public string? ExamesComplementares { get; set; }
    public string? HipoteseDiagnostica { get; set; }
    public string? Cid { get; set; }
    public string? Conclusao { get; set; }
    public string? Recomendacoes { get; set; }
    public string? Observacoes { get; set; }
}

/// <summary>
/// DTO para geração de PDF do laudo
/// </summary>
public class MedicalReportPdfDto
{
    public byte[] PdfBytes { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
}
