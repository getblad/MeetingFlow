import { BrowserRouter, Routes, Route, Link } from "react-router-dom";
import MeetingsPage from "./pages/MeetingsPage";
import MeetingDetailsPage from "./pages/MeetingDetailsPage";
import AdminMeetingsPage from "./pages/AdminMeetingsPage";
import DashboardPage from "./pages/DashboardPage";
import SpeakerProfilePage from "./pages/SpeakerProfilePage";
import AuditLogPage from "./pages/AuditLogPage";
import CreateRegistrationPage from "./pages/CreateRegistrationPage";

function App() {
  return (
    <BrowserRouter>
      <nav className="navbar">
        <Link to="/" className="brand">
          MeetingFlow
        </Link>
        <div className="nav-links">
          <Link to="/">Meetings</Link>
          <Link to="/dashboard">Dashboard</Link>
          <Link to="/admin/meetings">Admin: Meetings</Link>
          <Link to="/admin/audit-log">Audit Log</Link>
          <Link to="/register">Register</Link>
        </div>
      </nav>
      <main className="container">
        <Routes>
          <Route path="/" element={<MeetingsPage />} />
          <Route path="/meetings/:id" element={<MeetingDetailsPage />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/admin/meetings" element={<AdminMeetingsPage />} />
          <Route path="/admin/audit-log" element={<AuditLogPage />} />
          <Route path="/speakers/:id" element={<SpeakerProfilePage />} />
          <Route path="/register" element={<CreateRegistrationPage />} />
        </Routes>
      </main>
      <footer className="footer">
        <p>MeetingFlow &mdash; DTO Architecture Teaching Baseline</p>
      </footer>
    </BrowserRouter>
  );
}

export default App;
