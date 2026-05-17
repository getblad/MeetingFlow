-- Four logical bounded-context schemas in the single shared Postgres container.
-- DataAccessor owns: meetings, registrations, feedback.
-- NotificationsAccessor owns: notifications.
CREATE SCHEMA IF NOT EXISTS meetings;
CREATE SCHEMA IF NOT EXISTS registrations;
CREATE SCHEMA IF NOT EXISTS feedback;
CREATE SCHEMA IF NOT EXISTS notifications;
