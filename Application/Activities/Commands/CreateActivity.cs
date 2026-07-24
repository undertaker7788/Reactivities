using System;
using Application.Activities.DTOs;
using Application.Core;
using AutoMapper;
using Domain;
using MediatR;
using Persistence;

namespace Application.Activities.Commands;

public class CreateActivity
{
    public class Command : IRequest<Result<string>>
    {
        public required CreateActivityDto ActivityDto { get; set; }
    }

    public class Handler(AppDbContext context, IMapper mapper) : IRequestHandler<Command, Result<string>>
    {
        public async Task<Result<string>> Handle(Command request, CancellationToken cancellationToken)
        {

            // var activity = mapper.Map<Activity>(request.ActivityDto);
            Activity activity = new Activity
            {
                Title = request.ActivityDto.Title,
                Description = request.ActivityDto.Title,
                Date = request.ActivityDto.Date,
                Category = request.ActivityDto.Category,
                City = request.ActivityDto.City,
                Venue = request.ActivityDto.Venue,
                Latitude = request.ActivityDto.Latitude,
                Longitude = request.ActivityDto.Longitude,
            };

            context.Activities.Add(activity);
            
            // SaveChangesAsync 會回傳在 db 改變的狀態數量，如果為 0 表示沒有任何異動
            var result = await context.SaveChangesAsync(cancellationToken) > 0;

            if(!result) return Result<string>.Failure("Failed to careate the activity", 400);

            return Result<string>.Success(activity.Id);
        }
    }
}
