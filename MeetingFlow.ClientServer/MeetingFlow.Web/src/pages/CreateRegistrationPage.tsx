import { useState, useEffect, type FormEvent } from "react";
import { fetchMeetings } from "../api/meetingsApi";
import { createRegistration } from "../api/registrationsApi";
import type { Meeting } from "../types/models";

// Educational baseline:
// This form posts a Registration entity directly to the API.
// In production, prefer a dedicated create request model that only includes
// the fields the user should be able to set (meetingId, attendeeEmail, ticketType).
export default function CreateRegistrationPage() {
  const [meetings, setMeetings] = useState<Meeting[]>([]);
  const [meetingId, setMeetingId] = useState("");
  const [attendeeName, setAttendeeName] = useState("");
  const [attendeeEmail, setAttendeeEmail] = useState("");
  const [ticketType, setTicketType] = useState("General");
  const [success, setSuccess] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    fetchMeetings()
      .then(setMeetings)
      .catch(() => {});
  }, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSuccess(null);
    setSubmitting(true);

    try {
      // Educational baseline:
      // Sending a partial Registration entity directly.
      // The API accepts the full entity shape, so a malicious client could include
      // extra fields like internalPaymentReference.
      await createRegistration({
        meetingId,
        attendeeName,
        attendeeEmail,
        ticketType,
      });
      setSuccess("Registration created successfully!");
      setMeetingId("");
      setAttendeeName("");
      setAttendeeEmail("");
      setTicketType("General");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Unknown error");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <>
      <h1>Register for an Meeting</h1>

      <div className="warning-box">
        <strong>Educational Note:</strong> This form posts a Registration entity directly to the API. A malicious user
        could add extra fields (e.g., internalPaymentReference) to the request body. In production, always use a
        dedicated input/request model.
      </div>

      {success && (
        <div className="card" style={{ background: "#d4edda", borderColor: "#28a745", marginBottom: "1rem" }}>
          <p>{success}</p>
        </div>
      )}
      {error && <div className="error">{error}</div>}

      <form onSubmit={handleSubmit} style={{ maxWidth: 500 }}>
        <div className="field">
          <label>Meeting</label>
          <select value={meetingId} onChange={(e) => setMeetingId(e.target.value)} required>
            <option value="">-- Select Meeting --</option>
            {meetings
              .filter((ev) => ev.status === "Published")
              .map((ev) => (
                <option key={ev.id} value={ev.id}>
                  {ev.title} ({new Date(ev.startsAt).toLocaleDateString()})
                </option>
              ))}
          </select>
        </div>

        <div className="field">
          <label>Your Name</label>
          <input type="text" value={attendeeName} onChange={(e) => setAttendeeName(e.target.value)} required />
        </div>

        <div className="field">
          <label>Your Email</label>
          <input type="email" value={attendeeEmail} onChange={(e) => setAttendeeEmail(e.target.value)} required />
        </div>

        <div className="field">
          <label>Ticket Type</label>
          <select value={ticketType} onChange={(e) => setTicketType(e.target.value)}>
            <option value="General">General</option>
            <option value="VIP">VIP</option>
            <option value="Early Bird">Early Bird</option>
            <option value="Student">Student</option>
          </select>
        </div>

        <button type="submit" disabled={submitting}>
          {submitting ? "Registering..." : "Register"}
        </button>
      </form>
    </>
  );
}
