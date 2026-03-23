namespace EDFCatalogoTablasNet.Services
{
    /// <summary>
    /// Servicio para mantener el ID del circuito actual en Blazor Server
    /// </summary>
    public class CircuitIdService
    {
        private static readonly AsyncLocal<string?> _circuitId = new AsyncLocal<string?>();

        public string? CircuitId
        {
            get => _circuitId.Value;
            set => _circuitId.Value = value;
        }
    }
}

