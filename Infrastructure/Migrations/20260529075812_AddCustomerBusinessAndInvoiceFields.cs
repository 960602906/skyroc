using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerBusinessAndInvoiceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bank_account",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                table: "customer",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "business_scope",
                table: "customer",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "business_term",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "establish_date",
                table: "customer",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invoice_address",
                table: "customer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invoice_email",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invoice_phone",
                table: "customer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invoice_receiver_address",
                table: "customer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invoice_receiver_name",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invoice_receiver_phone",
                table: "customer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invoice_title",
                table: "customer",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "legal_representative",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registered_address",
                table: "customer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registered_capital",
                table: "customer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registration_authority",
                table: "customer",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registration_status",
                table: "customer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "taxpayer_identification_number",
                table: "customer",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unified_social_credit_code",
                table: "customer",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_customer_taxpayer_no",
                table: "customer",
                column: "taxpayer_identification_number");

            migrationBuilder.CreateIndex(
                name: "idx_customer_uscc",
                table: "customer",
                column: "unified_social_credit_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_customer_taxpayer_no",
                table: "customer");

            migrationBuilder.DropIndex(
                name: "idx_customer_uscc",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "bank_account",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "bank_name",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "business_scope",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "business_term",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "establish_date",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "invoice_address",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "invoice_email",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "invoice_phone",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "invoice_receiver_address",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "invoice_receiver_name",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "invoice_receiver_phone",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "invoice_title",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "legal_representative",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "registered_address",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "registered_capital",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "registration_authority",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "registration_status",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "taxpayer_identification_number",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "unified_social_credit_code",
                table: "customer");
        }
    }
}
