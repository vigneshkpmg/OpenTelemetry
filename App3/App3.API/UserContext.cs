using Microsoft.EntityFrameworkCore;

namespace App3;

public class UserContext:DbContext
{
    
    public UserContext (DbContextOptions<UserContext> options)
        : base(options)
    {
    }
    public DbSet<RegistrationRequest> Registration { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegistrationRequest>().ToTable("Registration");
    }
}