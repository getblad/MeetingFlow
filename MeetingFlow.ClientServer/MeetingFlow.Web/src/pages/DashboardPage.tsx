import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { fetchDashboard, type DashboardData } from "../api/dashboardApi";

// Educational baseline:
// The dashboard data includes full Meeting entities for upcoming meetings.
// In production, the API should return a dedicated dashboard shape with minimal data.
export default function DashboardPage() {
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchDashboard()
      .then(setData)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="loading">Loading dashboard...</div>;
  if (error) return <div className="error">Error: {error}</div>;
  if (!data) return <div className="error">No data available.</div>;

  return (
    <>
      <h1>Dashboard</h1>
      <div className="stats-grid">
        <div className="stat-card">
          <div className="number">{data.totalMeetings}</div>
          <div className="label">Total Meetings</div>
        </div>
        <div className="stat-card">
          <div className="number">{data.totalRegistrations}</div>
          <div className="label">Total Registrations</div>
        </div>
        <div className="stat-card">
          <div className="number">{data.averageFeedbackRating.toFixed(1)}</div>
          <div className="label">Avg. Feedback Rating</div>
        </div>
        <div className="stat-card">
          <div className="number">{data.totalSpeakers}</div>
          <div className="label">Speakers</div>
        </div>
      </div>

      <h2>Upcoming Meetings</h2>
      {data.upcomingMeetings.length > 0 ? (
        <div className="card-grid">
          {data.upcomingMeetings.map((ev) => (
            <div className="card" key={ev.id}>
              <h3>
                <Link to={`/meetings/${ev.id}`}>{ev.title}</Link>
              </h3>
              <p className="meta">{new Date(ev.startsAt).toLocaleDateString()}</p>
              <p className="meta">
                {ev.venue?.name} &mdash; {ev.venue?.city}
              </p>
              <p className="meta">{ev.registrations?.length ?? 0} registrations</p>
            </div>
          ))}
        </div>
      ) : (
        <p>No upcoming meetings.</p>
      )}
    </>
  );
}
