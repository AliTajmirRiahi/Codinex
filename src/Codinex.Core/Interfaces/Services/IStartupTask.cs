using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codinex.Core.Interfaces.Services
{
    public interface IStartupTask
    {
        Task StartAsync();
    }
}
