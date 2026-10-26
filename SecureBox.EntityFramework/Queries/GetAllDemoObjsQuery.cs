using Demo.Domain.Models;
using Demo.Domain.Queries;
using Microsoft.EntityFrameworkCore;

namespace Demo.EntityFramework.Queries;

public sealed class GetAllDemoObjsQuery(DemoDbContextFactory dbContextFactory) : IGetAllDemoObjQuery
{
    public async Task<IEnumerable<DemoObj>> ExecuteAsync()
    {
        using var dbContext = dbContextFactory.Create();
        var entries = await dbContext.DemoObjects.ToListAsync();
        return entries.Select(e => new DemoObj(e.Id, e.Date, e.Note));
    }
}
