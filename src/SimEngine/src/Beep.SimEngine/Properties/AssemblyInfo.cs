using System.Runtime.CompilerServices;

// Only this framework's own white-box instrumentation is privileged. External games,
// their players and their test drivers compile against public contracts.
[assembly: InternalsVisibleTo("Beep.SimEngine.Tests")]
