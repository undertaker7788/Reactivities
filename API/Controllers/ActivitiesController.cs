using System;
using Application.Activities.Commands;
using Application.Activities.DTOs;
using Application.Activities.Queries;
using Application.Core;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class ActivitiesController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<PagedList<ActivityDto, DateTime?>>> GetActivities([FromQuery]ActivityParams activityParams)
    {
        return HandleResult(await Mediator.Send(new GetActivityList.Query { Params = activityParams }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ActivityDto>> GetActivityDetail(string id)
    {
        var result = await Mediator.Send(new GetActivityDetails.Query { Id = id });
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<string>> CreateActivity(CreateActivityDto activityDto)
    {
        var result = await Mediator.Send(new CreateActivity.Command { ActivityDto = activityDto });
        return HandleResult(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "IsActivityHost")]
    public async Task<ActionResult> EditActivity(EditActivityDto activityDto)
    {
        var result = await Mediator.Send(new EditActivity.Command { ActivityDto = activityDto });
        return HandleResult(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "IsActivityHost")]
    public async Task<ActionResult> DeleteActivity(string id)
    {
        var result = await Mediator.Send(new DeleteActivity.Command { Id = id });
        return HandleResult(result);
    }

    [HttpPost("{id}/attend")]
    public async Task<ActionResult> Attend(string id)
    {
        return HandleResult(await Mediator.Send(new UpdateAttendance.Command { Id = id }));
    }
}