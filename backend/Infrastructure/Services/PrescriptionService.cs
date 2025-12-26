using Application.DTOs.Receitas;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Signatures;
using iText.Bouncycastle.Crypto;
using iText.Bouncycastle.X509;
using iText.Commons.Bouncycastle.Cert;
using iText.IO.Image;
using Org.BouncyCastle.Pkcs;
using QRCoder;

namespace Infrastructure.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PrescriptionService> _logger;

    public PrescriptionService(ApplicationDbContext context, ILogger<PrescriptionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PrescriptionDto?> GetPrescriptionByIdAsync(Guid id)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(p => p.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(p => p.Appointment)
            .FirstOrDefaultAsync(p => p.Id == id);

        return prescription != null ? MapToDto(prescription) : null;
    }

    public async Task<List<PrescriptionDto>> GetPrescriptionsByAppointmentIdAsync(Guid appointmentId)
    {
        var prescriptions = await _context.Prescriptions
            .Include(p => p.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(p => p.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(p => p.Appointment)
            .Where(p => p.AppointmentId == appointmentId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return prescriptions.Select(MapToDto).ToList();
    }

    public async Task<List<PrescriptionDto>> GetPrescriptionsByPatientIdAsync(Guid patientId)
    {
        var prescriptions = await _context.Prescriptions
            .Include(p => p.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(p => p.Patient)
            .Include(p => p.Appointment)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return prescriptions.Select(MapToDto).ToList();
    }

    public async Task<List<PrescriptionDto>> GetPrescriptionsByProfessionalIdAsync(Guid professionalId)
    {
        var prescriptions = await _context.Prescriptions
            .Include(p => p.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(p => p.Patient)
            .Include(p => p.Appointment)
            .Where(p => p.ProfessionalId == professionalId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return prescriptions.Select(MapToDto).ToList();
    }

    public async Task<PrescriptionDto> CreatePrescriptionAsync(CreatePrescriptionDto dto, Guid professionalId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Professional)
            .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId);

        if (appointment == null)
            throw new InvalidOperationException("Consulta não encontrada.");

        var prescription = new Prescription
        {
            AppointmentId = dto.AppointmentId,
            ProfessionalId = professionalId,
            PatientId = appointment.PatientId,
            ItemsJson = JsonSerializer.Serialize(dto.Items ?? new List<PrescriptionItemDto>())
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        return await GetPrescriptionByIdAsync(prescription.Id) ?? throw new InvalidOperationException("Erro ao criar receita.");
    }

    public async Task<PrescriptionDto?> UpdatePrescriptionAsync(Guid id, UpdatePrescriptionDto dto)
    {
        var prescription = await _context.Prescriptions.FindAsync(id);
        if (prescription == null) return null;

        if (prescription.SignedAt.HasValue)
            throw new InvalidOperationException("Não é possível alterar uma receita já assinada.");

        prescription.ItemsJson = JsonSerializer.Serialize(dto.Items);
        prescription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetPrescriptionByIdAsync(id);
    }

    public async Task<PrescriptionDto?> AddItemAsync(Guid prescriptionId, AddPrescriptionItemDto dto)
    {
        var prescription = await _context.Prescriptions.FindAsync(prescriptionId);
        if (prescription == null) return null;

        if (prescription.SignedAt.HasValue)
            throw new InvalidOperationException("Não é possível alterar uma receita já assinada.");

        var items = JsonSerializer.Deserialize<List<PrescriptionItem>>(prescription.ItemsJson) ?? new List<PrescriptionItem>();
        
        items.Add(new PrescriptionItem
        {
            Id = Guid.NewGuid().ToString(),
            Medicamento = dto.Medicamento,
            CodigoAnvisa = dto.CodigoAnvisa,
            Dosagem = dto.Dosagem,
            Frequencia = dto.Frequencia,
            Periodo = dto.Periodo,
            Posologia = dto.Posologia,
            Observacoes = dto.Observacoes
        });

        prescription.ItemsJson = JsonSerializer.Serialize(items);
        prescription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetPrescriptionByIdAsync(prescriptionId);
    }

    public async Task<PrescriptionDto?> RemoveItemAsync(Guid prescriptionId, string itemId)
    {
        var prescription = await _context.Prescriptions.FindAsync(prescriptionId);
        if (prescription == null) return null;

        if (prescription.SignedAt.HasValue)
            throw new InvalidOperationException("Não é possível alterar uma receita já assinada.");

        var items = JsonSerializer.Deserialize<List<PrescriptionItem>>(prescription.ItemsJson) ?? new List<PrescriptionItem>();
        items.RemoveAll(i => i.Id == itemId);

        prescription.ItemsJson = JsonSerializer.Serialize(items);
        prescription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetPrescriptionByIdAsync(prescriptionId);
    }

    public async Task<PrescriptionDto?> UpdateItemAsync(Guid prescriptionId, string itemId, UpdatePrescriptionItemDto dto)
    {
        var prescription = await _context.Prescriptions.FindAsync(prescriptionId);
        if (prescription == null) return null;

        if (prescription.SignedAt.HasValue)
            throw new InvalidOperationException("Não é possível alterar uma receita já assinada.");

        var items = JsonSerializer.Deserialize<List<PrescriptionItem>>(prescription.ItemsJson) ?? new List<PrescriptionItem>();
        var item = items.FirstOrDefault(i => i.Id == itemId);
        
        if (item == null)
            throw new InvalidOperationException("Medicamento não encontrado na receita.");

        item.Medicamento = dto.Medicamento;
        item.CodigoAnvisa = dto.CodigoAnvisa;
        item.Dosagem = dto.Dosagem;
        item.Frequencia = dto.Frequencia;
        item.Periodo = dto.Periodo;
        item.Posologia = dto.Posologia;
        item.Observacoes = dto.Observacoes;

        prescription.ItemsJson = JsonSerializer.Serialize(items);
        prescription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetPrescriptionByIdAsync(prescriptionId);
    }

    public async Task<PrescriptionPdfDto> GeneratePdfAsync(Guid prescriptionId)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(p => p.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(p => p.Appointment)
                .ThenInclude(a => a.Specialty)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId);

        if (prescription == null)
            throw new InvalidOperationException("Receita não encontrada.");

        var items = JsonSerializer.Deserialize<List<PrescriptionItem>>(prescription.ItemsJson) ?? new List<PrescriptionItem>();
        
        // Generate PDF content
        var pdfBytes = GeneratePrescriptionPdf(prescription, items);
        var pdfBase64 = Convert.ToBase64String(pdfBytes);
        
        // Generate document hash
        var documentHash = GenerateDocumentHash(prescription, items);

        var patientName = prescription.Patient != null 
            ? $"{prescription.Patient.Name} {prescription.Patient.LastName}" 
            : "Paciente";
        
        var isSigned = prescription.SignedAt.HasValue;
        
        return new PrescriptionPdfDto
        {
            PdfBase64 = pdfBase64,
            FileName = PdfDocumentHelper.GenerateFileName(
                "Receita",
                patientName,
                prescription.CreatedAt,
                isSigned ? "Assinada" : null),
            DocumentHash = documentHash,
            IsSigned = isSigned
        };
    }

    public async Task<PrescriptionDto?> SignPrescriptionAsync(Guid prescriptionId, SignPrescriptionDto dto)
    {
        var prescription = await _context.Prescriptions.FindAsync(prescriptionId);
        if (prescription == null) return null;

        if (prescription.SignedAt.HasValue)
            throw new InvalidOperationException("Receita já foi assinada.");

        var items = JsonSerializer.Deserialize<List<PrescriptionItem>>(prescription.ItemsJson) ?? new List<PrescriptionItem>();
        
        prescription.DigitalSignature = dto.Signature;
        prescription.CertificateThumbprint = dto.CertificateThumbprint;
        prescription.CertificateSubject = dto.CertificateSubject;
        prescription.SignedAt = DateTime.UtcNow;
        prescription.DocumentHash = GenerateDocumentHash(prescription, items);
        prescription.UpdatedAt = DateTime.UtcNow;

        // Reload full prescription to generate signed PDF
        var fullPrescription = await _context.Prescriptions
            .Include(p => p.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(p => p.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(p => p.Appointment)
                .ThenInclude(a => a.Specialty)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId);

        if (fullPrescription != null)
        {
            var pdfBytes = GeneratePrescriptionPdf(fullPrescription, items, true);
            prescription.SignedPdfBase64 = Convert.ToBase64String(pdfBytes);
        }

        await _context.SaveChangesAsync();

        return await GetPrescriptionByIdAsync(prescriptionId);
    }

    public async Task<bool> ValidateDocumentHashAsync(string documentHash)
    {
        var prescription = await _context.Prescriptions
            .FirstOrDefaultAsync(p => p.DocumentHash == documentHash);

        return prescription != null && prescription.SignedAt.HasValue;
    }

    public async Task DeletePrescriptionAsync(Guid id)
    {
        var prescription = await _context.Prescriptions.FindAsync(id);
        if (prescription != null)
        {
            _context.Prescriptions.Remove(prescription);
            await _context.SaveChangesAsync();
        }
    }

    private PrescriptionDto MapToDto(Prescription prescription)
    {
        var items = JsonSerializer.Deserialize<List<PrescriptionItem>>(prescription.ItemsJson) ?? new List<PrescriptionItem>();
        
        return new PrescriptionDto
        {
            Id = prescription.Id,
            AppointmentId = prescription.AppointmentId,
            ProfessionalId = prescription.ProfessionalId,
            ProfessionalName = prescription.Professional != null 
                ? $"{prescription.Professional.Name} {prescription.Professional.LastName}" 
                : null,
            ProfessionalCrm = prescription.Professional?.ProfessionalProfile?.Crm,
            PatientId = prescription.PatientId,
            PatientName = prescription.Patient != null 
                ? $"{prescription.Patient.Name} {prescription.Patient.LastName}" 
                : null,
            PatientCpf = prescription.Patient?.Cpf,
            Items = items.Select(i => new PrescriptionItemDto
            {
                Id = i.Id,
                Medicamento = i.Medicamento,
                CodigoAnvisa = i.CodigoAnvisa,
                Dosagem = i.Dosagem,
                Frequencia = i.Frequencia,
                Periodo = i.Periodo,
                Posologia = i.Posologia,
                Observacoes = i.Observacoes
            }).ToList(),
            IsSigned = prescription.SignedAt.HasValue,
            CertificateSubject = prescription.CertificateSubject,
            SignedAt = prescription.SignedAt,
            DocumentHash = prescription.DocumentHash,
            CreatedAt = prescription.CreatedAt,
            UpdatedAt = prescription.UpdatedAt
        };
    }

    private string GenerateDocumentHash(Prescription prescription, List<PrescriptionItem> items)
    {
        var content = new StringBuilder();
        content.Append(prescription.Id);
        content.Append(prescription.AppointmentId);
        content.Append(prescription.ProfessionalId);
        content.Append(prescription.PatientId);
        content.Append(prescription.CreatedAt.ToString("O"));
        
        foreach (var item in items)
        {
            content.Append(item.Medicamento);
            content.Append(item.Dosagem);
            content.Append(item.Frequencia);
            content.Append(item.Periodo);
            content.Append(item.Posologia);
        }

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content.ToString()));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private byte[] GeneratePrescriptionPdf(Prescription prescription, List<PrescriptionItem> items, bool isSigned = false)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new PdfWriter(memoryStream);
        using var pdfDoc = new PdfDocument(writer);
        using var document = new Document(pdfDoc);
        
        var professional = prescription.Professional;
        var patient = prescription.Patient;
        var professionalProfile = professional?.ProfessionalProfile;
        var patientProfile = patient?.PatientProfile;
        
        // Fontes padrão
        var boldFont = PdfDocumentHelper.GetBoldFont();
        var regularFont = PdfDocumentHelper.GetRegularFont();
        
        // === CABEÇALHO PADRÃO ===
        PdfDocumentHelper.AddHeader(document, boldFont, regularFont);
        
        // === TÍTULO DO DOCUMENTO ===
        PdfDocumentHelper.AddDocumentTitle(
            document,
            "RECEITUÁRIO MÉDICO",
            null,
            prescription.CreatedAt,
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
        
        // === CONTEÚDO: MEDICAMENTOS ===
        document.Add(new Paragraph("MEDICAMENTOS PRESCRITOS")
            .SetFont(boldFont)
            .SetFontSize(12)
            .SetFontColor(PdfDocumentHelper.PrimaryColor)
            .SetMarginBottom(15));
        
        var itemNumber = 1;
        foreach (var item in items)
        {
            var medicationTable = new Table(1).UseAllAvailableWidth().SetMarginBottom(12);
            var medicationCell = new Cell()
                .SetPadding(12)
                .SetBorder(new iText.Layout.Borders.SolidBorder(new DeviceRgb(226, 232, 240), 1))
                .SetBackgroundColor(PdfDocumentHelper.LightGray);
            
            medicationCell.Add(new Paragraph($"{itemNumber}. {item.Medicamento}")
                .SetFont(boldFont)
                .SetFontSize(12)
                .SetFontColor(PdfDocumentHelper.PrimaryColor));
            
            if (!string.IsNullOrEmpty(item.CodigoAnvisa))
            {
                medicationCell.Add(new Paragraph($"Registro ANVISA: {item.CodigoAnvisa}")
                    .SetFont(regularFont)
                    .SetFontSize(8)
                    .SetFontColor(PdfDocumentHelper.GrayColor));
            }
            
            var detailsTable = new Table(2).UseAllAvailableWidth().SetMarginTop(8);
            detailsTable.AddCell(CreateDetailCell($"Dosagem: {item.Dosagem}", regularFont));
            detailsTable.AddCell(CreateDetailCell($"Frequência: {item.Frequencia}", regularFont));
            detailsTable.AddCell(CreateDetailCell($"Período: {item.Periodo}", regularFont));
            detailsTable.AddCell(CreateDetailCell($"Posologia: {item.Posologia}", regularFont));
            medicationCell.Add(detailsTable);
            
            if (!string.IsNullOrEmpty(item.Observacoes))
            {
                medicationCell.Add(new Paragraph($"Observações: {item.Observacoes}")
                    .SetFont(regularFont)
                    .SetFontSize(9)
                    .SetItalic()
                    .SetFontColor(PdfDocumentHelper.GrayColor)
                    .SetMarginTop(8));
            }
            
            medicationTable.AddCell(medicationCell);
            document.Add(medicationTable);
            itemNumber++;
        }
        
        // === SEÇÃO DE ASSINATURAS (física + digital se aplicável) ===
        var professionalName = $"Dr(a). {professional?.Name} {professional?.LastName}";
        PdfDocumentHelper.AddSignatureSection(
            document,
            boldFont,
            regularFont,
            professionalName,
            professionalProfile?.Crm,
            professionalProfile?.State,
            isSigned && prescription.SignedAt.HasValue,
            prescription.CertificateSubject,
            prescription.SignedAt,
            prescription.CertificateThumbprint);
        
        // === RODAPÉ COM QR CODE E VALIDAÇÃO ===
        var documentHash = GenerateDocumentHash(prescription, items);
        PdfDocumentHelper.AddFooter(document, boldFont, regularFont, documentHash, DateTime.UtcNow);
        
        document.Close();
        
        // Adicionar números de página (pós-processamento)
        var pdfWithoutPages = memoryStream.ToArray();
        return PdfDocumentHelper.AddPageNumbersToBytes(pdfWithoutPages);
    }
    
    private Cell CreateDetailCell(string text, PdfFont font)
    {
        return new Cell()
            .Add(new Paragraph(text).SetFont(font).SetFontSize(9))
            .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
            .SetPadding(2);
    }
    
    public async Task<PrescriptionPdfDto> GenerateSignedPdfAsync(Guid prescriptionId, byte[] pfxBytes, string pfxPassword)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(p => p.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(p => p.Appointment)
                .ThenInclude(a => a.Specialty)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId);

        if (prescription == null)
            throw new InvalidOperationException("Receita nao encontrada.");

        var items = JsonSerializer.Deserialize<List<PrescriptionItem>>(prescription.ItemsJson) ?? new List<PrescriptionItem>();
        
        // Extrair informações do certificado primeiro para incluir no PDF
        using var cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword);
        var certificateSubject = cert.Subject;
        var certificateThumbprint = cert.Thumbprint;
        var signedAt = DateTime.UtcNow;
        
        // Atualizar a prescrição temporariamente para gerar o PDF com as informações de assinatura
        prescription.CertificateSubject = certificateSubject;
        prescription.CertificateThumbprint = certificateThumbprint;
        prescription.SignedAt = signedAt;
        
        // Generate PDF with signature info (isSigned = true para incluir o card verde)
        var pdfWithSignatureInfo = GeneratePrescriptionPdf(prescription, items, true);
        
        // Sign the PDF with the provided certificate
        var signedPdfBytes = SignPdfWithCertificate(pdfWithSignatureInfo, pfxBytes, pfxPassword, prescription);
        
        var pdfBase64 = Convert.ToBase64String(signedPdfBytes);
        var documentHash = GenerateDocumentHash(prescription, items);
        
        // Persistir as informações de assinatura
        prescription.DigitalSignature = Convert.ToBase64String(cert.GetCertHash());
        prescription.DocumentHash = documentHash;
        prescription.SignedPdfBase64 = pdfBase64;
        prescription.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        var patientName = prescription.Patient != null 
            ? $"{prescription.Patient.Name} {prescription.Patient.LastName}" 
            : "Paciente";
        
        return new PrescriptionPdfDto
        {
            PdfBase64 = pdfBase64,
            FileName = PdfDocumentHelper.GenerateFileName(
                "Receita",
                patientName,
                prescription.CreatedAt,
                "Assinada"),
            DocumentHash = documentHash,
            IsSigned = true
        };
    }
    
    private byte[] SignPdfWithCertificate(byte[] pdfBytes, byte[] pfxBytes, string password, Prescription prescription)
    {
        using var inputStream = new MemoryStream(pdfBytes);
        using var outputStream = new MemoryStream();
        
        // Load the PKCS12 store
        var pkcs12Store = new Pkcs12StoreBuilder().Build();
        pkcs12Store.Load(new MemoryStream(pfxBytes), password.ToCharArray());
        
        // Find the alias with a private key
        string? alias = null;
        foreach (var a in pkcs12Store.Aliases)
        {
            if (pkcs12Store.IsKeyEntry(a))
            {
                alias = a;
                break;
            }
        }
        
        if (alias == null)
            throw new InvalidOperationException("Certificado nao contem chave privada.");
        
        var privateKey = pkcs12Store.GetKey(alias).Key;
        var certificateChain = pkcs12Store.GetCertificateChain(alias);
        
        var bcCertificates = certificateChain
            .Select(c => new X509CertificateBC(c.Certificate))
            .Cast<IX509Certificate>()
            .ToArray();
        
        var reader = new PdfReader(inputStream);
        var signer = new PdfSigner(reader, outputStream, new StampingProperties());
        
        // Set signature metadata (using PdfSigner methods instead of deprecated PdfSignatureAppearance)
        signer.SetReason("Receita medica assinada digitalmente");
        signer.SetLocation("TeleCuidar - Plataforma de Telemedicina");
        signer.SetContact(prescription.Professional?.Email ?? "");
        
        // Sign the document
        var pks = new PrivateKeySignature(new PrivateKeyBC(privateKey), DigestAlgorithms.SHA256);
        signer.SignDetached(pks, bcCertificates, null, null, null, 0, PdfSigner.CryptoStandard.CADES);
        
        return outputStream.ToArray();
    }
}
