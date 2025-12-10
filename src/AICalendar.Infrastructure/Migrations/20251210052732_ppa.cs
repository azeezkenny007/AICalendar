using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AICalendar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ppa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("07c1c80d-5df2-4687-bad3-b5b23698fc24"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("1c0c993e-5672-4831-86e4-781cc87bea24"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("2a058559-7a67-4bd6-a6f7-df8462f567f0"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("32b58245-f0ad-4792-969f-1c00d8ce6938"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("3852ece4-80e8-465a-821d-654b06fc3fe8"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("3c0de316-5bd7-4f0f-98b7-40e44ca22910"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("4110d7db-18f9-42be-8e0a-e71a16d9e491"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("5971ddd1-e3db-4be3-96b5-3102b90df3a9"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("5cc1673d-f2b8-4785-b7e6-f61f595251b9"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("6260b21a-ed1f-47d4-9316-af759964433e"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("67997a0c-8a05-48ba-9fa5-d92775a29743"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("97e8fd95-7141-4ae7-801b-dfba6b70257c"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("b0a92282-6d97-434c-abfd-f7f5614822ef"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("b7ea89cb-2559-488e-b5ef-eacb88081806"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("bffa99a6-ab46-45a7-bdb5-ee072272e866"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("d472a244-8aab-44f5-8fdc-d5295a979301"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("d8934afd-a4fc-40d6-9cfe-cd5718ea6a0a"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("d9d9c296-5c2e-4281-9aad-e584fe435b0b"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("e12d8cb9-04d5-44d5-84a5-b587ede2a6a8"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("fa165945-6fdd-45f8-a4bc-311b5456b6f4"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("fb24c131-58f6-48f0-9607-b489129ef266"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("fd9bbc93-11ed-4928-8a3a-9b2eb45db203"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("fec73829-637c-4dad-9a1a-3a11133fca98"));

            migrationBuilder.UpdateData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("404b34a0-93e0-4758-8ad5-4437d2caea22"),
                columns: new[] { "MerchantId", "ReceiverId" },
                values: new object[] { "Bank", "Recipient Account" });

            migrationBuilder.UpdateData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("8cf4a85b-cbaa-468f-8856-d2e468e0e3e6"),
                columns: new[] { "MerchantId", "ReceiverId" },
                values: new object[] { "Bank", "Recipient Account" });

            migrationBuilder.UpdateData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("a150d813-d704-46d0-b46b-56952201e14e"),
                columns: new[] { "MerchantId", "ReceiverId" },
                values: new object[] { "Bank", "Recipient Account" });

            migrationBuilder.InsertData(
                table: "Transactions",
                columns: new[] { "Id", "Amount", "CreatedAt", "Description", "IsDiscarded", "IsKept", "MerchantId", "ReceiverId", "TransactionDate", "Type", "UserId" },
                values: new object[,]
                {
                    { new Guid("07554c1c-5c1a-42ae-a19a-dc053042ff05"), 1789.01m, new DateTime(2025, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "MTN Airtime", false, false, "MTN Nigeria", "MTN Nigeria", new DateTime(2025, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "AirtimeTopup", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("47401c0b-23fe-4fd1-8fba-f28e8540067d"), 19012.34m, new DateTime(2025, 6, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), "Old Mutual Insurance", false, false, "Old Mutual", "Old Mutual", new DateTime(2025, 6, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), "InsurancePayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("4924eed0-f364-4bd1-8285-a1eb977053ab"), 1456.78m, new DateTime(2025, 6, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "MTN Airtime", false, false, "MTN Nigeria", "MTN Nigeria", new DateTime(2025, 6, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "AirtimeTopup", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("4cf972d2-dcae-425e-a1b0-d40746cb4453"), 19456.78m, new DateTime(2025, 10, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Old Mutual Insurance", false, false, "Old Mutual", "Old Mutual", new DateTime(2025, 10, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "InsurancePayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("4d7ff0b1-e123-4141-974e-3508bcdf12f8"), 13456.78m, new DateTime(2025, 7, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "EKEDC Electricity", false, false, "EKEDC", "EKEDC", new DateTime(2025, 7, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "BillPayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("4f82759d-647a-4667-9aa8-2dc93ee8cd89"), 12567.89m, new DateTime(2025, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Carbon Loan Repayment", false, false, "Carbon Microfinance", "Carbon Microfinance", new DateTime(2025, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "LoanRepayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("55aa21e6-a20f-41a4-99da-9688e4e92672"), 1897.65m, new DateTime(2025, 11, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "MTN Airtime", false, false, "MTN Nigeria", "MTN Nigeria", new DateTime(2025, 11, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "AirtimeTopup", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("5b810987-11e5-43c4-ae2d-0417dd3ad2e1"), 18901.23m, new DateTime(2025, 11, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Old Mutual Insurance", false, false, "Old Mutual", "Old Mutual", new DateTime(2025, 11, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "InsurancePayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("65bfb54d-f046-46d6-aac5-774ee7cd300e"), 12890.12m, new DateTime(2025, 11, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), "Carbon Loan Repayment", false, false, "Carbon Microfinance", "Carbon Microfinance", new DateTime(2025, 11, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), "LoanRepayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("7bda5d55-f502-4ac0-809f-4e5c13fc591c"), 21456.78m, new DateTime(2025, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "PiggyVest Savings", false, false, "PiggyVest", "PiggyVest", new DateTime(2025, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "SavingsContribution", new Guid("33333333-3333-3333-3333-333333333333") },
                    { new Guid("87befee3-48c3-4572-be0c-37661d4df564"), 19234.56m, new DateTime(2025, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Old Mutual Insurance", false, false, "Old Mutual", "Old Mutual", new DateTime(2025, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "InsurancePayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("adc0da95-6780-4025-815f-1820ea73ac79"), 12456.78m, new DateTime(2025, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "Carbon Loan Repayment", false, false, "Carbon Microfinance", "Carbon Microfinance", new DateTime(2025, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "LoanRepayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("b9a63988-7e72-458b-84aa-2233125b0743"), 13901.23m, new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "EKEDC Electricity", false, false, "EKEDC", "EKEDC", new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "BillPayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("c69bffec-c07a-4428-b4fe-b6a2a6dd9152"), 12678.9m, new DateTime(2025, 10, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "Carbon Loan Repayment", false, false, "Carbon Microfinance", "Carbon Microfinance", new DateTime(2025, 10, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "LoanRepayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("c7778715-0633-420c-a7a3-37e2c92387cf"), 1678.9m, new DateTime(2025, 11, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "MTN Airtime", false, false, "MTN Nigeria", "MTN Nigeria", new DateTime(2025, 11, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "AirtimeTopup", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("d1208895-4bf4-4f46-989f-161d26e62b8b"), 1678.9m, new DateTime(2025, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "MTN Airtime", false, false, "MTN Nigeria", "MTN Nigeria", new DateTime(2025, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "AirtimeTopup", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("df423671-9e45-4b15-9ee2-e8e14e70922b"), 14234.56m, new DateTime(2025, 11, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "EKEDC Electricity", false, false, "EKEDC", "EKEDC", new DateTime(2025, 11, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "BillPayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("e011e828-6819-4c2c-8bde-8affc000ad3a"), 12456.78m, new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Carbon Loan Repayment", false, false, "Carbon Microfinance", "Carbon Microfinance", new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "LoanRepayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("e4d890a8-93b1-4dc0-8707-f6bac76ae2b9"), 5213.45m, new DateTime(2025, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Online Shopping", false, false, "Jumia", "Jumia", new DateTime(2025, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "VoucherRedeem", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("e79ee3cc-c8b9-456b-8133-f0358c83abf3"), 14567.89m, new DateTime(2025, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "EKEDC Electricity", false, false, "EKEDC", "EKEDC", new DateTime(2025, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "BillPayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("e871a2ba-ee01-470d-a457-7cec2f8dd67f"), 1765.43m, new DateTime(2025, 11, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "MTN Airtime", false, false, "MTN Nigeria", "MTN Nigeria", new DateTime(2025, 11, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "AirtimeTopup", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("eb8e0992-b15b-4a65-a04a-0829fa23bff3"), 1523.45m, new DateTime(2025, 9, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "MTN Airtime", false, false, "MTN Nigeria", "MTN Nigeria", new DateTime(2025, 9, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "AirtimeTopup", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("f74ac175-db78-47c7-8f82-184e6027ec31"), 10890.12m, new DateTime(2025, 11, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), "Transfer to Salem", false, false, "Salem Ibrahim", "Salem Ibrahim", new DateTime(2025, 11, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), "TransferLocal", new Guid("44444444-4444-4444-4444-444444444444") }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 10, 5, 27, 30, 970, DateTimeKind.Utc).AddTicks(7794));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 10, 5, 27, 30, 970, DateTimeKind.Utc).AddTicks(7794));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 10, 5, 27, 30, 970, DateTimeKind.Utc).AddTicks(7794));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 10, 5, 27, 30, 970, DateTimeKind.Utc).AddTicks(7794));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("07554c1c-5c1a-42ae-a19a-dc053042ff05"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("47401c0b-23fe-4fd1-8fba-f28e8540067d"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("4924eed0-f364-4bd1-8285-a1eb977053ab"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("4cf972d2-dcae-425e-a1b0-d40746cb4453"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("4d7ff0b1-e123-4141-974e-3508bcdf12f8"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("4f82759d-647a-4667-9aa8-2dc93ee8cd89"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("55aa21e6-a20f-41a4-99da-9688e4e92672"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("5b810987-11e5-43c4-ae2d-0417dd3ad2e1"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("65bfb54d-f046-46d6-aac5-774ee7cd300e"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("7bda5d55-f502-4ac0-809f-4e5c13fc591c"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("87befee3-48c3-4572-be0c-37661d4df564"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("adc0da95-6780-4025-815f-1820ea73ac79"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("b9a63988-7e72-458b-84aa-2233125b0743"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("c69bffec-c07a-4428-b4fe-b6a2a6dd9152"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("c7778715-0633-420c-a7a3-37e2c92387cf"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("d1208895-4bf4-4f46-989f-161d26e62b8b"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("df423671-9e45-4b15-9ee2-e8e14e70922b"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("e011e828-6819-4c2c-8bde-8affc000ad3a"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("e4d890a8-93b1-4dc0-8707-f6bac76ae2b9"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("e79ee3cc-c8b9-456b-8133-f0358c83abf3"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("e871a2ba-ee01-470d-a457-7cec2f8dd67f"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("eb8e0992-b15b-4a65-a04a-0829fa23bff3"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("f74ac175-db78-47c7-8f82-184e6027ec31"));

            migrationBuilder.UpdateData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("404b34a0-93e0-4758-8ad5-4437d2caea22"),
                columns: new[] { "MerchantId", "ReceiverId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("8cf4a85b-cbaa-468f-8856-d2e468e0e3e6"),
                columns: new[] { "MerchantId", "ReceiverId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("a150d813-d704-46d0-b46b-56952201e14e"),
                columns: new[] { "MerchantId", "ReceiverId" },
                values: new object[] { null, null });

            migrationBuilder.InsertData(
                table: "Transactions",
                columns: new[] { "Id", "Amount", "CreatedAt", "Description", "IsDiscarded", "IsKept", "MerchantId", "ReceiverId", "TransactionDate", "Type", "UserId" },
                values: new object[,]
                {
                    { new Guid("07c1c80d-5df2-4687-bad3-b5b23698fc24"), 14567.89m, new DateTime(2025, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "EKEDC Electricity", false, false, "EKEDC", "EKEDC", new DateTime(2025, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "BillPayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("1c0c993e-5672-4831-86e4-781cc87bea24"), 12678.9m, new DateTime(2025, 10, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "Carbon Loan Repayment", false, false, "Carbon Microfinance", "Carbon Microfinance", new DateTime(2025, 10, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "LoanRepayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("2a058559-7a67-4bd6-a6f7-df8462f567f0"), 1523.45m, new DateTime(2025, 9, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "MTN Airtime", false, false, "MTN Nigeria", "MTN Nigeria", new DateTime(2025, 9, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "AirtimeTopup", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("32b58245-f0ad-4792-969f-1c00d8ce6938"), 12456.78m, new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Carbon Loan Repayment", false, false, "Carbon Microfinance", "Carbon Microfinance", new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "LoanRepayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("3852ece4-80e8-465a-821d-654b06fc3fe8"), 1789.01m, new DateTime(2025, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "MTN Airtime", false, false, "MTN Nigeria", "MTN Nigeria", new DateTime(2025, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "AirtimeTopup", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("3c0de316-5bd7-4f0f-98b7-40e44ca22910"), 14234.56m, new DateTime(2025, 11, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "EKEDC Electricity", false, false, "EKEDC", "EKEDC", new DateTime(2025, 11, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "BillPayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("4110d7db-18f9-42be-8e0a-e71a16d9e491"), 1678.9m, new DateTime(2025, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "MTN Airtime", false, false, "MTN Nigeria", "MTN Nigeria", new DateTime(2025, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "AirtimeTopup", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("5971ddd1-e3db-4be3-96b5-3102b90df3a9"), 12890.12m, new DateTime(2025, 11, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), "Carbon Loan Repayment", false, false, "Carbon Microfinance", "Carbon Microfinance", new DateTime(2025, 11, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), "LoanRepayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("5cc1673d-f2b8-4785-b7e6-f61f595251b9"), 10890.12m, new DateTime(2025, 11, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), "Transfer to Salem", false, false, "Salem Ibrahim", "Salem Ibrahim", new DateTime(2025, 11, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), "TransferLocal", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("6260b21a-ed1f-47d4-9316-af759964433e"), 1897.65m, new DateTime(2025, 11, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "MTN Airtime", false, false, "MTN Nigeria", "MTN Nigeria", new DateTime(2025, 11, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "AirtimeTopup", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("67997a0c-8a05-48ba-9fa5-d92775a29743"), 13901.23m, new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "EKEDC Electricity", false, false, "EKEDC", "EKEDC", new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "BillPayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("97e8fd95-7141-4ae7-801b-dfba6b70257c"), 1765.43m, new DateTime(2025, 11, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "MTN Airtime", false, false, "MTN Nigeria", "MTN Nigeria", new DateTime(2025, 11, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "AirtimeTopup", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("b0a92282-6d97-434c-abfd-f7f5614822ef"), 18901.23m, new DateTime(2025, 11, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Old Mutual Insurance", false, false, "Old Mutual", "Old Mutual", new DateTime(2025, 11, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "InsurancePayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("b7ea89cb-2559-488e-b5ef-eacb88081806"), 13456.78m, new DateTime(2025, 7, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "EKEDC Electricity", false, false, "EKEDC", "EKEDC", new DateTime(2025, 7, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "BillPayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("bffa99a6-ab46-45a7-bdb5-ee072272e866"), 12456.78m, new DateTime(2025, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "Carbon Loan Repayment", false, false, "Carbon Microfinance", "Carbon Microfinance", new DateTime(2025, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "LoanRepayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("d472a244-8aab-44f5-8fdc-d5295a979301"), 1678.9m, new DateTime(2025, 11, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "MTN Airtime", false, false, "MTN Nigeria", "MTN Nigeria", new DateTime(2025, 11, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "AirtimeTopup", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("d8934afd-a4fc-40d6-9cfe-cd5718ea6a0a"), 1456.78m, new DateTime(2025, 6, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "MTN Airtime", false, false, "MTN Nigeria", "MTN Nigeria", new DateTime(2025, 6, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "AirtimeTopup", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("d9d9c296-5c2e-4281-9aad-e584fe435b0b"), 5213.45m, new DateTime(2025, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Online Shopping", false, false, "Jumia", "Jumia", new DateTime(2025, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "VoucherRedeem", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("e12d8cb9-04d5-44d5-84a5-b587ede2a6a8"), 19234.56m, new DateTime(2025, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Old Mutual Insurance", false, false, "Old Mutual", "Old Mutual", new DateTime(2025, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "InsurancePayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("fa165945-6fdd-45f8-a4bc-311b5456b6f4"), 12567.89m, new DateTime(2025, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Carbon Loan Repayment", false, false, "Carbon Microfinance", "Carbon Microfinance", new DateTime(2025, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "LoanRepayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("fb24c131-58f6-48f0-9607-b489129ef266"), 19012.34m, new DateTime(2025, 6, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), "Old Mutual Insurance", false, false, "Old Mutual", "Old Mutual", new DateTime(2025, 6, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), "InsurancePayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("fd9bbc93-11ed-4928-8a3a-9b2eb45db203"), 19456.78m, new DateTime(2025, 10, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Old Mutual Insurance", false, false, "Old Mutual", "Old Mutual", new DateTime(2025, 10, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "InsurancePayment", new Guid("44444444-4444-4444-4444-444444444444") },
                    { new Guid("fec73829-637c-4dad-9a1a-3a11133fca98"), 21456.78m, new DateTime(2025, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "PiggyVest Savings", false, false, "PiggyVest", "PiggyVest", new DateTime(2025, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "SavingsContribution", new Guid("33333333-3333-3333-3333-333333333333") }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 10, 5, 19, 43, 995, DateTimeKind.Utc).AddTicks(3876));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 10, 5, 19, 43, 995, DateTimeKind.Utc).AddTicks(3876));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 10, 5, 19, 43, 995, DateTimeKind.Utc).AddTicks(3876));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 10, 5, 19, 43, 995, DateTimeKind.Utc).AddTicks(3876));
        }
    }
}
