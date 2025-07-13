namespace AIApiTracer.Models;

public class AIApiTracerOptions
{
    public const string SectionName = "AIApiTracer";
    
    public bool EnableOpenAICompatForwarding { get; set; }
}