// Educational baseline:
// These TypeScript types mirror the backend EF Core entities directly.
// In production, you would NOT mirror persistence models 1:1 in the frontend.
// Instead, you would define smaller, page/component-specific types or generate
// them from an OpenAPI spec.

export type Meeting = {
  id: string;
  title: string;
  description: string;
  status: string;
  startsAt: string;
  endsAt: string;
  createdAt: string;
  updatedAt?: string | null;
  internalNotes?: string | null;
  adminOnlyCode?: string | null;
  venueId: string;
  venue?: Venue;
  sessions: Session[];
  registrations: Registration[];
  feedback: Feedback[];
};

export type Session = {
  id: string;
  meetingId: string;
  meeting?: Meeting;
  speakerId: string;
  speaker?: Speaker;
  title: string;
  description: string;
  startsAt: string;
  endsAt: string;
  roomName: string;
  internalNotes?: string | null;
};

export type Speaker = {
  id: string;
  fullName: string;
  bio: string;
  email: string;
  phone?: string | null;
  company?: string | null;
  internalNotes?: string | null;
  sessions: Session[];
};

export type Attendee = {
  id: string;
  fullName: string;
  email: string;
  phone?: string | null;
  company?: string | null;
  internalNotes?: string | null;
  registrations: Registration[];
  feedback: Feedback[];
  notifications: Notification[];
};

export type Registration = {
  id: string;
  meetingId: string;
  meeting?: Meeting;
  attendeeId: string;
  attendee?: Attendee;
  registeredAt: string;
  ticketType: string;
  paymentStatus: string;
  internalPaymentReference?: string | null;
};

export type Venue = {
  id: string;
  name: string;
  address: string;
  city: string;
  capacity: number;
  internalContactName?: string | null;
  internalContactPhone?: string | null;
  meetings: Meeting[];
};

export type Feedback = {
  id: string;
  meetingId: string;
  meeting?: Meeting;
  attendeeId: string;
  attendee?: Attendee;
  rating: number;
  comment: string;
  createdAt: string;
  moderationNotes?: string | null;
};

export type Notification = {
  id: string;
  attendeeId: string;
  attendee?: Attendee;
  type: string;
  subject: string;
  body: string;
  rawPayloadJson?: string | null;
  sentAt?: string | null;
};

export type AuditLogEntry = {
  id: string;
  entityType: string;
  entityId: string;
  action: string;
  actorName: string;
  createdAt: string;
  technicalDetails?: string | null;
};
