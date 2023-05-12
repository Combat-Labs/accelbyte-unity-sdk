// Copyright (c) 2021-2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;

namespace AccelByte.Core
{
    internal abstract class WebRequestScheduler : IDisposable
    {
        protected readonly List<WebRequestTask> requestTask = new List<WebRequestTask>();
        private readonly WebRequestTaskOrderComparer orderComparer = new WebRequestTaskOrderComparer();

        private bool disposed = false;

        ~WebRequestScheduler()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;
            GC.SuppressFinalize(this);

            for (int i = 0; i < requestTask.Count; i++)
            {
                requestTask[i]?.Dispose();
            }

            requestTask.Clear();
        }

        internal abstract void StartScheduler();
        internal abstract void StopScheduler();

        internal void AddTask(WebRequestTask newTask)
        {
            requestTask.Add(newTask);
            requestTask.Sort(orderComparer);
        }
    }
}