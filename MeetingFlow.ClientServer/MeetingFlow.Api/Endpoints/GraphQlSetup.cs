using MeetingFlow.Api.Data;
using MeetingFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MeetingFlow.Api.Endpoints;

// Educational baseline:
// GraphQL exposes EF Core entity types directly here.
// In production, consider separating GraphQL schema types from persistence models.
// With Hot Chocolate, each query resolver returns EF entity types or IQueryable<Entity>.
// This means the GraphQL schema is tightly coupled to the database schema.
public static class GraphQlSetup
{
    public class Query
    {
        // Educational baseline:
        // Returns IQueryable<Meeting> directly — the GraphQL schema mirrors the EF Core entity.
        // A client can request any field on Meeting, including InternalNotes, AdminOnlyCode, etc.
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Meeting> GetMeetings(MeetingFlowDbContext dbContext)
            => dbContext.Meetings;

        public async Task<Meeting?> GetMeetingById(MeetingFlowDbContext dbContext, Guid id)
            => await dbContext.Meetings
                .Include(e => e.Venue)
                .Include(e => e.Sessions).ThenInclude(s => s.Speaker)
                .Include(e => e.Registrations)
                .Include(e => e.Feedback)
                .FirstOrDefaultAsync(e => e.Id == id);

        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Speaker> GetSpeakers(MeetingFlowDbContext dbContext)
            => dbContext.Speakers;

        public async Task<Speaker?> GetSpeakerById(MeetingFlowDbContext dbContext, Guid id)
            => await dbContext.Speakers
                .Include(s => s.Sessions)
                .FirstOrDefaultAsync(s => s.Id == id);
    }
}

/*
Example GraphQL queries for testing:

# Meeting cards (public listing)
query MeetingCards {
  meetings {
    id
    title
    startsAt
    venue {
      name
      city
    }
  }
}

# Meeting details
query MeetingDetails($id: UUID!) {
  meetingById(id: $id) {
    id
    title
    description
    sessions {
      id
      title
      startsAt
      speaker {
        id
        fullName
      }
    }
  }
}

# Admin data — notice InternalNotes and AdminOnlyCode are accessible!
query AdminMeetings {
  meetings {
    id
    title
    status
    internalNotes
    adminOnlyCode
  }
}
*/
