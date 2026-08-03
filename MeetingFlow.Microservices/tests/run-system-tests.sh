#!/usr/bin/env bash

set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MICROSERVICES_DIR="$(cd -- "$SCRIPT_DIR/.." && pwd)"
COMPOSE_FILE="$MICROSERVICES_DIR/docker-compose.yml"
SYSTEM_ENV_FILE="$SCRIPT_DIR/system-tests.env"
TEST_PROJECT="$SCRIPT_DIR/MeetingFlow.Microservices.IntegrationTests/MeetingFlow.Microservices.IntegrationTests.csproj"
PROJECT_NAME="meetingflow-system-tests-$$"

COMPOSE=(
    docker compose
    --project-name "$PROJECT_NAME"
    --file "$COMPOSE_FILE"
    --env-file "$SYSTEM_ENV_FILE"
)

cleanup() {
    local exit_code=$?
    trap - EXIT INT TERM

    if (( exit_code != 0 )); then
        echo
        echo "System test failed. Docker Compose logs:"
        "${COMPOSE[@]}" logs --no-color --tail 200 || true
    fi

    echo "Stopping system-test environment '$PROJECT_NAME'..."
    "${COMPOSE[@]}" down --volumes --remove-orphans --timeout 10 || true
    exit "$exit_code"
}

trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

host_port() {
    local service=$1
    local container_port=$2
    local endpoint

    endpoint=$("${COMPOSE[@]}" port "$service" "$container_port" | tail -n 1)
    echo "${endpoint##*:}"
}

wait_for_http() {
    local service=$1
    local url=$2
    local deadline=$((SECONDS + 45))

    until curl --fail --silent --show-error --max-time 2 "$url/health" >/dev/null 2>&1; do
        if (( SECONDS >= deadline )); then
            echo "Service '$service' did not become healthy at '$url/health'." >&2
            return 1
        fi
        sleep 0.25
    done
}

wait_for_notification_consumer() {
    local deadline=$((SECONDS + 30))

    until "${COMPOSE[@]}" exec -T rabbitmq \
        rabbitmqctl list_queues name consumers --quiet 2>/dev/null \
        | grep -Eq 'notifications\.registration-created[[:space:]]+[1-9][0-9]*'; do
        if (( SECONDS >= deadline )); then
            echo "NotificationsAccessor did not subscribe to the registration queue." >&2
            return 1
        fi
        sleep 0.25
    done
}

echo "Starting isolated system-test environment '$PROJECT_NAME'..."
"${COMPOSE[@]}" up --build --detach --wait --wait-timeout 180

DATAACCESSOR_URL="http://127.0.0.1:$(host_port dataaccessor 5010)"
NOTIFICATIONS_URL="http://127.0.0.1:$(host_port notifications-accessor 5011)"
SCHEDULING_URL="http://127.0.0.1:$(host_port scheduling-engine 5020)"
AI_CHAT_URL="http://127.0.0.1:$(host_port ai-chat-engine 5040)"
MEETINGS_URL="http://127.0.0.1:$(host_port meetings-manager 5030)"
REGISTRATIONS_URL="http://127.0.0.1:$(host_port registrations-manager 5031)"
GATEWAY_URL="http://127.0.0.1:$(host_port gateway 8080)"

wait_for_http "DataAccessor" "$DATAACCESSOR_URL"
wait_for_http "NotificationsAccessor" "$NOTIFICATIONS_URL"
wait_for_http "SchedulingEngine" "$SCHEDULING_URL"
wait_for_http "AiChatEngine" "$AI_CHAT_URL"
wait_for_http "MeetingsManager" "$MEETINGS_URL"
wait_for_http "RegistrationsManager" "$REGISTRATIONS_URL"
wait_for_http "Gateway" "$GATEWAY_URL"
wait_for_notification_consumer

export MEETINGFLOW_SYSTEM_GATEWAY_URL="$GATEWAY_URL"
export MEETINGFLOW_SYSTEM_NOTIFICATIONS_URL="$NOTIFICATIONS_URL"

echo "Environment is ready. Running the system test through $GATEWAY_URL..."
dotnet test "$TEST_PROJECT" --filter "Category=System"
