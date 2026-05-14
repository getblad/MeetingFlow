using MeetingFlow.Monolith.Models;

namespace MeetingFlow.Monolith.Data;

public static class SeedData
{
    // Venue IDs
    static readonly Guid Venue1 = Guid.Parse("a1000000-0000-0000-0000-000000000001");
    static readonly Guid Venue2 = Guid.Parse("a1000000-0000-0000-0000-000000000002");
    static readonly Guid Venue3 = Guid.Parse("a1000000-0000-0000-0000-000000000003");

    // Meeting IDs
    static readonly Guid Meeting1 = Guid.Parse("b2000000-0000-0000-0000-000000000001");
    static readonly Guid Meeting2 = Guid.Parse("b2000000-0000-0000-0000-000000000002");
    static readonly Guid Meeting3 = Guid.Parse("b2000000-0000-0000-0000-000000000003");
    static readonly Guid Meeting4 = Guid.Parse("b2000000-0000-0000-0000-000000000004");
    static readonly Guid Meeting5 = Guid.Parse("b2000000-0000-0000-0000-000000000005");

    // Speaker IDs
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

        var venues = CreateVenues();
        db.Venues.AddRange(venues);

        var speakers = CreateSpeakers();
        db.Speakers.AddRange(speakers);

        var meetings = CreateMeetings();
        db.Meetings.AddRange(meetings);

        var sessions = CreateSessions();
        db.Sessions.AddRange(sessions);

        var attendees = CreateAttendees();
        db.Attendees.AddRange(attendees);

        var registrations = CreateRegistrations(attendees);
        db.Registrations.AddRange(registrations);

        var feedback = CreateFeedback(attendees);
        db.Feedback.AddRange(feedback);

        var notifications = CreateNotifications(attendees);
        db.Notifications.AddRange(notifications);

        var auditLog = CreateAuditLog();
        db.AuditLogEntries.AddRange(auditLog);

        db.SaveChanges();
    }

    static List<Venue> CreateVenues() =>
    [
        new Venue
        {
            Id = Venue1, Name = "TechHub Convention Center",
            Address = "123 Innovation Drive", City = "San Francisco", Capacity = 2000,
            InternalContactName = "Maria Gonzalez", InternalContactPhone = "+1-415-555-0101"
        },
        new Venue
        {
            Id = Venue2, Name = "Innovation Campus",
            Address = "456 Tech Boulevard", City = "Austin", Capacity = 800,
            InternalContactName = "Tom Bradley", InternalContactPhone = "+1-512-555-0202"
        },
        new Venue
        {
            Id = Venue3, Name = "Digital Arts Center",
            Address = "789 Creative Way", City = "Seattle", Capacity = 500,
            InternalContactName = "Yuki Tanaka", InternalContactPhone = "+1-206-555-0303"
        }
    ];

    static List<Speaker> CreateSpeakers() =>
    [
        new Speaker
        {
            Id = Speaker1, FullName = "Elena Rodriguez", Bio = "Cloud architect with 15 years of experience building distributed systems at scale.",
            Email = "elena.rodriguez@techcorp.com", Phone = "+1-415-555-1001", Company = "AWS",
            InternalNotes = "Preferred speaker. Negotiated reduced fee for 2026 season. Budget code: SPK-2026-A1."
        },
        new Speaker
        {
            Id = Speaker2, FullName = "Marcus Chen", Bio = "Frontend lead specializing in micro-frontends and design systems.",
            Email = "marcus.chen@vercel.dev", Phone = "+1-650-555-1002", Company = "Vercel",
            InternalNotes = "Requires AV setup with dual monitors. Has dietary restriction: vegetarian."
        },
        new Speaker
        {
            Id = Speaker3, FullName = "Sarah Okafor", Bio = "VP of Engineering focused on payment systems and API design.",
            Email = "sarah.okafor@stripe.com", Phone = "+1-415-555-1003", Company = "Stripe",
            InternalNotes = "VIP speaker. Flew in business class last time — check contract for travel terms."
        },
        new Speaker
        {
            Id = Speaker4, FullName = "David Kim", Bio = "CTO and startup advisor with deep expertise in consensus algorithms.",
            Email = "david.kim@techstart.io", Phone = "+1-510-555-1004", Company = "TechStart",
            InternalNotes = "Sometimes cancels last minute. Have backup speaker ready."
        },
        new Speaker
        {
            Id = Speaker5, FullName = "Aisha Patel", Bio = "ML engineer building AI-powered developer tools and testing frameworks.",
            Email = "aisha.patel@google.com", Phone = "+1-650-555-1005", Company = "Google",
            InternalNotes = "Google requires pre-approval for all public talks. Allow 3 weeks lead time."
        },
        new Speaker
        {
            Id = Speaker6, FullName = "James Morrison", Bio = "DevOps lead and observability expert with deep Kubernetes experience.",
            Email = "james.morrison@netflix.com", Phone = "+1-408-555-1006", Company = "Netflix",
            InternalNotes = "Charges $5,000 per session. Invoice to be sent to AP-Meetings mailbox."
        },
        new Speaker
        {
            Id = Speaker7, FullName = "Lisa Tanaka", Bio = "Product VP driving design system adoption across enterprise organizations.",
            Email = "lisa.tanaka@atlassian.com", Phone = "+1-415-555-1007", Company = "Atlassian",
            InternalNotes = "Wants to promote Atlassian products. Keep slides review mandatory."
        },
        new Speaker
        {
            Id = Speaker8, FullName = "Robert Andersen", Bio = "Security architect specializing in zero-trust and cloud security patterns.",
            Email = "robert.andersen@cloudflare.com", Phone = "+1-512-555-1008", Company = "Cloudflare",
            InternalNotes = "Background check completed 2025-12. Clearance level: standard."
        }
    ];

    static List<Meeting> CreateMeetings()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            new Meeting
            {
                Id = Meeting1, Title = "Frontend Architecture Summit", VenueId = Venue1,
                Description = "A two-day summit exploring modern frontend architecture patterns including micro-frontends, design systems, and performance optimization.",
                Status = "Published", StartsAt = now.AddDays(30), EndsAt = now.AddDays(31),
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-5),
                InternalNotes = "Expecting 1500+ attendees. Extra security requested. Budget approved: $120,000.",
                AdminOnlyCode = "FAS-2026-PROMO-50OFF"
            },
            new Meeting
            {
                Id = Meeting2, Title = "Cloud Integration Day", VenueId = Venue2,
                Description = "One-day intensive on multi-cloud strategy, serverless patterns, and cloud security.",
                Status = "Published", StartsAt = now.AddDays(60), EndsAt = now.AddDays(60).AddHours(10),
                CreatedAt = now.AddDays(-45), UpdatedAt = now.AddDays(-10),
                InternalNotes = "Co-sponsored by AWS and Azure. Revenue split: 60/40.",
                AdminOnlyCode = "CID-2026-EARLY-30"
            },
            new Meeting
            {
                Id = Meeting3, Title = "Distributed Systems Workshop", VenueId = Venue1,
                Description = "Hands-on workshop covering consensus algorithms, meeting sourcing, and observability in distributed systems.",
                Status = "Draft", StartsAt = now.AddDays(90), EndsAt = now.AddDays(91),
                CreatedAt = now.AddDays(-30),
                InternalNotes = "Workshop materials not finalized. David Kim may cancel — see speaker notes.",
                AdminOnlyCode = "DSW-2026-INTERNAL"
            },
            new Meeting
            {
                Id = Meeting4, Title = "Product Engineering Meetup", VenueId = Venue3,
                Description = "An evening meetup for product engineers covering feature flags, data-driven decisions, and platform engineering.",
                Status = "Published", StartsAt = now.AddDays(14), EndsAt = now.AddDays(14).AddHours(5),
                CreatedAt = now.AddDays(-90), UpdatedAt = now.AddDays(-2),
                InternalNotes = "Small venue — cap at 400. Waitlist enabled.",
                AdminOnlyCode = "PEM-2026-VIP"
            },
            new Meeting
            {
                Id = Meeting5, Title = "AI Tools for Developers", VenueId = Venue2,
                Description = "Exploring AI-assisted development: code review, testing, CI/CD, and ethical considerations.",
                Status = "Cancelled", StartsAt = now.AddDays(120), EndsAt = now.AddDays(121),
                CreatedAt = now.AddDays(-20),
                InternalNotes = "Cancelled due to budget cuts. Refund all tickets. Legal review pending.",
                AdminOnlyCode = "AITD-2026-CANCELLED"
            }
        ];
    }

    static List<Session> CreateSessions()
    {
        var baseDate = DateTimeOffset.UtcNow;
        return
        [
            // Meeting 1 - Frontend Architecture Summit
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000001"), MeetingId = Meeting1, SpeakerId = Speaker2,
                Title = "Micro-Frontends at Scale", Description = "How to decompose monolithic frontends into independently deployable micro-frontends.",
                StartsAt = baseDate.AddDays(30).AddHours(9), EndsAt = baseDate.AddDays(30).AddHours(10), RoomName = "Main Hall",
                InternalNotes = "Marcus requested a 15-min Q&A extension." },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000002"), MeetingId = Meeting1, SpeakerId = Speaker7,
                Title = "Design Systems That Last", Description = "Building and maintaining design systems that scale across multiple teams.",
                StartsAt = baseDate.AddDays(30).AddHours(10), EndsAt = baseDate.AddDays(30).AddHours(11), RoomName = "Main Hall",
                InternalNotes = "Slides must be reviewed for Atlassian branding." },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000003"), MeetingId = Meeting1, SpeakerId = Speaker1,
                Title = "Performance Optimization in React", Description = "Advanced techniques for optimizing React application performance.",
                StartsAt = baseDate.AddDays(30).AddHours(13), EndsAt = baseDate.AddDays(30).AddHours(14), RoomName = "Room A",
                InternalNotes = null },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000004"), MeetingId = Meeting1, SpeakerId = Speaker4,
                Title = "Accessibility-First Architecture", Description = "Designing frontend architectures with accessibility as a first-class concern.",
                StartsAt = baseDate.AddDays(30).AddHours(14), EndsAt = baseDate.AddDays(30).AddHours(15), RoomName = "Room A",
                InternalNotes = "David confirmed attendance." },

            // Meeting 2 - Cloud Integration Day
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000005"), MeetingId = Meeting2, SpeakerId = Speaker1,
                Title = "Multi-Cloud Strategy", Description = "Practical approaches to multi-cloud deployments and vendor management.",
                StartsAt = baseDate.AddDays(60).AddHours(9), EndsAt = baseDate.AddDays(60).AddHours(10), RoomName = "Keynote Hall",
                InternalNotes = null },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000006"), MeetingId = Meeting2, SpeakerId = Speaker6,
                Title = "Serverless Meeting Processing", Description = "Building meeting-driven architectures with serverless functions.",
                StartsAt = baseDate.AddDays(60).AddHours(10), EndsAt = baseDate.AddDays(60).AddHours(11), RoomName = "Keynote Hall",
                InternalNotes = "Invoice pending — $5,000." },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000007"), MeetingId = Meeting2, SpeakerId = Speaker8,
                Title = "Cloud Security Patterns", Description = "Zero-trust security patterns for cloud-native applications.",
                StartsAt = baseDate.AddDays(60).AddHours(13), EndsAt = baseDate.AddDays(60).AddHours(14), RoomName = "Room B",
                InternalNotes = null },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000008"), MeetingId = Meeting2, SpeakerId = Speaker3,
                Title = "API Gateway Best Practices", Description = "Designing resilient and scalable API gateways.",
                StartsAt = baseDate.AddDays(60).AddHours(14), EndsAt = baseDate.AddDays(60).AddHours(15), RoomName = "Room B",
                InternalNotes = "Sarah may bring a Stripe colleague as co-presenter." },

            // Meeting 3 - Distributed Systems Workshop
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000009"), MeetingId = Meeting3, SpeakerId = Speaker4,
                Title = "Consensus Algorithms Deep Dive", Description = "Raft, Paxos, and practical consensus in modern systems.",
                StartsAt = baseDate.AddDays(90).AddHours(9), EndsAt = baseDate.AddDays(90).AddHours(11), RoomName = "Workshop Lab 1",
                InternalNotes = "Hands-on lab. 50 laptop stations needed." },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000010"), MeetingId = Meeting3, SpeakerId = Speaker3,
                Title = "Meeting Sourcing in Practice", Description = "Real-world meeting sourcing with .NET and EventStoreDB.",
                StartsAt = baseDate.AddDays(90).AddHours(11), EndsAt = baseDate.AddDays(90).AddHours(13), RoomName = "Workshop Lab 1",
                InternalNotes = null },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000011"), MeetingId = Meeting3, SpeakerId = Speaker6,
                Title = "Observability at Scale", Description = "Distributed tracing, metrics, and logging for microservices.",
                StartsAt = baseDate.AddDays(90).AddHours(14), EndsAt = baseDate.AddDays(90).AddHours(16), RoomName = "Workshop Lab 2",
                InternalNotes = "Netflix demo environment access needed. Contact James 48h before." },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000012"), MeetingId = Meeting3, SpeakerId = Speaker1,
                Title = "Distributed Tracing Workshop", Description = "Hands-on workshop implementing distributed tracing with OpenTelemetry.",
                StartsAt = baseDate.AddDays(91).AddHours(9), EndsAt = baseDate.AddDays(91).AddHours(12), RoomName = "Workshop Lab 2",
                InternalNotes = null },

            // Meeting 4 - Product Engineering Meetup
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000013"), MeetingId = Meeting4, SpeakerId = Speaker7,
                Title = "Feature Flags Done Right", Description = "Implementing feature flags without creating technical debt.",
                StartsAt = baseDate.AddDays(14).AddHours(18), EndsAt = baseDate.AddDays(14).AddHours(19), RoomName = "Lounge A",
                InternalNotes = null },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000014"), MeetingId = Meeting4, SpeakerId = Speaker5,
                Title = "Data-Driven Product Decisions", Description = "Using analytics and A/B testing to drive product engineering.",
                StartsAt = baseDate.AddDays(14).AddHours(19), EndsAt = baseDate.AddDays(14).AddHours(20), RoomName = "Lounge A",
                InternalNotes = "Google pre-approval received 2026-04-15." },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000015"), MeetingId = Meeting4, SpeakerId = Speaker2,
                Title = "Building Internal Platforms", Description = "Creating developer platforms that your teams actually want to use.",
                StartsAt = baseDate.AddDays(14).AddHours(20), EndsAt = baseDate.AddDays(14).AddHours(21), RoomName = "Lounge B",
                InternalNotes = null },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000016"), MeetingId = Meeting4, SpeakerId = Speaker8,
                Title = "Technical Debt Management", Description = "Strategies for measuring and managing technical debt in product teams.",
                StartsAt = baseDate.AddDays(14).AddHours(21), EndsAt = baseDate.AddDays(14).AddHours(22), RoomName = "Lounge B",
                InternalNotes = null },

            // Meeting 5 - AI Tools for Developers
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000017"), MeetingId = Meeting5, SpeakerId = Speaker5,
                Title = "AI-Assisted Code Review", Description = "How AI tools are changing the code review process.",
                StartsAt = baseDate.AddDays(120).AddHours(9), EndsAt = baseDate.AddDays(120).AddHours(10), RoomName = "Main Stage",
                InternalNotes = "Meeting cancelled — session void." },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000018"), MeetingId = Meeting5, SpeakerId = Speaker4,
                Title = "LLM-Powered Testing", Description = "Using large language models to generate and maintain test suites.",
                StartsAt = baseDate.AddDays(120).AddHours(10), EndsAt = baseDate.AddDays(120).AddHours(11), RoomName = "Main Stage",
                InternalNotes = "Meeting cancelled — session void." },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000019"), MeetingId = Meeting5, SpeakerId = Speaker6,
                Title = "AI in CI/CD Pipelines", Description = "Integrating AI into continuous integration and deployment workflows.",
                StartsAt = baseDate.AddDays(120).AddHours(13), EndsAt = baseDate.AddDays(120).AddHours(14), RoomName = "Room C",
                InternalNotes = "Meeting cancelled — session void." },
            new Session { Id = Guid.Parse("d4000000-0000-0000-0000-000000000020"), MeetingId = Meeting5, SpeakerId = Speaker3,
                Title = "Ethical AI in Development", Description = "Responsible AI practices for software development teams.",
                StartsAt = baseDate.AddDays(120).AddHours(14), EndsAt = baseDate.AddDays(120).AddHours(15), RoomName = "Room C",
                InternalNotes = "Meeting cancelled — session void." }
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
            ("Fatima Al-Rashid", "fatima.alrashid@gmail.com", "+1-555-0015", "GulfTech"),
            ("Kevin O'Brien", "kevin.obrien@icloud.com", "+1-555-0016", "DublinDev"),
            ("Maria Popova", "maria.popova@yandex.ru", "+1-555-0017", "MoscowCode"),
            ("Chris Taylor", "chris.taylor@gmail.com", "+1-555-0018", "AussieTech"),
            ("Nadia Belmoktar", "nadia.belm@gmail.com", "+1-555-0019", "AfricaDev"),
            ("Jun Park", "jun.park@naver.com", "+1-555-0020", "SeoulSoft"),
            ("Sarah Mitchell", "sarah.mitchell@outlook.com", "+1-555-0021", "MapleCode"),
            ("Ricardo Gomez", "ricardo.gomez@gmail.com", "+1-555-0022", "BuenosDev"),
            ("Ingrid Svensson", "ingrid.svensson@gmail.com", "+1-555-0023", "NordicTech"),
            ("Daniel Kim", "daniel.kim2@gmail.com", "+1-555-0024", "SilconValleyStartup"),
            ("Zara Patel", "zara.patel@outlook.com", "+1-555-0025", "LondonFintech"),
            ("Benjamin Lee", "benjamin.lee@gmail.com", "+1-555-0026", "SingaporeSoft"),
            ("Chloe Dubois", "chloe.dubois@orange.fr", "+1-555-0027", "ParisTech"),
            ("Ryan Walsh", "ryan.walsh@gmail.com", "+1-555-0028", "BostonDev"),
            ("Aiko Yamamoto", "aiko.yamamoto@gmail.com", "+1-555-0029", "TokyoAI"),
            ("Lucas Ferreira", "lucas.ferreira@gmail.com", "+1-555-0030", "SaoPauloDev")
        };

        for (int i = 0; i < names.Length; i++)
        {
            attendees.Add(new Attendee
            {
                Id = Guid.Parse($"e5000000-0000-0000-0000-{(i + 1):D12}"),
                FullName = names[i].Item1,
                Email = names[i].Item2,
                Phone = names[i].Item3,
                Company = names[i].Item4,
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

        // Distribute 40 registrations across 5 meetings
        var meetingAssignments = new[]
        {
            (Meeting1, 12), (Meeting2, 10), (Meeting3, 8), (Meeting4, 6), (Meeting5, 4)
        };

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
                Rating = 5, Comment = "Incredible summit! The micro-frontends talk was exactly what I needed.", CreatedAt = now.AddDays(-2),
                ModerationNotes = "Approved. Potential testimonial candidate." },
            new Feedback { Id = Guid.Parse("a7000000-0000-0000-0000-000000000002"), MeetingId = Meeting1, AttendeeId = attendees[1].Id,
                Rating = 4, Comment = "Great content, but the venue was a bit crowded.", CreatedAt = now.AddDays(-1),
                ModerationNotes = "Noted. Forward venue concern to operations." },
            new Feedback { Id = Guid.Parse("a7000000-0000-0000-0000-000000000003"), MeetingId = Meeting1, AttendeeId = attendees[2].Id,
                Rating = 3, Comment = "Some sessions were too basic for senior developers.", CreatedAt = now.AddDays(-1),
                ModerationNotes = null },
            new Feedback { Id = Guid.Parse("a7000000-0000-0000-0000-000000000004"), MeetingId = Meeting2, AttendeeId = attendees[3].Id,
                Rating = 5, Comment = "The cloud security session was outstanding!", CreatedAt = now.AddDays(-3),
                ModerationNotes = "Approved." },
            new Feedback { Id = Guid.Parse("a7000000-0000-0000-0000-000000000005"), MeetingId = Meeting2, AttendeeId = attendees[4].Id,
                Rating = 4, Comment = "Well-organized meeting. Would attend again.", CreatedAt = now.AddDays(-2),
                ModerationNotes = null },
            new Feedback { Id = Guid.Parse("a7000000-0000-0000-0000-000000000006"), MeetingId = Meeting2, AttendeeId = attendees[5].Id,
                Rating = 2, Comment = "Expected more hands-on labs. Too much lecture.", CreatedAt = now.AddDays(-4),
                ModerationNotes = "Flagged for review — consider adding workshop tracks." },
            new Feedback { Id = Guid.Parse("a7000000-0000-0000-0000-000000000007"), MeetingId = Meeting4, AttendeeId = attendees[6].Id,
                Rating = 5, Comment = "Perfect meetup format! Love the evening sessions.", CreatedAt = now.AddDays(-1),
                ModerationNotes = null },
            new Feedback { Id = Guid.Parse("a7000000-0000-0000-0000-000000000008"), MeetingId = Meeting4, AttendeeId = attendees[7].Id,
                Rating = 4, Comment = "Feature flags talk was very practical.", CreatedAt = now.AddDays(-1),
                ModerationNotes = "Approved." },
            new Feedback { Id = Guid.Parse("a7000000-0000-0000-0000-000000000009"), MeetingId = Meeting4, AttendeeId = attendees[8].Id,
                Rating = 3, Comment = "Good meeting but parking was difficult.", CreatedAt = now.AddDays(-2),
                ModerationNotes = "Forward to venue operations." },
            new Feedback { Id = Guid.Parse("a7000000-0000-0000-0000-000000000010"), MeetingId = Meeting1, AttendeeId = attendees[9].Id,
                Rating = 5, Comment = "Best tech conference I've attended this year!", CreatedAt = now.AddDays(-1),
                ModerationNotes = "Use in marketing materials — get written consent first." }
        ];
    }

    static List<Notification> CreateNotifications(List<Attendee> attendees)
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            new Notification { Id = Guid.Parse("b8000000-0000-0000-0000-000000000001"), AttendeeId = attendees[0].Id,
                Type = "Email", Subject = "Registration Confirmed — Frontend Architecture Summit",
                Body = "Hi Alex, your registration for Frontend Architecture Summit is confirmed. See you there!",
                RawPayloadJson = "{\"template\":\"reg_confirm\",\"smtp_id\":\"msg_abc123\",\"provider\":\"SendGrid\",\"cost_cents\":0.12}",
                SentAt = now.AddDays(-10) },
            new Notification { Id = Guid.Parse("b8000000-0000-0000-0000-000000000002"), AttendeeId = attendees[1].Id,
                Type = "Email", Subject = "Registration Confirmed — Frontend Architecture Summit",
                Body = "Hi Priya, your registration for Frontend Architecture Summit is confirmed!",
                RawPayloadJson = "{\"template\":\"reg_confirm\",\"smtp_id\":\"msg_def456\",\"provider\":\"SendGrid\",\"cost_cents\":0.12}",
                SentAt = now.AddDays(-9) },
            new Notification { Id = Guid.Parse("b8000000-0000-0000-0000-000000000003"), AttendeeId = attendees[3].Id,
                Type = "SMS", Subject = "Meeting Reminder",
                Body = "Reminder: Cloud Integration Day is in 7 days. Don't forget your laptop!",
                RawPayloadJson = "{\"provider\":\"Twilio\",\"sid\":\"SM_xyz789\",\"cost_cents\":1.50,\"phone\":\"+1-555-0004\"}",
                SentAt = now.AddDays(-5) },
            new Notification { Id = Guid.Parse("b8000000-0000-0000-0000-000000000004"), AttendeeId = attendees[10].Id,
                Type = "Email", Subject = "Meeting Cancelled — AI Tools for Developers",
                Body = "We regret to inform you that AI Tools for Developers has been cancelled. A full refund will be processed.",
                RawPayloadJson = "{\"template\":\"meeting_cancel\",\"smtp_id\":\"msg_ghi012\",\"provider\":\"SendGrid\",\"cost_cents\":0.12,\"refund_batch\":\"REFUND-2026-05\"}",
                SentAt = now.AddDays(-3) },
            new Notification { Id = Guid.Parse("b8000000-0000-0000-0000-000000000005"), AttendeeId = attendees[0].Id,
                Type = "Push", Subject = "Schedule Update",
                Body = "The session 'Micro-Frontends at Scale' has been moved to Main Hall.",
                RawPayloadJson = "{\"provider\":\"Firebase\",\"token\":\"fcm_token_abc\",\"priority\":\"high\",\"ttl_seconds\":86400}",
                SentAt = now.AddDays(-1) }
        ];
    }

    static List<AuditLogEntry> CreateAuditLog()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            new AuditLogEntry { Id = Guid.Parse("c9000000-0000-0000-0000-000000000001"), EntityType = "Meeting", EntityId = Meeting1,
                Action = "Created", ActorName = "admin@meetingflow.dev", CreatedAt = now.AddDays(-60),
                TechnicalDetails = "INSERT INTO Meetings VALUES(...) | Transaction ID: tx_001 | Duration: 12ms | Connection: SqliteConn#4" },
            new AuditLogEntry { Id = Guid.Parse("c9000000-0000-0000-0000-000000000002"), EntityType = "Meeting", EntityId = Meeting1,
                Action = "Updated", ActorName = "admin@meetingflow.dev", CreatedAt = now.AddDays(-5),
                TechnicalDetails = "UPDATE Meetings SET Status='Published' WHERE Id='...' | Transaction ID: tx_102 | Rows affected: 1" },
            new AuditLogEntry { Id = Guid.Parse("c9000000-0000-0000-0000-000000000003"), EntityType = "Meeting", EntityId = Meeting5,
                Action = "StatusChanged", ActorName = "manager@meetingflow.dev", CreatedAt = now.AddDays(-3),
                TechnicalDetails = "Status: Published -> Cancelled | Trigger: manual | IP: 192.168.1.105 | User-Agent: Mozilla/5.0" },
            new AuditLogEntry { Id = Guid.Parse("c9000000-0000-0000-0000-000000000004"), EntityType = "Speaker", EntityId = Speaker4,
                Action = "Updated", ActorName = "admin@meetingflow.dev", CreatedAt = now.AddDays(-15),
                TechnicalDetails = "UPDATE Speakers SET Bio='...' | Field changed: Bio | Old length: 45 | New length: 89" },
            new AuditLogEntry { Id = Guid.Parse("c9000000-0000-0000-0000-000000000005"), EntityType = "Registration", EntityId = Guid.Parse("f6000000-0000-0000-0000-000000000001"),
                Action = "Created", ActorName = "system", CreatedAt = now.AddDays(-30),
                TechnicalDetails = "Auto-registration via API | Endpoint: POST /api/registrations | Response: 201 | Latency: 89ms" },
            new AuditLogEntry { Id = Guid.Parse("c9000000-0000-0000-0000-000000000006"), EntityType = "Venue", EntityId = Venue1,
                Action = "Updated", ActorName = "ops@meetingflow.dev", CreatedAt = now.AddDays(-20),
                TechnicalDetails = "Capacity changed: 1800 -> 2000 | Reason: venue expansion completed" },
            new AuditLogEntry { Id = Guid.Parse("c9000000-0000-0000-0000-000000000007"), EntityType = "Feedback", EntityId = Guid.Parse("a7000000-0000-0000-0000-000000000006"),
                Action = "Moderated", ActorName = "moderator@meetingflow.dev", CreatedAt = now.AddDays(-4),
                TechnicalDetails = "Feedback flagged for review | Moderation queue: Q-2026-05 | Priority: low" },
            new AuditLogEntry { Id = Guid.Parse("c9000000-0000-0000-0000-000000000008"), EntityType = "Notification", EntityId = Guid.Parse("b8000000-0000-0000-0000-000000000004"),
                Action = "Sent", ActorName = "system", CreatedAt = now.AddDays(-3),
                TechnicalDetails = "Batch notification: meeting_cancel | Recipients: 4 | Provider: SendGrid | Batch ID: batch_2026_cancel_001" },
            new AuditLogEntry { Id = Guid.Parse("c9000000-0000-0000-0000-000000000009"), EntityType = "Attendee", EntityId = Guid.Parse("e5000000-0000-0000-0000-000000000001"),
                Action = "DataExport", ActorName = "admin@meetingflow.dev", CreatedAt = now.AddDays(-7),
                TechnicalDetails = "GDPR data export requested | Format: JSON | Size: 2.3KB | Download link expires: 2026-05-20" },
            new AuditLogEntry { Id = Guid.Parse("c9000000-0000-0000-0000-000000000010"), EntityType = "Meeting", EntityId = Meeting2,
                Action = "Published", ActorName = "admin@meetingflow.dev", CreatedAt = now.AddDays(-10),
                TechnicalDetails = "Meeting published | Notifications queued: 10 | Social media post scheduled: Twitter, LinkedIn" }
        ];
    }
}
