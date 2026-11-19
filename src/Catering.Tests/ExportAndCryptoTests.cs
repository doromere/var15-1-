using Catering.Core.Common;
using Catering.Core.Models;
using Catering.Core.Services;
using Xunit;

namespace Catering.Tests;

public class ExportAndCryptoTests
{
    [Fact]
    public void Sha256_ComputeHash_MatchesKnownTestVector()
    {
        // RFC 6234 / NIST: SHA256("abc") = ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad
        Assert.Equal(
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            Sha256Helper.ComputeHash("abc"));
    }

    [Fact]
    public void CanonicalString_HasDocumentFieldsInOrder()
    {
        string canonical = Sha256Helper.BuildCanonicalString(
            "ПР23-КТР-2026-00042",
            new DateTime(2026, 6, 12, 12, 30, 0, DateTimeKind.Unspecified),
            "ООО «Вектор Медиа»",
            336_250.00m,
            4);

        Assert.Equal(
            "ПР23-КТР-2026-00042|2026-06-12 12:30:00|ООО «Вектор Медиа»|336250.00|4",
            canonical);
    }

    [Fact]
    public void DocumentSignature_IsStable_AndPrintedIntoExport()
    {
        CateringContract contract = BuildContract();
        string signature = Sha256Helper.ComputeDocumentSignature(
            contract.ContractNumber, contract.EventDatetime,
            contract.CateringClient!.ClientName, contract.TotalBudget, contract.MenuItems.Count);

        var export = new DocumentExportService();
        string text = export.BuildEstimateText(contract);

        Assert.Contains("ПОДПИСЬ SHA256: " + signature, text);
        Assert.True(DocumentNumberGenerator.IsValid(contract.ContractNumber));
    }

    [Fact]
    public void Export_ToTxtAndCsv_WritesSignatureFooter()
    {
        CateringContract contract = BuildContract();
        var export = new DocumentExportService();

        string dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            string txtPath = Path.Combine(dir, "smeta.txt");
            string csvPath = Path.Combine(dir, "smeta.csv");
            string htmlPath = Path.Combine(dir, "smeta.html");

            export.Export(contract, txtPath);
            export.Export(contract, csvPath);
            export.Export(contract, htmlPath);

            Assert.Contains("SHA256:", File.ReadAllText(txtPath));
            Assert.Contains("ПР23-КТР-", File.ReadAllText(csvPath));
            Assert.Contains("SHA256", File.ReadAllText(htmlPath));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void DocumentNumber_FollowsMask_Pr23KtrYearD5()
    {
        string number = DocumentNumberGenerator.Generate(2026, 42);
        Assert.Equal("ПР23-КТР-2026-00042", number);
        Assert.True(DocumentNumberGenerator.IsValid(number));
        Assert.False(DocumentNumberGenerator.IsValid("ИНВ-2026-00042"));
        Assert.False(DocumentNumberGenerator.IsValid("ПР23-КТР-2026-42"));
    }

    [Fact]
    public void CancelReason_Validator_RequiresTenCharacters()
    {
        Assert.False(InputValidator.IsValidCancelReason("9 симв.!!"));
        Assert.True(InputValidator.IsValidCancelReason("Клиент отменил по семейным обстоятельствам"));
    }

    [Fact]
    public void Phone_Validator_AcceptsRussianFormats()
    {
        Assert.True(InputValidator.IsValidPhone("+7 (384) 221-30-40"));
        Assert.True(InputValidator.IsValidPhone("8 (913) 455-67-21"));
        Assert.False(InputValidator.IsValidPhone("12345"));
    }

    private static CateringContract BuildContract() => new()
    {
        Id = 7,
        ContractNumber = "ПР23-КТР-2026-00042",
        UserId = 1,
        CateringClientId = 1,
        EventTypeId = 1,
        EventDatetime = new DateTime(2026, 6, 12, 12, 30, 0, DateTimeKind.Unspecified),
        GuestsCount = 30,
        TotalBudget = 33_625.00m,
        Status = ContractStatuses.Approved,
        CateringClient = new CateringClient
        {
            Id = 1,
            ClientName = "ООО «Вектор Медиа»",
            Phone = "+7 (384) 221-30-40",
            EventLocation = "Площадь Советская, 12 (открытая площадка)",
            IsVip = false,
        },
        EventType = new EventType { Id = 1, TypeTitle = "Свадьба", ServiceMultiplier = 1.25m, MinGuests = 30 },
        User = new User { Id = 1, FullName = "Иванов Сергей Петрович" },
        MenuItems =
        {
            new ContractMenuItem
            {
                Id = 1,
                ContractId = 7,
                BanquetDishId = 1,
                ServingsCount = 20,
                EquipmentRentFee = 5_000m,
                LineCost = 8_700.00m,
                BanquetDish = new BanquetDish { Id = 1, DishTitle = "Брускетта с лососем", Category = "Холодные закуски", PortionWeightG = 90, CostPerPerson = 185.00m },
            },
            new ContractMenuItem
            {
                Id = 2,
                ContractId = 7,
                BanquetDishId = 3,
                ServingsCount = 10,
                EquipmentRentFee = 3_000m,
                LineCost = 6_200.00m,
                BanquetDish = new BanquetDish { Id = 3, DishTitle = "Тартар из тунца", Category = "Холодные закуски", PortionWeightG = 110, CostPerPerson = 320.00m },
            },
        },
    };
}
