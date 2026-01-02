namespace Application.DTOs.ExamRequests;

/// <summary>
/// DTO para retorno de solicitação de exame
/// </summary>
public class ExamRequestDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid ProfessionalId { get; set; }
    public string? ProfessionalName { get; set; }
    public string? ProfessionalCrm { get; set; }
    public Guid PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientCpf { get; set; }
    public string NomeExame { get; set; } = string.Empty;
    public string? CodigoExame { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public DateTime DataEmissao { get; set; }
    public DateTime? DataLimite { get; set; }
    public string IndicacaoClinica { get; set; } = string.Empty;
    public string? HipoteseDiagnostica { get; set; }
    public string? Cid { get; set; }
    public string? Observacoes { get; set; }
    public string? InstrucoesPreparo { get; set; }
    public bool IsSigned { get; set; }
    public string? CertificateSubject { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? DocumentHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO para criação de solicitação de exame
/// </summary>
public class CreateExamRequestDto
{
    public Guid AppointmentId { get; set; }
    public string NomeExame { get; set; } = string.Empty;
    public string? CodigoExame { get; set; }
    public string Categoria { get; set; } = "laboratorial";
    public string Prioridade { get; set; } = "normal";
    public DateTime? DataLimite { get; set; }
    public string IndicacaoClinica { get; set; } = string.Empty;
    public string? HipoteseDiagnostica { get; set; }
    public string? Cid { get; set; }
    public string? Observacoes { get; set; }
    public string? InstrucoesPreparo { get; set; }
}

/// <summary>
/// DTO para atualização de solicitação de exame
/// </summary>
public class UpdateExamRequestDto
{
    public string? NomeExame { get; set; }
    public string? CodigoExame { get; set; }
    public string? Categoria { get; set; }
    public string? Prioridade { get; set; }
    public DateTime? DataLimite { get; set; }
    public string? IndicacaoClinica { get; set; }
    public string? HipoteseDiagnostica { get; set; }
    public string? Cid { get; set; }
    public string? Observacoes { get; set; }
    public string? InstrucoesPreparo { get; set; }
}

/// <summary>
/// DTO para PDF gerado
/// </summary>
public class ExamRequestPdfDto
{
    public string PdfBase64 { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DocumentHash { get; set; } = string.Empty;
    public bool IsSigned { get; set; }
}
