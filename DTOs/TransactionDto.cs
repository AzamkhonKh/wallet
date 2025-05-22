using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace WalletNet.DTOs
{
    public class TransactionCreateDto
    {
        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public string? Description { get; set; }

        public string? Category { get; set; }

        [Required]
        public int SpaceId { get; set; }

        public IFormFile? Photo { get; set; }
    }

    public class TransactionUpdateDto
    {
        public decimal? Amount { get; set; }

        public DateTime? Date { get; set; }

        public string? Description { get; set; }

        public string? Category { get; set; }

        public int? SpaceId { get; set; }

        public IFormFile? Photo { get; set; } // To upload a new photo
        public bool RemovePhoto { get; set; } = false; // Flag to indicate if existing photo should be removed
    }

    public class TransactionResponseDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? PhotoPath { get; set; }
        public int SpaceId { get; set; }
        public int UserId { get; set; }
    }
}
