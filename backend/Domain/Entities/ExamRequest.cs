using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Tipo/categoria de exame
/// </summary>
public enum ExamCategory
{
    Laboratorial,
    Imagem,
    Cardiologico,
    Oftalmologico,
    Audiometrico,
    Neurologico,
    Endoscopico,
    Outro
}

/// <summary>
/// Prioridade do exame
/// </summary>
public enum ExamPriority
{
    Normal,
    Urgente
}

/// <summary>
/// Representa uma solicitação de exame médico emitida durante uma consulta
/// </summary>
public class ExamRequest : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid ProfessionalId { get; set; }
    public Guid PatientId { get; set; }
    
    /// <summary>
    /// Nome do exame solicitado
    /// </summary>
    public string NomeExame { get; set; } = string.Empty;
    
    /// <summary>
    /// Código do exame (TUSS, SIGTAP, etc.)
    /// </summary>
    public string? CodigoExame { get; set; }
    
    /// <summary>
    /// Categoria do exame
    /// </summary>
    public ExamCategory Categoria { get; set; } = ExamCategory.Laboratorial;
    
    /// <summary>
    /// Prioridade do exame
    /// </summary>
    public ExamPriority Prioridade { get; set; } = ExamPriority.Normal;
    
    /// <summary>
    /// Data de emissão da solicitação
    /// </summary>
    public DateTime DataEmissao { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data limite para realização do exame
    /// </summary>
    public DateTime? DataLimite { get; set; }
    
    /// <summary>
    /// Indicação clínica / justificativa
    /// </summary>
    public string IndicacaoClinica { get; set; } = string.Empty;
    
    /// <summary>
    /// Hipótese diagnóstica
    /// </summary>
    public string? HipoteseDiagnostica { get; set; }
    
    /// <summary>
    /// Código CID
    /// </summary>
    public string? Cid { get; set; }
    
    /// <summary>
    /// Observações adicionais
    /// </summary>
    public string? Observacoes { get; set; }
    
    /// <summary>
    /// Instruções de preparo para o paciente
    /// </summary>
    public string? InstrucoesPreparo { get; set; }
    
    /// <summary>
    /// Assinatura digital em Base64 (ICP-Brasil)
    /// </summary>
    public string? DigitalSignature { get; set; }
    
    /// <summary>
    /// Thumbprint do certificado usado para assinatura
    /// </summary>
    public string? CertificateThumbprint { get; set; }
    
    /// <summary>
    /// Subject (CN) do certificado
    /// </summary>
    public string? CertificateSubject { get; set; }
    
    /// <summary>
    /// Data/hora da assinatura
    /// </summary>
    public DateTime? SignedAt { get; set; }
    
    /// <summary>
    /// Hash único do documento para validação
    /// </summary>
    public string? DocumentHash { get; set; }
    
    /// <summary>
    /// PDF assinado armazenado em Base64
    /// </summary>
    public string? SignedPdfBase64 { get; set; }
    
    // Navigation properties
    public Appointment Appointment { get; set; } = null!;
    public User Professional { get; set; } = null!;
    public User Patient { get; set; } = null!;
    
    /// <summary>
    /// Indica se o documento está assinado digitalmente
    /// </summary>
    public bool IsSigned => !string.IsNullOrEmpty(DigitalSignature);
}
