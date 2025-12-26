using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.Kernel.Colors;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Events;
using iText.Kernel.Geom;
using System.Text.RegularExpressions;
using QRCoder;

namespace Infrastructure.Services;

/// <summary>
/// Helper para geração de PDFs padronizados da plataforma TeleCuidar
/// Segue o padrão de documento médico com cabeçalho, corpo e rodapé padronizados
/// </summary>
public static class PdfDocumentHelper
{
    // Cores padrão da plataforma
    public static readonly DeviceRgb PrimaryColor = new(37, 99, 235);      // Azul TeleCuidar
    public static readonly DeviceRgb SuccessColor = new(34, 197, 94);      // Verde sucesso
    public static readonly DeviceRgb GrayColor = new(100, 116, 139);       // Cinza texto
    public static readonly DeviceRgb LightGray = new(248, 250, 252);       // Cinza claro background
    public static readonly DeviceRgb DarkText = new(15, 23, 42);           // Texto escuro
    
    // Informações do estabelecimento
    private const string PLATFORM_NAME = "TeleCuidar";
    private const string PLATFORM_DESCRIPTION = "Plataforma de Telemedicina";
    private const string CNPJ = "00.000.000/0001-00";
    private const string ADDRESS = "Rua Exemplo, 123 - Centro - Cidade/UF - CEP: 00000-000";
    private const string PHONE = "(00) 0000-0000";
    private const string EMAIL = "contato@telecuidar.com.br";
    private const string WEBSITE = "www.telecuidar.com.br";
    private const string VALIDATION_URL = "https://validar.iti.gov.br";

    /// <summary>
    /// Gera um nome de arquivo padronizado para documentos PDF
    /// Formato: TeleCuidar_TipoDoc_NomePaciente_Data[_Sufixo].pdf
    /// </summary>
    public static string GenerateFileName(
        string documentType,
        string patientName,
        DateTime date,
        string? suffix = null,
        string? subType = null)
    {
        // Sanitizar nome do paciente (remover caracteres especiais, manter apenas letras e espaços)
        var sanitizedName = SanitizeFileName(patientName);
        
        // Formatar data
        var dateStr = date.ToString("yyyy-MM-dd");
        
        // Construir nome do arquivo
        var parts = new List<string> { "TeleCuidar", documentType };
        
        if (!string.IsNullOrEmpty(subType))
            parts.Add(SanitizeFileName(subType));
        
        parts.Add(sanitizedName);
        parts.Add(dateStr);
        
        if (!string.IsNullOrEmpty(suffix))
            parts.Add(suffix);
        
        return string.Join("_", parts) + ".pdf";
    }

    /// <summary>
    /// Sanitiza uma string para uso em nome de arquivo
    /// Remove caracteres especiais, acentos e mantém apenas alfanuméricos
    /// </summary>
    public static string SanitizeFileName(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "Desconhecido";
        
        // Remover acentos
        var normalized = input.Normalize(System.Text.NormalizationForm.FormD);
        var withoutAccents = new string(normalized.Where(c => 
            System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != 
            System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
        
        // Substituir espaços por underscores e remover caracteres inválidos
        var sanitized = Regex.Replace(withoutAccents, @"[^a-zA-Z0-9\s]", "");
        sanitized = Regex.Replace(sanitized, @"\s+", "-");
        
        // Limitar tamanho e capitalizar
        if (sanitized.Length > 30)
            sanitized = sanitized.Substring(0, 30);
        
        return sanitized.Trim('-');
    }

    /// <summary>
    /// Adiciona o cabeçalho padrão ao documento PDF
    /// </summary>
    public static void AddHeader(Document document, PdfFont boldFont, PdfFont regularFont)
    {
        // === HEADER COM LOGO ===
        var headerTable = new Table(new float[] { 1, 2 }).UseAllAvailableWidth().SetMarginBottom(10);
        
        // Logo cell (esquerda)
        var logoCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE);
        
        // Tentar carregar a logo do projeto
        bool logoLoaded = false;
        var basePaths = new[]
        {
            AppDomain.CurrentDomain.BaseDirectory,
            Directory.GetCurrentDirectory(),
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ""
        };
        
        var logoFileNames = new[] { "logo.png", "telecuidado.png", "logo.jpg", "logo.jpeg" };
        
        foreach (var basePath in basePaths)
        {
            foreach (var fileName in logoFileNames)
            {
                var logoPath = System.IO.Path.Combine(basePath, "wwwroot", "assets", fileName);
                
                try
                {
                    if (System.IO.File.Exists(logoPath))
                    {
                        var logoImage = ImageDataFactory.Create(logoPath);
                        var logo = new iText.Layout.Element.Image(logoImage).SetWidth(50).SetHeight(50);
                        logoCell.Add(logo);
                        logoLoaded = true;
                        Console.WriteLine($"[PDF] Logo carregada com sucesso: {logoPath}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PDF] Erro ao carregar logo de {logoPath}: {ex.Message}");
                }
            }
            
            if (logoLoaded) break;
        }
        
        if (!logoLoaded)
        {
            Console.WriteLine($"[PDF] Nenhuma logo encontrada. Caminhos tentados:");
            foreach (var basePath in basePaths)
            {
                Console.WriteLine($"[PDF]   Base: {basePath}");
            }
        }
        
        // Tentar converter ICO para PNG bytes se não encontrou imagem suportada
        if (!logoLoaded)
        {
            var icoPaths = new[]
            {
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "assets", "logo.ico"),
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "assets", "favicon.ico")
            };
            
            foreach (var icoPath in icoPaths)
            {
                try
                {
                    if (System.IO.File.Exists(icoPath))
                    {
                        // Tentar carregar ICO diretamente (algumas versões do iText suportam)
                        var icoBytes = System.IO.File.ReadAllBytes(icoPath);
                        var logoImage = ImageDataFactory.Create(icoBytes);
                        var logo = new iText.Layout.Element.Image(logoImage).SetWidth(50).SetHeight(50);
                        logoCell.Add(logo);
                        logoLoaded = true;
                        break;
                    }
                }
                catch
                {
                    // ICO não suportado, continuar
                }
            }
        }
        
        if (!logoLoaded)
        {
            // Fallback: criar logo estilizado com bordas
            var logoContainer = new Table(1).SetWidth(55).SetMarginBottom(5);
            var logoInnerCell = new Cell()
                .SetBorder(new iText.Layout.Borders.SolidBorder(PrimaryColor, 2))
                .SetBorderRadius(new iText.Layout.Properties.BorderRadius(8))
                .SetBackgroundColor(new DeviceRgb(239, 246, 255)) // azul claro
                .SetPadding(8)
                .SetTextAlignment(TextAlignment.CENTER);
            
            logoInnerCell.Add(new Paragraph("TC")
                .SetFont(boldFont)
                .SetFontSize(22)
                .SetFontColor(PrimaryColor)
                .SetMarginBottom(0));
            
            logoContainer.AddCell(logoInnerCell);
            logoCell.Add(logoContainer);
        }
        
        // Adiciona nome e descrição da plataforma abaixo do logo
        logoCell.Add(new Paragraph(PLATFORM_NAME)
            .SetFont(boldFont)
            .SetFontSize(16)
            .SetFontColor(PrimaryColor)
            .SetMarginTop(5)
            .SetMarginBottom(0));
        logoCell.Add(new Paragraph(PLATFORM_DESCRIPTION)
            .SetFont(regularFont)
            .SetFontSize(9)
            .SetFontColor(GrayColor)
            .SetMarginBottom(0));
        
        headerTable.AddCell(logoCell);
        
        // Informações do estabelecimento (direita)
        var infoCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetVerticalAlignment(VerticalAlignment.MIDDLE);
        infoCell.Add(new Paragraph($"CNPJ: {CNPJ}")
            .SetFont(regularFont)
            .SetFontSize(8)
            .SetFontColor(GrayColor)
            .SetMarginBottom(2));
        infoCell.Add(new Paragraph(ADDRESS)
            .SetFont(regularFont)
            .SetFontSize(8)
            .SetFontColor(GrayColor)
            .SetMarginBottom(2));
        infoCell.Add(new Paragraph($"Tel: {PHONE} | {EMAIL}")
            .SetFont(regularFont)
            .SetFontSize(8)
            .SetFontColor(GrayColor)
            .SetMarginBottom(2));
        infoCell.Add(new Paragraph(WEBSITE)
            .SetFont(regularFont)
            .SetFontSize(8)
            .SetFontColor(PrimaryColor)
            .SetMarginBottom(0));
        
        headerTable.AddCell(infoCell);
        document.Add(headerTable);
        
        // Linha separadora
        document.Add(new Paragraph("")
            .SetBorderBottom(new iText.Layout.Borders.SolidBorder(PrimaryColor, 2))
            .SetMarginBottom(15));
    }

    /// <summary>
    /// Adiciona o título do documento
    /// </summary>
    public static void AddDocumentTitle(Document document, string title, string? subtitle, DateTime emissionDate, PdfFont boldFont, PdfFont regularFont)
    {
        // Título principal
        document.Add(new Paragraph("DOCUMENTO MÉDICO")
            .SetFont(boldFont)
            .SetFontSize(10)
            .SetFontColor(GrayColor)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(5));
        
        // Subtítulo específico
        document.Add(new Paragraph(title.ToUpperInvariant())
            .SetFont(boldFont)
            .SetFontSize(18)
            .SetFontColor(PrimaryColor)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(5));
        
        if (!string.IsNullOrEmpty(subtitle))
        {
            document.Add(new Paragraph(subtitle)
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetFontColor(GrayColor)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(5));
        }
        
        // Data de emissão
        var localDate = TimeZoneInfo.ConvertTimeFromUtc(emissionDate, 
            TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));
        document.Add(new Paragraph($"Data de Emissão: {localDate:dd/MM/yyyy 'às' HH:mm}")
            .SetFont(regularFont)
            .SetFontSize(10)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFontColor(DarkText)
            .SetMarginBottom(20));
    }

    /// <summary>
    /// Adiciona a seção de dados do paciente e profissional
    /// </summary>
    public static void AddPatientAndProfessionalInfo(
        Document document,
        PdfFont boldFont,
        PdfFont regularFont,
        string patientName,
        string? patientCpf,
        string? patientCns = null,
        DateTime? patientBirthDate = null,
        string? professionalName = null,
        string? professionalCrm = null,
        string? professionalUf = null,
        string? professionalEmail = null,
        string? professionalPhone = null)
    {
        var infoTable = new Table(2).UseAllAvailableWidth().SetMarginBottom(20);
        
        // Célula do paciente
        var patientCell = new Cell()
            .SetBackgroundColor(LightGray)
            .SetPadding(12)
            .SetBorder(new iText.Layout.Borders.SolidBorder(new DeviceRgb(226, 232, 240), 1));
        
        patientCell.Add(new Paragraph("DADOS DO PACIENTE")
            .SetFont(boldFont)
            .SetFontSize(10)
            .SetFontColor(PrimaryColor)
            .SetMarginBottom(8));
        
        patientCell.Add(new Paragraph($"Nome: {patientName}")
            .SetFont(regularFont)
            .SetFontSize(10)
            .SetFontColor(DarkText)
            .SetMarginBottom(3));
        
        if (!string.IsNullOrEmpty(patientCpf))
        {
            patientCell.Add(new Paragraph($"CPF: {patientCpf}")
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetFontColor(DarkText)
                .SetMarginBottom(3));
        }
        
        if (!string.IsNullOrEmpty(patientCns))
        {
            patientCell.Add(new Paragraph($"CNS: {patientCns}")
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetFontColor(DarkText)
                .SetMarginBottom(3));
        }
        
        if (patientBirthDate.HasValue)
        {
            patientCell.Add(new Paragraph($"Data de Nascimento: {patientBirthDate.Value:dd/MM/yyyy}")
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetFontColor(DarkText));
        }
        
        infoTable.AddCell(patientCell);
        
        // Célula do profissional
        var professionalCell = new Cell()
            .SetBackgroundColor(LightGray)
            .SetPadding(12)
            .SetBorder(new iText.Layout.Borders.SolidBorder(new DeviceRgb(226, 232, 240), 1));
        
        professionalCell.Add(new Paragraph("DADOS DO PROFISSIONAL")
            .SetFont(boldFont)
            .SetFontSize(10)
            .SetFontColor(PrimaryColor)
            .SetMarginBottom(8));
        
        if (!string.IsNullOrEmpty(professionalName))
        {
            professionalCell.Add(new Paragraph($"Nome: {professionalName}")
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetFontColor(DarkText)
                .SetMarginBottom(3));
        }
        
        if (!string.IsNullOrEmpty(professionalCrm))
        {
            var crmText = $"CRM: {professionalCrm}";
            if (!string.IsNullOrEmpty(professionalUf))
                crmText += $" - {professionalUf}";
            
            professionalCell.Add(new Paragraph(crmText)
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetFontColor(DarkText)
                .SetMarginBottom(3));
        }
        
        if (!string.IsNullOrEmpty(professionalEmail))
        {
            professionalCell.Add(new Paragraph($"E-mail: {professionalEmail}")
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetFontColor(DarkText)
                .SetMarginBottom(3));
        }
        
        if (!string.IsNullOrEmpty(professionalPhone))
        {
            professionalCell.Add(new Paragraph($"Telefone: {professionalPhone}")
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetFontColor(DarkText));
        }
        
        infoTable.AddCell(professionalCell);
        document.Add(infoTable);
    }

    /// <summary>
    /// Adiciona a assinatura digital ao documento
    /// </summary>
    public static void AddDigitalSignature(
        Document document,
        PdfFont boldFont,
        PdfFont regularFont,
        string signerName,
        DateTime signedAt,
        string? certificateIdentifier = null)
    {
        document.Add(new Paragraph("").SetMarginTop(20));
        
        var signedBadge = new Table(1).UseAllAvailableWidth();
        var badgeCell = new Cell()
            .SetBackgroundColor(new DeviceRgb(220, 252, 231))
            .SetBorder(new iText.Layout.Borders.SolidBorder(SuccessColor, 1))
            .SetPadding(12)
            .SetTextAlignment(TextAlignment.CENTER);
        
        badgeCell.Add(new Paragraph("✓ DOCUMENTO ASSINADO DIGITALMENTE")
            .SetFont(boldFont)
            .SetFontSize(11)
            .SetFontColor(SuccessColor)
            .SetMarginBottom(5));
        
        badgeCell.Add(new Paragraph($"Assinante: {signerName}")
            .SetFont(regularFont)
            .SetFontSize(9)
            .SetFontColor(GrayColor)
            .SetMarginBottom(2));
        
        if (!string.IsNullOrEmpty(certificateIdentifier))
        {
            badgeCell.Add(new Paragraph($"Identificador: {certificateIdentifier}")
                .SetFont(regularFont)
                .SetFontSize(9)
                .SetFontColor(GrayColor)
                .SetMarginBottom(2));
        }
        
        var signedAtLocal = TimeZoneInfo.ConvertTimeFromUtc(signedAt, 
            TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));
        badgeCell.Add(new Paragraph($"Data/Hora: {signedAtLocal:dd/MM/yyyy 'às' HH:mm:ss}")
            .SetFont(regularFont)
            .SetFontSize(9)
            .SetFontColor(GrayColor));
        
        signedBadge.AddCell(badgeCell);
        document.Add(signedBadge);
    }

    /// <summary>
    /// Adiciona a área de assinatura física do profissional (para impressão)
    /// </summary>
    public static void AddPhysicalSignatureArea(
        Document document,
        PdfFont boldFont,
        PdfFont regularFont,
        string professionalName,
        string? crm = null,
        string? uf = null)
    {
        var signatureTable = new Table(1).UseAllAvailableWidth().SetMarginTop(30);
        var signatureCell = new Cell()
            .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
            .SetTextAlignment(TextAlignment.CENTER);
        
        signatureCell.Add(new Paragraph("_______________________________")
            .SetFont(regularFont)
            .SetFontSize(12)
            .SetMarginBottom(5));
        signatureCell.Add(new Paragraph(professionalName)
            .SetFont(boldFont)
            .SetFontSize(12)
            .SetFontColor(DarkText)
            .SetMarginBottom(2));
        
        if (!string.IsNullOrEmpty(crm))
        {
            var crmText = $"CRM: {crm}";
            if (!string.IsNullOrEmpty(uf))
                crmText += $" - {uf}";
            
            signatureCell.Add(new Paragraph(crmText)
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetFontColor(GrayColor));
        }
        
        signatureTable.AddCell(signatureCell);
        document.Add(signatureTable);
    }

    /// <summary>
    /// Adiciona a seção completa de assinaturas (física + digital se aplicável)
    /// Este é o método padronizado que deve ser usado por todos os documentos
    /// </summary>
    public static void AddSignatureSection(
        Document document,
        PdfFont boldFont,
        PdfFont regularFont,
        string professionalName,
        string? crm = null,
        string? uf = null,
        bool isSigned = false,
        string? signerName = null,
        DateTime? signedAt = null,
        string? certificateIdentifier = null)
    {
        // 1. Área de assinatura física (sempre presente para permitir impressão)
        AddPhysicalSignatureArea(document, boldFont, regularFont, professionalName, crm, uf);
        
        // 2. Badge de assinatura digital (apenas se o documento foi assinado digitalmente)
        if (isSigned && signedAt.HasValue)
        {
            var signer = signerName ?? professionalName;
            AddDigitalSignature(document, boldFont, regularFont, signer, signedAt.Value, certificateIdentifier);
        }
    }

    /// <summary>
    /// Adiciona o rodapé padrão com QR Code e informações de validação
    /// </summary>
    public static void AddFooter(
        Document document,
        PdfFont boldFont,
        PdfFont regularFont,
        string documentHash,
        DateTime generatedAt)
    {
        // Linha separadora
        document.Add(new Paragraph("")
            .SetBorderTop(new iText.Layout.Borders.SolidBorder(PrimaryColor, 2))
            .SetMarginTop(25)
            .SetMarginBottom(15));
        
        // Tabela do rodapé com QR Code
        var footerTable = new Table(new float[] { 1, 3 }).UseAllAvailableWidth();
        
        // QR Code (esquerda)
        var qrCell = new Cell()
            .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
            .SetPadding(10)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);
        
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(VALIDATION_URL, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrBytes = qrCode.GetGraphic(5);
            
            var qrImage = ImageDataFactory.Create(qrBytes);
            var qr = new iText.Layout.Element.Image(qrImage).SetWidth(80).SetHeight(80);
            qrCell.Add(qr);
        }
        catch
        {
            qrCell.Add(new Paragraph("[QR Code]")
                .SetFont(regularFont)
                .SetFontSize(8)
                .SetFontColor(GrayColor)
                .SetTextAlignment(TextAlignment.CENTER));
        }
        
        footerTable.AddCell(qrCell);
        
        // Informações de validação (direita)
        var validationCell = new Cell()
            .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
            .SetPadding(10)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);
        
        validationCell.Add(new Paragraph("VALIDAÇÃO DO DOCUMENTO")
            .SetFont(boldFont)
            .SetFontSize(10)
            .SetFontColor(PrimaryColor)
            .SetMarginBottom(5));
        
        validationCell.Add(new Paragraph($"Hash de Validação: {documentHash}")
            .SetFont(regularFont)
            .SetFontSize(8)
            .SetFontColor(GrayColor)
            .SetMarginBottom(3));
        
        validationCell.Add(new Paragraph($"Escaneie o QR Code ou acesse {VALIDATION_URL}")
            .SetFont(regularFont)
            .SetFontSize(8)
            .SetFontColor(GrayColor)
            .SetMarginBottom(2));
        
        validationCell.Add(new Paragraph("para validar a assinatura digital deste documento.")
            .SetFont(regularFont)
            .SetFontSize(8)
            .SetFontColor(GrayColor)
            .SetMarginBottom(5));
        
        var generatedAtLocal = TimeZoneInfo.ConvertTimeFromUtc(generatedAt, 
            TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));
        validationCell.Add(new Paragraph($"Documento gerado em: {generatedAtLocal:dd/MM/yyyy 'às' HH:mm:ss}")
            .SetFont(regularFont)
            .SetFontSize(8)
            .SetFontColor(GrayColor));
        
        footerTable.AddCell(validationCell);
        document.Add(footerTable);
        
        // Disclaimer final
        document.Add(new Paragraph("Este documento foi gerado eletronicamente pela plataforma TeleCuidar e possui validade jurídica conforme legislação vigente.")
            .SetFont(regularFont)
            .SetFontSize(7)
            .SetFontColor(GrayColor)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(10));
    }

    /// <summary>
    /// Obtém a fonte Bold padrão
    /// </summary>
    public static PdfFont GetBoldFont() => PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

    /// <summary>
    /// Obtém a fonte Regular padrão
    /// </summary>
    public static PdfFont GetRegularFont() => PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

    /// <summary>
    /// Adiciona paginação a um PDF já gerado (pós-processamento)
    /// Recebe os bytes do PDF e retorna um novo PDF com números de página
    /// </summary>
    public static byte[] AddPageNumbersToBytes(byte[] pdfBytes)
    {
        using var inputStream = new MemoryStream(pdfBytes);
        using var outputStream = new MemoryStream();
        
        using var reader = new PdfReader(inputStream);
        using var pdfDoc = new PdfDocument(reader, new PdfWriter(outputStream));
        
        var totalPages = pdfDoc.GetNumberOfPages();
        var font = GetRegularFont();
        
        for (int i = 1; i <= totalPages; i++)
        {
            var page = pdfDoc.GetPage(i);
            var pageSize = page.GetPageSize();
            
            var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(page);
            canvas.BeginText()
                .SetFontAndSize(font, 8)
                .SetColor(GrayColor, true)
                .MoveText(pageSize.GetWidth() - 50, 20)
                .ShowText($"{i}/{totalPages}")
                .EndText();
        }
        
        pdfDoc.Close();
        return outputStream.ToArray();
    }

    /// <summary>
    /// Adiciona paginação ao documento (deve ser chamado antes de fechar o documento)
    /// DEPRECATED: Use AddPageNumbersToBytes para pós-processamento
    /// </summary>
    public static void AddPageNumbers(PdfDocument pdfDoc)
    {
        var totalPages = pdfDoc.GetNumberOfPages();
        var font = GetRegularFont();
        
        for (int i = 1; i <= totalPages; i++)
        {
            var page = pdfDoc.GetPage(i);
            var pageSize = page.GetPageSize();
            
            var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(page);
            canvas.BeginText()
                .SetFontAndSize(font, 8)
                .MoveText(pageSize.GetWidth() - 60, 20)
                .ShowText($"{i}/{totalPages}")
                .EndText();
        }
    }

    /// <summary>
    /// Registra o handler de paginação para adicionar números automaticamente
    /// </summary>
    public static PageNumberEventHandler RegisterPageNumberHandler(PdfDocument pdfDoc)
    {
        var handler = new PageNumberEventHandler(pdfDoc);
        pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, handler);
        return handler;
    }
}

/// <summary>
/// Event handler para adicionar números de página no canto inferior direito
/// </summary>
public class PageNumberEventHandler : IEventHandler
{
    private readonly PdfDocument _pdfDoc;
    private readonly PdfFont _font;

    public PageNumberEventHandler(PdfDocument pdfDoc)
    {
        _pdfDoc = pdfDoc;
        _font = PdfDocumentHelper.GetRegularFont();
    }

    public void HandleEvent(Event @event)
    {
        var docEvent = (PdfDocumentEvent)@event;
        var page = docEvent.GetPage();
        var pageSize = page.GetPageSize();
        var pageNumber = _pdfDoc.GetPageNumber(page);
        
        var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(page);
        canvas.BeginText()
            .SetFontAndSize(_font, 8)
            .SetColor(PdfDocumentHelper.GrayColor, true)
            .MoveText(pageSize.GetWidth() - 60, 20)
            .ShowText($"{pageNumber}/")
            .EndText();
    }

    /// <summary>
    /// Atualiza todos os números de página com o total correto (chamar após fechar o Document)
    /// </summary>
    public void UpdateTotalPages()
    {
        var totalPages = _pdfDoc.GetNumberOfPages();
        var font = PdfDocumentHelper.GetRegularFont();
        
        for (int i = 1; i <= totalPages; i++)
        {
            var page = _pdfDoc.GetPage(i);
            var pageSize = page.GetPageSize();
            
            // Adicionar o total depois da barra
            var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(page);
            canvas.BeginText()
                .SetFontAndSize(font, 8)
                .SetColor(PdfDocumentHelper.GrayColor, true)
                .MoveText(pageSize.GetWidth() - 40, 20)
                .ShowText($"{totalPages}")
                .EndText();
        }
    }
}
