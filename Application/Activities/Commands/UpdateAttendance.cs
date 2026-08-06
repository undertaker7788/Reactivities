using System;
using Application.Core;
using Application.Interfaces;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Activities.Commands;

public class UpdateAttendance
{
    public class Command : IRequest<Result<Unit>>
    {
        public required string Id { get; set; }
    }

    public class Handler(IUserAccessor userAccessor, AppDbContext context) : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            var activtiy = await context.Activities
                .Include(x => x.Attendees)
                .ThenInclude(x => x.User)
                .SingleOrDefaultAsync(x => x.Id == request.Id);

            if(activtiy == null)
            {
                return Result<Unit>.Failure("Activity not found", 404);
            }

            var user = await userAccessor.GetUserAsync();

            var attendee = activtiy.Attendees.FirstOrDefault(x => x.UserId == user.Id);
            var isHost = activtiy.Attendees.Any(x => x.UserId == user.Id && x.IsHost);

            // if(isHost)
            // {
            //     activtiy.IsCancelled = !activtiy.IsCancelled;
            // }
            // else
            // {
            //     if (attendee == null)
            //     {
            //         activtiy.Attendees.Add(new ActivityAttendee
            //         {
            //             ActivityId = request.Id,
            //             UserId = user.Id,
            //             IsHost = false
            //         });
            //     }
            //     else
            //     {
            //         activtiy.Attendees.Remove(attendee);
            //     }
            // }

            // 只要 user 是 activity host，不管目前 activity 是否 isCancelled，
            // attendee 一定一直會有這個 user 的資料。
            if (attendee != null)
            {
                if (isHost)
                {
                    activtiy.IsCancelled = !activtiy.IsCancelled;
                }
                else
                {
                    activtiy.Attendees.Remove(attendee);
                }
            }
            else
            {
                activtiy.Attendees.Add(new ActivityAttendee
                {
                    ActivityId = request.Id,
                    UserId = user.Id,
                    IsHost = false
                });
            }

            var result = await context.SaveChangesAsync();

            return result > 0
                ? Result<Unit>.Success(Unit.Value)
                : Result<Unit>.Failure("Problem udating the DB", 400);
        }
    }
}