using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalReportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MedicalReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfessionalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DataEmissao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HistoricoClinico = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    ExameFisico = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    ExamesComplementares = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    HipoteseDiagnostica = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Cid = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Conclusao = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false),
                    Recomendacoes = table.Column<string>(type: "TEXT", maxLength: 3000, nullable: true),
                    Observacoes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DigitalSignature = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: true),
                    CertificateThumbprint = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CertificateSubject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DocumentHash = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SignedPdfBase64 = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalReports_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicalReports_Users_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalReports_Users_ProfessionalId",
                        column: x => x.ProfessionalId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalReports_AppointmentId",
                table: "MedicalReports",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalReports_DocumentHash",
                table: "MedicalReports",
                column: "DocumentHash");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalReports_PatientId",
                table: "MedicalReports",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalReports_ProfessionalId",
                table: "MedicalReports",
                column: "ProfessionalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicalReports");
        }
    }
}
