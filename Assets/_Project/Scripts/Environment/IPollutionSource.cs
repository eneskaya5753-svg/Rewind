namespace EcoGame.Environment
{
    /// <summary>Anything that raises metrics every second while IsPolluting is true.</summary>
    public interface IPollutionSource
    {
        bool IsPolluting { get; }
        float PollutionRate { get; }
        float TemperatureRate { get; }
        float WaterPollutionRate { get; }
    }
}
