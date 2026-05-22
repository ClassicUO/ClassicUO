// SPDX-License-Identifier: BSD-2-Clause

// EventSink is a static singleton — every test that touches it shares state.
// We disable assembly-wide parallelization so tests can call EventSink.ClearAll()
// in their fixture without racing other test classes that subscribe handlers.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
