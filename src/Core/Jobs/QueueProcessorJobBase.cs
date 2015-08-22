﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Foundatio.Queues;
using Foundatio.Utility;
using Foundatio.Logging;
using Nito.AsyncEx.Synchronous;

namespace Foundatio.Jobs {
    public abstract class QueueProcessorJobBase<T> : JobBase, IQueueProcessorJob where T : class {
        protected readonly IQueue<T> _queue;

        public QueueProcessorJobBase(IQueue<T> queue) {
            _queue = queue;
        }

        protected bool AutoComplete { get; set; }

        protected async override Task<JobResult> RunInternalAsync(CancellationToken token) {
            QueueEntry<T> queueEntry = null;
            try {
                queueEntry = _queue.Dequeue(null, token);
            } catch (Exception ex) {
                if (!(ex is TimeoutException)) {
                    Logger.Error().Exception(ex).Message("Error trying to dequeue message: {0}", ex.Message).Write();
                    return JobResult.FromException(ex);
                }
            }

            if (queueEntry == null)
                return JobResult.Success;

            var lockValue = GetQueueItemLock(queueEntry);
            if (lockValue == null) {
                Logger.Warn().Message("Unable to acquire lock for queue entry '{0}'.", queueEntry.Id).Write();
                return JobResult.FailedWithMessage("Unable to acquire lock for queue entry.");
            }
            Logger.Trace().Message("Processing queue entry '{0}'.", queueEntry.Id).Write();

            using (lockValue) {
                try
                {
                    var result = await ProcessQueueItem(queueEntry);

                    if (!AutoComplete)
                        return result;

                    if (result.IsSuccess)
                        queueEntry.Complete();
                    else
                        queueEntry.Abandon();

                    return result;
                }
                catch
                {
                    queueEntry.Abandon();
                    throw;
                }
            }
        }

        protected virtual IDisposable GetQueueItemLock(QueueEntry<T> queueEntry) {
            return Disposable.Empty;
        }

        public void RunUntilEmpty(CancellationToken cancellationToken = default(CancellationToken)) {
            RunUntilEmptyAsync(cancellationToken).WaitWithoutException(cancellationToken);
        }

        public async Task RunUntilEmptyAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await RunContinuousAsync(cancellationToken: cancellationToken,
                continuationCallback: () =>
                {
                    long queueCount = _queue.GetQueueCount();
                    long workingCount = _queue.GetWorkingCount();
                    Logger.Trace().Message("RunUntilEmpty continuation: queue: {0} working={1}", queueCount, workingCount).Write();
                    return queueCount + workingCount > 0;
                });
        }

        protected abstract Task<JobResult> ProcessQueueItem(QueueEntry<T> queueEntry);
    }

    public interface IQueueProcessorJob : IDisposable {
        void RunUntilEmpty(CancellationToken cancellationToken = default(CancellationToken));
        Task RunUntilEmptyAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
