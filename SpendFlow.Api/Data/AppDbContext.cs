using Microsoft.EntityFrameworkCore;
using SpendFlow.Api.Entities;

namespace SpendFlow.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Expense> Expenses { get; set; }
}