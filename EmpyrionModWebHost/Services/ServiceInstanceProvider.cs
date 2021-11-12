namespace EmpyrionModWebHost.Services
{
    public interface IProvider<T>
    {
        T Get();
    }

    public class ServiceInstanceProvider<T> : IProvider<T>
    {
        IHttpContextAccessor contextAccessor;

        public ServiceInstanceProvider(IHttpContextAccessor contextAccessor)
        {
            this.contextAccessor = contextAccessor;
        }

        T IProvider<T>.Get()
        {
            return (T)contextAccessor?.HttpContext?.RequestServices?.GetService(typeof(T));
        }
    }
}
