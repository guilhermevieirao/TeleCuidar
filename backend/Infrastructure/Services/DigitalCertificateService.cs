using Application.DTOs.Certificates;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.Kernel.Colors;
using iText.Signatures;
using iText.Bouncycastle.Crypto;
using iText.Bouncycastle.X509;
using iText.Commons.Bouncycastle.Cert;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Pkcs;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Infrastructure.Services;

public class DigitalCertificateService : IDigitalCertificateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DigitalCertificateService> _logger;
    private readonly IPrescriptionService _prescriptionService;
    private readonly IMedicalCertificateService _medicalCertificateService;
    private readonly IExamRequestService _examRequestService;
    private readonly IMedicalReportService _medicalReportService;
    private readonly string _encryptionKey;

    public DigitalCertificateService(
        ApplicationDbContext context,
        ILogger<DigitalCertificateService> logger,
        IPrescriptionService prescriptionService,
        IMedicalCertificateService medicalCertificateService,
        IExamRequestService examRequestService,
        IMedicalReportService medicalReportService,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _prescriptionService = prescriptionService;
        _medicalCertificateService = medicalCertificateService;
        _examRequestService = examRequestService;
        _medicalReportService = medicalReportService;
        _encryptionKey = configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey não configurado");
    }

    public async Task<List<DigitalCertificateDto>> GetCertificatesByUserIdAsync(Guid userId)
    {
        var certificates = await _context.DigitalCertificates
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return certificates.Select(MapToDto).ToList();
    }

    public async Task<DigitalCertificateDto?> GetCertificateByIdAsync(Guid id, Guid userId)
    {
        var certificate = await _context.DigitalCertificates
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        return certificate != null ? MapToDto(certificate) : null;
    }

    public async Task<CertificateValidationResult> ValidateCertificateAsync(ValidateCertificateDto dto)
    {
        try
        {
            var pfxBytes = Convert.FromBase64String(dto.PfxBase64);
            using var certificate = X509CertificateLoader.LoadPkcs12(pfxBytes, dto.Password, X509KeyStorageFlags.Exportable);

            // Extrair informações do certificado
            var cpf = ExtractCpfFromCertificate(certificate);
            var name = ExtractNameFromCertificate(certificate);

            var result = new CertificateValidationResult
            {
                IsValid = true,
                Subject = certificate.Subject,
                Issuer = certificate.Issuer,
                Thumbprint = certificate.Thumbprint,
                CpfFromCertificate = cpf,
                NameFromCertificate = name,
                ExpirationDate = certificate.NotAfter,
                IssuedDate = certificate.NotBefore,
                IsExpired = DateTime.UtcNow > certificate.NotAfter,
                DaysUntilExpiration = (certificate.NotAfter - DateTime.UtcNow).Days
            };

            return result;
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning("Erro ao validar certificado: {Message}", ex.Message);
            return new CertificateValidationResult
            {
                IsValid = false,
                ErrorMessage = "Senha incorreta ou arquivo de certificado inválido"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao validar certificado");
            return new CertificateValidationResult
            {
                IsValid = false,
                ErrorMessage = "Erro ao processar o certificado"
            };
        }
    }

    public async Task<DigitalCertificateDto> SaveCertificateAsync(SaveCertificateDto dto, Guid userId)
    {
        // Validar o certificado primeiro
        var validation = await ValidateCertificateAsync(new ValidateCertificateDto
        {
            PfxBase64 = dto.PfxBase64,
            Password = dto.Password
        });

        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage ?? "Certificado inválido");
        }

        if (validation.IsExpired)
        {
            throw new InvalidOperationException("O certificado está expirado");
        }

        // Verificar se já existe um certificado com o mesmo thumbprint
        var existingCert = await _context.DigitalCertificates
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Thumbprint == validation.Thumbprint);

        if (existingCert != null)
        {
            throw new InvalidOperationException("Este certificado já está cadastrado");
        }

        // Criptografar o PFX e a senha (se QuickUse)
        var (encryptedPfx, iv) = EncryptData(dto.PfxBase64);
        string? encryptedPassword = null;

        if (dto.QuickUseEnabled)
        {
            (encryptedPassword, _) = EncryptData(dto.Password, iv);
        }

        var certificate = new DigitalCertificate
        {
            UserId = userId,
            DisplayName = dto.DisplayName,
            Subject = validation.Subject,
            Issuer = validation.Issuer,
            Thumbprint = validation.Thumbprint,
            CpfFromCertificate = validation.CpfFromCertificate,
            NameFromCertificate = validation.NameFromCertificate,
            ExpirationDate = validation.ExpirationDate,
            IssuedDate = validation.IssuedDate,
            EncryptedPfxBase64 = encryptedPfx,
            QuickUseEnabled = dto.QuickUseEnabled,
            EncryptedPassword = encryptedPassword,
            EncryptionIV = iv,
            IsActive = true
        };

        _context.DigitalCertificates.Add(certificate);
        await _context.SaveChangesAsync();

        return MapToDto(certificate);
    }

    public async Task<DigitalCertificateDto?> UpdateCertificateAsync(Guid id, UpdateCertificateDto dto, Guid userId)
    {
        var certificate = await _context.DigitalCertificates
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (certificate == null)
            return null;

        if (dto.DisplayName != null)
        {
            certificate.DisplayName = dto.DisplayName;
        }

        if (dto.QuickUseEnabled.HasValue)
        {
            if (dto.QuickUseEnabled.Value && !certificate.QuickUseEnabled)
            {
                // Ativando QuickUse - precisa da senha
                if (string.IsNullOrEmpty(dto.Password))
                {
                    throw new InvalidOperationException("Senha necessária para ativar uso rápido");
                }

                // Validar a senha descriptografando o PFX e testando
                var pfxBase64 = DecryptData(certificate.EncryptedPfxBase64, certificate.EncryptionIV);
                var pfxBytes = Convert.FromBase64String(pfxBase64);

                try
                {
                    using var _ = X509CertificateLoader.LoadPkcs12(pfxBytes, dto.Password, X509KeyStorageFlags.Exportable);
                }
                catch (CryptographicException)
                {
                    throw new InvalidOperationException("Senha incorreta");
                }

                var (encryptedPassword, _) = EncryptData(dto.Password, certificate.EncryptionIV);
                certificate.EncryptedPassword = encryptedPassword;
            }
            else if (!dto.QuickUseEnabled.Value)
            {
                // Desativando QuickUse
                certificate.EncryptedPassword = null;
            }

            certificate.QuickUseEnabled = dto.QuickUseEnabled.Value;
        }

        await _context.SaveChangesAsync();
        return MapToDto(certificate);
    }

    public async Task<bool> DeleteCertificateAsync(Guid id, Guid userId)
    {
        var certificate = await _context.DigitalCertificates
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (certificate == null)
            return false;

        _context.DigitalCertificates.Remove(certificate);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<SignDocumentResult> SignDocumentAsync(SignDocumentRequestDto dto, Guid userId)
    {
        try
        {
            X509Certificate2 x509Cert;
            string pfxBase64;
            string password;

            if (!string.IsNullOrEmpty(dto.OneTimePfxBase64))
            {
                // Usando certificado one-time
                if (string.IsNullOrEmpty(dto.Password))
                {
                    return new SignDocumentResult
                    {
                        Success = false,
                        ErrorMessage = "Senha necessária para certificado one-time"
                    };
                }

                pfxBase64 = dto.OneTimePfxBase64;
                password = dto.Password;
            }
            else if (dto.CertificateId.HasValue)
            {
                // Usando certificado salvo
                var savedCert = await _context.DigitalCertificates
                    .FirstOrDefaultAsync(c => c.Id == dto.CertificateId.Value && c.UserId == userId);

                if (savedCert == null)
                {
                    return new SignDocumentResult
                    {
                        Success = false,
                        ErrorMessage = "Certificado não encontrado"
                    };
                }

                if (DateTime.UtcNow > savedCert.ExpirationDate)
                {
                    return new SignDocumentResult
                    {
                        Success = false,
                        ErrorMessage = "O certificado está expirado"
                    };
                }

                pfxBase64 = DecryptData(savedCert.EncryptedPfxBase64, savedCert.EncryptionIV);

                if (savedCert.QuickUseEnabled && !string.IsNullOrEmpty(savedCert.EncryptedPassword))
                {
                    password = DecryptData(savedCert.EncryptedPassword, savedCert.EncryptionIV);
                }
                else if (!string.IsNullOrEmpty(dto.Password))
                {
                    password = dto.Password;
                }
                else
                {
                    return new SignDocumentResult
                    {
                        Success = false,
                        ErrorMessage = "Senha necessária"
                    };
                }

                // Atualizar último uso
                savedCert.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            else
            {
                return new SignDocumentResult
                {
                    Success = false,
                    ErrorMessage = "Certificado não especificado"
                };
            }

            // Carregar o certificado
            var pfxBytes = Convert.FromBase64String(pfxBase64);
            try
            {
                x509Cert = X509CertificateLoader.LoadPkcs12(pfxBytes, password, X509KeyStorageFlags.Exportable);
            }
            catch (CryptographicException)
            {
                return new SignDocumentResult
                {
                    Success = false,
                    ErrorMessage = "Senha incorreta"
                };
            }

            // Assinar o documento
            SignDocumentResult result;
            if (dto.DocumentType.ToLower() == "prescription")
            {
                result = await SignPrescriptionAsync(dto.DocumentId, x509Cert, userId, pfxBase64, password);
            }
            else if (dto.DocumentType.ToLower() == "certificate")
            {
                result = await SignMedicalCertificateAsync(dto.DocumentId, x509Cert, userId, pfxBase64, password);
            }
            else if (dto.DocumentType.ToLower() == "exam")
            {
                result = await SignExamRequestAsync(dto.DocumentId, x509Cert, userId, pfxBase64, password);
            }
            else if (dto.DocumentType.ToLower() == "report")
            {
                result = await SignMedicalReportAsync(dto.DocumentId, x509Cert, userId, pfxBase64, password);
            }
            else
            {
                return new SignDocumentResult
                {
                    Success = false,
                    ErrorMessage = "Tipo de documento inválido"
                };
            }

            x509Cert.Dispose();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao assinar documento");
            return new SignDocumentResult
            {
                Success = false,
                ErrorMessage = "Erro interno ao assinar documento"
            };
        }
    }

    public async Task<(DigitalCertificateDto Certificate, SignDocumentResult SignResult)> SaveCertificateAndSignAsync(
        SaveCertificateAndSignDto dto, Guid userId)
    {
        // Primeiro salva o certificado
        var savedCert = await SaveCertificateAsync(new SaveCertificateDto
        {
            PfxBase64 = dto.PfxBase64,
            Password = dto.Password,
            DisplayName = dto.DisplayName,
            QuickUseEnabled = dto.QuickUseEnabled
        }, userId);

        // Depois assina o documento
        var signResult = await SignDocumentAsync(new SignDocumentRequestDto
        {
            CertificateId = savedCert.Id,
            Password = dto.Password, // Passa a senha porque pode não ter QuickUse
            DocumentType = dto.DocumentType,
            DocumentId = dto.DocumentId
        }, userId);

        return (savedCert, signResult);
    }

    private async Task<SignDocumentResult> SignPrescriptionAsync(Guid prescriptionId, X509Certificate2 certificate, Guid userId, string pfxBase64, string password)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
                    .ThenInclude(pp => pp!.Specialty)
            .Include(p => p.Patient)
            .Include(p => p.Appointment)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId && p.ProfessionalId == userId);

        if (prescription == null)
        {
            return new SignDocumentResult
            {
                Success = false,
                ErrorMessage = "Receita não encontrada ou você não tem permissão"
            };
        }

        if (!string.IsNullOrEmpty(prescription.SignedPdfBase64))
        {
            return new SignDocumentResult
            {
                Success = false,
                ErrorMessage = "Esta receita já está assinada"
            };
        }

        try
        {
            var signedAt = DateTime.UtcNow;
            
            // Gerar PDF com informações de assinatura
            var pdfBytes = GeneratePrescriptionPdf(prescription, certificate.Subject, signedAt);
            
            // Assinar PDF no padrão PAdES
            var signedPdfBytes = SignPdfWithPAdES(pdfBytes, pfxBase64, password, certificate, "Receita Médica");
            
            // Criar hash do PDF assinado
            var documentHash = ComputeHashBytes(signedPdfBytes);

            // Atualizar o documento
            prescription.SignedPdfBase64 = Convert.ToBase64String(signedPdfBytes);
            prescription.DigitalSignature = "PAdES"; // Indica que é assinatura PAdES embutida no PDF
            prescription.CertificateThumbprint = certificate.Thumbprint;
            prescription.CertificateSubject = certificate.Subject;
            prescription.SignedAt = signedAt;
            prescription.DocumentHash = documentHash;

            await _context.SaveChangesAsync();

            return new SignDocumentResult
            {
                Success = true,
                DocumentHash = documentHash,
                CertificateSubject = certificate.Subject,
                SignedAt = prescription.SignedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar PDF assinado da receita");
            return new SignDocumentResult
            {
                Success = false,
                ErrorMessage = "Erro ao gerar PDF assinado: " + ex.Message
            };
        }
    }

    private async Task<SignDocumentResult> SignMedicalCertificateAsync(Guid certificateId, X509Certificate2 cert, Guid userId, string pfxBase64, string password)
    {
        var medCert = await _context.MedicalCertificates
            .Include(c => c.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
                    .ThenInclude(pp => pp!.Specialty)
            .Include(c => c.Patient)
            .Include(c => c.Appointment)
            .FirstOrDefaultAsync(c => c.Id == certificateId && c.ProfessionalId == userId);

        if (medCert == null)
        {
            return new SignDocumentResult
            {
                Success = false,
                ErrorMessage = "Atestado não encontrado ou você não tem permissão"
            };
        }

        if (!string.IsNullOrEmpty(medCert.SignedPdfBase64))
        {
            return new SignDocumentResult
            {
                Success = false,
                ErrorMessage = "Este atestado já está assinado"
            };
        }

        try
        {
            var signedAt = DateTime.UtcNow;
            
            // Gerar PDF com informações de assinatura
            var pdfBytes = GenerateMedicalCertificatePdf(medCert, cert.Subject, signedAt);
            
            // Assinar PDF no padrão PAdES
            var signedPdfBytes = SignPdfWithPAdES(pdfBytes, pfxBase64, password, cert, "Atestado Médico");
            
            // Criar hash do PDF assinado
            var documentHash = ComputeHashBytes(signedPdfBytes);

            // Atualizar o documento
            medCert.SignedPdfBase64 = Convert.ToBase64String(signedPdfBytes);
            medCert.DigitalSignature = "PAdES"; // Indica que é assinatura PAdES embutida no PDF
            medCert.CertificateThumbprint = cert.Thumbprint;
            medCert.CertificateSubject = cert.Subject;
            medCert.SignedAt = signedAt;
            medCert.DocumentHash = documentHash;

            await _context.SaveChangesAsync();

            return new SignDocumentResult
            {
                Success = true,
                DocumentHash = documentHash,
                CertificateSubject = cert.Subject,
                SignedAt = medCert.SignedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar PDF assinado do atestado");
            return new SignDocumentResult
            {
                Success = false,
                ErrorMessage = "Erro ao gerar PDF assinado: " + ex.Message
            };
        }
    }

    private async Task<SignDocumentResult> SignExamRequestAsync(Guid examId, X509Certificate2 cert, Guid userId, string pfxBase64, string password)
    {
        var exam = await _context.ExamRequests
            .Include(e => e.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
            .Include(e => e.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(e => e.Appointment)
            .FirstOrDefaultAsync(e => e.Id == examId && e.ProfessionalId == userId);

        if (exam == null)
        {
            return new SignDocumentResult
            {
                Success = false,
                ErrorMessage = "Solicitação de exame não encontrada ou você não tem permissão"
            };
        }

        if (!string.IsNullOrEmpty(exam.SignedPdfBase64))
        {
            return new SignDocumentResult
            {
                Success = false,
                ErrorMessage = "Esta solicitação de exame já está assinada"
            };
        }

        try
        {
            var signedAt = DateTime.UtcNow;
            
            // Gerar PDF com informações de assinatura
            var examService = _examRequestService as ExamRequestService;
            var pdfBytes = examService!.GenerateExamPdf(exam, true, cert.Subject, signedAt);
            
            // Assinar PDF no padrão PAdES
            var signedPdfBytes = SignPdfWithPAdES(pdfBytes, pfxBase64, password, cert, "Solicitação de Exame");
            
            // Criar hash do PDF assinado
            var documentHash = ComputeHashBytes(signedPdfBytes);

            // Atualizar o documento
            exam.SignedPdfBase64 = Convert.ToBase64String(signedPdfBytes);
            exam.DigitalSignature = "PAdES"; // Indica que é assinatura PAdES embutida no PDF
            exam.CertificateThumbprint = cert.Thumbprint;
            exam.CertificateSubject = cert.Subject;
            exam.SignedAt = signedAt;
            exam.DocumentHash = documentHash;

            await _context.SaveChangesAsync();

            return new SignDocumentResult
            {
                Success = true,
                DocumentHash = documentHash,
                CertificateSubject = cert.Subject,
                SignedAt = exam.SignedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar PDF assinado da solicitação de exame");
            return new SignDocumentResult
            {
                Success = false,
                ErrorMessage = "Erro ao gerar PDF assinado: " + ex.Message
            };
        }
    }

    private async Task<SignDocumentResult> SignMedicalReportAsync(Guid reportId, X509Certificate2 cert, Guid userId, string pfxBase64, string password)
    {
        var report = await _context.MedicalReports
            .Include(r => r.Professional)
                .ThenInclude(u => u.ProfessionalProfile)
                    .ThenInclude(pp => pp!.Specialty)
            .Include(r => r.Patient)
                .ThenInclude(u => u.PatientProfile)
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.Id == reportId && r.ProfessionalId == userId);

        if (report == null)
        {
            return new SignDocumentResult
            {
                Success = false,
                ErrorMessage = "Laudo não encontrado ou você não tem permissão"
            };
        }

        if (!string.IsNullOrEmpty(report.SignedPdfBase64))
        {
            return new SignDocumentResult
            {
                Success = false,
                ErrorMessage = "Este laudo já está assinado"
            };
        }

        try
        {
            var signedAt = DateTime.UtcNow;
            
            // Gerar PDF com informações de assinatura
            var reportService = _medicalReportService as MedicalReportService;
            var pdfBytes = reportService!.GenerateReportPdf(report, true, cert.Subject, signedAt);
            
            // Assinar PDF no padrão PAdES
            var signedPdfBytes = SignPdfWithPAdES(pdfBytes, pfxBase64, password, cert, "Laudo Médico");
            
            // Criar hash do PDF assinado
            var documentHash = ComputeHashBytes(signedPdfBytes);

            // Atualizar o documento
            report.SignedPdfBase64 = Convert.ToBase64String(signedPdfBytes);
            report.DigitalSignature = "PAdES"; // Indica que é assinatura PAdES embutida no PDF
            report.CertificateThumbprint = cert.Thumbprint;
            report.CertificateSubject = cert.Subject;
            report.SignedAt = signedAt;
            report.DocumentHash = documentHash;

            await _context.SaveChangesAsync();

            return new SignDocumentResult
            {
                Success = true,
                DocumentHash = documentHash,
                CertificateSubject = cert.Subject,
                SignedAt = report.SignedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar PDF assinado do laudo");
            return new SignDocumentResult
            {
                Success = false,
                ErrorMessage = "Erro ao gerar PDF assinado: " + ex.Message
            };
        }
    }

    private byte[] GeneratePrescriptionPdf(Prescription prescription, string? certificateSubject = null, DateTime? signedAt = null)
    {
        var items = JsonSerializer.Deserialize<List<PrescriptionItem>>(prescription.ItemsJson) ?? new();
        
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
        
        // === SEÇÃO DE ASSINATURAS ===
        var professionalName = $"Dr(a). {professional?.Name} {professional?.LastName}";
        PdfDocumentHelper.AddPhysicalSignatureArea(
            document,
            boldFont,
            regularFont,
            professionalName,
            professionalProfile?.Crm,
            professionalProfile?.State);
        
        // === CARD VERDE DE ASSINATURA DIGITAL ===
        if (signedAt.HasValue && !string.IsNullOrEmpty(certificateSubject))
        {
            var signerName = ExtractNameFromCertificateSubject(certificateSubject);
            PdfDocumentHelper.AddDigitalSignature(document, boldFont, regularFont, signerName, signedAt.Value);
        }
        
        // === RODAPÉ COM QR CODE E VALIDAÇÃO ===
        var documentHash = ComputeHash($"{prescription.Id}|{prescription.ItemsJson}|{prescription.ProfessionalId}|{prescription.PatientId}");
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

    private byte[] GenerateMedicalCertificatePdf(MedicalCertificate medCert, string? certificateSubject = null, DateTime? signedAt = null)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new PdfWriter(memoryStream);
        using var pdfDoc = new PdfDocument(writer);
        using var document = new Document(pdfDoc);
        
        var professional = medCert.Professional;
        var patient = medCert.Patient;
        var professionalProfile = professional?.ProfessionalProfile;
        var patientProfile = patient?.PatientProfile;
        
        // Fontes padrão
        var boldFont = PdfDocumentHelper.GetBoldFont();
        var regularFont = PdfDocumentHelper.GetRegularFont();
        
        // Tipo de atestado para título
        var tipoTexto = medCert.Tipo switch
        {
            MedicalCertificateType.Comparecimento => "Comparecimento",
            MedicalCertificateType.Afastamento => "Afastamento",
            MedicalCertificateType.Aptidao => "Aptidão",
            MedicalCertificateType.Acompanhante => "Acompanhante",
            _ => ""
        };
        
        // === CABEÇALHO PADRÃO ===
        PdfDocumentHelper.AddHeader(document, boldFont, regularFont);
        
        // === TÍTULO DO DOCUMENTO ===
        PdfDocumentHelper.AddDocumentTitle(
            document,
            $"ATESTADO MÉDICO",
            $"Atestado de {tipoTexto}",
            medCert.DataEmissao,
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
        
        // === CONTEÚDO: DECLARAÇÃO ===
        document.Add(new Paragraph("DECLARAÇÃO")
            .SetFont(boldFont)
            .SetFontSize(12)
            .SetFontColor(PdfDocumentHelper.PrimaryColor)
            .SetMarginBottom(15));
        
        // Conteúdo do atestado em caixa
        var contentTable = new Table(1).UseAllAvailableWidth().SetMarginBottom(15);
        var contentCell = new Cell()
            .SetPadding(15)
            .SetBorder(new iText.Layout.Borders.SolidBorder(new DeviceRgb(226, 232, 240), 1))
            .SetBackgroundColor(PdfDocumentHelper.LightGray);
        
        contentCell.Add(new Paragraph(medCert.Conteudo)
            .SetFont(regularFont)
            .SetFontSize(11)
            .SetTextAlignment(TextAlignment.JUSTIFIED)
            .SetFontColor(PdfDocumentHelper.DarkText));
        
        contentTable.AddCell(contentCell);
        document.Add(contentTable);
        
        // Informações adicionais se aplicável
        if (medCert.Tipo == MedicalCertificateType.Afastamento)
        {
            var infoTable = new Table(2).UseAllAvailableWidth().SetMarginBottom(15);
            
            if (medCert.DataInicio.HasValue && medCert.DataFim.HasValue)
            {
                var periodoCell = new Cell()
                    .SetPadding(10)
                    .SetBorder(new iText.Layout.Borders.SolidBorder(new DeviceRgb(226, 232, 240), 1));
                periodoCell.Add(new Paragraph("Período de Afastamento")
                    .SetFont(boldFont)
                    .SetFontSize(10)
                    .SetFontColor(PdfDocumentHelper.GrayColor)
                    .SetMarginBottom(5));
                periodoCell.Add(new Paragraph($"{medCert.DataInicio:dd/MM/yyyy} a {medCert.DataFim:dd/MM/yyyy}")
                    .SetFont(regularFont)
                    .SetFontSize(11)
                    .SetFontColor(PdfDocumentHelper.DarkText));
                infoTable.AddCell(periodoCell);
            }
            
            if (medCert.DiasAfastamento.HasValue)
            {
                var diasCell = new Cell()
                    .SetPadding(10)
                    .SetBorder(new iText.Layout.Borders.SolidBorder(new DeviceRgb(226, 232, 240), 1));
                diasCell.Add(new Paragraph("Total de Dias")
                    .SetFont(boldFont)
                    .SetFontSize(10)
                    .SetFontColor(PdfDocumentHelper.GrayColor)
                    .SetMarginBottom(5));
                diasCell.Add(new Paragraph($"{medCert.DiasAfastamento} dia(s)")
                    .SetFont(regularFont)
                    .SetFontSize(11)
                    .SetFontColor(PdfDocumentHelper.DarkText));
                infoTable.AddCell(diasCell);
            }
            
            document.Add(infoTable);
        }
        
        if (!string.IsNullOrEmpty(medCert.Cid))
        {
            document.Add(new Paragraph($"CID: {medCert.Cid}")
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetFontColor(PdfDocumentHelper.GrayColor)
                .SetMarginBottom(10));
        }
        
        if (!string.IsNullOrEmpty(medCert.Observacoes))
        {
            document.Add(new Paragraph($"Observações: {medCert.Observacoes}")
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetItalic()
                .SetFontColor(PdfDocumentHelper.GrayColor)
                .SetMarginBottom(10));
        }
        
        // === SEÇÃO DE ASSINATURAS ===
        var professionalName = $"Dr(a). {professional?.Name} {professional?.LastName}";
        PdfDocumentHelper.AddPhysicalSignatureArea(
            document,
            boldFont,
            regularFont,
            professionalName,
            professionalProfile?.Crm,
            professionalProfile?.State);
        
        // === CARD VERDE DE ASSINATURA DIGITAL ===
        if (signedAt.HasValue && !string.IsNullOrEmpty(certificateSubject))
        {
            var signerName = ExtractNameFromCertificateSubject(certificateSubject);
            PdfDocumentHelper.AddDigitalSignature(document, boldFont, regularFont, signerName, signedAt.Value);
        }
        
        // === RODAPÉ COM QR CODE E VALIDAÇÃO ===
        var documentHash = ComputeHash($"{medCert.Id}|{medCert.Conteudo}|{medCert.ProfessionalId}|{medCert.PatientId}|{medCert.Tipo}");
        PdfDocumentHelper.AddFooter(document, boldFont, regularFont, documentHash, DateTime.UtcNow);
        
        document.Close();
        
        // Adicionar números de página (pós-processamento)
        var pdfWithoutPages = memoryStream.ToArray();
        return PdfDocumentHelper.AddPageNumbersToBytes(pdfWithoutPages);
    }

    private byte[] SignPdfWithPAdES(byte[] pdfBytes, string pfxBase64, string password, X509Certificate2 x509Cert, string reason)
    {
        // Carregar o PFX com Bouncy Castle para obter a chave privada
        var pfxBytes = Convert.FromBase64String(pfxBase64);
        var pkcs12Store = new Pkcs12StoreBuilder().Build();
        
        using var pfxStream = new MemoryStream(pfxBytes);
        pkcs12Store.Load(pfxStream, password.ToCharArray());

        // Encontrar o alias com chave privada
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
        {
            throw new InvalidOperationException("Certificado não contém chave privada");
        }

        var privateKey = pkcs12Store.GetKey(alias).Key;
        var certificateEntry = pkcs12Store.GetCertificate(alias);
        var chain = new IX509Certificate[] { new X509CertificateBC(certificateEntry.Certificate) };

        // Criar o PDF assinado
        using var inputStream = new MemoryStream(pdfBytes);
        using var outputStream = new MemoryStream();
        
        var reader = new PdfReader(inputStream);
        var signer = new PdfSigner(reader, outputStream, new StampingProperties());

        // Configurar metadados da assinatura (usando API atual)
        var signerName = ExtractNameFromCertificate(x509Cert) ?? x509Cert.Subject;
        signer.SetReason(reason);
        signer.SetLocation("Brasil");
        signer.SetContact(x509Cert.Subject);

        // Criar assinatura PAdES
        var privateKeyBC = new PrivateKeyBC(privateKey);
        IExternalSignature pks = new PrivateKeySignature(privateKeyBC, DigestAlgorithms.SHA256);
        
        signer.SignDetached(pks, chain, null, null, null, 0, PdfSigner.CryptoStandard.CADES);

        return outputStream.ToArray();
    }

    private string ComputeHashBytes(byte[] content)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(content);
        return Convert.ToHexString(bytes).ToLower();
    }

    private string ExtractNameFromCertificateSubject(string subject)
    {
        // Extrair o CN (Common Name) do subject do certificado
        // Formato típico: CN=NOME COMPLETO:12345678901, OU=...
        var cnMatch = Regex.Match(subject, @"CN=([^,]+)");
        if (cnMatch.Success)
        {
            var cn = cnMatch.Groups[1].Value.Trim();
            // Remover CPF se presente (formato: NOME:12345678901)
            var colonIndex = cn.IndexOf(':');
            if (colonIndex > 0)
            {
                cn = cn.Substring(0, colonIndex);
            }
            return cn;
        }
        return subject;
    }

    private string? ExtractCpfFromCertificate(X509Certificate2 certificate)
    {
        // CPF pode estar no Subject ou em extensões OID específicas
        // Padrão ICP-Brasil: OID 2.16.76.1.3.1 (dados PF)
        var subject = certificate.Subject;
        
        // Tentar extrair CPF do campo CN ou de extensões
        var cpfMatch = Regex.Match(subject, @":(\d{11})");
        if (cpfMatch.Success)
        {
            return FormatCpf(cpfMatch.Groups[1].Value);
        }

        // Tentar OID ICP-Brasil
        try
        {
            foreach (var extension in certificate.Extensions)
            {
                if (extension.Oid?.Value == "2.16.76.1.3.1")
                {
                    var data = extension.RawData;
                    var text = Encoding.UTF8.GetString(data);
                    var match = Regex.Match(text, @"\d{11}");
                    if (match.Success)
                    {
                        return FormatCpf(match.Value);
                    }
                }
            }
        }
        catch { /* Ignorar erros de parsing */ }

        return null;
    }

    private string? ExtractNameFromCertificate(X509Certificate2 certificate)
    {
        var subject = certificate.Subject;
        var cnMatch = Regex.Match(subject, @"CN=([^,]+)");
        if (cnMatch.Success)
        {
            var cn = cnMatch.Groups[1].Value;
            // Remover CPF se estiver no CN
            cn = Regex.Replace(cn, @":\d{11}$", "");
            return cn.Trim();
        }
        return null;
    }

    private string FormatCpf(string cpf)
    {
        if (cpf.Length == 11)
        {
            return $"{cpf.Substring(0, 3)}.{cpf.Substring(3, 3)}.{cpf.Substring(6, 3)}-{cpf.Substring(9, 2)}";
        }
        return cpf;
    }

    private (string EncryptedData, string IV) EncryptData(string data, string? existingIV = null)
    {
        using var aes = Aes.Create();
        aes.Key = DeriveKey(_encryptionKey);
        
        if (!string.IsNullOrEmpty(existingIV))
        {
            aes.IV = Convert.FromBase64String(existingIV);
        }
        else
        {
            aes.GenerateIV();
        }

        using var encryptor = aes.CreateEncryptor();
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var encryptedBytes = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);

        return (Convert.ToBase64String(encryptedBytes), Convert.ToBase64String(aes.IV));
    }

    private string DecryptData(string encryptedData, string iv)
    {
        using var aes = Aes.Create();
        aes.Key = DeriveKey(_encryptionKey);
        aes.IV = Convert.FromBase64String(iv);

        using var decryptor = aes.CreateDecryptor();
        var encryptedBytes = Convert.FromBase64String(encryptedData);
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private byte[] DeriveKey(string key)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
    }

    private string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLower();
    }

    private static DigitalCertificateDto MapToDto(DigitalCertificate entity)
    {
        return new DigitalCertificateDto
        {
            Id = entity.Id,
            DisplayName = entity.DisplayName,
            Subject = entity.Subject,
            Issuer = entity.Issuer,
            Thumbprint = entity.Thumbprint,
            CpfFromCertificate = entity.CpfFromCertificate,
            NameFromCertificate = entity.NameFromCertificate,
            ExpirationDate = entity.ExpirationDate,
            IssuedDate = entity.IssuedDate,
            QuickUseEnabled = entity.QuickUseEnabled,
            IsActive = entity.IsActive,
            LastUsedAt = entity.LastUsedAt,
            CreatedAt = entity.CreatedAt
        };
    }
}
