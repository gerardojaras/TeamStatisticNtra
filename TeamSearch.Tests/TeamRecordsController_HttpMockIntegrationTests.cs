using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TeamSearch.Application.Services;
using TeamSearch.Server.Controllers;
using TeamSearch.Shared;
using TeamSearch.Shared.Dtos;
using Xunit;

namespace TeamSearch.Tests;

public class TeamRecordsController_HttpMockIntegrationTests
{
    [Fact]
    public async Task GetList_Http_ReturnsMockedData()
    {
        var mockSvc = new Mock<ITeamRecordService>();
        mockSvc.Setup(s => s.ListAsync(It.IsAny<TeamRecordQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamRecordDto>
            {
                new() { Id = 1, Team = "Mocked" }
            });
        mockSvc.Setup(s => s.CountAsync(It.IsAny<TeamRecordQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services => { services.AddScoped(_ => mockSvc.Object); });
            });

        // Resolve the mocked service from the host to ensure ConfigureTestServices replacement worked
        using var scope = factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ITeamRecordService>();
        var controller = new TeamRecordsController(svc);

        var q = new TeamRecordQuery();
        var actionResult = await controller.List(q);
        var ok = actionResult.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var api = ok!.Value as ApiResponse<List<TeamRecordDto>>;
        api.Should().NotBeNull();
        api!.Data.Should().ContainSingle(d => d.Team == "Mocked");
    }
}