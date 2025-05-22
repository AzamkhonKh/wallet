using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BudgetMaster.Models; // For Space model
using WalletNet.Models;    // For User model
using WalletNet.Services;  // For ISpaceService
using WalletNet.DTOs;     // For Space DTOs

namespace WalletNet.Controllers
{
    [Authorize]
    [Route("api/spaces")]
    [ApiController]
    public class SpacesController : ControllerBase
    {
        private readonly ISpaceService _spaceService;
        private readonly UserManager<User> _userManager;

        public SpacesController(ISpaceService spaceService, UserManager<User> userManager)
        {
            _spaceService = spaceService;
            _userManager = userManager;
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                // This should ideally not happen if [Authorize] is working correctly
                // and the NameIdentifier claim is an int.
                // Consider how User.Id (int) is stored in claims. If it's the ASP.NET Identity User.Id (string),
                // then this needs adjustment to fetch the User object and use its int Id.
                // For now, assuming NameIdentifier directly holds the int User.Id.
                throw new InvalidOperationException("User ID not found or invalid in token.");
            }
            return userId;
        }

        // GET: api/spaces
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SpaceResponseDto>>> GetSpaces()
        {
            var userId = GetCurrentUserId();
            var spaces = await _spaceService.GetSpacesByUserIdAsync(userId);
            
            var spaceDtos = spaces.Select(s => new SpaceResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                CreatedAt = s.CreatedAt,
                UserId = s.UserId
            });
            
            return Ok(spaceDtos);
        }

        // GET: api/spaces/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<SpaceResponseDto>> GetSpace(int id)
        {
            var userId = GetCurrentUserId();
            var space = await _spaceService.GetSpaceByIdAsync(id, userId);

            if (space == null)
            {
                return NotFound();
            }

            var spaceDto = new SpaceResponseDto
            {
                Id = space.Id,
                Name = space.Name,
                Description = space.Description,
                CreatedAt = space.CreatedAt,
                UserId = space.UserId
            };

            return Ok(spaceDto);
        }

        // POST: api/spaces
        [HttpPost]
        public async Task<ActionResult<SpaceResponseDto>> CreateSpace([FromBody] SpaceCreateDto spaceCreateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            
            var space = new Space // Mapping DTO to Space model
            {
                Name = spaceCreateDto.Name,
                Description = spaceCreateDto.Description
                // UserId and CreatedAt will be set by the service
            };

            var createdSpace = await _spaceService.CreateSpaceAsync(space, userId);

            var spaceDto = new SpaceResponseDto
            {
                Id = createdSpace.Id,
                Name = createdSpace.Name,
                Description = createdSpace.Description,
                CreatedAt = createdSpace.CreatedAt,
                UserId = createdSpace.UserId
            };
            
            return CreatedAtAction(nameof(GetSpace), new { id = createdSpace.Id }, spaceDto);
        }

        // PUT: api/spaces/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSpace(int id, [FromBody] SpaceUpdateDto spaceUpdateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();

            // Map DTO to a temporary Space object for passing update data
            var spaceUpdateData = new Space
            {
                Name = spaceUpdateDto.Name,
                Description = spaceUpdateDto.Description
            };
            
            var updatedSpace = await _spaceService.UpdateSpaceAsync(id, spaceUpdateData, userId);

            if (updatedSpace == null)
            {
                return NotFound(); // Or Forbid if the space exists but doesn't belong to the user
            }

            return NoContent(); // Standard response for successful PUT
        }

        // DELETE: api/spaces/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSpace(int id)
        {
            var userId = GetCurrentUserId();
            var success = await _spaceService.DeleteSpaceAsync(id, userId);

            if (!success)
            {
                return NotFound(); // Or Forbid
            }

            return NoContent();
        }
    }
}
