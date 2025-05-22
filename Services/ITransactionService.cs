using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using WalletNet.DTOs;

namespace WalletNet.Services
{
    public interface ITransactionService
    {
        Task<TransactionResponseDto?> GetTransactionByIdAsync(int transactionId, int userId);
        Task<IEnumerable<TransactionResponseDto>> GetTransactionsByUserIdAsync(int userId);
        Task<IEnumerable<TransactionResponseDto>> GetTransactionsBySpaceIdAsync(int spaceId, int userId);
        Task<TransactionResponseDto?> CreateTransactionAsync(TransactionCreateDto transactionDto, int userId);
        Task<TransactionResponseDto?> UpdateTransactionAsync(int transactionId, TransactionUpdateDto transactionDto, int userId);
        Task<bool> DeleteTransactionAsync(int transactionId, int userId);
    }
}
