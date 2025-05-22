using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WalletNet.Data;
using WalletNet.Services;
using WalletNet.DTOs;
using BudgetMaster.Models; // For Space
using WalletNet.Models;   // For User, Transaction

namespace WalletNet.Tests.Services
{
    public class TransactionServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var dbContext = new ApplicationDbContext(options);
            
            // Seed initial data
            var user = new User { Id = 1, UserName = "testuser", Email = "test@example.com" };
            dbContext.Users.Add(user);
            
            var space = new Space { Id = 1, Name = "Test Space", UserId = 1, CreatedAt = DateTime.UtcNow };
            dbContext.Spaces.Add(space);
            
            dbContext.SaveChanges();
            return dbContext;
        }

        private Mock<IWebHostEnvironment> GetMockWebHostEnvironment()
        {
            var mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
            mockWebHostEnvironment.Setup(m => m.WebRootPath).Returns(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            mockWebHostEnvironment.Setup(m => m.ContentRootPath).Returns(Directory.GetCurrentDirectory()); // Fallback if WebRootPath is null
            return mockWebHostEnvironment;
        }

        private Mock<ReceiptDetectionService> GetMockReceiptDetectionService()
        {
            // ReceiptDetectionService might have its own dependencies if not simple.
            // For now, assuming it can be mocked directly or has a parameterless constructor
            // or its dependencies can be easily mocked if needed.
            // If it requires specific setup (e.g., model paths for ML), that would be more complex.
            var mockReceiptDetectionService = new Mock<ReceiptDetectionService>(
                Mock.Of<ILogger<ReceiptDetectionService>>(), 
                GetMockWebHostEnvironment().Object // Assuming it takes IWebHostEnvironment
                // Add other dependencies if GetReceiptDetectionService has them.
                // For example, if it needs IConfiguration: Mock.Of<IConfiguration>()
            ); 

            // Setup default behavior for DetectAndCropReceiptAsync
            mockReceiptDetectionService
                .Setup(s => s.DetectAndCropReceiptAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string originalPath, string processedPath) => {
                    // Simulate file creation for processed file if needed by subsequent logic,
                    // or simply return true/false based on test case.
                    // For a basic test, we can assume it "succeeds" by creating a dummy file or just returns true.
                    // File.Copy(originalPath, processedPath, true); // Simplistic simulation
                    return true; 
                });
            return mockReceiptDetectionService;
        }

        [Fact]
        public async Task CreateTransactionAsync_ValidInputWithPhoto_ShouldCreateAndReturnTransaction()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var mockLogger = new Mock<ILogger<TransactionService>>();
            var mockWebHostEnvironment = GetMockWebHostEnvironment();
            var mockReceiptDetectionService = GetMockReceiptDetectionService();

            var transactionService = new TransactionService(
                dbContext, 
                mockLogger.Object, 
                mockWebHostEnvironment.Object, 
                mockReceiptDetectionService.Object);

            var userId = 1;
            var spaceId = 1; // Seeded in GetInMemoryDbContext

            // Mock IFormFile
            var mockPhoto = new Mock<IFormFile>();
            var fileName = "test_receipt.jpg";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write("Simulated image data"); // Dummy data
            writer.Flush();
            ms.Position = 0;
            mockPhoto.Setup(p => p.FileName).Returns(fileName);
            mockPhoto.Setup(p => p.Length).Returns(ms.Length);
            mockPhoto.Setup(p => p.CopyToAsync(It.IsAny<Stream>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns((Stream stream, System.Threading.CancellationToken token) => ms.CopyToAsync(stream));
            mockPhoto.Setup(p => p.OpenReadStream()).Returns(ms);


            var transactionCreateDto = new TransactionCreateDto
            {
                Amount = 100.50m,
                Date = DateTime.UtcNow,
                Description = "Test Transaction with Photo",
                Category = "Groceries",
                SpaceId = spaceId,
                Photo = mockPhoto.Object
            };

            // Act
            var createdTransactionDto = await transactionService.CreateTransactionAsync(transactionCreateDto, userId);

            // Assert
            Assert.NotNull(createdTransactionDto);
            Assert.Equal(transactionCreateDto.Amount, createdTransactionDto.Amount);
            Assert.Equal(transactionCreateDto.Description, createdTransactionDto.Description);
            Assert.Equal(userId, createdTransactionDto.UserId);
            Assert.Equal(spaceId, createdTransactionDto.SpaceId);
            Assert.NotNull(createdTransactionDto.PhotoPath); // PhotoPath should be set
            Assert.Contains($"/receipts/{userId}/processed/", createdTransactionDto.PhotoPath); // Assuming processed path is returned

            // Verify in DB
            var transactionInDb = await dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == createdTransactionDto.Id);
            Assert.NotNull(transactionInDb);
            Assert.Equal(createdTransactionDto.Amount, transactionInDb.Amount);
            Assert.Equal(userId, transactionInDb.UserId);
            Assert.Equal(createdTransactionDto.PhotoPath, transactionInDb.PhotoPath);

            // Verify mock calls (optional, but good practice)
            mockReceiptDetectionService.Verify(s => s.DetectAndCropReceiptAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task CreateTransactionAsync_ValidInputWithoutPhoto_ShouldCreateAndReturnTransaction()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var mockLogger = new Mock<ILogger<TransactionService>>();
            var mockWebHostEnvironment = GetMockWebHostEnvironment();
            var mockReceiptDetectionService = GetMockReceiptDetectionService();

            var transactionService = new TransactionService(
                dbContext, 
                mockLogger.Object, 
                mockWebHostEnvironment.Object, 
                mockReceiptDetectionService.Object);

            var userId = 1;
            var spaceId = 1;

            var transactionCreateDto = new TransactionCreateDto
            {
                Amount = 50.25m,
                Date = DateTime.UtcNow,
                Description = "Test Transaction No Photo",
                Category = "Utilities",
                SpaceId = spaceId,
                Photo = null // No photo
            };

            // Act
            var createdTransactionDto = await transactionService.CreateTransactionAsync(transactionCreateDto, userId);

            // Assert
            Assert.NotNull(createdTransactionDto);
            Assert.Equal(transactionCreateDto.Amount, createdTransactionDto.Amount);
            Assert.Null(createdTransactionDto.PhotoPath); // PhotoPath should be null

            var transactionInDb = await dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == createdTransactionDto.Id);
            Assert.NotNull(transactionInDb);
            Assert.Null(transactionInDb.PhotoPath);
            
            mockReceiptDetectionService.Verify(s => s.DetectAndCropReceiptAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
