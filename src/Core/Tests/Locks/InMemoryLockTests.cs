using Foundatio.Caching;
using Foundatio.Lock;
using Foundatio.Messaging;
using Foundatio.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Foundatio.Tests {
    public class InMemoryLockTests : LockTestBase {
        public InMemoryLockTests(CaptureFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }

        protected override ILockProvider GetLockProvider() {
            return new CacheLockProvider(new InMemoryCacheClient(), new InMemoryMessageBus());
        }

        [Fact]
        public override void CanAcquireAndReleaseLock() {
            base.CanAcquireAndReleaseLock();
        }

        [Fact]
        public override void LockWillTimeout() {
            base.LockWillTimeout();
        }
    }
}