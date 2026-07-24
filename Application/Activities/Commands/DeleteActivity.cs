using System;
using Application.Core;
using MediatR;
using Persistence;

namespace Application.Activities.Commands;

public class DeleteActivity
{
    public class Command : IRequest<Result<Unit>>
    {
        public required string Id { get; set; }
    }

    public class Handler(AppDbContext context) : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            var activity = await context.Activities
                .FindAsync([request.Id], cancellationToken);

            if(activity == null) return Result<Unit>.Failure("Activity not found", 404);

            context.Remove(activity);

            // SaveChangesAsync 會回傳在 db 改變的狀態數量，如果為 0 表示沒有任何異動
            var result = await context.SaveChangesAsync(cancellationToken) > 0;

            if(!result) return Result<Unit>.Failure("Failed to delete the activity", 400);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}