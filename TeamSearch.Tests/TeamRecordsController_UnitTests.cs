using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TeamSearch.Application.Services;
using TeamSearch.Server.Controllers;
using TeamSearch.Shared;
using TeamSearch.Shared.Dtos;
using Xunit;

namespace TeamSearch.Tests;

public class TeamRecordsController_UnitTests
{
    [Fact]
    public async Task List_ReturnsSuccessResponse_WithItemsAndMeta()
    {
        var svc = new Mock<ITeamRecordService>();
        var items = new List<TeamRecordDto>
        {
            new() { Id = 1, Team = "Alpha", Wins = 10 }
        };
        svc.Setup(s => s.ListAsync(It.IsAny<TeamRecordQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        svc.Setup(s => s.CountAsync(It.IsAny<TeamRecordQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var ctrl = new TeamRecordsController(svc.Object);

        var q = new TeamRecordQuery(); // defaults
        var actionResult = await ctrl.List(q, CancellationToken.None);

        var ok = actionResult.Result as OkObjectResult;
        ok.Should().NotBeNull();

        var api = ok!.Value as ApiResponse<List<TeamRecordDto>>;
        api.Should().NotBeNull();
        api!.Success.Should().BeTrue();
        api.Data.Should().HaveCount(1);
        api.Meta.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_ReturnsFailureResponse_WhenNotFound()
    {
        var svc = new Mock<ITeamRecordService>();
        svc.Setup(s => s.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamRecordDto?)null);

        var ctrl = new TeamRecordsController(svc.Object);

        var actionResult = await ctrl.Get(999, CancellationToken.None);
        var ok = actionResult.Result as OkObjectResult;
        ok.Should().NotBeNull();

        var api = ok!.Value as ApiResponse<TeamRecordDto?>;
        api.Should().NotBeNull();
        api!.Success.Should().BeFalse();
        api.Error.Should().NotBeNull();
        api.Error!.Code.Should().Be("not_found");
    }
}