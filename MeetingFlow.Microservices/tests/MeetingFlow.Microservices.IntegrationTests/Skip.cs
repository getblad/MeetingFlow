namespace MeetingFlow.Microservices.IntegrationTests;

// Small helper to short-circuit when the docker stack isn't running. xUnit 2.9
// has no first-class Skip API, so we throw a recognisable exception that surfaces
// "stack not running" as the test failure message instead of a wall of asserts.
static class Skip
{
    public static void If(GatewayFixture fx)
    {
        if (!fx.StackUp)
            throw new InvalidOperationException(fx.StackDownReason ?? "Stack not reachable");
    }
}
