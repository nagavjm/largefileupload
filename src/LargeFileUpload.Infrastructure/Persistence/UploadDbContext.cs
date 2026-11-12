using System.Reflection;
using LargeFileUpload.Application.Common.Interfaces;
using LargeFileUpload.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LargeFileUpload.Infrastructure.Persistence;

public class UploadDbContext : DbContext, IApplicationDbContext
{
    public UploadDbContext(DbContextOptions<UploadDbContext> options) : base(options)
    {
    }

    public DbSet<UploadSession> UploadSessions => Set<UploadSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
