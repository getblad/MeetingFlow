using DataAccessor.Contracts;
using System.Net.Http.Json;

namespace AiChatEngine.Clients;

public class DataAccessorClient(HttpClient http)
{
    public async Task<IReadOnlyList<MeetingSummaryDto>> GetMeetingsAsync()
        => await http.GetFromJsonAsync<List<MeetingSummaryDto>>("/data/meetings") ?? [];

    public async Task<MeetingDetailsDto?> GetMeetingAsync(Guid id)
    {
        var response = await http.GetAsync($"/data/meetings/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MeetingDetailsDto>();
    }

    public async Task<IReadOnlyList<MeetingTaskDto>> GetTasksAsync()
        => await http.GetFromJsonAsync<List<MeetingTaskDto>>("/data/tasks") ?? [];

    public async Task<IReadOnlyList<MeetingTaskDto>> GetTasksByMeetingAsync(Guid meetingId)
        => await http.GetFromJsonAsync<List<MeetingTaskDto>>(
            $"/data/tasks/by-meeting/{meetingId}") ?? [];

    public async Task<MeetingTaskDto?> CreateTaskAsync(CreateMeetingTaskRequest request)
    {
        var response = await http.PostAsJsonAsync("/data/tasks", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MeetingTaskDto>()
            : null;
    }

    public async Task<MeetingTaskDto?> CompleteTaskAsync(Guid taskId)
    {
        var existing = await http.GetFromJsonAsync<MeetingTaskDto>($"/data/tasks/{taskId}");
        if (existing is null) return null;
        var update = new UpdateMeetingTaskRequest(
            existing.Title,
            IsCompleted: true,
            existing.AssignedTo);
        var response = await http.PutAsJsonAsync($"/data/tasks/{taskId}", update);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MeetingTaskDto>()
            : null;
    }

    public async Task<bool> DeleteTaskAsync(Guid taskId)
    {
        var resp = await http.DeleteAsync($"/data/tasks/{taskId}");
        return resp.IsSuccessStatusCode;
    }
}
