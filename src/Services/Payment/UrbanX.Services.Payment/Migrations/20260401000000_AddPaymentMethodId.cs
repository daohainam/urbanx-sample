using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanX.Services.Payment.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodId",
                table: "Payments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethodId",
                table: "Payments");
        }
    }
}
