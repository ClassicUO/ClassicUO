// SPDX-License-Identifier: BSD-2-Clause

// EventSink is a static, process-wide event hub. Tests that subscribe to it
// (EventSinkTests, PacketHandlersTests, the per-manager Subscribers/* suites)
// all call EventSink.ClearAll() during their fixture lifecycle, which races
// when xUnit runs test classes in parallel. Disable parallel test execution
// across this assembly so static state changes are serialized.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
