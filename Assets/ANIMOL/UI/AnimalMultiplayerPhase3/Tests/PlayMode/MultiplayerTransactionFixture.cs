using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ANIMOL.AnimalUiV2;

namespace ANIMOL.AnimalMultiplayerPhase3.Tests
{
    // Synthetic authority in the test assembly. Not an operational service or receipt issuer.
    public sealed class MultiplayerTransactionFixture : AnimalMultiplayerBackendAdapterBase
    {
        protected override bool SupportsRoomEntryReconciliation => true; // Test authority only.
        public Func<MultiplayerSelectionRequest, Task<AnimalUiSnapshot>> OnRead;
        public Func<SelectionCommitRequest, Task<AnimalUiCommitResult>> OnSubmit;
        public readonly List<SelectionCommitRequest> Requests = new List<SelectionCommitRequest>();
        public int Reads;
        protected override Task<AnimalUiSnapshot> ReadMultiplayerSnapshotAsync(MultiplayerSelectionRequest c, CancellationToken token)
        { Reads++; return OnRead(c); }
        protected override Task<AnimalUiCommitResult> SubmitRoomEntryAsync(MultiplayerSelectionRequest c, SelectionCommitRequest r, CancellationToken token)
        { Requests.Add(r.Clone()); return OnSubmit(r); }
    }
}
