using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnhollowerBaseLib;

namespace VRCFaceTracking.Varjo
{
    class VarjoTrackingInterface : ITrackingModule
    {
        private Thread _updateThread;
        private static CancellationTokenSource _cancellationToken;
        private static readonly VarjoTracker tracker = new VarjoTracker();

        public bool SupportsEye => true;
        public bool SupportsLip => false;

        public (bool eyeSuccess, bool lipSuccess) Initialize(bool eye, bool lip)
        {
            _cancellationToken?.Cancel();
            _updateThread?.Abort();
            bool pipeConnected = tracker.ConnectToPipe();

            return (pipeConnected, false);
        }

        public void StartThread()
        {
            _cancellationToken = new CancellationTokenSource();
            _updateThread = new Thread(() =>
            {
                IL2CPP.il2cpp_thread_attach(IL2CPP.il2cpp_domain_get());
                while (!_cancellationToken.IsCancellationRequested)
                {
                    Update();
                    Thread.Sleep(10);
                }
            });
            _updateThread.Start();
        }

        public void Teardown()
        {
            _cancellationToken.Cancel();
            tracker.Teardown();
            _cancellationToken.Dispose();
        }

        public void Update()
        {
            if (!UnifiedLibManager.EyeEnabled) return;

            tracker.Update();

            UnifiedTrackingData.LatestEyeData.UpdateData(tracker.memoryGazeData);
        }
    }
}
