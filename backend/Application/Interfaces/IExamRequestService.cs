using Application.DTOs.ExamRequests;

namespace Application.Interfaces;

public interface IExamRequestService
{
    /// <summary>
    /// Obtém uma solicitação de exame por ID
    /// </summary>
    Task<ExamRequestDto?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Obtém todas as solicitações de exame de uma consulta
    /// </summary>
    Task<List<ExamRequestDto>> GetByAppointmentIdAsync(Guid appointmentId);
    
    /// <summary>
    /// Obtém todas as solicitações de exame de um paciente
    /// </summary>
    Task<List<ExamRequestDto>> GetByPatientIdAsync(Guid patientId);
    
    /// <summary>
    /// Obtém todas as solicitações de exame emitidas por um profissional
    /// </summary>
    Task<List<ExamRequestDto>> GetByProfessionalIdAsync(Guid professionalId);
    
    /// <summary>
    /// Cria uma nova solicitação de exame
    /// </summary>
    Task<ExamRequestDto> CreateAsync(CreateExamRequestDto dto, Guid professionalId);
    
    /// <summary>
    /// Atualiza uma solicitação de exame (apenas se não estiver assinada)
    /// </summary>
    Task<ExamRequestDto?> UpdateAsync(Guid id, UpdateExamRequestDto dto);
    
    /// <summary>
    /// Exclui uma solicitação de exame (apenas se não estiver assinada)
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
    
    /// <summary>
    /// Gera PDF da solicitação de exame (sem assinatura)
    /// </summary>
    Task<ExamRequestPdfDto> GeneratePdfAsync(Guid id);
    
    /// <summary>
    /// Valida hash do documento
    /// </summary>
    Task<bool> ValidateDocumentHashAsync(string documentHash);
}
