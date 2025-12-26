using Application.DTOs.MedicalCertificates;

namespace Application.Interfaces;

public interface IMedicalCertificateService
{
    /// <summary>
    /// Obtém um atestado por ID
    /// </summary>
    Task<MedicalCertificateDto?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Obtém todos os atestados de uma consulta
    /// </summary>
    Task<List<MedicalCertificateDto>> GetByAppointmentIdAsync(Guid appointmentId);
    
    /// <summary>
    /// Obtém todos os atestados de um paciente
    /// </summary>
    Task<List<MedicalCertificateDto>> GetByPatientIdAsync(Guid patientId);
    
    /// <summary>
    /// Obtém todos os atestados emitidos por um profissional
    /// </summary>
    Task<List<MedicalCertificateDto>> GetByProfessionalIdAsync(Guid professionalId);
    
    /// <summary>
    /// Cria um novo atestado
    /// </summary>
    Task<MedicalCertificateDto> CreateAsync(CreateMedicalCertificateDto dto, Guid professionalId);
    
    /// <summary>
    /// Atualiza um atestado (apenas se não estiver assinado)
    /// </summary>
    Task<MedicalCertificateDto?> UpdateAsync(Guid id, UpdateMedicalCertificateDto dto);
    
    /// <summary>
    /// Exclui um atestado (apenas se não estiver assinado)
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
    
    /// <summary>
    /// Gera PDF do atestado (sem assinatura)
    /// </summary>
    Task<MedicalCertificatePdfDto> GeneratePdfAsync(Guid id);
    
    /// <summary>
    /// Assina o atestado com certificado salvo na plataforma
    /// </summary>
    Task<MedicalCertificateDto?> SignWithSavedCertificateAsync(Guid id, Guid savedCertificateId, string? password);
    
    /// <summary>
    /// Assina o atestado com arquivo PFX enviado
    /// </summary>
    Task<MedicalCertificateDto?> SignWithPfxAsync(Guid id, byte[] pfxBytes, string password);
    
    /// <summary>
    /// Valida hash do documento
    /// </summary>
    Task<bool> ValidateDocumentHashAsync(string documentHash);
}
