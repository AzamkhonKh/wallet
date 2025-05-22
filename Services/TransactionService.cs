using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WalletNet.Data;
using WalletNet.DTOs;
using WalletNet.Models; // For Transaction (entity) and User

namespace WalletNet.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TransactionService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ReceiptDetectionService _receiptDetectionService; // Added

        public TransactionService(
            ApplicationDbContext context, 
            ILogger<TransactionService> logger, 
            IWebHostEnvironment webHostEnvironment,
            ReceiptDetectionService receiptDetectionService) // Added
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _receiptDetectionService = receiptDetectionService; // Added
        }

        private async Task<string?> SavePhotoAsync(IFormFile? photoFile, int userId)
        {
            if (photoFile == null || photoFile.Length == 0)
            {
                return null;
            }

            string webRootPath = _webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath;

            var userOriginalsFolder = Path.Combine(webRootPath, "receipts", userId.ToString(), "originals");
            if (!Directory.Exists(userOriginalsFolder)) Directory.CreateDirectory(userOriginalsFolder);

            var userProcessedFolder = Path.Combine(webRootPath, "receipts", userId.ToString(), "processed");
            if (!Directory.Exists(userProcessedFolder)) Directory.CreateDirectory(userProcessedFolder);

            var originalFileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
            var originalFilePath = Path.Combine(userOriginalsFolder, originalFileName);
            var relativeOriginalPath = $"/receipts/{userId}/originals/{originalFileName}";

            try
            {
                await using (var stream = new FileStream(originalFilePath, FileMode.Create))
                {
                    await photoFile.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving original photo for user {UserId}", userId);
                return null; // If saving original fails, abort.
            }

            var processedFileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName); // Or force .jpg/.png
            var processedFilePath = Path.Combine(userProcessedFolder, processedFileName);
            var relativeProcessedPath = $"/receipts/{userId}/processed/{processedFileName}";

            try
            {
                bool processingSuccess = await _receiptDetectionService.DetectAndCropReceiptAsync(originalFilePath, processedFilePath);
                if (processingSuccess)
                {
                    // Optionally delete the original file if desired
                    // try { File.Delete(originalFilePath); } catch (Exception delEx) { _logger.LogWarning(delEx, "Failed to delete original photo after processing: {originalFilePath}", originalFilePath); }
                    _logger.LogInformation("Receipt processed successfully for user {UserId}. Processed file: {ProcessedPath}", userId, relativeProcessedPath);
                    return relativeProcessedPath;
                }
                else
                {
                    _logger.LogWarning("Receipt processing failed for user {UserId}. Original file: {OriginalPath}", userId, relativeOriginalPath);
                    return relativeOriginalPath; // Return original if processing fails
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during receipt processing for user {UserId}. Original file: {OriginalPath}", userId, relativeOriginalPath);
                return relativeOriginalPath; // Return original if processing throws an error
            }
        }
        
        private void DeletePhoto(string? photoPath)
        {
            if (string.IsNullOrEmpty(photoPath)) return;

            // Convert relative path to absolute path
            // Assuming photoPath starts with '/'
            var absolutePath = Path.Combine(_webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath, photoPath.TrimStart('/'));
            
            try
            {
                if (File.Exists(absolutePath))
                {
                    File.Delete(absolutePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo at path {PhotoPath}", photoPath);
                // Log error but don't let it break the main operation
            }
        }


        public async Task<TransactionResponseDto?> CreateTransactionAsync(TransactionCreateDto transactionDto, int userId)
        {
            // Verify SpaceId belongs to the user
            var spaceExists = await _context.Spaces
                                    .AnyAsync(s => s.Id == transactionDto.SpaceId && s.UserId == userId);
            if (!spaceExists)
            {
                _logger.LogWarning("User {UserId} attempted to create transaction in invalid or unauthorized Space {SpaceId}", userId, transactionDto.SpaceId);
                return null; // Or throw specific exception
            }

            string? photoPath = null;
            if (transactionDto.Photo != null)
            {
                photoPath = await SavePhotoAsync(transactionDto.Photo, userId);
            }

            var transaction = new Transaction
            {
                Amount = transactionDto.Amount,
                Date = transactionDto.Date.ToUniversalTime(), // Store in UTC
                Description = transactionDto.Description,
                Category = transactionDto.Category,
                SpaceId = transactionDto.SpaceId,
                UserId = userId,
                PhotoPath = photoPath
            };

            try
            {
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                return MapTransactionToDto(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transaction for User {UserId}", userId);
                if (photoPath != null) DeletePhoto(photoPath); // Rollback photo save if DB save fails
                return null; 
            }
        }

        public async Task<TransactionResponseDto?> GetTransactionByIdAsync(int transactionId, int userId)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

            return transaction == null ? null : MapTransactionToDto(transaction);
        }

        public async Task<IEnumerable<TransactionResponseDto>> GetTransactionsByUserIdAsync(int userId)
        {
            return await _context.Transactions
                .Where(t => t.UserId == userId)
                .Select(t => MapTransactionToDto(t))
                .ToListAsync();
        }

        public async Task<IEnumerable<TransactionResponseDto>> GetTransactionsBySpaceIdAsync(int spaceId, int userId)
        {
            // Ensure the space belongs to the user first
            var space = await _context.Spaces.FirstOrDefaultAsync(s => s.Id == spaceId && s.UserId == userId);
            if (space == null)
            {
                return new List<TransactionResponseDto>(); // Or throw an exception
            }

            return await _context.Transactions
                .Where(t => t.SpaceId == spaceId && t.UserId == userId) // Redundant UserId check but good for safety
                .Select(t => MapTransactionToDto(t))
                .ToListAsync();
        }
        
        public async Task<TransactionResponseDto?> UpdateTransactionAsync(int transactionId, TransactionUpdateDto transactionDto, int userId)
        {
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

            if (existingTransaction == null)
            {
                return null; // Not found or not authorized
            }

            // Update SpaceId if provided and verify ownership
            if (transactionDto.SpaceId.HasValue && transactionDto.SpaceId.Value != existingTransaction.SpaceId)
            {
                var spaceExists = await _context.Spaces
                                        .AnyAsync(s => s.Id == transactionDto.SpaceId.Value && s.UserId == userId);
                if (!spaceExists)
                {
                    _logger.LogWarning("User {UserId} attempted to update transaction {TransactionId} to invalid or unauthorized Space {SpaceId}", userId, transactionId, transactionDto.SpaceId.Value);
                    return null; // Or throw specific exception
                }
                existingTransaction.SpaceId = transactionDto.SpaceId.Value;
            }

            // Handle photo update
            if (transactionDto.RemovePhoto && !string.IsNullOrEmpty(existingTransaction.PhotoPath))
            {
                DeletePhoto(existingTransaction.PhotoPath);
                existingTransaction.PhotoPath = null;
            }
            else if (transactionDto.Photo != null)
            {
                if (!string.IsNullOrEmpty(existingTransaction.PhotoPath))
                {
                    DeletePhoto(existingTransaction.PhotoPath); // Delete old photo
                }
                existingTransaction.PhotoPath = await SavePhotoAsync(transactionDto.Photo, userId);
            }

            // Update other properties
            if (transactionDto.Amount.HasValue) existingTransaction.Amount = transactionDto.Amount.Value;
            if (transactionDto.Date.HasValue) existingTransaction.Date = transactionDto.Date.Value.ToUniversalTime(); // Store in UTC
            if (transactionDto.Description != null) existingTransaction.Description = transactionDto.Description; // Allow clearing
            if (transactionDto.Category != null) existingTransaction.Category = transactionDto.Category; // Allow clearing

            try
            {
                await _context.SaveChangesAsync();
                return MapTransactionToDto(existingTransaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction {TransactionId} for User {UserId}", transactionId, userId);
                return null;
            }
        }

        public async Task<bool> DeleteTransactionAsync(int transactionId, int userId)
        {
            var transactionToDelete = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

            if (transactionToDelete == null)
            {
                return false; // Not found or not authorized
            }

            // Delete associated photo
            if (!string.IsNullOrEmpty(transactionToDelete.PhotoPath))
            {
                DeletePhoto(transactionToDelete.PhotoPath);
            }

            try
            {
                _context.Transactions.Remove(transactionToDelete);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting transaction {TransactionId} for User {UserId}", transactionId, userId);
                return false;
            }
        }

        // Manual mapping helper
        private TransactionResponseDto MapTransactionToDto(Transaction transaction)
        {
            return new TransactionResponseDto
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Date = transaction.Date,
                Description = transaction.Description,
                Category = transaction.Category,
                PhotoPath = transaction.PhotoPath,
                SpaceId = transaction.SpaceId,
                UserId = transaction.UserId
            };
        }
    }
}
