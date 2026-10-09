using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAW.Migrations
{
    /// <inheritdoc />
    public partial class sequence_Guias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "SeqGuiaEntrada");

            migrationBuilder.CreateSequence<int>(
                name: "SeqGuiaSalida");

            migrationBuilder.CreateSequence<int>(
                name: "SeqGuiaTraslado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "SeqGuiaEntrada");

            migrationBuilder.DropSequence(
                name: "SeqGuiaSalida");

            migrationBuilder.DropSequence(
                name: "SeqGuiaTraslado");
        }
    }
}
