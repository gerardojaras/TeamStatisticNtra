using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TeamSearch.Application.Services;
using TeamSearch.Server.Controllers;
using TeamSearch.Shared;
using TeamSearch.Shared.Dtos;
using Xunit;

namespace TeamSearch.Tests;

public class TeamRecordsController_IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TeamRecordsController_IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HostDI_ResolvesControllerAndList_ReturnsApiResponse()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ITeamRecordService>();

        var controller = new TeamRecordsController(svc);
        var q = new TeamRecordQuery();
        var actionResult = await controller.List(q);

        var ok = actionResult.Result as OkObjectResult;
        ok.Should().NotBeNull();

        var api = ok!.Value as ApiResponse<List<TeamRecordDto>>;
        api.Should().NotBeNull();
        api!.Success.Should().BeTrue();
    }
}