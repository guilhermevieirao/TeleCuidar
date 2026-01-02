using Application.DTOs.MedicalReports;
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

public class MedicalReportService : IMedicalReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicalReportService> _logger;

    public MedicalReportService(
        ApplicationDbContext context,
        ILogger<MedicalReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MedicalReportDto?> GetByIdAsync(Guid id)
    {
        var report = await _context.MedicalReports
            .Include(r => r.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
                    .ThenInclude(pp => pp!.Specialty)
            .Include(r => r.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.Id == id);

        return report != null ? MapToDto(report) : null;
    }

    public async Task<List<MedicalReportDto>> GetByAppointmentIdAsync(Guid appointmentId)
    {
        var reports = await _context.MedicalReports
            .Include(r => r.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
                    .ThenInclude(pp => pp!.Specialty)
            .Include(r => r.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(r => r.Appointment)
            .Where(r => r.AppointmentId == appointmentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reports.Select(MapToDto).ToList();
    }

    public async Task<List<MedicalReportDto>> GetByPatientIdAsync(Guid patientId)
    {
        var reports = await _context.MedicalReports
            .Include(r => r.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(r => r.Patient)
            .Include(r => r.Appointment)
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reports.Select(MapToDto).ToList();
    }

    public async Task<List<MedicalReportDto>> GetByProfessionalIdAsync(Guid professionalId)
    {
        var reports = await _context.MedicalReports
            .Include(r => r.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(r => r.Patient)
            .Include(r => r.Appointment)
            .Where(r => r.ProfessionalId == professionalId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reports.Select(MapToDto).ToList();
    }

    public async Task<MedicalReportDto> CreateAsync(CreateMedicalReportDto dto, Guid professionalId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Professional)
            .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId);

        if (appointment == null)
            throw new InvalidOperationException("Consulta não encontrada.");

        var tipo = ParseTipo(dto.Tipo);

        var report = new MedicalReport
        {
            Id = Guid.NewGuid(),
            AppointmentId = dto.AppointmentId,
            ProfessionalId = professionalId,
            PatientId = appointment.PatientId,
            Tipo = tipo,
            Titulo = dto.Titulo,
            DataEmissao = dto.DataEmissao ?? DateTime.UtcNow,
            HistoricoClinico = dto.HistoricoClinico,
            ExameFisico = dto.ExameFisico,
            ExamesComplementares = dto.ExamesComplementares,
            HipoteseDiagnostica = dto.HipoteseDiagnostica,
            Cid = dto.Cid,
            Conclusao = dto.Conclusao,
            Recomendacoes = dto.Recomendacoes,
            Observacoes = dto.Observacoes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Gerar hash único do documento
        report.DocumentHash = GenerateDocumentHash(report);

        _context.MedicalReports.Add(report);
        await _context.SaveChangesAsync();

        // Recarregar com includes
        return await GetByIdAsync(report.Id) ?? MapToDto(report);
    }

    public async Task<MedicalReportDto?> UpdateAsync(Guid id, UpdateMedicalReportDto dto, Guid professionalId)
    {
        var report = await _context.MedicalReports
            .Include(r => r.Professional)
            .FirstOrDefaultAsync(r => r.Id == id && r.ProfessionalId == professionalId);

        if (report == null)
            return null;

        // Não permitir edição de laudos já assinados
        if (report.IsSigned)
            throw new InvalidOperationException("Não é possível editar um laudo já assinado digitalmente.");

        if (dto.Tipo != null)
            report.Tipo = ParseTipo(dto.Tipo);
        if (dto.Titulo != null)
            report.Titulo = dto.Titulo;
        if (dto.DataEmissao.HasValue)
            report.DataEmissao = dto.DataEmissao.Value;
        if (dto.HistoricoClinico != null)
            report.HistoricoClinico = dto.HistoricoClinico;
        if (dto.ExameFisico != null)
            report.ExameFisico = dto.ExameFisico;
        if (dto.ExamesComplementares != null)
            report.ExamesComplementares = dto.ExamesComplementares;
        if (dto.HipoteseDiagnostica != null)
            report.HipoteseDiagnostica = dto.HipoteseDiagnostica;
        if (dto.Cid != null)
            report.Cid = dto.Cid;
        if (dto.Conclusao != null)
            report.Conclusao = dto.Conclusao;
        if (dto.Recomendacoes != null)
            report.Recomendacoes = dto.Recomendacoes;
        if (dto.Observacoes != null)
            report.Observacoes = dto.Observacoes;

        report.UpdatedAt = DateTime.UtcNow;
        report.DocumentHash = GenerateDocumentHash(report);

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid professionalId)
    {
        var report = await _context.MedicalReports
            .FirstOrDefaultAsync(r => r.Id == id && r.ProfessionalId == professionalId);

        if (report == null)
            return false;

        // Não permitir exclusão de laudos já assinados
        if (report.IsSigned)
            throw new InvalidOperationException("Não é possível excluir um laudo já assinado digitalmente.");

        _context.MedicalReports.Remove(report);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<MedicalReportPdfDto> GeneratePdfAsync(Guid id)
    {
        var report = await _context.MedicalReports
            .Include(r => r.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
                    .ThenInclude(pp => pp!.Specialty)
            .Include(r => r.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null)
            throw new InvalidOperationException("Laudo não encontrado.");

        var pdfBytes = GenerateReportPdf(report);
        var tipoLabel = GetTipoLabel(report.Tipo);

        return new MedicalReportPdfDto
        {
            PdfBytes = pdfBytes,
            FileName = PdfDocumentHelper.GenerateFileName(
                "Laudo",
                $"{report.Patient?.Name}_{report.Patient?.LastName}",
                report.DataEmissao,
                report.IsSigned ? "Assinado" : null,
                tipoLabel),
            ContentType = "application/pdf"
        };
    }

    public async Task<MedicalReportDto?> ValidateByHashAsync(string hash)
    {
        var report = await _context.MedicalReports
            .Include(r => r.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(r => r.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.DocumentHash == hash);

        return report != null ? MapToDto(report) : null;
    }

    private MedicalReportDto MapToDto(MedicalReport report)
    {
        var professional = report.Professional;
        var patient = report.Patient;
        var professionalProfile = professional?.ProfessionalProfile;
        var patientProfile = patient?.PatientProfile;

        return new MedicalReportDto
        {
            Id = report.Id,
            AppointmentId = report.AppointmentId,
            ProfessionalId = report.ProfessionalId,
            PatientId = report.PatientId,
            Tipo = report.Tipo.ToString(),
            Titulo = report.Titulo,
            DataEmissao = report.DataEmissao,
            HistoricoClinico = report.HistoricoClinico,
            ExameFisico = report.ExameFisico,
            ExamesComplementares = report.ExamesComplementares,
            HipoteseDiagnostica = report.HipoteseDiagnostica,
            Cid = report.Cid,
            Conclusao = report.Conclusao,
            Recomendacoes = report.Recomendacoes,
            Observacoes = report.Observacoes,
            ProfessionalName = professional != null ? $"{professional.Name} {professional.LastName}" : null,
            ProfessionalCrm = professionalProfile?.Crm,
            ProfessionalUf = professionalProfile?.State,
            ProfessionalSpecialty = professionalProfile?.Specialty?.Name,
            PatientName = patient != null ? $"{patient.Name} {patient.LastName}" : null,
            PatientCpf = patient?.Cpf,
            PatientCns = patientProfile?.Cns,
            PatientBirthDate = patientProfile?.BirthDate,
            IsSigned = report.IsSigned,
            CertificateSubject = report.CertificateSubject,
            SignedAt = report.SignedAt,
            DocumentHash = report.DocumentHash,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    private MedicalReportType ParseTipo(string tipo)
    {
        return tipo.ToLowerInvariant() switch
        {
            "exame" => MedicalReportType.Exame,
            "procedimento" => MedicalReportType.Procedimento,
            "pericial" => MedicalReportType.Pericial,
            "avaliacao" or "avaliação" => MedicalReportType.Avaliacao,
            "parecertecnico" or "parecer_tecnico" or "parecer técnico" => MedicalReportType.ParecerTecnico,
            "complementar" => MedicalReportType.Complementar,
            _ => MedicalReportType.Outro
        };
    }

    private string GetTipoLabel(MedicalReportType tipo)
    {
        return tipo switch
        {
            MedicalReportType.Exame => "Exame",
            MedicalReportType.Procedimento => "Procedimento",
            MedicalReportType.Pericial => "Pericial",
            MedicalReportType.Avaliacao => "Avaliação",
            MedicalReportType.ParecerTecnico => "Parecer Técnico",
            MedicalReportType.Complementar => "Complementar",
            _ => "Outro"
        };
    }

    private string GenerateDocumentHash(MedicalReport report)
    {
        var content = new StringBuilder();
        content.Append(report.Id);
        content.Append(report.AppointmentId);
        content.Append(report.ProfessionalId);
        content.Append(report.PatientId);
        content.Append(report.Tipo);
        content.Append(report.Titulo);
        content.Append(report.Conclusao);
        content.Append(report.DataEmissao.ToString("yyyyMMddHHmmss"));

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content.ToString()));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public byte[] GenerateReportPdf(MedicalReport report, bool isSigned = false, string? certificateSubject = null, DateTime? signedAt = null)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new PdfWriter(memoryStream);
        using var pdfDoc = new PdfDocument(writer);
        using var document = new Document(pdfDoc);
        
        var professional = report.Professional;
        var patient = report.Patient;
        var professionalProfile = professional?.ProfessionalProfile;
        var patientProfile = patient?.PatientProfile;
        
        // Fontes padrão
        var boldFont = PdfDocumentHelper.GetBoldFont();
        var regularFont = PdfDocumentHelper.GetRegularFont();
        
        // === CABEÇALHO PADRÃO ===
        PdfDocumentHelper.AddHeader(document, boldFont, regularFont);
        
        // === TÍTULO DO DOCUMENTO ===
        var tipoLabel = GetTipoLabel(report.Tipo);
        PdfDocumentHelper.AddDocumentTitle(
            document,
            "LAUDO MÉDICO",
            $"Laudo de {tipoLabel}",
            report.DataEmissao,
            boldFont,
            regularFont);
        
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
        
        // === TÍTULO DO LAUDO ===
        if (!string.IsNullOrEmpty(report.Titulo))
        {
            document.Add(new Paragraph(report.Titulo.ToUpper())
                .SetFont(boldFont)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(PdfDocumentHelper.PrimaryColor)
                .SetMarginBottom(15));
        }
        
        // === HISTÓRICO CLÍNICO ===
        if (!string.IsNullOrEmpty(report.HistoricoClinico))
        {
            AddSection(document, boldFont, regularFont, "HISTÓRICO CLÍNICO", report.HistoricoClinico);
        }
        
        // === EXAME FÍSICO ===
        if (!string.IsNullOrEmpty(report.ExameFisico))
        {
            AddSection(document, boldFont, regularFont, "EXAME FÍSICO", report.ExameFisico);
        }
        
        // === EXAMES COMPLEMENTARES ===
        if (!string.IsNullOrEmpty(report.ExamesComplementares))
        {
            AddSection(document, boldFont, regularFont, "EXAMES COMPLEMENTARES", report.ExamesComplementares);
        }
        
        // === HIPÓTESE DIAGNÓSTICA E CID ===
        if (!string.IsNullOrEmpty(report.HipoteseDiagnostica) || !string.IsNullOrEmpty(report.Cid))
        {
            document.Add(new Paragraph("DIAGNÓSTICO")
                .SetFont(boldFont)
                .SetFontSize(12)
                .SetFontColor(PdfDocumentHelper.PrimaryColor)
                .SetMarginBottom(10));
            
            var diagTable = new Table(2).UseAllAvailableWidth().SetMarginBottom(15);
            
            if (!string.IsNullOrEmpty(report.HipoteseDiagnostica))
            {
                var hipoteseCell = new Cell()
                    .SetBorder(new iText.Layout.Borders.SolidBorder(PdfDocumentHelper.GrayColor, 1))
                    .SetPadding(10)
                    .SetBackgroundColor(PdfDocumentHelper.LightGray);
                hipoteseCell.Add(new Paragraph("Hipótese Diagnóstica")
                    .SetFont(regularFont)
                    .SetFontSize(9)
                    .SetFontColor(PdfDocumentHelper.GrayColor));
                hipoteseCell.Add(new Paragraph(report.HipoteseDiagnostica)
                    .SetFont(boldFont)
                    .SetFontSize(11)
                    .SetFontColor(PdfDocumentHelper.DarkText));
                diagTable.AddCell(hipoteseCell);
            }
            
            if (!string.IsNullOrEmpty(report.Cid))
            {
                var cidCell = new Cell()
                    .SetBorder(new iText.Layout.Borders.SolidBorder(PdfDocumentHelper.GrayColor, 1))
                    .SetPadding(10)
                    .SetBackgroundColor(PdfDocumentHelper.LightGray);
                cidCell.Add(new Paragraph("CID-10")
                    .SetFont(regularFont)
                    .SetFontSize(9)
                    .SetFontColor(PdfDocumentHelper.GrayColor));
                cidCell.Add(new Paragraph(report.Cid)
                    .SetFont(boldFont)
                    .SetFontSize(11)
                    .SetFontColor(PdfDocumentHelper.DarkText));
                diagTable.AddCell(cidCell);
            }
            
            // Preencher células vazias se necessário
            int cellCount = 0;
            if (!string.IsNullOrEmpty(report.HipoteseDiagnostica)) cellCount++;
            if (!string.IsNullOrEmpty(report.Cid)) cellCount++;
            if (cellCount == 1)
            {
                diagTable.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            }
            
            document.Add(diagTable);
        }
        
        // === CONCLUSÃO ===
        document.Add(new Paragraph("CONCLUSÃO")
            .SetFont(boldFont)
            .SetFontSize(12)
            .SetFontColor(PdfDocumentHelper.PrimaryColor)
            .SetMarginBottom(10));
        
        var conclusaoTable = new Table(1).UseAllAvailableWidth().SetMarginBottom(15);
        var conclusaoCell = new Cell()
            .SetBorder(new iText.Layout.Borders.SolidBorder(new DeviceRgb(16, 185, 129), 2))
            .SetPadding(15)
            .SetBackgroundColor(new DeviceRgb(236, 253, 245));
        conclusaoCell.Add(new Paragraph(report.Conclusao)
            .SetFont(regularFont)
            .SetFontSize(11)
            .SetTextAlignment(TextAlignment.JUSTIFIED)
            .SetFontColor(PdfDocumentHelper.DarkText));
        conclusaoTable.AddCell(conclusaoCell);
        document.Add(conclusaoTable);
        
        // === RECOMENDAÇÕES ===
        if (!string.IsNullOrEmpty(report.Recomendacoes))
        {
            document.Add(new Paragraph("RECOMENDAÇÕES / CONDUTA")
                .SetFont(boldFont)
                .SetFontSize(12)
                .SetFontColor(PdfDocumentHelper.PrimaryColor)
                .SetMarginBottom(10));
            
            var recomTable = new Table(1).UseAllAvailableWidth().SetMarginBottom(15);
            var recomCell = new Cell()
                .SetBorder(new iText.Layout.Borders.SolidBorder(new DeviceRgb(59, 130, 246), 1))
                .SetPadding(15)
                .SetBackgroundColor(new DeviceRgb(239, 246, 255));
            recomCell.Add(new Paragraph(report.Recomendacoes)
                .SetFont(regularFont)
                .SetFontSize(11)
                .SetFontColor(PdfDocumentHelper.DarkText));
            recomTable.AddCell(recomCell);
            document.Add(recomTable);
        }
        
        // === OBSERVAÇÕES ===
        if (!string.IsNullOrEmpty(report.Observacoes))
        {
            document.Add(new Paragraph("OBSERVAÇÕES")
                .SetFont(boldFont)
                .SetFontSize(10)
                .SetFontColor(PdfDocumentHelper.GrayColor)
                .SetMarginBottom(5));
            document.Add(new Paragraph(report.Observacoes)
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
        
        // Usar parâmetros passados ou valores do objeto report
        var actualCertificateSubject = certificateSubject ?? report.CertificateSubject;
        var actualSignedAt = signedAt ?? report.SignedAt;
        var hasSignature = (isSigned || report.IsSigned) && (actualSignedAt.HasValue || report.SignedAt.HasValue);
        
        PdfDocumentHelper.AddSignatureSection(
            document,
            boldFont,
            regularFont,
            professionalName,
            crm,
            uf,
            hasSignature,
            actualCertificateSubject,
            actualSignedAt ?? report.SignedAt,
            report.CertificateThumbprint);
        
        // === RODAPÉ COM QR CODE E VALIDAÇÃO ===
        var documentHash = GenerateDocumentHash(report);
        PdfDocumentHelper.AddFooter(document, boldFont, regularFont, documentHash, DateTime.UtcNow);
        
        document.Close();
        
        // Adicionar números de página (pós-processamento)
        var pdfWithoutPages = memoryStream.ToArray();
        return PdfDocumentHelper.AddPageNumbersToBytes(pdfWithoutPages);
    }
    
    private void AddSection(Document document, iText.Kernel.Font.PdfFont boldFont, iText.Kernel.Font.PdfFont regularFont, string title, string content)
    {
        document.Add(new Paragraph(title)
            .SetFont(boldFont)
            .SetFontSize(12)
            .SetFontColor(PdfDocumentHelper.PrimaryColor)
            .SetMarginBottom(10));
        
        var table = new Table(1).UseAllAvailableWidth().SetMarginBottom(15);
        var cell = new Cell()
            .SetBorder(new iText.Layout.Borders.SolidBorder(PdfDocumentHelper.GrayColor, 1))
            .SetPadding(15)
            .SetBackgroundColor(PdfDocumentHelper.LightGray);
        cell.Add(new Paragraph(content)
            .SetFont(regularFont)
            .SetFontSize(11)
            .SetTextAlignment(TextAlignment.JUSTIFIED)
            .SetFontColor(PdfDocumentHelper.DarkText));
        table.AddCell(cell);
        document.Add(table);
    }
}
