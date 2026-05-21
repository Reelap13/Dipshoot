namespace Game.ProcGen
{
    public enum GenerationDiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class GenerationDiagnostic
    {
        public GenerationDiagnostic(GenerationDiagnosticSeverity severity, string message, string passId = null)
        {
            Severity = severity;
            Message = message;
            PassId = passId;
        }

        public GenerationDiagnosticSeverity Severity { get; }

        public string Message { get; }

        public string PassId { get; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(PassId)
                ? $"{Severity}: {Message}"
                : $"{Severity} [{PassId}]: {Message}";
        }
    }
}
