using System.Collections.Generic;
using System.Threading.Tasks;
using BudgetMaster.Models; // Assuming Space is in BudgetMaster.Models
using WalletNet.Models; // For User, if needed directly, though userId is int

namespace WalletNet.Services
{
    public interface ISpaceService
    {
        Task<Space?> GetSpaceByIdAsync(int spaceId, int userId);
        Task<IEnumerable<Space>> GetSpacesByUserIdAsync(int userId);
        Task<Space> CreateSpaceAsync(Space space, int userId); // Space object here will be mapped from a DTO
        Task<Space?> UpdateSpaceAsync(int spaceId, Space spaceUpdateData, int userId); // spaceUpdateData will be mapped from a DTO
        Task<bool> DeleteSpaceAsync(int spaceId, int userId);
    }
}
