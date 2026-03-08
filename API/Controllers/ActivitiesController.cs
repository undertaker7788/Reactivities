using System;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace API.Controllers;

public class ActivitiesController(AppDbContext context) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<List<Activity>>> GetActivities()
    {
        return await context.Activities.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Activity>> GetActivityDetail(string id)
    {
        // FindAsync() 會去搜尋 primary key 欄位是否有對應的資料
        var activity = await context.Activities.FindAsync(id);

        // 查無資料的話，回傳 404 not found
        if(activity == null) return NotFound();

        return activity;
    }
}
