using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codify.Core.Workspace;
    public sealed class WorkspaceState(WorkspaceSnapshot snapshot)
    {
        public WorkspaceSnapshot Snapshot { get; } = snapshot;
    }
