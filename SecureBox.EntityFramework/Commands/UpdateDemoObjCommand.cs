using Demo.Domain.Commands;
using Demo.Domain.DTOs;
using Demo.Domain.Models;

namespace Demo.EntityFramework.Commands;

public sealed class UpdateDemoObjCommand(DemoDbContextFactory dbContextFactory)
    : IUpdateDemoObjCommand
{
    public async Task ExecuteAsync(DemoObj entry)
    {
        using var dbContext = dbContextFactory.Create();
        var demoObjDto = new DemoObjDto()
        {
            Id = entry.Id,
            Date = entry.Date,
            Note = entry.Note,
        };
        dbContext.DemoObjects.Update(demoObjDto);
        await dbContext.SaveChangesAsync();
    }
}
