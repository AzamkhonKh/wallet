using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WalletNet.DTOs;     // For Transaction DTOs
using WalletNet.Models;    // For User model
using WalletNet.Services;  // For ITransactionService

namespace WalletNet.Controllers
{
    [Authorize]
    [Route("api/transactions")] // Changed route
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        // UserManager might not be strictly needed if all user-related logic is in GetCurrentUserId
        // and the service handles user authorization based on userId.
        // private readonly UserManager<User> _userManager; 

        public TransactionController(ITransactionService transactionService) // UserManager<User> userManager)
        {
            _transactionService = transactionService;
            // _userManager = userManager;
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                throw new InvalidOperationException("User ID not found or invalid in token.");
            }
            return userId;
        }

        // GET: api/transactions/user
        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetUserTransactions()
        {
            var userId = GetCurrentUserId();
            var transactions = await _transactionService.GetTransactionsByUserIdAsync(userId);
            return Ok(transactions);
        }

        // GET: api/transactions/space/{spaceId}
        [HttpGet("space/{spaceId}")]
        public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetSpaceTransactions(int spaceId)
        {
            var userId = GetCurrentUserId();
            var transactions = await _transactionService.GetTransactionsBySpaceIdAsync(spaceId, userId);
            // The service should handle if the user is not authorized for the space,
            // potentially returning an empty list or the GetTransactionsBySpaceIdAsync could throw/return null.
            // If it returns null (or an empty list when not found/authorized), Ok(transactions) is fine.
            return Ok(transactions);
        }
        
        // GET: api/transactions/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionResponseDto>> GetTransaction(int id)
        {
            var userId = GetCurrentUserId();
            var transaction = await _transactionService.GetTransactionByIdAsync(id, userId);

            if (transaction == null)
            {
                return NotFound();
            }
            return Ok(transaction);
        }

        // POST: api/transactions
        [HttpPost]
        public async Task<ActionResult<TransactionResponseDto>> CreateTransaction([FromForm] TransactionCreateDto transactionCreateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var createdTransaction = await _transactionService.CreateTransactionAsync(transactionCreateDto, userId);

            if (createdTransaction == null)
            {
                // This could happen if, for example, SpaceId is invalid or doesn't belong to user
                return BadRequest("Could not create transaction. Invalid SpaceId or other parameters.");
            }
            
            return CreatedAtAction(nameof(GetTransaction), new { id = createdTransaction.Id }, createdTransaction);
        }

        // PUT: api/transactions/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<TransactionResponseDto>> UpdateTransaction(int id, [FromForm] TransactionUpdateDto transactionUpdateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var updatedTransaction = await _transactionService.UpdateTransactionAsync(id, transactionUpdateDto, userId);

            if (updatedTransaction == null)
            {
                return NotFound(); // Or BadRequest if validation failed within the service
            }
            
            return Ok(updatedTransaction); // Return the updated transaction
        }

        // DELETE: api/transactions/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var userId = GetCurrentUserId();
            var success = await _transactionService.DeleteTransactionAsync(id, userId);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
