using Microsoft.EntityFrameworkCore;
using SkyTech.Models;
using SkyTech.Models.SkytechWorkers;

namespace SkyTech.Data
{
	public static class DbInitializer
	{
		public static async Task SeedAsync(ApplicationDbContext context)
		{
			await context.Database.MigrateAsync();

			await SeedDepartmentsAsync(context);
			await SeedStaffPositionsAsync(context);
			await SeedSystemAdminAsync(context);
			await SeedRolesAsync(context); // Add this first
			await SeedHRAsync(context);
			await SeedStaffAsync(context);
		}
		private static async Task SeedRolesAsync(
	ApplicationDbContext context)
		{
			bool exists = await context.Set<Role>()
				.AnyAsync(role => role.RoleName == "Staff");

			if (exists)
				return;

			context.Set<Role>().Add(new Role
			{
				RoleName = "Staff"
			});

			await context.SaveChangesAsync();
		}
		private static async Task SeedDepartmentsAsync(
			ApplicationDbContext context)
		{
			var departmentNames = new[]
			{
				"Sales Floor",
				"Management",
				"Technical Support",
				"Cash Desk"
			};

			foreach (string departmentName in departmentNames)
			{
				bool exists = await context.Set<Department>()
					.AnyAsync(d => d.DepartmentName == departmentName);

				if (exists)
					continue;

				context.Set<Department>().Add(new Department
				{
					DepartmentName = departmentName,
					IsActive = true,
					MinimumStaffRequired = 1
				});
			}

			await context.SaveChangesAsync();
		}

		private static async Task SeedStaffPositionsAsync(
			ApplicationDbContext context)
		{
			var positionData = new[]
			{
				new
				{
					PositionName = "Cleaner",
					HourlyRate = 30.00,
					DepartmentName = "Sales Floor"
				},
				new
				{
					PositionName = "Supervisor",
					HourlyRate = 30.00,
					DepartmentName = "Management"
				},
				new
				{
					PositionName = "Technician",
					HourlyRate = 30.00,
					DepartmentName = "Technical Support"
				},
				new
				{
					PositionName = "Cashier",
					HourlyRate = 30.00,
					DepartmentName = "Cash Desk"
				}
			};

			foreach (var item in positionData)
			{
				bool exists = await context.Set<StaffPosition>()
					.AnyAsync(p => p.PositionName == item.PositionName);

				if (exists)
					continue;

				var department = await context.Set<Department>()
					.SingleAsync(d =>
						d.DepartmentName == item.DepartmentName);

				context.Set<StaffPosition>().Add(new StaffPosition
				{
					PositionName = item.PositionName,
					HourlyRate = item.HourlyRate,
					DepartmentId = department.DepartmentId
				});
			}

			await context.SaveChangesAsync();
		}

		private static async Task SeedSystemAdminAsync(
			ApplicationDbContext context)
		{
			const string adminEmail = "admin@gmail.com";

			bool exists = await context.Set<SystemAdmin>()
				.AnyAsync(a => a.Email == adminEmail);

			if (exists)
				return;

			context.Set<SystemAdmin>().Add(new SystemAdmin
			{
				FirstName = "Fanele",
				LastName = "Dhladhla",
				Email = adminEmail,
				Password = "Fanele.23",
				UserType = "SystemAdmin",
				RegisteredOn = DateTime.Now
			});

			await context.SaveChangesAsync();
		}

		private static async Task SeedHRAsync(
			ApplicationDbContext context)
		{
			const string hrEmail = "hr@gmail.com";

			bool exists = await context.Set<HR>()
				.AnyAsync(h => h.Email == hrEmail);

			if (exists)
				return;

			context.Set<HR>().Add(new HR
			{
				Firstname = "Nzuzo",
				LastName = "Kunene",
				Email = hrEmail,
				UserType = "HR",
				Password = "Fanele.23"
			});

			await context.SaveChangesAsync();
		}

		private static async Task SeedStaffAsync(ApplicationDbContext context)
		{
			var branches = await context.Set<Branch>()
				.OrderBy(branch => branch.BranchId)
				.ToListAsync();

			var roleId = await context.Set<Role>()
				.OrderBy(role => role.RoleId)
				.Select(role => (int?)role.RoleId)
				.FirstOrDefaultAsync();

			var defaultOwnerId = await context.Set<ShopOwner>()
				.OrderBy(owner => owner.OwnerId)
				.Select(owner => (int?)owner.OwnerId)
				.FirstOrDefaultAsync();

			var missingRecords = new List<string>();

			if (branches.Count == 0)
				missingRecords.Add("Branch");

			if (!roleId.HasValue)
				missingRecords.Add("Role");

			if (!defaultOwnerId.HasValue)
				missingRecords.Add("ShopOwner");

			if (missingRecords.Count > 0)
			{
				Console.WriteLine(
					"Staff seeding was skipped. Missing required records: " +
					string.Join(", ", missingRecords));

				return;
			}

			var staffData = new[]
			{
		new
		{
			Number = 1,
			FirstName = "Customer",
			LastName = "One",
			PositionName = "Cleaner"
		},
		new
		{
			Number = 2,
			FirstName = "Customer",
			LastName = "Two",
			PositionName = "Cleaner"
		},
		new
		{
			Number = 3,
			FirstName = "Customer",
			LastName = "Three",
			PositionName = "Supervisor"
		},
		new
		{
			Number = 4,
			FirstName = "Customer",
			LastName = "Four",
			PositionName = "Supervisor"
		},
		new
		{
			Number = 5,
			FirstName = "Customer",
			LastName = "Five",
			PositionName = "Supervisor"
		},
		new
		{
			Number = 6,
			FirstName = "Customer",
			LastName = "Six",
			PositionName = "Technician"
		},
		new
		{
			Number = 7,
			FirstName = "Customer",
			LastName = "Seven",
			PositionName = "Technician"
		},
		new
		{
			Number = 8,
			FirstName = "Customer",
			LastName = "Eight",
			PositionName = "Technician"
		}
	};

			var positions = await context.Set<StaffPosition>()
				.ToDictionaryAsync(
					position => position.PositionName,
					StringComparer.OrdinalIgnoreCase);

			foreach (var branch in branches)
			{
				// Use the owner assigned to this branch.
				// If none exists, use the first available owner.
				var branchOwnerId = await context.Set<ShopOwner>()
					.Where(owner => owner.BranchId == branch.BranchId)
					.OrderBy(owner => owner.OwnerId)
					.Select(owner => (int?)owner.OwnerId)
					.FirstOrDefaultAsync();

				int ownerId = branchOwnerId ?? defaultOwnerId.Value;

				foreach (var item in staffData)
				{
					string username =
						$"branch{branch.BranchId}customer{item.Number}";

					string email =
						$"branch{branch.BranchId}customer{item.Number}@gmail.com";

					string phone =
						$"0{branch.BranchId:D3}{item.Number:D6}";

					bool staffExists = await context.Set<Staff>()
						.AnyAsync(staff =>
							staff.BranchId == branch.BranchId &&
							staff.Username == username);

					if (staffExists)
						continue;

					if (!positions.TryGetValue(
							item.PositionName,
							out StaffPosition? position))
					{
						Console.WriteLine(
							$"Staff member '{email}' was skipped because position " +
							$"'{item.PositionName}' was not found.");

						continue;
					}

					context.Set<Staff>().Add(new Staff
					{
						OwnerId = ownerId,
						PositionId = position.PositionId,
						BranchId = branch.BranchId,
						RoleId = roleId.Value,

						Username = username,
						FirstName = item.FirstName,
						LastName = item.LastName,
						Email = email,
						PhoneNumber = phone,

						StaffAvailability =
							IsTheStaffMemberAvailable.Active,

						EmploymentType =
							(SelectEmploymentType)0,

						Password = "Fanele.23",
						UserType = "Staff",
						DateEmployed = DateTime.Now,

						ReferencePhotoPath = null,
						ReferenceEmbedding = null
					});
				}
			}

			await context.SaveChangesAsync();

			Console.WriteLine(
				$"Staff seeding completed for {branches.Count} branch(es).");
		}
	}
}