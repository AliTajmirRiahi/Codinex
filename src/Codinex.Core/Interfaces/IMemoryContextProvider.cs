using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{
    /// <summary>
    /// Provides information about the currently open documents.
    /// </summary>
    public interface IMemoryContextProvider
    {
        MemoryContext GetContext();
    }
}
