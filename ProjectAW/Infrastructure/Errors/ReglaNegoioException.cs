namespace ProjectAW.Infrastructure.Errors
{
    public class ReglaNegocioException : Exception
    {
        public ReglaNegocioException(string mensaje) : base(mensaje) { }
    }
}
