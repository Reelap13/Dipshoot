namespace Game.ProcGen
{
    public abstract class ProcGenPass
    {
        public abstract string Id { get; }

        public virtual GeneratorBackendKind Backend => GeneratorBackendKind.MainThread;

        public virtual void Declare(GenerationPassContract contract)
        {
        }

        public abstract void Execute(GenerationContext context);
    }
}
