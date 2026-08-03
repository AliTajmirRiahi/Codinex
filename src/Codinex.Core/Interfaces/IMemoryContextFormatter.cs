using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{
    public interface IMemoryContextFormatter
    {
        public string Format(MemoryContext context);
    }
}
