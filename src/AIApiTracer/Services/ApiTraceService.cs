using AIApiTracer.Models;
using System.Collections.Concurrent;

namespace AIApiTracer.Services;

public interface IApiTraceService
{
    void AddTrace(ApiTraceRecord record);
    IEnumerable<ApiTraceRecord> GetTraces();
    ApiTraceRecord? GetTrace(Guid id);
    void ClearTraces();
}

public class ApiTraceService : IApiTraceService
{
    private readonly ConcurrentDictionary<Guid, ApiTraceRecord> _traces = new();
    private readonly int _maxRecords;

    public ApiTraceService(IConfiguration configuration)
    {
        _maxRecords = configuration.GetValue<int>("ApiTrace:MaxRecords", 1000);
    }

    public void AddTrace(ApiTraceRecord record)
    {
        _traces.TryAdd(record.Id, record);
        
        // Remove oldest records if we exceed the limit
        if (_traces.Count > _maxRecords)
        {
            var recordsToRemove = _traces.Values
                .OrderBy(r => r.Timestamp)
                .Take(_traces.Count - _maxRecords)
                .Select(r => r.Id)
                .ToList();
            
            foreach (var id in recordsToRemove)
            {
                _traces.TryRemove(id, out _);
            }
        }
    }

    public IEnumerable<ApiTraceRecord> GetTraces()
    {
        return _traces.Values.OrderByDescending(r => r.Timestamp);
    }

    public ApiTraceRecord? GetTrace(Guid id)
    {
        return _traces.TryGetValue(id, out var record) ? record : null;
    }

    public void ClearTraces()
    {
        _traces.Clear();
    }
}