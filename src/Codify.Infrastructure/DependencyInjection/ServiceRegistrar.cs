using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Codify.Infrastructure.DependencyInjection
{
    public class ServiceRegistrar
    {

        public static RegistrationReport Register(
            IServiceCollection services,
            Assembly assembly);
    }
}
