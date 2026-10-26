using Demo.Domain.Commands;
using Demo.Domain.DTOs;

namespace Demo.EntityFramework.Commands;

public sealed class DeleteDemoObjCommand(DemoDbContextFactory dbContextFactory)
    : IDeleteDemoObjCommand
{
    public async Task ExecuteAsync(long id)
    {
        using var dbContext = dbContextFactory.Create();
        var demoObjDto = new DemoObjDto() { Id = id };
        dbContext.DemoObjects.Remove(demoObjDto);
        await dbContext.SaveChangesAsync();
    }
}
