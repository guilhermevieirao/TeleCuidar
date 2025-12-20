using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Tests.Unit.Domain.Entities;

public class AppointmentTests
{
    [Fact]
    public void Appointment_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var appointment = new Appointment();

        // Assert
        appointment.Id.Should().NotBeEmpty();
        appointment.PatientId.Should().BeEmpty();
        appointment.ProfessionalId.Should().BeEmpty();
        appointment.SpecialtyId.Should().BeEmpty();
        appointment.Attachments.Should().NotBeNull().And.BeEmpty();
        appointment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(AppointmentStatus.Scheduled)]
    [InlineData(AppointmentStatus.Confirmed)]
    [InlineData(AppointmentStatus.InProgress)]
    [InlineData(AppointmentStatus.Completed)]
    [InlineData(AppointmentStatus.Cancelled)]
    public void Appointment_ShouldAcceptAllValidStatuses(AppointmentStatus status)
    {
        // Arrange
        var appointment = new Appointment();

        // Act
        appointment.Status = status;

        // Assert
        appointment.Status.Should().Be(status);
    }

    [Theory]
    [InlineData(AppointmentType.FirstVisit)]
    [InlineData(AppointmentType.Return)]
    [InlineData(AppointmentType.Routine)]
    [InlineData(AppointmentType.Emergency)]
    [InlineData(AppointmentType.Common)]
    public void Appointment_ShouldAcceptAllValidTypes(AppointmentType type)
    {
        // Arrange
        var appointment = new Appointment();

        // Act
        appointment.Type = type;

        // Assert
        appointment.Type.Should().Be(type);
    }

    [Fact]
    public void Appointment_ShouldSetDateAndTimeCorrectly()
    {
        // Arrange
        var appointment = new Appointment();
        var date = new DateTime(2025, 12, 25);
        var time = new TimeSpan(14, 30, 0);
        var endTime = new TimeSpan(15, 0, 0);

        // Act
        appointment.Date = date;
        appointment.Time = time;
        appointment.EndTime = endTime;

        // Assert
        appointment.Date.Should().Be(date);
        appointment.Time.Should().Be(time);
        appointment.EndTime.Should().Be(endTime);
    }

    [Fact]
    public void Appointment_ShouldAllowNullableFields()
    {
        // Arrange
        var appointment = new Appointment();

        // Assert
        appointment.Observation.Should().BeNull();
        appointment.MeetLink.Should().BeNull();
        appointment.PreConsultationJson.Should().BeNull();
        appointment.BiometricsJson.Should().BeNull();
        appointment.AttachmentsChatJson.Should().BeNull();
        appointment.EndTime.Should().BeNull();
    }

    [Fact]
    public void Appointment_ShouldSetAIGeneratedData()
    {
        // Arrange
        var appointment = new Appointment();
        var summary = "Resumo da consulta gerado por IA";
        var diagnosis = "Hipótese diagnóstica";
        var summaryDate = DateTime.UtcNow;
        var diagnosisDate = DateTime.UtcNow;

        // Act
        appointment.AISummary = summary;
        appointment.AISummaryGeneratedAt = summaryDate;
        appointment.AIDiagnosticHypothesis = diagnosis;
        appointment.AIDiagnosisGeneratedAt = diagnosisDate;

        // Assert
        appointment.AISummary.Should().Be(summary);
        appointment.AISummaryGeneratedAt.Should().Be(summaryDate);
        appointment.AIDiagnosticHypothesis.Should().Be(diagnosis);
        appointment.AIDiagnosisGeneratedAt.Should().Be(diagnosisDate);
    }

    [Fact]
    public void Appointment_ShouldSetMeetLink()
    {
        // Arrange
        var appointment = new Appointment();
        var meetLink = "https://meet.google.com/abc-defg-hij";

        // Act
        appointment.MeetLink = meetLink;

        // Assert
        appointment.MeetLink.Should().Be(meetLink);
    }

    [Fact]
    public void Appointment_ShouldLinkToPatientAndProfessional()
    {
        // Arrange
        var patient = new User { Name = "Paciente", Role = UserRole.PATIENT };
        var professional = new User { Name = "Médico", Role = UserRole.PROFESSIONAL };
        var specialty = new Specialty { Name = "Cardiologia" };

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            ProfessionalId = professional.Id,
            SpecialtyId = specialty.Id,
            Patient = patient,
            Professional = professional,
            Specialty = specialty
        };

        // Assert
        appointment.PatientId.Should().Be(patient.Id);
        appointment.ProfessionalId.Should().Be(professional.Id);
        appointment.SpecialtyId.Should().Be(specialty.Id);
        appointment.Patient.Should().Be(patient);
        appointment.Professional.Should().Be(professional);
        appointment.Specialty.Should().Be(specialty);
    }
}
