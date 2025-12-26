using Application.DTOs.MedicalCertificates;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using QRCoder;

namespace Infrastructure.Services;

public class MedicalCertificateService : IMedicalCertificateService
{
    private readonly ApplicationDbContext _context;
    private readonly ICertificateStorageService _certificateStorageService;
    private readonly ILogger<MedicalCertificateService> _logger;

    public MedicalCertificateService(
        ApplicationDbContext context,
        ICertificateStorageService certificateStorageService,
        ILogger<MedicalCertificateService> logger)
    {
        _context = context;
        _certificateStorageService = certificateStorageService;
        _logger = logger;
    }

    public async Task<MedicalCertificateDto?> GetByIdAsync(Guid id)
    {
        var certificate = await _context.MedicalCertificates
            .Include(c => c.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(c => c.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(c => c.Appointment)
            .FirstOrDefaultAsync(c => c.Id == id);

        return certificate != null ? MapToDto(certificate) : null;
    }

    public async Task<List<MedicalCertificateDto>> GetByAppointmentIdAsync(Guid appointmentId)
    {
        var certificates = await _context.MedicalCertificates
            .Include(c => c.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(c => c.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(c => c.Appointment)
            .Where(c => c.AppointmentId == appointmentId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return certificates.Select(MapToDto).ToList();
    }

    public async Task<List<MedicalCertificateDto>> GetByPatientIdAsync(Guid patientId)
    {
        var certificates = await _context.MedicalCertificates
            .Include(c => c.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(c => c.Patient)
            .Include(c => c.Appointment)
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return certificates.Select(MapToDto).ToList();
    }

    public async Task<List<MedicalCertificateDto>> GetByProfessionalIdAsync(Guid professionalId)
    {
        var certificates = await _context.MedicalCertificates
            .Include(c => c.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(c => c.Patient)
            .Include(c => c.Appointment)
            .Where(c => c.ProfessionalId == professionalId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return certificates.Select(MapToDto).ToList();
    }

    public async Task<MedicalCertificateDto> CreateAsync(CreateMedicalCertificateDto dto, Guid professionalId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Professional)
            .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId);

        if (appointment == null)
            throw new InvalidOperationException("Consulta não encontrada.");

        var tipo = ParseTipo(dto.Tipo);

        var certificate = new MedicalCertificate
        {
            AppointmentId = dto.AppointmentId,
            ProfessionalId = professionalId,
            PatientId = appointment.PatientId,
            Tipo = tipo,
            DataEmissao = DateTime.UtcNow,
            DataInicio = dto.DataInicio,
            DataFim = dto.DataFim,
            DiasAfastamento = dto.DiasAfastamento,
            Cid = dto.Cid,
            Conteudo = dto.Conteudo,
            Observacoes = dto.Observacoes
        };

        _context.MedicalCertificates.Add(certificate);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(certificate.Id) ?? throw new InvalidOperationException("Erro ao criar atestado.");
    }

    public async Task<MedicalCertificateDto?> UpdateAsync(Guid id, UpdateMedicalCertificateDto dto)
    {
        var certificate = await _context.MedicalCertificates.FindAsync(id);
        if (certificate == null) return null;

        if (certificate.IsSigned)
            throw new InvalidOperationException("Não é possível alterar um atestado já assinado.");

        if (!string.IsNullOrEmpty(dto.Tipo))
            certificate.Tipo = ParseTipo(dto.Tipo);
        
        if (dto.DataInicio.HasValue)
            certificate.DataInicio = dto.DataInicio;
            
        if (dto.DataFim.HasValue)
            certificate.DataFim = dto.DataFim;
            
        if (dto.DiasAfastamento.HasValue)
            certificate.DiasAfastamento = dto.DiasAfastamento;
            
        if (dto.Cid != null)
            certificate.Cid = dto.Cid;
            
        if (dto.Conteudo != null)
            certificate.Conteudo = dto.Conteudo;
            
        if (dto.Observacoes != null)
            certificate.Observacoes = dto.Observacoes;

        certificate.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var certificate = await _context.MedicalCertificates.FindAsync(id);
        if (certificate == null) return false;

        if (certificate.IsSigned)
            throw new InvalidOperationException("Não é possível excluir um atestado já assinado.");

        _context.MedicalCertificates.Remove(certificate);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<MedicalCertificatePdfDto> GeneratePdfAsync(Guid id)
    {
        var certificate = await _context.MedicalCertificates
            .Include(c => c.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(c => c.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(c => c.Appointment)
                .ThenInclude(a => a.Specialty)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (certificate == null)
            throw new InvalidOperationException("Atestado não encontrado.");

        var pdfBytes = GenerateCertificatePdf(certificate);
        var pdfBase64 = Convert.ToBase64String(pdfBytes);
        var documentHash = GenerateDocumentHash(certificate);

        var patientName = certificate.Patient != null 
            ? $"{certificate.Patient.Name} {certificate.Patient.LastName}" 
            : "Paciente";
        
        return new MedicalCertificatePdfDto
        {
            PdfBase64 = pdfBase64,
            FileName = PdfDocumentHelper.GenerateFileName(
                "Atestado",
                patientName,
                certificate.CreatedAt,
                certificate.IsSigned ? "Assinado" : null,
                certificate.Tipo.ToString()),
            DocumentHash = documentHash,
            IsSigned = certificate.IsSigned
        };
    }

    public async Task<MedicalCertificateDto?> SignWithSavedCertificateAsync(Guid id, Guid savedCertificateId, string? password)
    {
        var certificate = await _context.MedicalCertificates
            .Include(c => c.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(c => c.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(c => c.Appointment)
                .ThenInclude(a => a.Specialty)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (certificate == null) return null;

        if (certificate.IsSigned)
            throw new InvalidOperationException("Atestado já foi assinado.");

        // Get the saved certificate
        var (pfxBytes, certPassword) = await _certificateStorageService.GetCertificateForSigningAsync(certificate.ProfessionalId, savedCertificateId, password);
        if (pfxBytes == null)
            throw new InvalidOperationException("Certificado não encontrado ou senha incorreta.");

        return await SignCertificateAsync(certificate, pfxBytes, certPassword);
    }

    public async Task<MedicalCertificateDto?> SignWithPfxAsync(Guid id, byte[] pfxBytes, string password)
    {
        var certificate = await _context.MedicalCertificates
            .Include(c => c.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(c => c.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(c => c.Appointment)
                .ThenInclude(a => a.Specialty)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (certificate == null) return null;

        if (certificate.IsSigned)
            throw new InvalidOperationException("Atestado já foi assinado.");

        return await SignCertificateAsync(certificate, pfxBytes, password);
    }

    private async Task<MedicalCertificateDto?> SignCertificateAsync(MedicalCertificate certificate, byte[] pfxBytes, string password)
    {
        try
        {
            using var x509 = X509CertificateLoader.LoadPkcs12(pfxBytes, password, X509KeyStorageFlags.Exportable);
            
            certificate.CertificateThumbprint = x509.Thumbprint;
            certificate.CertificateSubject = x509.GetNameInfo(X509NameType.SimpleName, false);
            certificate.SignedAt = DateTime.UtcNow;
            certificate.DocumentHash = GenerateDocumentHash(certificate);
            
            // Generate signature
            var dataToSign = Encoding.UTF8.GetBytes(certificate.DocumentHash);
            using var rsa = x509.GetRSAPrivateKey();
            if (rsa != null)
            {
                var signature = rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                certificate.DigitalSignature = Convert.ToBase64String(signature);
            }

            // Generate signed PDF
            var pdfBytes = GenerateCertificatePdf(certificate, true);
            certificate.SignedPdfBase64 = Convert.ToBase64String(pdfBytes);

            certificate.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetByIdAsync(certificate.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao assinar atestado {Id}", certificate.Id);
            throw new InvalidOperationException("Erro ao assinar o atestado. Verifique a senha do certificado.");
        }
    }

    public async Task<bool> ValidateDocumentHashAsync(string documentHash)
    {
        var certificate = await _context.MedicalCertificates
            .FirstOrDefaultAsync(c => c.DocumentHash == documentHash);

        return certificate != null && certificate.IsSigned;
    }

    private MedicalCertificateDto MapToDto(MedicalCertificate certificate)
    {
        return new MedicalCertificateDto
        {
            Id = certificate.Id,
            AppointmentId = certificate.AppointmentId,
            ProfessionalId = certificate.ProfessionalId,
            ProfessionalName = certificate.Professional != null 
                ? $"{certificate.Professional.Name} {certificate.Professional.LastName}" 
                : null,
            ProfessionalCrm = certificate.Professional?.ProfessionalProfile?.Crm,
            PatientId = certificate.PatientId,
            PatientName = certificate.Patient != null 
                ? $"{certificate.Patient.Name} {certificate.Patient.LastName}" 
                : null,
            PatientCpf = certificate.Patient?.Cpf,
            Tipo = certificate.Tipo.ToString().ToLowerInvariant(),
            DataEmissao = certificate.DataEmissao,
            DataInicio = certificate.DataInicio,
            DataFim = certificate.DataFim,
            DiasAfastamento = certificate.DiasAfastamento,
            Cid = certificate.Cid,
            Conteudo = certificate.Conteudo,
            Observacoes = certificate.Observacoes,
            IsSigned = certificate.IsSigned,
            CertificateSubject = certificate.CertificateSubject,
            SignedAt = certificate.SignedAt,
            DocumentHash = certificate.DocumentHash,
            CreatedAt = certificate.CreatedAt,
            UpdatedAt = certificate.UpdatedAt
        };
    }

    private MedicalCertificateType ParseTipo(string tipo)
    {
        return tipo.ToLowerInvariant() switch
        {
            "comparecimento" => MedicalCertificateType.Comparecimento,
            "afastamento" => MedicalCertificateType.Afastamento,
            "aptidao" => MedicalCertificateType.Aptidao,
            "acompanhante" => MedicalCertificateType.Acompanhante,
            "outro" => MedicalCertificateType.Outro,
            _ => MedicalCertificateType.Outro
        };
    }

    private string GetTipoLabel(MedicalCertificateType tipo)
    {
        return tipo switch
        {
            MedicalCertificateType.Comparecimento => "ATESTADO DE COMPARECIMENTO",
            MedicalCertificateType.Afastamento => "ATESTADO DE AFASTAMENTO",
            MedicalCertificateType.Aptidao => "ATESTADO DE APTIDÃO",
            MedicalCertificateType.Acompanhante => "ATESTADO DE ACOMPANHANTE",
            MedicalCertificateType.Outro => "ATESTADO MÉDICO",
            _ => "ATESTADO MÉDICO"
        };
    }

    private string GenerateDocumentHash(MedicalCertificate certificate)
    {
        var content = new StringBuilder();
        content.Append(certificate.Id);
        content.Append(certificate.AppointmentId);
        content.Append(certificate.ProfessionalId);
        content.Append(certificate.PatientId);
        content.Append(certificate.Tipo);
        content.Append(certificate.DataEmissao.ToString("O"));
        content.Append(certificate.Conteudo);
        content.Append(certificate.DiasAfastamento);
        content.Append(certificate.Cid);
        content.Append(certificate.CreatedAt.ToString("O"));

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content.ToString()));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private byte[] GenerateCertificatePdf(MedicalCertificate certificate, bool isSigned = false)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new PdfWriter(memoryStream);
        using var pdfDoc = new PdfDocument(writer);
        using var document = new Document(pdfDoc);
        
        var professional = certificate.Professional;
        var patient = certificate.Patient;
        var professionalProfile = professional?.ProfessionalProfile;
        var patientProfile = patient?.PatientProfile;
        
        // Fontes padrão
        var boldFont = PdfDocumentHelper.GetBoldFont();
        var regularFont = PdfDocumentHelper.GetRegularFont();
        
        // === CABEÇALHO PADRÃO ===
        PdfDocumentHelper.AddHeader(document, boldFont, regularFont);
        
        // === TÍTULO DO DOCUMENTO ===
        var tipoLabel = GetTipoLabel(certificate.Tipo);
        PdfDocumentHelper.AddDocumentTitle(
            document,
            tipoLabel,
            null,
            certificate.DataEmissao,
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
        
        // === CONTEÚDO DO ATESTADO ===
        document.Add(new Paragraph("CONTEÚDO DO ATESTADO")
            .SetFont(boldFont)
            .SetFontSize(12)
            .SetFontColor(PdfDocumentHelper.PrimaryColor)
            .SetMarginBottom(10));
        
        var contentTable = new Table(1).UseAllAvailableWidth().SetMarginBottom(15);
        var contentCell = new Cell()
            .SetBorder(new iText.Layout.Borders.SolidBorder(PdfDocumentHelper.GrayColor, 1))
            .SetPadding(15)
            .SetBackgroundColor(PdfDocumentHelper.LightGray);
        contentCell.Add(new Paragraph(certificate.Conteudo)
            .SetFont(regularFont)
            .SetFontSize(11)
            .SetFontColor(PdfDocumentHelper.DarkText)
            .SetTextAlignment(TextAlignment.JUSTIFIED));
        contentTable.AddCell(contentCell);
        document.Add(contentTable);
        
        // === INFORMAÇÕES ADICIONAIS (para Afastamento) ===
        if (certificate.Tipo == MedicalCertificateType.Afastamento && certificate.DiasAfastamento.HasValue)
        {
            var infoTable = new Table(4).UseAllAvailableWidth().SetMarginBottom(15);
            
            // Dias de afastamento
            var diasCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(8);
            diasCell.Add(new Paragraph("Dias de Afastamento")
                .SetFont(regularFont)
                .SetFontSize(9)
                .SetFontColor(PdfDocumentHelper.GrayColor));
            diasCell.Add(new Paragraph(certificate.DiasAfastamento.Value.ToString())
                .SetFont(boldFont)
                .SetFontSize(14)
                .SetFontColor(PdfDocumentHelper.PrimaryColor));
            infoTable.AddCell(diasCell);
            
            // Data início
            if (certificate.DataInicio.HasValue)
            {
                var inicioCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(8);
                inicioCell.Add(new Paragraph("Data Início")
                    .SetFont(regularFont)
                    .SetFontSize(9)
                    .SetFontColor(PdfDocumentHelper.GrayColor));
                inicioCell.Add(new Paragraph(certificate.DataInicio.Value.ToString("dd/MM/yyyy"))
                    .SetFont(boldFont)
                    .SetFontSize(12)
                    .SetFontColor(PdfDocumentHelper.DarkText));
                infoTable.AddCell(inicioCell);
            }
            
            // Data fim
            if (certificate.DataFim.HasValue)
            {
                var fimCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(8);
                fimCell.Add(new Paragraph("Data Fim")
                    .SetFont(regularFont)
                    .SetFontSize(9)
                    .SetFontColor(PdfDocumentHelper.GrayColor));
                fimCell.Add(new Paragraph(certificate.DataFim.Value.ToString("dd/MM/yyyy"))
                    .SetFont(boldFont)
                    .SetFontSize(12)
                    .SetFontColor(PdfDocumentHelper.DarkText));
                infoTable.AddCell(fimCell);
            }
            
            // CID
            if (!string.IsNullOrEmpty(certificate.Cid))
            {
                var cidCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(8);
                cidCell.Add(new Paragraph("CID")
                    .SetFont(regularFont)
                    .SetFontSize(9)
                    .SetFontColor(PdfDocumentHelper.GrayColor));
                cidCell.Add(new Paragraph(certificate.Cid)
                    .SetFont(boldFont)
                    .SetFontSize(12)
                    .SetFontColor(PdfDocumentHelper.DarkText));
                infoTable.AddCell(cidCell);
            }
            
            document.Add(infoTable);
        }
        
        // === OBSERVAÇÕES ===
        if (!string.IsNullOrEmpty(certificate.Observacoes))
        {
            document.Add(new Paragraph("OBSERVAÇÕES")
                .SetFont(boldFont)
                .SetFontSize(10)
                .SetFontColor(PdfDocumentHelper.GrayColor)
                .SetMarginBottom(5));
            document.Add(new Paragraph(certificate.Observacoes)
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
        
        PdfDocumentHelper.AddSignatureSection(
            document,
            boldFont,
            regularFont,
            professionalName,
            crm,
            uf,
            (isSigned || certificate.IsSigned) && certificate.SignedAt.HasValue,
            certificate.CertificateSubject,
            certificate.SignedAt,
            certificate.CertificateThumbprint);
        
        // === RODAPÉ COM QR CODE E VALIDAÇÃO ===
        var documentHash = GenerateDocumentHash(certificate);
        PdfDocumentHelper.AddFooter(document, boldFont, regularFont, documentHash, DateTime.UtcNow);
        
        document.Close();
        
        // Adicionar números de página (pós-processamento)
        var pdfWithoutPages = memoryStream.ToArray();
        return PdfDocumentHelper.AddPageNumbersToBytes(pdfWithoutPages);
    }
}