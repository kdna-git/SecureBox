using Demo.Domain.Commands;
using Demo.Domain.DTOs;
using Demo.Domain.Models;

namespace Demo.EntityFramework.Commands;

public sealed class CreateDemoObjCommand(DemoDbContextFactory dbContextFactory)
    : ICreateDemoObjCommand
{
    public async Task<DemoObj> ExecuteAsync(DemoObj entry)
    {
        using var dbContext = dbContextFactory.Create();
        var demoObjDto = new DemoObjDto() { Date = entry.Date, Note = entry.Note };
        dbContext.DemoObjects.Add(demoObjDto);
        await dbContext.SaveChangesAsync();

        return new(demoObjDto.Id, demoObjDto.Date, demoObjDto.Note);
    }
}
