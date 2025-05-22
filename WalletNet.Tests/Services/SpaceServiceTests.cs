using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using WalletNet.Data;
using WalletNet.Services;
using BudgetMaster.Models; // For Space
using WalletNet.Models;   // For User (if needed, though Id is int)

namespace WalletNet.Tests.Services
{
    public class SpaceServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique name for each test
                .Options;
            var dbContext = new ApplicationDbContext(options);
            // Seed initial data if necessary, e.g., a User
            dbContext.Users.Add(new User { Id = 1, UserName = "testuser", Email = "test@example.com" });
            dbContext.SaveChanges();
            return dbContext;
        }

        [Fact]
        public async Task CreateSpaceAsync_ValidInput_ShouldCreateAndReturnSpace()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var mockLogger = new Mock<ILogger<SpaceService>>();
            var spaceService = new SpaceService(dbContext, mockLogger.Object);

            var userId = 1; // Assuming this user exists from GetInMemoryDbContext setup
            var spaceToCreate = new Space
            {
                Name = "Test Space",
                Description = "Test Description"
                // UserId and CreatedAt will be set by the service
            };

            // Act
            var createdSpace = await spaceService.CreateSpaceAsync(spaceToCreate, userId);

            // Assert
            Assert.NotNull(createdSpace);
            Assert.Equal(spaceToCreate.Name, createdSpace.Name);
            Assert.Equal(spaceToCreate.Description, createdSpace.Description);
            Assert.Equal(userId, createdSpace.UserId);
            Assert.NotEqual(default(DateTime), createdSpace.CreatedAt); // Ensure CreatedAt is set

            // Verify in DB
            var spaceInDb = await dbContext.Spaces.FirstOrDefaultAsync(s => s.Id == createdSpace.Id);
            Assert.NotNull(spaceInDb);
            Assert.Equal(createdSpace.Name, spaceInDb.Name);
            Assert.Equal(userId, spaceInDb.UserId);
        }
    }
}
