using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnhollowerBaseLib;

namespace VRCFaceTracking.Varjo
{
    class VarjoTrackingInterface : ITrackingModule
    {
        private static Thread _updateThread;
        private static CancellationTokenSource _cancellationToken;

        public bool SupportsEye => true;
        public bool SupportsLip => false;

        public (bool eyeSuccess, bool lipSuccess) Initialize(bool eye, bool lip)
        {
            _cancellationToken?.Cancel();
            _updateThread?.Abort();
            bool eyeEnabled = VarjoTracker.Init();
            VarjoTracker.SessionSetPriority(-999); //the absolute background layer. Required to avoid overlapping with OpenVR driver
            VarjoTracker.GazeInit();

            //if (_updateThread != null && !_updateThread.IsAlive) _updateThread.Start();
            return (eyeEnabled, false);
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

            if (UnifiedLibManager.EyeEnabled) VarjoTracker.SessionShutDown();

            _cancellationToken.Dispose();
        }

        public void Update()
        {
            if (UnifiedLibManager.EyeEnabled) UpdateEye();
        }

        private void UpdateEye()
        {
            VarjoTracker.GazeData gazeData = VarjoTracker.GetGaze();
            UnifiedTrackingData.LatestEyeData.UpdateData(gazeData);
        }
    }
}
