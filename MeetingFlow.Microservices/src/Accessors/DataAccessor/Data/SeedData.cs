using DataAccessor.Models;

namespace DataAccessor.Data;

public static class SeedData
{
    static readonly Guid Venue1 = Guid.Parse("a1000000-0000-0000-0000-000000000001");
    static readonly Guid Venue2 = Guid.Parse("a1000000-0000-0000-0000-000000000002");
    static readonly Guid Venue3 = Guid.Parse("a1000000-0000-0000-0000-000000000003");

    static readonly Guid Meeting1 = Guid.Parse("b2000000-0000-0000-0000-000000000001");
    static readonly Guid Meeting2 = Guid.Parse("b2000000-0000-0000-0000-000000000002");
    static readonly Guid Meeting3 = Guid.Parse("b2000000-0000-0000-0000-000000000003");
    static readonly Guid Meeting4 = Guid.Parse("b2000000-0000-0000-0000-000000000004");
    static readonly Guid Meeting5 = Guid.Parse("b2000000-0000-0000-0000-000000000005");

    static readonly Guid Speaker1 = Guid.Parse("c3000000-0000-0000-0000-000000000001");
    static readonly Guid Speaker2 = Guid.Parse("c3000000-0000-0000-0000-000000000002");
    static readonly Guid Speaker3 = Guid.Parse("c3000000-0000-0000-0000-000000000003");
    static readonly Guid Speaker4 = Guid.Parse("c3000000-0000-0000-0000-000000000004");
    static readonly Guid Speaker5 = Guid.Parse("c3000000-0000-0000-0000-000000000005");
    static readonly Guid Speaker6 = Guid.Parse("c3000000-0000-0000-0000-000000000006");
    static readonly Guid Speaker7 = Guid.Parse("c3000000-0000-0000-0000-000000000007");
    static readonly Guid Speaker8 = Guid.Parse("c3000000-0000-0000-0000-000000000008");

    public static void Initialize(MeetingFlowDbContext db)
    {
        if (db.Venues.Any()) return;

        db.Venues.AddRange(CreateVenues());
        db.Speakers.AddRange(CreateSpeakers());
        db.Meetings.AddRange(CreateMeetings());
        db.Sessions.AddRange(CreateSessions());

        var attendees = CreateAttendees();
        db.Attendees.AddRange(attendees);
        db.Registrations.AddRange(CreateRegistrations(attendees));
        db.Feedback.AddRange(CreateFeedback(attendees));
        db.AuditLogEntries.AddRange(CreateAuditLog());

        db.SaveChanges();
    }

    static List<Venue> CreateVenues() =>
    [
        new Venue { Id = Venue1, Name = "TechHub Convention Center", Address = "123 Innovation Drive", City = "San Francisco", Capacity = 2000,
            InternalContactName = "Maria Gonzalez", InternalContactPhone = "+1-415-555-0101" },
        new Venue { Id = Venue2, Name = "Innovation Campus", Address = "456 Tech Boulevard", City = "Austin", Capacity = 800,
            InternalContactName = "Tom Bradley", InternalContactPhone = "+1-512-555-0202" },
        new Venue { Id = Venue3, Name = "Digital Arts Center", Address = "789 Creative Way", City = "Seattle", Capacity = 500,
            InternalContactName = "Yuki Tanaka", InternalContactPhone = "+1-206-555-0303" }
    ];

    static List<Speaker> CreateSpeakers() =>
    [
        new Speaker { Id = Speaker1, FullName = "Elena Rodriguez", Bio = "Cloud architect with 15 years of experience.",
            Email = "elena.rodriguez@techcorp.com", Phone = "+1-415-555-1001", Company = "AWS",
            InternalNotes = "Preferred speaker. Budget code: SPK-2026-A1." },
        new Speaker { Id = Speaker2, FullName = "Marcus Chen", Bio = "Frontend lead specializing in micro-frontends.",
            Email = "marcus.chen@vercel.dev", Phone = "+1-650-555-1002", Company = "Vercel",
            InternalNotes = "Requires AV setup with dual monitors. Vegetarian." },
        new Speaker { Id = Speaker3, FullName = "Sarah Okafor", Bio = "VP of Engineering focused on payment systems.",
            Email = "sarah.okafor@stripe.com", Phone = "+1-415-555-1003", Company = "Stripe",
            InternalNotes = "VIP speaker. Check contract for travel terms." },
        new Speaker { Id = Speaker4, FullName = "David Kim", Bio = "CTO and startup advisor.",
            Email = "david.kim@techstart.io", Phone = "+1-510-555-1004", Company = "TechStart",
            InternalNotes = "Sometimes cancels last minute. Have backup ready." },
        new Speaker { Id = Speaker5, FullName = "Aisha Patel", Bio = "ML engineer building AI-powered developer tools.",
            Email = "aisha.patel@google.com", Phone = "+1-650-555-1005", Company = "Google",
            InternalNotes = "Google requires pre-approval. Allow 3 weeks lead time." },
        new Speaker { Id = Speaker6, FullName = "James Morrison", Bio = "DevOps lead and observability expert.",
            Email = "james.morrison@netflix.com", Phone = "+1-408-555-1006", Company = "Netflix",
            InternalNotes = "Charges $5,000 per session. Invoice AP-Meetings." },
        new Speaker { Id = Speaker7, FullName = "Lisa Tanaka", Bio = "Product VP driving design system adoption.",
            Email = "lisa.tanaka@atlassian.com", Phone = "+1-415-555-1007", Company = "Atlassian",
            InternalNotes = "Slides review mandatory." },
        new Speaker { Id = Speaker8, FullName = "Robert Andersen", Bio = "Security architect specializing in zero-trust.",
            Email = "robert.andersen@cloudflare.com", Phone = "+1-512-555-1008", Company = "Cloudflare",
            InternalNotes = "Background check completed 2025-12." }
    ];

    static List<Meeting> CreateMeetings()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            new Meeting { Id = Meeting1, Title = "Frontend Architecture Summit", VenueId = Venue1,
                Description = "Two-day summit on modern frontend architecture.",
                Status = "Published", StartsAt = now.AddDays(30), EndsAt = now.AddDays(31),
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-5),
                InternalNotes = "Expecting 1500+ attendees. Budget: $120,000.",
                AdminOnlyCode = "FAS-2026-PROMO-50OFF" },
            new Meeting { Id = Meeting2, Title = "Cloud Integration Day", VenueId = Venue2,
                Description = "One-day intensive on multi-cloud strategy.",
                Status = "Published", StartsAt = now.AddDays(60), EndsAt = now.AddDays(60).AddHours(10),
                CreatedAt = now.AddDays(-45), UpdatedAt = now.AddDays(-10),
                InternalNotes = "Co-sponsored AWS+Azure. Revenue split 60/40.",
                AdminOnlyCode = "CID-2026-EARLY-30" },
            new Meeting { Id = Meeting3, Title = "Distributed Systems Workshop", VenueId = Venue1,
                Description = "Hands-on workshop on consensus algorithms.",
                Status = "Draft", StartsAt = now.AddDays(90), EndsAt = now.AddDays(91),
                CreatedAt = now.AddDays(-30),
                InternalNotes = "Workshop materials not finalized.",
                AdminOnlyCode = "DSW-2026-INTERNAL" },
            new Meeting { Id = Meeting4, Title = "Product Engineering Meetup", VenueId = Venue3,
                Description = "Evening meetup for product engineers.",
                Status = "Published", StartsAt = now.AddDays(14), EndsAt = now.AddDays(14).AddHours(5),
                CreatedAt = now.AddDays(-90), UpdatedAt = now.AddDays(-2),
                InternalNotes = "Small venue — cap at 400. Waitlist enabled.",
                AdminOnlyCode = "PEM-2026-VIP" },
            new Meeting { Id = Meeting5, Title = "AI Tools for Developers", VenueId = Venue2,
                Description = "Exploring AI-assisted development.",
                Status = "Cancelled", StartsAt = now.AddDays(120), EndsAt = now.AddDays(121),
                CreatedAt = now.AddDays(-20),
                InternalNotes = "Cancelled due to budget cuts. Refund all tickets.",
                AdminOnlyCode = "AITD-2026-CANCELLED" }
        ];
    }

    static List<Session> CreateSessions()
    {
        var baseDate = DateTimeOffset.UtcNow;
        return
        [
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000001"), MeetingId = Meeting1, SpeakerId = Speaker2,
                Title = "Micro-Frontends at Scale", Description = "Decomposing monolithic frontends.",
                StartsAt = baseDate.AddDays(30).AddHours(9), EndsAt = baseDate.AddDays(30).AddHours(10), RoomName = "Main Hall",
                InternalNotes = "Marcus requested 15-min Q&A extension." },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000002"), MeetingId = Meeting1, SpeakerId = Speaker7,
                Title = "Design Systems That Last", Description = "Building scalable design systems.",
                StartsAt = baseDate.AddDays(30).AddHours(10), EndsAt = baseDate.AddDays(30).AddHours(11), RoomName = "Main Hall",
                InternalNotes = "Slides reviewed for Atlassian branding." },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000003"), MeetingId = Meeting1, SpeakerId = Speaker1,
                Title = "Performance Optimization in React", Description = "Advanced React performance techniques.",
                StartsAt = baseDate.AddDays(30).AddHours(13), EndsAt = baseDate.AddDays(30).AddHours(14), RoomName = "Room A" },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000005"), MeetingId = Meeting2, SpeakerId = Speaker1,
                Title = "Multi-Cloud Strategy", Description = "Practical multi-cloud approaches.",
                StartsAt = baseDate.AddDays(60).AddHours(9), EndsAt = baseDate.AddDays(60).AddHours(10), RoomName = "Keynote Hall" },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000006"), MeetingId = Meeting2, SpeakerId = Speaker6,
                Title = "Serverless Meeting Processing", Description = "Event-driven serverless architectures.",
                StartsAt = baseDate.AddDays(60).AddHours(10), EndsAt = baseDate.AddDays(60).AddHours(11), RoomName = "Keynote Hall",
                InternalNotes = "Invoice pending — $5,000." },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000013"), MeetingId = Meeting4, SpeakerId = Speaker7,
                Title = "Feature Flags Done Right", Description = "Feature flags without technical debt.",
                StartsAt = baseDate.AddDays(14).AddHours(18), EndsAt = baseDate.AddDays(14).AddHours(19), RoomName = "Lounge A" },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000014"), MeetingId = Meeting4, SpeakerId = Speaker5,
                Title = "Data-Driven Product Decisions", Description = "Analytics and A/B testing.",
                StartsAt = baseDate.AddDays(14).AddHours(19), EndsAt = baseDate.AddDays(14).AddHours(20), RoomName = "Lounge A",
                InternalNotes = "Google pre-approval received." }
        ];
    }

    static List<Attendee> CreateAttendees()
    {
        var attendees = new List<Attendee>();
        var names = new[]
        {
            ("Alex Johnson", "alex.johnson@gmail.com", "+1-555-0001", "Acme Corp"),
            ("Priya Sharma", "priya.sharma@outlook.com", "+1-555-0002", "TechVentures"),
            ("Michael Brown", "michael.brown@yahoo.com", "+1-555-0003", "StartupXYZ"),
            ("Sofia Martinez", "sofia.martinez@proton.me", "+1-555-0004", "InnovateCo"),
            ("James Wilson", "james.wilson@gmail.com", "+1-555-0005", "DevHouse"),
            ("Yuki Sato", "yuki.sato@company.jp", "+1-555-0006", "NihonTech"),
            ("Emma Davis", "emma.davis@fastmail.com", "+1-555-0007", "CloudNine Inc"),
            ("Carlos Rivera", "carlos.rivera@gmail.com", "+1-555-0008", "LatamDev"),
            ("Anna Kowalski", "anna.kowalski@outlook.com", "+1-555-0009", "EuroTech"),
            ("David Nguyen", "david.nguyen@gmail.com", "+1-555-0010", "VietCode"),
            ("Rachel Green", "rachel.green@company.com", "+1-555-0011", "FashionTech"),
            ("Omar Hassan", "omar.hassan@gmail.com", "+1-555-0012", "MidEastDev"),
            ("Linda Chen", "linda.chen@outlook.com", "+1-555-0013", "PacificSoft"),
            ("Thomas Mueller", "thomas.mueller@web.de", "+1-555-0014", "BerlinBytes"),
            ("Fatima Al-Rashid", "fatima.alrashid@gmail.com", "+1-555-0015", "GulfTech")
        };

        for (int i = 0; i < names.Length; i++)
        {
            attendees.Add(new Attendee
            {
                Id = Guid.Parse($"e5000000-0000-0000-0000-{(i + 1):D12}"),
                FullName = names[i].Item1, Email = names[i].Item2, Phone = names[i].Item3, Company = names[i].Item4,
                InternalNotes = i < 5 ? $"VIP attendee — priority support. CRM ID: ATT-{1000 + i}." : null
            });
        }
        return attendees;
    }

    static List<Registration> CreateRegistrations(List<Attendee> attendees)
    {
        var registrations = new List<Registration>();
        var now = DateTimeOffset.UtcNow;
        var ticketTypes = new[] { "General", "VIP", "Early Bird", "Student" };
        var paymentStatuses = new[] { "Paid", "Pending", "Refunded", "Paid" };
        var meetingAssignments = new[] { (Meeting1, 6), (Meeting2, 5), (Meeting4, 4) };

        int regIndex = 0;
        int attendeeIndex = 0;
        foreach (var (meetingId, count) in meetingAssignments)
        {
            for (int i = 0; i < count; i++)
            {
                registrations.Add(new Registration
                {
                    Id = Guid.Parse($"f6000000-0000-0000-0000-{(regIndex + 1):D12}"),
                    MeetingId = meetingId,
                    AttendeeId = attendees[attendeeIndex % attendees.Count].Id,
                    RegisteredAt = now.AddDays(-Random.Shared.Next(5, 60)),
                    TicketType = ticketTypes[regIndex % ticketTypes.Length],
                    PaymentStatus = paymentStatuses[regIndex % paymentStatuses.Length],
                    InternalPaymentReference = $"PAY-{Guid.NewGuid().ToString()[..8].ToUpper()}-REF"
                });
                regIndex++;
                attendeeIndex++;
            }
        }
        return registrations;
    }

    static List<Feedback> CreateFeedback(List<Attendee> attendees)
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            new Feedback { Id = Guid.Parse("a7000000-0000-0000-0000-000000000001"), MeetingId = Meeting1, AttendeeId = attendees[0].Id,
                Rating = 5, Comment = "Incredible summit!", CreatedAt = now.AddDays(-2),
                ModerationNotes = "Approved. Testimonial candidate." },
            new Feedback { Id = Guid.Parse("a7000000-0000-0000-0000-000000000002"), MeetingId = Meeting1, AttendeeId = attendees[1].Id,
                Rating = 4, Comment = "Great content, venue was crowded.", CreatedAt = now.AddDays(-1),
                ModerationNotes = "Forward venue concern to operations." },
            new Feedback { Id = Guid.Parse("a7000000-0000-0000-0000-000000000004"), MeetingId = Meeting2, AttendeeId = attendees[3].Id,
                Rating = 5, Comment = "Cloud security session was outstanding!", CreatedAt = now.AddDays(-3),
                ModerationNotes = "Approved." },
            new Feedback { Id = Guid.Parse("a7000000-0000-0000-0000-000000000007"), MeetingId = Meeting4, AttendeeId = attendees[6].Id,
                Rating = 5, Comment = "Perfect meetup format!", CreatedAt = now.AddDays(-1) }
        ];
    }

    static List<AuditLogEntry> CreateAuditLog()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            new AuditLogEntry { Id = Guid.Parse("c9000000-0000-0000-0000-000000000001"), EntityType = "Meeting", EntityId = Meeting1,
                Action = "Created", ActorName = "admin@meetingflow.dev", CreatedAt = now.AddDays(-60),
                TechnicalDetails = "INSERT INTO Meetings VALUES(...) | tx_001 | 12ms" },
            new AuditLogEntry { Id = Guid.Parse("c9000000-0000-0000-0000-000000000003"), EntityType = "Meeting", EntityId = Meeting5,
                Action = "StatusChanged", ActorName = "manager@meetingflow.dev", CreatedAt = now.AddDays(-3),
                TechnicalDetails = "Status: Published -> Cancelled | IP: 192.168.1.105" }
        ];
    }
}
