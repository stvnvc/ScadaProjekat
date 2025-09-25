using Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProcessingModule
{

    public class Acquisitor : IDisposable
    {
        private AutoResetEvent acquisitionTrigger;
        private IProcessingManager processingManager;
        private Thread acquisitionWorker;
        private IStateUpdater stateUpdater;
        private IConfiguration configuration;

		public Acquisitor(AutoResetEvent acquisitionTrigger, IProcessingManager processingManager, IStateUpdater stateUpdater, IConfiguration configuration)
        {
            this.stateUpdater = stateUpdater;
            this.acquisitionTrigger = acquisitionTrigger;
            this.processingManager = processingManager;
            this.configuration = configuration;
            this.InitializeAcquisitionThread();
            this.StartAcquisitionThread();
        }

        #region Private Methods

        private void InitializeAcquisitionThread()
        {
            this.acquisitionWorker = new Thread(Acquisition_DoWork);
            this.acquisitionWorker.Name = "Acquisition thread";
        }

		private void StartAcquisitionThread()
        {
            acquisitionWorker.Start();
        }

		private void Acquisition_DoWork()
        {
            List<IConfigItem> configItems = configuration.GetConfigurationItems();
            while (true)
            {
                acquisitionTrigger.WaitOne();
                foreach (var i in configItems)
                {
                    i.SecondsPassedSinceLastPoll += 1;
                    if (i.SecondsPassedSinceLastPoll == i.AcquisitionInterval) //ovo nam bas ni ne treba jer sve cita na 1 sekundu
                    {
                        processingManager.ExecuteReadCommand(i, this.configuration.GetTransactionId(), this.configuration.UnitAddress, i.StartAddress, i.NumberOfRegisters);

                        i.SecondsPassedSinceLastPoll = 0;
                    }
                }
            }
        }

        #endregion Private Methods

        /// <inheritdoc />
        public void Dispose()
        {
            acquisitionWorker.Abort();
        }
    }
}