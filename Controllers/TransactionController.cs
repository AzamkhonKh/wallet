namespace wallet_net.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WalletNet.Models;
using WalletNet.Data;

[Route("api/transaction")]
[ApiController]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TransactionController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Transactions
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
    {
        return await _context.Transactions.ToListAsync();
    }

    // GET: api/Transactions/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Transaction>> GetTransaction(long id)
    {
        var Transaction = await _context.Transactions.FindAsync(id);

        if (Transaction == null)
        {
            return NotFound();
        }

        return Transaction;
    }

    // PUT: api/Transactions/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTransaction(long id, Transaction Transaction)
    {
        if (id != Transaction.Id)
        {
            return BadRequest();
        }

        _context.Entry(Transaction).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TransactionExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/Transactions
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Transaction>> PostTransaction(Transaction Transaction)
    {
        _context.Transactions.Add(Transaction);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetTransaction", new { id = Transaction.Id }, Transaction);
    }

    // DELETE: api/Transactions/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(long id)
    {
        var Transaction = await _context.Transactions.FindAsync(id);
        if (Transaction == null)
        {
            return NotFound();
        }

        _context.Transactions.Remove(Transaction);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TransactionExists(long id)
    {
        return _context.Transactions.Any(e => e.Id == id);
    }
}
