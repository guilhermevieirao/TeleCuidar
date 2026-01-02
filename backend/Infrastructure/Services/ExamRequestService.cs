using Application.DTOs.ExamRequests;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;

namespace Infrastructure.Services;

public class ExamRequestService : IExamRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExamRequestService> _logger;

    public ExamRequestService(
        ApplicationDbContext context,
        ILogger<ExamRequestService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ExamRequestDto?> GetByIdAsync(Guid id)
    {
        var exam = await _context.ExamRequests
            .Include(e => e.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(e => e.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(e => e.Appointment)
            .FirstOrDefaultAsync(e => e.Id == id);

        return exam != null ? MapToDto(exam) : null;
    }

    public async Task<List<ExamRequestDto>> GetByAppointmentIdAsync(Guid appointmentId)
    {
        var exams = await _context.ExamRequests
            .Include(e => e.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(e => e.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(e => e.Appointment)
            .Where(e => e.AppointmentId == appointmentId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return exams.Select(MapToDto).ToList();
    }

    public async Task<List<ExamRequestDto>> GetByPatientIdAsync(Guid patientId)
    {
        var exams = await _context.ExamRequests
            .Include(e => e.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(e => e.Patient)
            .Include(e => e.Appointment)
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return exams.Select(MapToDto).ToList();
    }

    public async Task<List<ExamRequestDto>> GetByProfessionalIdAsync(Guid professionalId)
    {
        var exams = await _context.ExamRequests
            .Include(e => e.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(e => e.Patient)
            .Include(e => e.Appointment)
            .Where(e => e.ProfessionalId == professionalId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return exams.Select(MapToDto).ToList();
    }

    public async Task<ExamRequestDto> CreateAsync(CreateExamRequestDto dto, Guid professionalId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Professional)
            .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId);

        if (appointment == null)
            throw new InvalidOperationException("Consulta não encontrada.");

        var categoria = ParseCategoria(dto.Categoria);
        var prioridade = ParsePrioridade(dto.Prioridade);

        var exam = new ExamRequest
        {
            AppointmentId = dto.AppointmentId,
            ProfessionalId = professionalId,
            PatientId = appointment.PatientId,
            NomeExame = dto.NomeExame,
            CodigoExame = dto.CodigoExame,
            Categoria = categoria,
            Prioridade = prioridade,
            DataEmissao = DateTime.UtcNow,
            DataLimite = dto.DataLimite,
            IndicacaoClinica = dto.IndicacaoClinica,
            HipoteseDiagnostica = dto.HipoteseDiagnostica,
            Cid = dto.Cid,
            Observacoes = dto.Observacoes,
            InstrucoesPreparo = dto.InstrucoesPreparo
        };

        _context.ExamRequests.Add(exam);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(exam.Id) ?? throw new InvalidOperationException("Erro ao criar solicitação de exame.");
    }

    public async Task<ExamRequestDto?> UpdateAsync(Guid id, UpdateExamRequestDto dto)
    {
        var exam = await _context.ExamRequests.FindAsync(id);
        if (exam == null) return null;

        if (exam.IsSigned)
            throw new InvalidOperationException("Não é possível alterar uma solicitação de exame já assinada.");

        if (!string.IsNullOrEmpty(dto.NomeExame))
            exam.NomeExame = dto.NomeExame;
        
        if (dto.CodigoExame != null)
            exam.CodigoExame = dto.CodigoExame;
            
        if (!string.IsNullOrEmpty(dto.Categoria))
            exam.Categoria = ParseCategoria(dto.Categoria);
            
        if (!string.IsNullOrEmpty(dto.Prioridade))
            exam.Prioridade = ParsePrioridade(dto.Prioridade);
            
        if (dto.DataLimite.HasValue)
            exam.DataLimite = dto.DataLimite;
            
        if (dto.IndicacaoClinica != null)
            exam.IndicacaoClinica = dto.IndicacaoClinica;
            
        if (dto.HipoteseDiagnostica != null)
            exam.HipoteseDiagnostica = dto.HipoteseDiagnostica;
            
        if (dto.Cid != null)
            exam.Cid = dto.Cid;
            
        if (dto.Observacoes != null)
            exam.Observacoes = dto.Observacoes;
            
        if (dto.InstrucoesPreparo != null)
            exam.InstrucoesPreparo = dto.InstrucoesPreparo;

        exam.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var exam = await _context.ExamRequests.FindAsync(id);
        if (exam == null) return false;

        if (exam.IsSigned)
            throw new InvalidOperationException("Não é possível excluir uma solicitação de exame já assinada.");

        _context.ExamRequests.Remove(exam);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ExamRequestPdfDto> GeneratePdfAsync(Guid id)
    {
        var exam = await _context.ExamRequests
            .Include(e => e.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(e => e.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(e => e.Appointment)
                .ThenInclude(a => a.Specialty)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam == null)
            throw new InvalidOperationException("Solicitação de exame não encontrada.");

        var pdfBytes = GenerateExamPdf(exam);
        var pdfBase64 = Convert.ToBase64String(pdfBytes);
        var documentHash = GenerateDocumentHash(exam);

        var patientName = exam.Patient != null 
            ? $"{exam.Patient.Name} {exam.Patient.LastName}" 
            : "Paciente";
        
        return new ExamRequestPdfDto
        {
            PdfBase64 = pdfBase64,
            FileName = PdfDocumentHelper.GenerateFileName(
                "Exame",
                patientName,
                exam.CreatedAt,
                exam.IsSigned ? "Assinado" : null,
                exam.Categoria.ToString()),
            DocumentHash = documentHash,
            IsSigned = exam.IsSigned
        };
    }

    public async Task<bool> ValidateDocumentHashAsync(string documentHash)
    {
        var exam = await _context.ExamRequests
            .FirstOrDefaultAsync(e => e.DocumentHash == documentHash);

        return exam != null && exam.IsSigned;
    }

    private ExamRequestDto MapToDto(ExamRequest exam)
    {
        return new ExamRequestDto
        {
            Id = exam.Id,
            AppointmentId = exam.AppointmentId,
            ProfessionalId = exam.ProfessionalId,
            ProfessionalName = exam.Professional != null 
                ? $"{exam.Professional.Name} {exam.Professional.LastName}" 
                : null,
            ProfessionalCrm = exam.Professional?.ProfessionalProfile?.Crm,
            PatientId = exam.PatientId,
            PatientName = exam.Patient != null 
                ? $"{exam.Patient.Name} {exam.Patient.LastName}" 
                : null,
            PatientCpf = exam.Patient?.Cpf,
            NomeExame = exam.NomeExame,
            CodigoExame = exam.CodigoExame,
            Categoria = exam.Categoria.ToString().ToLowerInvariant(),
            Prioridade = exam.Prioridade.ToString().ToLowerInvariant(),
            DataEmissao = exam.DataEmissao,
            DataLimite = exam.DataLimite,
            IndicacaoClinica = exam.IndicacaoClinica,
            HipoteseDiagnostica = exam.HipoteseDiagnostica,
            Cid = exam.Cid,
            Observacoes = exam.Observacoes,
            InstrucoesPreparo = exam.InstrucoesPreparo,
            IsSigned = exam.IsSigned,
            CertificateSubject = exam.CertificateSubject,
            SignedAt = exam.SignedAt,
            DocumentHash = exam.DocumentHash,
            CreatedAt = exam.CreatedAt,
            UpdatedAt = exam.UpdatedAt
        };
    }

    private ExamCategory ParseCategoria(string categoria)
    {
        return categoria.ToLowerInvariant() switch
        {
            "laboratorial" => ExamCategory.Laboratorial,
            "imagem" => ExamCategory.Imagem,
            "cardiologico" => ExamCategory.Cardiologico,
            "oftalmologico" => ExamCategory.Oftalmologico,
            "audiometrico" => ExamCategory.Audiometrico,
            "neurologico" => ExamCategory.Neurologico,
            "endoscopico" => ExamCategory.Endoscopico,
            "outro" => ExamCategory.Outro,
            _ => ExamCategory.Outro
        };
    }

    private ExamPriority ParsePrioridade(string prioridade)
    {
        return prioridade.ToLowerInvariant() switch
        {
            "urgente" => ExamPriority.Urgente,
            "normal" => ExamPriority.Normal,
            _ => ExamPriority.Normal
        };
    }

    private string GetCategoriaLabel(ExamCategory categoria)
    {
        return categoria switch
        {
            ExamCategory.Laboratorial => "EXAME LABORATORIAL",
            ExamCategory.Imagem => "EXAME DE IMAGEM",
            ExamCategory.Cardiologico => "EXAME CARDIOLÓGICO",
            ExamCategory.Oftalmologico => "EXAME OFTALMOLÓGICO",
            ExamCategory.Audiometrico => "EXAME AUDIOMÉTRICO",
            ExamCategory.Neurologico => "EXAME NEUROLÓGICO",
            ExamCategory.Endoscopico => "EXAME ENDOSCÓPICO",
            ExamCategory.Outro => "SOLICITAÇÃO DE EXAME",
            _ => "SOLICITAÇÃO DE EXAME"
        };
    }

    private string GetPrioridadeLabel(ExamPriority prioridade)
    {
        return prioridade switch
        {
            ExamPriority.Urgente => "URGENTE",
            ExamPriority.Normal => "Normal",
            _ => "Normal"
        };
    }

    private string GenerateDocumentHash(ExamRequest exam)
    {
        var content = new StringBuilder();
        content.Append(exam.Id);
        content.Append(exam.AppointmentId);
        content.Append(exam.ProfessionalId);
        content.Append(exam.PatientId);
        content.Append(exam.NomeExame);
        content.Append(exam.Categoria);
        content.Append(exam.DataEmissao.ToString("O"));
        content.Append(exam.IndicacaoClinica);
        content.Append(exam.CreatedAt.ToString("O"));

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content.ToString()));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public byte[] GenerateExamPdf(ExamRequest exam, bool isSigned = false, string? certificateSubject = null, DateTime? signedAt = null)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new PdfWriter(memoryStream);
        using var pdfDoc = new PdfDocument(writer);
        using var document = new Document(pdfDoc);
        
        var professional = exam.Professional;
        var patient = exam.Patient;
        var professionalProfile = professional?.ProfessionalProfile;
        var patientProfile = patient?.PatientProfile;
        
        // Fontes padrão
        var boldFont = PdfDocumentHelper.GetBoldFont();
        var regularFont = PdfDocumentHelper.GetRegularFont();
        
        // === CABEÇALHO PADRÃO ===
        PdfDocumentHelper.AddHeader(document, boldFont, regularFont);
        
        // === TÍTULO DO DOCUMENTO ===
        var categoriaLabel = GetCategoriaLabel(exam.Categoria);
        PdfDocumentHelper.AddDocumentTitle(
            document,
            "SOLICITAÇÃO DE EXAME",
            categoriaLabel,
            exam.DataEmissao,
            boldFont,
            regularFont);
        
        // === BADGE DE PRIORIDADE (se urgente) ===
        if (exam.Prioridade == ExamPriority.Urgente)
        {
            var urgentTable = new Table(1).UseAllAvailableWidth().SetMarginBottom(15);
            var urgentCell = new Cell()
                .SetBorder(new iText.Layout.Borders.SolidBorder(new DeviceRgb(220, 38, 38), 2))
                .SetPadding(10)
                .SetBackgroundColor(new DeviceRgb(254, 242, 242))
                .SetTextAlignment(TextAlignment.CENTER);
            urgentCell.Add(new Paragraph("⚠️ EXAME URGENTE")
                .SetFont(boldFont)
                .SetFontSize(14)
                .SetFontColor(new DeviceRgb(220, 38, 38)));
            urgentTable.AddCell(urgentCell);
            document.Add(urgentTable);
        }
        
        // === DADOS DO PACIENTE E PROFISSIONAL ===
        PdfDocumentHelper.AddPatientAndProfessionalInfo(
            document,
            boldFont,
            regularFont,
            patientName: $"{patient?.Name} {patient?.LastName}",
            patientCpf: patient?.Cpf,
            patientCns: patientProfile?.Cns,
            patientBirthDate: patientProfile?.BirthDate,
            professionalName: $"{professional?.Name} {professional?.LastName}",
            professionalCrm: professionalProfile?.Crm,
            professionalUf: professionalProfile?.State,
            professionalEmail: professional?.Email,
            professionalPhone: professional?.Phone);
        
        // === EXAME SOLICITADO ===
        document.Add(new Paragraph("EXAME SOLICITADO")
            .SetFont(boldFont)
            .SetFontSize(12)
            .SetFontColor(PdfDocumentHelper.PrimaryColor)
            .SetMarginBottom(10));
        
        var examTable = new Table(1).UseAllAvailableWidth().SetMarginBottom(15);
        var examCell = new Cell()
            .SetBorder(new iText.Layout.Borders.SolidBorder(PdfDocumentHelper.GrayColor, 1))
            .SetPadding(15)
            .SetBackgroundColor(PdfDocumentHelper.LightGray);
        
        examCell.Add(new Paragraph(exam.NomeExame)
            .SetFont(boldFont)
            .SetFontSize(14)
            .SetFontColor(PdfDocumentHelper.DarkText));
        
        if (!string.IsNullOrEmpty(exam.CodigoExame))
        {
            examCell.Add(new Paragraph($"Código: {exam.CodigoExame}")
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetFontColor(PdfDocumentHelper.GrayColor));
        }
        
        examTable.AddCell(examCell);
        document.Add(examTable);
        
        // === INDICAÇÃO CLÍNICA ===
        document.Add(new Paragraph("INDICAÇÃO CLÍNICA / JUSTIFICATIVA")
            .SetFont(boldFont)
            .SetFontSize(12)
            .SetFontColor(PdfDocumentHelper.PrimaryColor)
            .SetMarginBottom(10));
        
        var indicacaoTable = new Table(1).UseAllAvailableWidth().SetMarginBottom(15);
        var indicacaoCell = new Cell()
            .SetBorder(new iText.Layout.Borders.SolidBorder(PdfDocumentHelper.GrayColor, 1))
            .SetPadding(15)
            .SetBackgroundColor(PdfDocumentHelper.LightGray);
        indicacaoCell.Add(new Paragraph(exam.IndicacaoClinica)
            .SetFont(regularFont)
            .SetFontSize(11)
            .SetFontColor(PdfDocumentHelper.DarkText)
            .SetTextAlignment(TextAlignment.JUSTIFIED));
        indicacaoTable.AddCell(indicacaoCell);
        document.Add(indicacaoTable);
        
        // === INFORMAÇÕES ADICIONAIS ===
        var hasAdditionalInfo = !string.IsNullOrEmpty(exam.HipoteseDiagnostica) || 
                                !string.IsNullOrEmpty(exam.Cid) || 
                                exam.DataLimite.HasValue;
        
        if (hasAdditionalInfo)
        {
            var infoTable = new Table(3).UseAllAvailableWidth().SetMarginBottom(15);
            
            // Hipótese diagnóstica
            if (!string.IsNullOrEmpty(exam.HipoteseDiagnostica))
            {
                var hipoteseCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(8);
                hipoteseCell.Add(new Paragraph("Hipótese Diagnóstica")
                    .SetFont(regularFont)
                    .SetFontSize(9)
                    .SetFontColor(PdfDocumentHelper.GrayColor));
                hipoteseCell.Add(new Paragraph(exam.HipoteseDiagnostica)
                    .SetFont(boldFont)
                    .SetFontSize(11)
                    .SetFontColor(PdfDocumentHelper.DarkText));
                infoTable.AddCell(hipoteseCell);
            }
            
            // CID
            if (!string.IsNullOrEmpty(exam.Cid))
            {
                var cidCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(8);
                cidCell.Add(new Paragraph("CID")
                    .SetFont(regularFont)
                    .SetFontSize(9)
                    .SetFontColor(PdfDocumentHelper.GrayColor));
                cidCell.Add(new Paragraph(exam.Cid)
                    .SetFont(boldFont)
                    .SetFontSize(12)
                    .SetFontColor(PdfDocumentHelper.DarkText));
                infoTable.AddCell(cidCell);
            }
            
            // Data limite
            if (exam.DataLimite.HasValue)
            {
                var limiteCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(8);
                limiteCell.Add(new Paragraph("Realizar até")
                    .SetFont(regularFont)
                    .SetFontSize(9)
                    .SetFontColor(PdfDocumentHelper.GrayColor));
                limiteCell.Add(new Paragraph(exam.DataLimite.Value.ToString("dd/MM/yyyy"))
                    .SetFont(boldFont)
                    .SetFontSize(12)
                    .SetFontColor(PdfDocumentHelper.DarkText));
                infoTable.AddCell(limiteCell);
            }
            
            document.Add(infoTable);
        }
        
        // === INSTRUÇÕES DE PREPARO ===
        if (!string.IsNullOrEmpty(exam.InstrucoesPreparo))
        {
            document.Add(new Paragraph("INSTRUÇÕES DE PREPARO")
                .SetFont(boldFont)
                .SetFontSize(12)
                .SetFontColor(PdfDocumentHelper.PrimaryColor)
                .SetMarginBottom(10));
            
            var preparoTable = new Table(1).UseAllAvailableWidth().SetMarginBottom(15);
            var preparoCell = new Cell()
                .SetBorder(new iText.Layout.Borders.SolidBorder(new DeviceRgb(234, 179, 8), 1))
                .SetPadding(15)
                .SetBackgroundColor(new DeviceRgb(254, 252, 232));
            preparoCell.Add(new Paragraph(exam.InstrucoesPreparo)
                .SetFont(regularFont)
                .SetFontSize(11)
                .SetFontColor(PdfDocumentHelper.DarkText));
            preparoTable.AddCell(preparoCell);
            document.Add(preparoTable);
        }
        
        // === OBSERVAÇÕES ===
        if (!string.IsNullOrEmpty(exam.Observacoes))
        {
            document.Add(new Paragraph("OBSERVAÇÕES")
                .SetFont(boldFont)
                .SetFontSize(10)
                .SetFontColor(PdfDocumentHelper.GrayColor)
                .SetMarginBottom(5));
            document.Add(new Paragraph(exam.Observacoes)
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetFontColor(PdfDocumentHelper.GrayColor)
                .SetItalic()
                .SetMarginBottom(15));
        }
        
        // === SEÇÃO DE ASSINATURAS (física + digital se aplicável) ===
        var professionalName = professional != null ? $"Dr(a). {professional.Name} {professional.LastName}" : "N/A";
        var crm = professionalProfile?.Crm ?? "N/A";
        var uf = professionalProfile?.State ?? "";
        
        // Usar parâmetros passados ou valores do objeto exam
        var actualCertificateSubject = certificateSubject ?? exam.CertificateSubject;
        var actualSignedAt = signedAt ?? exam.SignedAt;
        var hasSignature = (isSigned || exam.IsSigned) && (actualSignedAt.HasValue || exam.SignedAt.HasValue);
        
        PdfDocumentHelper.AddSignatureSection(
            document,
            boldFont,
            regularFont,
            professionalName,
            crm,
            uf,
            hasSignature,
            actualCertificateSubject,
            actualSignedAt ?? exam.SignedAt,
            exam.CertificateThumbprint);
        
        // === RODAPÉ COM QR CODE E VALIDAÇÃO ===
        var documentHash = GenerateDocumentHash(exam);
        PdfDocumentHelper.AddFooter(document, boldFont, regularFont, documentHash, DateTime.UtcNow);
        
        document.Close();
        
        // Adicionar números de página (pós-processamento)
        var pdfWithoutPages = memoryStream.ToArray();
        return PdfDocumentHelper.AddPageNumbersToBytes(pdfWithoutPages);
    }
}
