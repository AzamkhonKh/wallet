using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BudgetMaster.Models; // For Space
using WalletNet.Data;
using WalletNet.Models; // For User

namespace WalletNet.Services
{
    public class SpaceService : ISpaceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SpaceService> _logger;

        public SpaceService(ApplicationDbContext context, ILogger<SpaceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Space?> GetSpaceByIdAsync(int spaceId, int userId)
        {
            try
            {
                return await _context.Spaces
                    .FirstOrDefaultAsync(s => s.Id == spaceId && s.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving space by ID {SpaceId} for User {UserId}", spaceId, userId);
                return null;
            }
        }

        public async Task<IEnumerable<Space>> GetSpacesByUserIdAsync(int userId)
        {
            try
            {
                return await _context.Spaces
                    .Where(s => s.UserId == userId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving spaces for User {UserId}", userId);
                return new List<Space>(); // Return empty list on error
            }
        }

        public async Task<Space> CreateSpaceAsync(Space space, int userId)
        {
            if (space == null)
            {
                throw new ArgumentNullException(nameof(space));
            }

            space.UserId = userId; // Ensure UserId is set from authenticated user
            space.CreatedAt = DateTime.UtcNow;

            try
            {
                _context.Spaces.Add(space);
                await _context.SaveChangesAsync();
                return space;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating space for User {UserId}", userId);
                // Consider re-throwing or handling more gracefully depending on desired behavior
                throw; 
            }
        }

        public async Task<Space?> UpdateSpaceAsync(int spaceId, Space spaceUpdateData, int userId)
        {
            if (spaceUpdateData == null)
            {
                throw new ArgumentNullException(nameof(spaceUpdateData));
            }

            try
            {
                var existingSpace = await _context.Spaces
                    .FirstOrDefaultAsync(s => s.Id == spaceId && s.UserId == userId);

                if (existingSpace == null)
                {
                    return null; // Not found or not authorized
                }

                // Update only provided fields (Name and Description)
                if (!string.IsNullOrWhiteSpace(spaceUpdateData.Name))
                {
                    existingSpace.Name = spaceUpdateData.Name;
                }
                if (spaceUpdateData.Description != null) // Allow clearing description
                {
                    existingSpace.Description = spaceUpdateData.Description;
                }
                // UserId and CreatedAt should not be updated here

                await _context.SaveChangesAsync();
                return existingSpace;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating space {SpaceId} for User {UserId}", spaceId, userId);
                throw;
            }
        }

        public async Task<bool> DeleteSpaceAsync(int spaceId, int userId)
        {
            try
            {
                var spaceToDelete = await _context.Spaces
                    .FirstOrDefaultAsync(s => s.Id == spaceId && s.UserId == userId);

                if (spaceToDelete == null)
                {
                    return false; // Not found or not authorized
                }

                _context.Spaces.Remove(spaceToDelete);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting space {SpaceId} for User {UserId}", spaceId, userId);
                return false;
            }
        }
    }
}
