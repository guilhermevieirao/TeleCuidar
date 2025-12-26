namespace Application.DTOs.Receitas;

public class PrescriptionDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid ProfessionalId { get; set; }
    public string? ProfessionalName { get; set; }
    public string? ProfessionalCrm { get; set; }
    public Guid PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientCpf { get; set; }
    public List<PrescriptionItemDto> Items { get; set; } = new();
    public bool IsSigned { get; set; }
    public string? CertificateSubject { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? DocumentHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PrescriptionItemDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Medicamento { get; set; } = string.Empty;
    public string? CodigoAnvisa { get; set; }
    public string Dosagem { get; set; } = string.Empty;
    public string Frequencia { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public string Posologia { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
}

public class CreatePrescriptionDto
{
    public Guid AppointmentId { get; set; }
    public List<PrescriptionItemDto> Items { get; set; } = new();
}

public class UpdatePrescriptionDto
{
    public List<PrescriptionItemDto> Items { get; set; } = new();
}

public class AddPrescriptionItemDto
{
    public string Medicamento { get; set; } = string.Empty;
    public string? CodigoAnvisa { get; set; }
    public string Dosagem { get; set; } = string.Empty;
    public string Frequencia { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public string Posologia { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
}

public class UpdatePrescriptionItemDto
{
    public string Medicamento { get; set; } = string.Empty;
    public string? CodigoAnvisa { get; set; }
    public string Dosagem { get; set; } = string.Empty;
    public string Frequencia { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public string Posologia { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
}

public class SignPrescriptionDto
{
    public string CertificateThumbprint { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string CertificateSubject { get; set; } = string.Empty;
}

public class PrescriptionPdfDto
{
    public string PdfBase64 { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DocumentHash { get; set; } = string.Empty;
    public bool IsSigned { get; set; }
}

public class GenerateSignedPdfDto
{
    public string PfxBase64 { get; set; } = string.Empty;
    public string PfxPassword { get; set; } = string.Empty;
}

public class SignWithSavedCertDto
{
    public Guid CertificateId { get; set; }
    public string? Password { get; set; }
}

public class MedicamentoAnvisaDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? PrincipioAtivo { get; set; }
    public string? Laboratorio { get; set; }
    public string? Apresentacao { get; set; }
    public string? Categoria { get; set; }
}
