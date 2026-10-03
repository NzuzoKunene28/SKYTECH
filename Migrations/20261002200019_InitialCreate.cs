using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SkyTech.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    BranchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BranchLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    RadiusMeters = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.BranchId);
                });

            migrationBuilder.CreateTable(
                name: "CreditRecords",
                columns: table => new
                {
                    NationalId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreditScore = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditRecords", x => x.NationalId);
                });

            migrationBuilder.CreateTable(
                name: "CriminalRecords",
                columns: table => new
                {
                    NationalId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClearanceStatus = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriminalRecords", x => x.NationalId);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegisteredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TestWritten = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerId);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryDrivers",
                columns: table => new
                {
                    DriverId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DriverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DriverSurname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResidentialAddress = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LicenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VehicleType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BankType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    ApplicationStatus = table.Column<int>(type: "int", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegisteredOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryDrivers", x => x.DriverId);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MinimumStaffRequired = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "HRs",
                columns: table => new
                {
                    HRId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Firstname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRs", x => x.HRId);
                });

            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                columns: table => new
                {
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveTypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequiresDocument = table.Column<bool>(type: "bit", nullable: false),
                    MinimumNoticeDays = table.Column<int>(type: "int", nullable: false),
                    MaximumConsecutiveDays = table.Column<int>(type: "int", nullable: true),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    RequiresBalance = table.Column<bool>(type: "bit", nullable: false),
                    CycleMonths = table.Column<int>(type: "int", nullable: true),
                    MinimumEmploymentMonths = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.LeaveTypeId);
                });

            migrationBuilder.CreateTable(
                name: "PublicHolidays",
                columns: table => new
                {
                    PublicHolidayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HolidayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HolidayDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicHolidays", x => x.PublicHolidayId);
                });

            migrationBuilder.CreateTable(
                name: "RepairItemCategories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairItemCategories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "SystemAdmins",
                columns: table => new
                {
                    AdminId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegisteredOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAdmins", x => x.AdminId);
                });

            migrationBuilder.CreateTable(
                name: "Rosters",
                columns: table => new
                {
                    RosterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    WeekStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WeekEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rosters", x => x.RosterId);
                    table.ForeignKey(
                        name: "FK_Rosters_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shopowners",
                columns: table => new
                {
                    OwnerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    IdBookNumber = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    Names = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResidentialAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BankType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShopAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShopName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationStatus = table.Column<int>(type: "int", nullable: false),
                    RegisteredOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shopowners", x => x.OwnerId);
                    table.ForeignKey(
                        name: "FK_shopowners_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Carts",
                columns: table => new
                {
                    CartId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCheckedOut = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.CartId);
                    table.ForeignKey(
                        name: "FK_Carts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanApplications",
                columns: table => new
                {
                    LoanApplicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MonthlySalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProofOfIncomeDocPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: true),
                    IsEmployed = table.Column<bool>(type: "bit", nullable: false),
                    CreditScoreAtApplication = table.Column<int>(type: "int", nullable: false),
                    ClearanceStatusAtApplication = table.Column<int>(type: "int", nullable: false),
                    EligibilityResult = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanApplications", x => x.LoanApplicationId);
                    table.ForeignKey(
                        name: "FK_LoanApplications_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanApplications_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverIdentityBooks",
                columns: table => new
                {
                    DriverIdentityBookId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SavedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverIdentityBooks", x => x.DriverIdentityBookId);
                    table.ForeignKey(
                        name: "FK_DriverIdentityBooks_DeliveryDrivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DeliveryDrivers",
                        principalColumn: "DriverId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriversLicences",
                columns: table => new
                {
                    LicenceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SavedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriversLicences", x => x.LicenceId);
                    table.ForeignKey(
                        name: "FK_DriversLicences_DeliveryDrivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DeliveryDrivers",
                        principalColumn: "DriverId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProposeItems",
                columns: table => new
                {
                    PropItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    AssignedDriverId = table.Column<int>(type: "int", nullable: true),
                    ProdName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProdCategory = table.Column<int>(type: "int", nullable: false),
                    ProdCondition = table.Column<int>(type: "int", nullable: false),
                    ProdDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProdStatus = table.Column<int>(type: "int", nullable: false),
                    AmountProposed = table.Column<double>(type: "float", nullable: false),
                    StoreAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SavedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CollectionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCollected = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposeItems", x => x.PropItemId);
                    table.ForeignKey(
                        name: "FK_ProposeItems_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId");
                    table.ForeignKey(
                        name: "FK_ProposeItems_DeliveryDrivers_AssignedDriverId",
                        column: x => x.AssignedDriverId,
                        principalTable: "DeliveryDrivers",
                        principalColumn: "DriverId");
                });

            migrationBuilder.CreateTable(
                name: "StaffPositions",
                columns: table => new
                {
                    PositionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HourlyRate = table.Column<double>(type: "float", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffPositions", x => x.PositionId);
                    table.ForeignKey(
                        name: "FK_StaffPositions_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepairRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ItemDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssueDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_RepairRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId");
                    table.ForeignKey(
                        name: "FK_RepairRequests_RepairItemCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "RepairItemCategories",
                        principalColumn: "CategoryId");
                });

            migrationBuilder.CreateTable(
                name: "Technicians",
                columns: table => new
                {
                    TechnicianId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnicianFirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TechnicianLastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegisteredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technicians", x => x.TechnicianId);
                    table.ForeignKey(
                        name: "FK_Technicians_RepairItemCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "RepairItemCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AddProducts",
                columns: table => new
                {
                    ProdId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    ProdName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProdCategory = table.Column<int>(type: "int", nullable: false),
                    ProdDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProdPrice = table.Column<double>(type: "float", nullable: false),
                    ProdSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProdQuantity = table.Column<int>(type: "int", nullable: false),
                    ProdType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddProducts", x => x.ProdId);
                    table.ForeignKey(
                        name: "FK_AddProducts_shopowners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "shopowners",
                        principalColumn: "OwnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusRegCertificates",
                columns: table => new
                {
                    CertId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SavedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusRegCertificates", x => x.CertId);
                    table.ForeignKey(
                        name: "FK_BusRegCertificates_shopowners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "shopowners",
                        principalColumn: "OwnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaseAgreements",
                columns: table => new
                {
                    LeaseAgreementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    CustomerAgreed = table.Column<bool>(type: "bit", nullable: false),
                    AgreedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TermMonths = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DeliveryAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeliveryType = table.Column<int>(type: "int", nullable: false),
                    DriverId = table.Column<int>(type: "int", nullable: true),
                    ScheduledDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveryPin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDelivered = table.Column<bool>(type: "bit", nullable: false),
                    PaystackCustomerCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaystackPlanCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaystackSubscriptionCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaystackEmailToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaystackAuthorizationCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InitialTransactionReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnBankCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnAccountHolderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefundRecipientCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefundTransferCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefundTransferStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnReminderSentOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaseAgreements", x => x.LeaseAgreementId);
                    table.ForeignKey(
                        name: "FK_LeaseAgreements_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId");
                    table.ForeignKey(
                        name: "FK_LeaseAgreements_DeliveryDrivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DeliveryDrivers",
                        principalColumn: "DriverId");
                    table.ForeignKey(
                        name: "FK_LeaseAgreements_shopowners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "shopowners",
                        principalColumn: "OwnerId");
                });

            migrationBuilder.CreateTable(
                name: "LeaseProducts",
                columns: table => new
                {
                    LeaseProdId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    LeaseProdName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeaseProdCategory = table.Column<int>(type: "int", nullable: false),
                    LeaseProdDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeaseProdSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MonthlyRate = table.Column<double>(type: "float", nullable: false),
                    SecurityDeposit = table.Column<double>(type: "float", nullable: false),
                    MinLeaseMonths = table.Column<int>(type: "int", nullable: false),
                    MaxLeaseMonths = table.Column<int>(type: "int", nullable: false),
                    EarlyReturnFeePercentage = table.Column<double>(type: "float", nullable: false),
                    LateFeeAmount = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaseProducts", x => x.LeaseProdId);
                    table.ForeignKey(
                        name: "FK_LeaseProducts_shopowners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "shopowners",
                        principalColumn: "OwnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    OrderedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DriverId = table.Column<int>(type: "int", nullable: true),
                    DeliveryAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalAmount = table.Column<double>(type: "float", nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    DeliveredOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    DeliveryPin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduledDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrderType = table.Column<int>(type: "int", nullable: false),
                    PickupOrderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId");
                    table.ForeignKey(
                        name: "FK_Orders_DeliveryDrivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DeliveryDrivers",
                        principalColumn: "DriverId");
                    table.ForeignKey(
                        name: "FK_Orders_shopowners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "shopowners",
                        principalColumn: "OwnerId");
                });

            migrationBuilder.CreateTable(
                name: "OwnerIdentityBooks",
                columns: table => new
                {
                    OwnerIdentityBookId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SavedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerIdentityBooks", x => x.OwnerIdentityBookId);
                    table.ForeignKey(
                        name: "FK_OwnerIdentityBooks_shopowners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "shopowners",
                        principalColumn: "OwnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollateralItems",
                columns: table => new
                {
                    CollateralItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanApplicationId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeclaredValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AiScanResult = table.Column<int>(type: "int", nullable: false),
                    AiScanNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    AiConfidencePercent = table.Column<int>(type: "int", nullable: true),
                    AiMatchedCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImagePerceptualHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UploadedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollateralItems", x => x.CollateralItemId);
                    table.ForeignKey(
                        name: "FK_CollateralItems_LoanApplications_LoanApplicationId",
                        column: x => x.LoanApplicationId,
                        principalTable: "LoanApplications",
                        principalColumn: "LoanApplicationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanAssessments",
                columns: table => new
                {
                    LoanAssessmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanApplicationId = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    CreditScoreAtAssessment = table.Column<int>(type: "int", nullable: false),
                    ClearanceStatusAtAssessment = table.Column<int>(type: "int", nullable: false),
                    StillMeetsCreditThreshold = table.Column<bool>(type: "bit", nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    DecisionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StartedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanAssessments", x => x.LoanAssessmentId);
                    table.ForeignKey(
                        name: "FK_LoanAssessments_LoanApplications_LoanApplicationId",
                        column: x => x.LoanApplicationId,
                        principalTable: "LoanApplications",
                        principalColumn: "LoanApplicationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanAssessments_shopowners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "shopowners",
                        principalColumn: "OwnerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanDisbursements",
                columns: table => new
                {
                    LoanDisbursementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanApplicationId = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BankCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AmountDisbursed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RecipientCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TransferCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TransferStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    FailureMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttemptedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanDisbursements", x => x.LoanDisbursementId);
                    table.ForeignKey(
                        name: "FK_LoanDisbursements_LoanApplications_LoanApplicationId",
                        column: x => x.LoanApplicationId,
                        principalTable: "LoanApplications",
                        principalColumn: "LoanApplicationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanDisbursements_shopowners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "shopowners",
                        principalColumn: "OwnerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Loans",
                columns: table => new
                {
                    LoanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanApplicationId = table.Column<int>(type: "int", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    TotalRepayable = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DisbursedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SettledOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CollateralCollected = table.Column<bool>(type: "bit", nullable: false),
                    CollateralCollectedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CollectedByOwnerId = table.Column<int>(type: "int", nullable: true),
                    CollectionToken = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loans", x => x.LoanId);
                    table.ForeignKey(
                        name: "FK_Loans_LoanApplications_LoanApplicationId",
                        column: x => x.LoanApplicationId,
                        principalTable: "LoanApplications",
                        principalColumn: "LoanApplicationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProposalCheckouts",
                columns: table => new
                {
                    CheckoutId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropItemId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    SelectDelivery = table.Column<int>(type: "int", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BankCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    SubmittedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalCheckouts", x => x.CheckoutId);
                    table.ForeignKey(
                        name: "FK_ProposalCheckouts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId");
                    table.ForeignKey(
                        name: "FK_ProposalCheckouts_ProposeItems_PropItemId",
                        column: x => x.PropItemId,
                        principalTable: "ProposeItems",
                        principalColumn: "PropItemId");
                });

            migrationBuilder.CreateTable(
                name: "ProposedProdPic",
                columns: table => new
                {
                    ProposedId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SavedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProposeItemPropItemId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposedProdPic", x => x.ProposedId);
                    table.ForeignKey(
                        name: "FK_ProposedProdPic_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProposedProdPic_ProposeItems_ProposeItemPropItemId",
                        column: x => x.ProposeItemPropItemId,
                        principalTable: "ProposeItems",
                        principalColumn: "PropItemId");
                });

            migrationBuilder.CreateTable(
                name: "ReceiptPics",
                columns: table => new
                {
                    ReceiptId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SavedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProposeItemPropItemId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptPics", x => x.ReceiptId);
                    table.ForeignKey(
                        name: "FK_ReceiptPics_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceiptPics_ProposeItems_ProposeItemPropItemId",
                        column: x => x.ProposeItemPropItemId,
                        principalTable: "ProposeItems",
                        principalColumn: "PropItemId");
                });

            migrationBuilder.CreateTable(
                name: "JobPosts",
                columns: table => new
                {
                    PostId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HRId = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Department = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<int>(type: "int", nullable: false),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    SalaryMin = table.Column<double>(type: "float", nullable: true),
                    SalaryMax = table.Column<double>(type: "float", nullable: true),
                    Responsibilities = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    ContractTerms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiredCerts = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredSkillsAndXP = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClosingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPosts", x => x.PostId);
                    table.ForeignKey(
                        name: "FK_JobPosts_HRs_HRId",
                        column: x => x.HRId,
                        principalTable: "HRs",
                        principalColumn: "HRId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobPosts_StaffPositions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "StaffPositions",
                        principalColumn: "PositionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobPosts_shopowners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "shopowners",
                        principalColumn: "OwnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobRoleKnowledges",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    HRId = table.Column<int>(type: "int", nullable: true),
                    MaxHeadcount = table.Column<int>(type: "int", nullable: false),
                    CompanyResponsibilities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredSkills = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredExperience = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredQualifications = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtherCompanyInformation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRoleKnowledges", x => x.RoleId);
                    table.ForeignKey(
                        name: "FK_JobRoleKnowledges_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobRoleKnowledges_StaffPositions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "StaffPositions",
                        principalColumn: "PositionId");
                });

            migrationBuilder.CreateTable(
                name: "Staffs",
                columns: table => new
                {
                    StaffId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    StaffAvailability = table.Column<int>(type: "int", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateEmployed = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    ReferencePhotoPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceEmbedding = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staffs", x => x.StaffId);
                    table.ForeignKey(
                        name: "FK_Staffs_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Staffs_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Staffs_StaffPositions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "StaffPositions",
                        principalColumn: "PositionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Staffs_shopowners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "shopowners",
                        principalColumn: "OwnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepairTrackings",
                columns: table => new
                {
                    TrackingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepairRequestId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairTrackings", x => x.TrackingId);
                    table.ForeignKey(
                        name: "FK_RepairTrackings_RepairRequests_RepairRequestId",
                        column: x => x.RepairRequestId,
                        principalTable: "RepairRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Quotations",
                columns: table => new
                {
                    QuotationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepairRequestId = table.Column<int>(type: "int", nullable: false),
                    TechnicianId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TechnicianNotes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotations", x => x.QuotationId);
                    table.ForeignKey(
                        name: "FK_Quotations_RepairRequests_RepairRequestId",
                        column: x => x.RepairRequestId,
                        principalTable: "RepairRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Quotations_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "TechnicianId");
                });

            migrationBuilder.CreateTable(
                name: "RepairAssignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepairRequestId = table.Column<int>(type: "int", nullable: false),
                    TechnicianId = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignmentNotes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairAssignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_RepairAssignments_RepairRequests_RepairRequestId",
                        column: x => x.RepairRequestId,
                        principalTable: "RepairRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepairAssignments_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "TechnicianId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    CartItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartId = table.Column<int>(type: "int", nullable: false),
                    ProdId = table.Column<int>(type: "int", nullable: false),
                    ProdName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProdQuantity = table.Column<int>(type: "int", nullable: false),
                    PriceAtTimeAdded = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.CartItemId);
                    table.ForeignKey(
                        name: "FK_CartItems_AddProducts_ProdId",
                        column: x => x.ProdId,
                        principalTable: "AddProducts",
                        principalColumn: "ProdId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "CartId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductPicture",
                columns: table => new
                {
                    ProdPictureId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SavedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddProductProdId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPicture", x => x.ProdPictureId);
                    table.ForeignKey(
                        name: "FK_ProductPicture_AddProducts_AddProductProdId",
                        column: x => x.AddProductProdId,
                        principalTable: "AddProducts",
                        principalColumn: "ProdId");
                    table.ForeignKey(
                        name: "FK_ProductPicture_shopowners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "shopowners",
                        principalColumn: "OwnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeasePayments",
                columns: table => new
                {
                    LeasePaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaseAgreementId = table.Column<int>(type: "int", nullable: false),
                    RenewalTermMonths = table.Column<int>(type: "int", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AmountDue = table.Column<double>(type: "float", nullable: false),
                    AmountPaid = table.Column<double>(type: "float", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaystackTransactionReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReminderSentOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LateFeeApplied = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeasePayments", x => x.LeasePaymentId);
                    table.ForeignKey(
                        name: "FK_LeasePayments_LeaseAgreements_LeaseAgreementId",
                        column: x => x.LeaseAgreementId,
                        principalTable: "LeaseAgreements",
                        principalColumn: "LeaseAgreementId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaseProductPictures",
                columns: table => new
                {
                    LeasePictureId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaseProdId = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SavedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaseProductPictures", x => x.LeasePictureId);
                    table.ForeignKey(
                        name: "FK_LeaseProductPictures_LeaseProducts_LeaseProdId",
                        column: x => x.LeaseProdId,
                        principalTable: "LeaseProducts",
                        principalColumn: "LeaseProdId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaseUnits",
                columns: table => new
                {
                    LeaseUnitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaseProdId = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConditionNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaseUnits", x => x.LeaseUnitId);
                    table.ForeignKey(
                        name: "FK_LeaseUnits_LeaseProducts_LeaseProdId",
                        column: x => x.LeaseProdId,
                        principalTable: "LeaseProducts",
                        principalColumn: "LeaseProdId");
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    OrderItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProdId = table.Column<int>(type: "int", nullable: false),
                    ProdName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    PriceAtTimeOfPurchase = table.Column<double>(type: "float", nullable: false),
                    ReturnPStatus = table.Column<int>(type: "int", nullable: false),
                    RetunReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuantityReturned = table.Column<int>(type: "int", nullable: false),
                    RefundOrExchange = table.Column<int>(type: "int", nullable: false),
                    IsAssingedForCollection = table.Column<bool>(type: "bit", nullable: false),
                    IsAssingedForSecDelivery = table.Column<bool>(type: "bit", nullable: false),
                    DriverId = table.Column<int>(type: "int", nullable: true),
                    CollectionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NewDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.OrderItemId);
                    table.ForeignKey(
                        name: "FK_OrderItems_AddProducts_ProdId",
                        column: x => x.ProdId,
                        principalTable: "AddProducts",
                        principalColumn: "ProdId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_DeliveryDrivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DeliveryDrivers",
                        principalColumn: "DriverId");
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PickupOrders",
                columns: table => new
                {
                    PickupOrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    PickupBranchId = table.Column<int>(type: "int", nullable: false),
                    PickupReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PickupFee = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CollectedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledPickupDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickupOrders", x => x.PickupOrderId);
                    table.ForeignKey(
                        name: "FK_PickupOrders_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId");
                    table.ForeignKey(
                        name: "FK_PickupOrders_shopowners_PickupBranchId",
                        column: x => x.PickupBranchId,
                        principalTable: "shopowners",
                        principalColumn: "OwnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollateralVerifications",
                columns: table => new
                {
                    CollateralVerificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanAssessmentId = table.Column<int>(type: "int", nullable: false),
                    CollateralItemId = table.Column<int>(type: "int", nullable: false),
                    VerificationImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false),
                    MatchConfidencePercent = table.Column<int>(type: "int", nullable: true),
                    AiNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StaffOverride = table.Column<bool>(type: "bit", nullable: false),
                    StaffOverrideReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollateralVerifications", x => x.CollateralVerificationId);
                    table.ForeignKey(
                        name: "FK_CollateralVerifications_CollateralItems_CollateralItemId",
                        column: x => x.CollateralItemId,
                        principalTable: "CollateralItems",
                        principalColumn: "CollateralItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollateralVerifications_LoanAssessments_LoanAssessmentId",
                        column: x => x.LoanAssessmentId,
                        principalTable: "LoanAssessments",
                        principalColumn: "LoanAssessmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FraudFlags",
                columns: table => new
                {
                    FraudFlagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanAssessmentId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RelatedLoanApplicationId = table.Column<int>(type: "int", nullable: true),
                    Dismissed = table.Column<bool>(type: "bit", nullable: false),
                    DismissReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FraudFlags", x => x.FraudFlagId);
                    table.ForeignKey(
                        name: "FK_FraudFlags_LoanAssessments_LoanAssessmentId",
                        column: x => x.LoanAssessmentId,
                        principalTable: "LoanAssessments",
                        principalColumn: "LoanAssessmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanPayments",
                columns: table => new
                {
                    LoanPaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    PaidOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BalanceAfterPayment = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanPayments", x => x.LoanPaymentId);
                    table.ForeignKey(
                        name: "FK_LoanPayments_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "LoanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    AssessmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PassMark = table.Column<int>(type: "int", nullable: false),
                    TimeLimit = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.AssessmentId);
                    table.ForeignKey(
                        name: "FK_Assessments_JobPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "JobPosts",
                        principalColumn: "PostId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HRJobRoleKnowledge",
                columns: table => new
                {
                    HRsHRId = table.Column<int>(type: "int", nullable: false),
                    JobRoleKnowledgesRoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRJobRoleKnowledge", x => new { x.HRsHRId, x.JobRoleKnowledgesRoleId });
                    table.ForeignKey(
                        name: "FK_HRJobRoleKnowledge_HRs_HRsHRId",
                        column: x => x.HRsHRId,
                        principalTable: "HRs",
                        principalColumn: "HRId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HRJobRoleKnowledge_JobRoleKnowledges_JobRoleKnowledgesRoleId",
                        column: x => x.JobRoleKnowledgesRoleId,
                        principalTable: "JobRoleKnowledges",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    LeaveRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WorkingDaysRequested = table.Column<int>(type: "int", nullable: false),
                    SupportingDocumentPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.LeaveRequestId);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "LeaveTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RosterShifts",
                columns: table => new
                {
                    RosterShiftId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RosterId = table.Column<int>(type: "int", nullable: false),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RosterShifts", x => x.RosterShiftId);
                    table.ForeignKey(
                        name: "FK_RosterShifts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RosterShifts_Rosters_RosterId",
                        column: x => x.RosterId,
                        principalTable: "Rosters",
                        principalColumn: "RosterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RosterShifts_StaffPositions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "StaffPositions",
                        principalColumn: "PositionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RosterShifts_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffLeaveBalances",
                columns: table => new
                {
                    StaffLeaveBalanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    TotalAllocatedDays = table.Column<int>(type: "int", nullable: false),
                    UsedDays = table.Column<int>(type: "int", nullable: false),
                    CycleStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CycleEndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffLeaveBalances", x => x.StaffLeaveBalanceId);
                    table.ForeignKey(
                        name: "FK_StaffLeaveBalances_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "LeaveTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffLeaveBalances_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffRotationOrders",
                columns: table => new
                {
                    StaffRotationOrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    QueuePosition = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffRotationOrders", x => x.StaffRotationOrderId);
                    table.ForeignKey(
                        name: "FK_StaffRotationOrders_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffRotationOrders_StaffPositions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "StaffPositions",
                        principalColumn: "PositionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffRotationOrders_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuotationItems",
                columns: table => new
                {
                    QuotationItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationId = table.Column<int>(type: "int", nullable: false),
                    ItemDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationItems", x => x.QuotationItemId);
                    table.ForeignKey(
                        name: "FK_QuotationItems_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "Quotations",
                        principalColumn: "QuotationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Receipts",
                columns: table => new
                {
                    ReceiptId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepairRequestId = table.Column<int>(type: "int", nullable: false),
                    QuotationId = table.Column<int>(type: "int", nullable: true),
                    TechnicianId = table.Column<int>(type: "int", nullable: true),
                    IssuedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinalAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipts", x => x.ReceiptId);
                    table.ForeignKey(
                        name: "FK_Receipts_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "Quotations",
                        principalColumn: "QuotationId");
                    table.ForeignKey(
                        name: "FK_Receipts_RepairRequests_RepairRequestId",
                        column: x => x.RepairRequestId,
                        principalTable: "RepairRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Receipts_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "TechnicianId");
                });

            migrationBuilder.CreateTable(
                name: "LeaseAgreementItems",
                columns: table => new
                {
                    LeaseAgreementItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaseAgreementId = table.Column<int>(type: "int", nullable: false),
                    LeaseUnitId = table.Column<int>(type: "int", nullable: false),
                    MonthlyRateAtLease = table.Column<double>(type: "float", nullable: false),
                    DepositAtLease = table.Column<double>(type: "float", nullable: false),
                    ReturnStatus = table.Column<int>(type: "int", nullable: false),
                    CollectionDriverId = table.Column<int>(type: "int", nullable: true),
                    ScheduledCollectionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualCollectionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConditionAtReturn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DamageFee = table.Column<double>(type: "float", nullable: true),
                    DepositRefunded = table.Column<double>(type: "float", nullable: true),
                    EarlyReturnFee = table.Column<double>(type: "float", nullable: true),
                    ReturnConditionImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiDamageAssessment = table.Column<int>(type: "int", nullable: true),
                    AiConfidencePercent = table.Column<int>(type: "int", nullable: true),
                    AiSuggestedDamageFee = table.Column<double>(type: "float", nullable: true),
                    AiNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StaffOverride = table.Column<bool>(type: "bit", nullable: false),
                    StaffOverrideReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaseAgreementItems", x => x.LeaseAgreementItemId);
                    table.ForeignKey(
                        name: "FK_LeaseAgreementItems_DeliveryDrivers_CollectionDriverId",
                        column: x => x.CollectionDriverId,
                        principalTable: "DeliveryDrivers",
                        principalColumn: "DriverId");
                    table.ForeignKey(
                        name: "FK_LeaseAgreementItems_LeaseAgreements_LeaseAgreementId",
                        column: x => x.LeaseAgreementId,
                        principalTable: "LeaseAgreements",
                        principalColumn: "LeaseAgreementId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeaseAgreementItems_LeaseUnits_LeaseUnitId",
                        column: x => x.LeaseUnitId,
                        principalTable: "LeaseUnits",
                        principalColumn: "LeaseUnitId");
                });

            migrationBuilder.CreateTable(
                name: "InterBranchTransfers",
                columns: table => new
                {
                    TransferId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderItemId = table.Column<int>(type: "int", nullable: false),
                    SourceBranchId = table.Column<int>(type: "int", nullable: false),
                    TargetBranchId = table.Column<int>(type: "int", nullable: false),
                    DriverId = table.Column<int>(type: "int", nullable: true),
                    TransferCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransferStatus = table.Column<int>(type: "int", nullable: false),
                    ScheduledTransferDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveredToTargetOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDelivered = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterBranchTransfers", x => x.TransferId);
                    table.ForeignKey(
                        name: "FK_InterBranchTransfers_DeliveryDrivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "DeliveryDrivers",
                        principalColumn: "DriverId");
                    table.ForeignKey(
                        name: "FK_InterBranchTransfers_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "OrderItemId");
                    table.ForeignKey(
                        name: "FK_InterBranchTransfers_shopowners_SourceBranchId",
                        column: x => x.SourceBranchId,
                        principalTable: "shopowners",
                        principalColumn: "OwnerId");
                    table.ForeignKey(
                        name: "FK_InterBranchTransfers_shopowners_TargetBranchId",
                        column: x => x.TargetBranchId,
                        principalTable: "shopowners",
                        principalColumn: "OwnerId");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationForms",
                columns: table => new
                {
                    ApplicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    AssessmentId = table.Column<int>(type: "int", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    IDNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Experience = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Skills = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Certificates = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Links = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasInternertAccess = table.Column<bool>(type: "bit", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TestWritten = table.Column<bool>(type: "bit", nullable: false),
                    TestScore = table.Column<int>(type: "int", nullable: true),
                    JobStat = table.Column<int>(type: "int", nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Department = table.Column<int>(type: "int", nullable: false),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    SalaryMin = table.Column<double>(type: "float", nullable: true),
                    SalaryMax = table.Column<double>(type: "float", nullable: true),
                    MatchedSkillCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationForms", x => x.ApplicationId);
                    table.ForeignKey(
                        name: "FK_ApplicationForms_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "AssessmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationForms_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationForms_JobPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "JobPosts",
                        principalColumn: "PostId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizQuestions",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionB = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionC = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionD = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssessmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizQuestions", x => x.QuestionId);
                    table.ForeignKey(
                        name: "FK_QuizQuestions_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "AssessmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    AttendanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RosterShiftId = table.Column<int>(type: "int", nullable: false),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckInLat = table.Column<double>(type: "float", nullable: true),
                    CheckInLng = table.Column<double>(type: "float", nullable: true),
                    CheckInPhotoPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckOutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckOutLat = table.Column<double>(type: "float", nullable: true),
                    CheckOutLng = table.Column<double>(type: "float", nullable: true),
                    CheckOutPhotoPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.AttendanceId);
                    table.ForeignKey(
                        name: "FK_Attendances_RosterShifts_RosterShiftId",
                        column: x => x.RosterShiftId,
                        principalTable: "RosterShifts",
                        principalColumn: "RosterShiftId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShiftChangeRequests",
                columns: table => new
                {
                    ShiftChangeRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    RosterShiftId = table.Column<int>(type: "int", nullable: false),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestedWorkDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedStartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    RequestedEndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    SwapWithStaffId = table.Column<int>(type: "int", nullable: true),
                    SwapWithRosterShiftId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedByHRId = table.Column<int>(type: "int", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftChangeRequests", x => x.ShiftChangeRequestId);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_HRs_ReviewedByHRId",
                        column: x => x.ReviewedByHRId,
                        principalTable: "HRs",
                        principalColumn: "HRId");
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_RosterShifts_RosterShiftId",
                        column: x => x.RosterShiftId,
                        principalTable: "RosterShifts",
                        principalColumn: "RosterShiftId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_RosterShifts_SwapWithRosterShiftId",
                        column: x => x.SwapWithRosterShiftId,
                        principalTable: "RosterShifts",
                        principalColumn: "RosterShiftId");
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_Staffs_SwapWithStaffId",
                        column: x => x.SwapWithStaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId");
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    ContractId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    DateSent = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CandidateSignedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanySignedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CandidateSignature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanySignature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContractStat = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.ContractId);
                    table.ForeignKey(
                        name: "FK_Contracts_ApplicationForms_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "ApplicationForms",
                        principalColumn: "ApplicationId");
                    table.ForeignKey(
                        name: "FK_Contracts_JobPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "JobPosts",
                        principalColumn: "PostId");
                });

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    ResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Passed = table.Column<bool>(type: "bit", nullable: false),
                    DateWritten = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResults", x => x.ResultId);
                    table.ForeignKey(
                        name: "FK_TestResults_ApplicationForms_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "ApplicationForms",
                        principalColumn: "ApplicationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Interviews",
                columns: table => new
                {
                    InterviewId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ResultId = table.Column<int>(type: "int", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JitsiMeetingLink = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EvaluationNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EvaluationScore = table.Column<int>(type: "int", nullable: true),
                    Salary = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EvaluatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByHR = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interviews", x => x.InterviewId);
                    table.ForeignKey(
                        name: "FK_Interviews_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Interviews_TestResults_ResultId",
                        column: x => x.ResultId,
                        principalTable: "TestResults",
                        principalColumn: "ResultId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "LeaveTypes",
                columns: new[] { "LeaveTypeId", "CycleMonths", "IsActive", "IsPaid", "LeaveTypeName", "MaximumConsecutiveDays", "MinimumEmploymentMonths", "MinimumNoticeDays", "RequiresBalance", "RequiresDocument" },
                values: new object[,]
                {
                    { 1, 12, true, true, "Annual Leave", 10, 0, 7, true, false },
                    { 2, 36, true, true, "Sick Leave", 30, 0, 0, true, true },
                    { 3, 12, true, true, "Family Responsibility Leave", 3, 4, 0, true, false },
                    { 4, null, true, false, "Unpaid Leave", 10, 0, 14, false, false }
                });

            migrationBuilder.InsertData(
                table: "PublicHolidays",
                columns: new[] { "PublicHolidayId", "HolidayDate", "HolidayName", "IsActive" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "New Year's Day", true },
                    { 2, new DateTime(2026, 3, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "Human Rights Day", true },
                    { 3, new DateTime(2026, 4, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Good Friday", true },
                    { 4, new DateTime(2026, 4, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "Family Day", true },
                    { 5, new DateTime(2026, 4, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), "Freedom Day", true },
                    { 6, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Workers' Day", true },
                    { 7, new DateTime(2026, 6, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "Youth Day", true },
                    { 8, new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), "National Women's Day", true },
                    { 9, new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "National Women's Day Observed", true },
                    { 10, new DateTime(2026, 9, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "Heritage Day", true },
                    { 11, new DateTime(2026, 12, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "Day of Reconciliation", true },
                    { 12, new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Christmas Day", true },
                    { 13, new DateTime(2026, 12, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), "Day of Goodwill", true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AddProducts_OwnerId",
                table: "AddProducts",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationForms_AssessmentId",
                table: "ApplicationForms",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationForms_CustomerId",
                table: "ApplicationForms",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationForms_PostId",
                table: "ApplicationForms",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_PostId",
                table: "Assessments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_RosterShiftId",
                table: "Attendances",
                column: "RosterShiftId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusRegCertificates_OwnerId",
                table: "BusRegCertificates",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProdId",
                table: "CartItems",
                column: "ProdId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_CustomerId",
                table: "Carts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CollateralItems_LoanApplicationId",
                table: "CollateralItems",
                column: "LoanApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_CollateralVerifications_CollateralItemId",
                table: "CollateralVerifications",
                column: "CollateralItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CollateralVerifications_LoanAssessmentId",
                table: "CollateralVerifications",
                column: "LoanAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ApplicationId",
                table: "Contracts",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_PostId",
                table: "Contracts",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverIdentityBooks_DriverId",
                table: "DriverIdentityBooks",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriversLicences_DriverId",
                table: "DriversLicences",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_FraudFlags_LoanAssessmentId",
                table: "FraudFlags",
                column: "LoanAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_HRJobRoleKnowledge_JobRoleKnowledgesRoleId",
                table: "HRJobRoleKnowledge",
                column: "JobRoleKnowledgesRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_InterBranchTransfers_DriverId",
                table: "InterBranchTransfers",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_InterBranchTransfers_OrderItemId",
                table: "InterBranchTransfers",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InterBranchTransfers_SourceBranchId",
                table: "InterBranchTransfers",
                column: "SourceBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InterBranchTransfers_TargetBranchId",
                table: "InterBranchTransfers",
                column: "TargetBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_CustomerId",
                table: "Interviews",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_ResultId",
                table: "Interviews",
                column: "ResultId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_HRId",
                table: "JobPosts",
                column: "HRId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_OwnerId",
                table: "JobPosts",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_PositionId",
                table: "JobPosts",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRoleKnowledges_DepartmentId",
                table: "JobRoleKnowledges",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRoleKnowledges_PositionId",
                table: "JobRoleKnowledges",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaseAgreementItems_CollectionDriverId",
                table: "LeaseAgreementItems",
                column: "CollectionDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaseAgreementItems_LeaseAgreementId",
                table: "LeaseAgreementItems",
                column: "LeaseAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaseAgreementItems_LeaseUnitId",
                table: "LeaseAgreementItems",
                column: "LeaseUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaseAgreements_CustomerId",
                table: "LeaseAgreements",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaseAgreements_DriverId",
                table: "LeaseAgreements",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaseAgreements_OwnerId",
                table: "LeaseAgreements",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_LeasePayments_LeaseAgreementId",
                table: "LeasePayments",
                column: "LeaseAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaseProductPictures_LeaseProdId",
                table: "LeaseProductPictures",
                column: "LeaseProdId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaseProducts_OwnerId",
                table: "LeaseProducts",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaseUnits_LeaseProdId",
                table: "LeaseUnits",
                column: "LeaseProdId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_StaffId",
                table: "LeaveRequests",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplications_BranchId",
                table: "LoanApplications",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplications_CustomerId",
                table: "LoanApplications",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAssessments_LoanApplicationId",
                table: "LoanAssessments",
                column: "LoanApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAssessments_OwnerId",
                table: "LoanAssessments",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDisbursements_LoanApplicationId",
                table: "LoanDisbursements",
                column: "LoanApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDisbursements_OwnerId",
                table: "LoanDisbursements",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayments_LoanId",
                table: "LoanPayments",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_LoanApplicationId",
                table: "Loans",
                column: "LoanApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_StaffId",
                table: "Notifications",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_DriverId",
                table: "OrderItems",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProdId",
                table: "OrderItems",
                column: "ProdId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DriverId",
                table: "Orders",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OwnerId",
                table: "Orders",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerIdentityBooks_OwnerId",
                table: "OwnerIdentityBooks",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PickupOrders_OrderId",
                table: "PickupOrders",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickupOrders_PickupBranchId",
                table: "PickupOrders",
                column: "PickupBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPicture_AddProductProdId",
                table: "ProductPicture",
                column: "AddProductProdId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPicture_OwnerId",
                table: "ProductPicture",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalCheckouts_CustomerId",
                table: "ProposalCheckouts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalCheckouts_PropItemId",
                table: "ProposalCheckouts",
                column: "PropItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProposedProdPic_CustomerId",
                table: "ProposedProdPic",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposedProdPic_ProposeItemPropItemId",
                table: "ProposedProdPic",
                column: "ProposeItemPropItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposeItems_AssignedDriverId",
                table: "ProposeItems",
                column: "AssignedDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposeItems_CustomerId",
                table: "ProposeItems",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_HolidayDate",
                table: "PublicHolidays",
                column: "HolidayDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_AssessmentId",
                table: "QuizQuestions",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationItems_QuotationId",
                table: "QuotationItems",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_RepairRequestId",
                table: "Quotations",
                column: "RepairRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_TechnicianId",
                table: "Quotations",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptPics_CustomerId",
                table: "ReceiptPics",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptPics_ProposeItemPropItemId",
                table: "ReceiptPics",
                column: "ProposeItemPropItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_QuotationId",
                table: "Receipts",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_RepairRequestId",
                table: "Receipts",
                column: "RepairRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_TechnicianId",
                table: "Receipts",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAssignments_RepairRequestId",
                table: "RepairAssignments",
                column: "RepairRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAssignments_TechnicianId",
                table: "RepairAssignments",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRequests_CategoryId",
                table: "RepairRequests",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRequests_CustomerId",
                table: "RepairRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTrackings_RepairRequestId",
                table: "RepairTrackings",
                column: "RepairRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Rosters_BranchId",
                table: "Rosters",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterShifts_BranchId",
                table: "RosterShifts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterShifts_PositionId",
                table: "RosterShifts",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterShifts_RosterId",
                table: "RosterShifts",
                column: "RosterId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterShifts_StaffId",
                table: "RosterShifts",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_ReviewedByHRId",
                table: "ShiftChangeRequests",
                column: "ReviewedByHRId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_RosterShiftId",
                table: "ShiftChangeRequests",
                column: "RosterShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_StaffId",
                table: "ShiftChangeRequests",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_SwapWithRosterShiftId",
                table: "ShiftChangeRequests",
                column: "SwapWithRosterShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_SwapWithStaffId",
                table: "ShiftChangeRequests",
                column: "SwapWithStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_shopowners_BranchId",
                table: "shopowners",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffLeaveBalances_LeaveTypeId",
                table: "StaffLeaveBalances",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffLeaveBalances_StaffId_LeaveTypeId_CycleStartDate",
                table: "StaffLeaveBalances",
                columns: new[] { "StaffId", "LeaveTypeId", "CycleStartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffPositions_DepartmentId",
                table: "StaffPositions",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRotationOrders_BranchId",
                table: "StaffRotationOrders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRotationOrders_PositionId",
                table: "StaffRotationOrders",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRotationOrders_StaffId",
                table: "StaffRotationOrders",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_BranchId",
                table: "Staffs",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_OwnerId",
                table: "Staffs",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_PositionId",
                table: "Staffs",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_RoleId",
                table: "Staffs",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_CategoryId",
                table: "Technicians",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_ApplicationId",
                table: "TestResults",
                column: "ApplicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "BusRegCertificates");

            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "CollateralVerifications");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "CreditRecords");

            migrationBuilder.DropTable(
                name: "CriminalRecords");

            migrationBuilder.DropTable(
                name: "DriverIdentityBooks");

            migrationBuilder.DropTable(
                name: "DriversLicences");

            migrationBuilder.DropTable(
                name: "FraudFlags");

            migrationBuilder.DropTable(
                name: "HRJobRoleKnowledge");

            migrationBuilder.DropTable(
                name: "InterBranchTransfers");

            migrationBuilder.DropTable(
                name: "Interviews");

            migrationBuilder.DropTable(
                name: "LeaseAgreementItems");

            migrationBuilder.DropTable(
                name: "LeasePayments");

            migrationBuilder.DropTable(
                name: "LeaseProductPictures");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "LoanDisbursements");

            migrationBuilder.DropTable(
                name: "LoanPayments");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OwnerIdentityBooks");

            migrationBuilder.DropTable(
                name: "PickupOrders");

            migrationBuilder.DropTable(
                name: "ProductPicture");

            migrationBuilder.DropTable(
                name: "ProposalCheckouts");

            migrationBuilder.DropTable(
                name: "ProposedProdPic");

            migrationBuilder.DropTable(
                name: "PublicHolidays");

            migrationBuilder.DropTable(
                name: "QuizQuestions");

            migrationBuilder.DropTable(
                name: "QuotationItems");

            migrationBuilder.DropTable(
                name: "ReceiptPics");

            migrationBuilder.DropTable(
                name: "Receipts");

            migrationBuilder.DropTable(
                name: "RepairAssignments");

            migrationBuilder.DropTable(
                name: "RepairTrackings");

            migrationBuilder.DropTable(
                name: "ShiftChangeRequests");

            migrationBuilder.DropTable(
                name: "StaffLeaveBalances");

            migrationBuilder.DropTable(
                name: "StaffRotationOrders");

            migrationBuilder.DropTable(
                name: "SystemAdmins");

            migrationBuilder.DropTable(
                name: "Carts");

            migrationBuilder.DropTable(
                name: "CollateralItems");

            migrationBuilder.DropTable(
                name: "LoanAssessments");

            migrationBuilder.DropTable(
                name: "JobRoleKnowledges");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "TestResults");

            migrationBuilder.DropTable(
                name: "LeaseUnits");

            migrationBuilder.DropTable(
                name: "LeaseAgreements");

            migrationBuilder.DropTable(
                name: "Loans");

            migrationBuilder.DropTable(
                name: "ProposeItems");

            migrationBuilder.DropTable(
                name: "Quotations");

            migrationBuilder.DropTable(
                name: "RosterShifts");

            migrationBuilder.DropTable(
                name: "LeaveTypes");

            migrationBuilder.DropTable(
                name: "AddProducts");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "ApplicationForms");

            migrationBuilder.DropTable(
                name: "LeaseProducts");

            migrationBuilder.DropTable(
                name: "LoanApplications");

            migrationBuilder.DropTable(
                name: "RepairRequests");

            migrationBuilder.DropTable(
                name: "Technicians");

            migrationBuilder.DropTable(
                name: "Rosters");

            migrationBuilder.DropTable(
                name: "Staffs");

            migrationBuilder.DropTable(
                name: "DeliveryDrivers");

            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "RepairItemCategories");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "JobPosts");

            migrationBuilder.DropTable(
                name: "HRs");

            migrationBuilder.DropTable(
                name: "StaffPositions");

            migrationBuilder.DropTable(
                name: "shopowners");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Branches");
        }
    }
}
