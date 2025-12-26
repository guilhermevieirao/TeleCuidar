namespace Application.DTOs.MedicalCertificates;

/// <summary>
/// DTO para retorno de atestado médico
/// </summary>
public class MedicalCertificateDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid ProfessionalId { get; set; }
    public string? ProfessionalName { get; set; }
    public string? ProfessionalCrm { get; set; }
    public Guid PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientCpf { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public DateTime DataEmissao { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public int? DiasAfastamento { get; set; }
    public string? Cid { get; set; }
    public string Conteudo { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
    public bool IsSigned { get; set; }
    public string? CertificateSubject { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? DocumentHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO para criação de atestado médico
/// </summary>
public class CreateMedicalCertificateDto
{
    public Guid AppointmentId { get; set; }
    public string Tipo { get; set; } = "comparecimento";
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public int? DiasAfastamento { get; set; }
    public string? Cid { get; set; }
    public string Conteudo { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
}

/// <summary>
/// DTO para atualização de atestado médico
/// </summary>
public class UpdateMedicalCertificateDto
{
    public string? Tipo { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public int? DiasAfastamento { get; set; }
    public string? Cid { get; set; }
    public string? Conteudo { get; set; }
    public string? Observacoes { get; set; }
}

/// <summary>
/// DTO para assinatura com certificado salvo
/// </summary>
public class SignMedicalCertificateDto
{
    public Guid SavedCertificateId { get; set; }
    public string? Password { get; set; }
}

/// <summary>
/// DTO para assinatura com arquivo PFX enviado como Base64
/// </summary>
public class SignMedicalCertificateWithPfxDto
{
    public string PfxBase64 { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO para PDF gerado
/// </summary>
public class MedicalCertificatePdfDto
{
    public string PdfBase64 { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DocumentHash { get; set; } = string.Empty;
    public bool IsSigned { get; set; }
}
