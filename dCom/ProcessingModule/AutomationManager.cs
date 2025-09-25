using Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for automated work.
    /// </summary>
    public class AutomationManager : IAutomationManager, IDisposable
    {
        private Thread automationWorker;
        private AutoResetEvent automationTrigger;
        private IStorage storage;
        private IProcessingManager processingManager;
        private int delayBetweenCommands;
        private IConfiguration configuration;
        private bool stopRequested = false;

        public AutomationManager(IStorage storage, IProcessingManager processingManager, AutoResetEvent automationTrigger, IConfiguration configuration)
        {
            this.storage = storage;
            this.processingManager = processingManager;
            this.configuration = configuration;
            this.automationTrigger = automationTrigger;
        }

        private void InitializeAndStartThreads()
        {
            InitializeAutomationWorkerThread();
            StartAutomationWorkerThread();
        }

        private void InitializeAutomationWorkerThread()
        {
            automationWorker = new Thread(AutomationWorker_DoWork);
            automationWorker.Name = "Automation Thread";
        }

        private void StartAutomationWorkerThread()
        {
            automationWorker.Start();
        }

        private void AutomationWorker_DoWork()
        {
            EGUConverter eguConverter = new EGUConverter();
            PointIdentifier pidBaterija = new PointIdentifier(PointType.ANALOG_OUTPUT, 2000); //stavljeno na pocetak jer je lakse za pracenje
            PointIdentifier pidT1 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 1000);
            PointIdentifier pidT2 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 1001);
            PointIdentifier pidT3 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 1002);
            PointIdentifier pidT4 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 1003);
            PointIdentifier pidI1 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 3000);
            PointIdentifier pidI2 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 3001);

            List<PointIdentifier> all = new List<PointIdentifier> { pidBaterija, pidT1, pidT2, pidT3, pidT4, pidI1, pidI2 };

            while (!stopRequested)
            {
                automationTrigger.WaitOne();
                var pts = storage.GetPoints(all);
                if (pts.Count != 7) continue;

                //lakse je da sve podelimo na varijable nego da svaki put pristupamo listi
                var baterija = pts[0] as IAnalogPoint; // raw podaci, mi koristimo EGU vrednost
                var usb1 = pts[1] as IDigitalPoint; // T1
                var usb2 = pts[2] as IDigitalPoint; // T2
                var usb3 = pts[3] as IDigitalPoint; // T3
                var uticnica = pts[4] as IDigitalPoint; // T4
                var napajanje1 = pts[5] as IDigitalPoint; // I1
                var napajanje2 = pts[6] as IDigitalPoint; // I2

                if (baterija == null) continue;
                IConfigItem cfg = baterija.ConfigItem;

                double capEGU = baterija.EguValue; //koristimo EGU vrednost za kapacitet baterije
                double praznjenje = 0;
                if (usb1.State == DState.ON) praznjenje += 1; //moglo je i preko raw value ali je ovako lepse
                if (usb2.State == DState.ON) praznjenje += 1;
                if (usb3.State == DState.ON) praznjenje += 1;
                if (uticnica.State == DState.ON) praznjenje += 3;

                double punjenje = 0;
                if (napajanje1.State == DState.ON) punjenje += 2;
                if (napajanje2.State == DState.ON) punjenje += 3;

                // Racunamo da ne mogu oba napajanja da budu upaljena istovremeno
                // Ako jesu, gasimo slabije (I1)
                if (napajanje1.State == DState.ON && napajanje2.State == DState.ON)
                {
                    processingManager.ExecuteWriteCommand(napajanje1.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, pidI1.Address, 0);
                }

                // primenjujemo punjenje i praznjenje na kapacitet baterije
                capEGU += punjenje;
                capEGU -= praznjenje;

                // Low alarm: ako je vrednost ispod LowLimit, iskljuci uticnicu T4 i ukljuci jace napajanje I2
                bool lowAlarm = capEGU < cfg.LowLimit;
                if (lowAlarm)
                {
                    if (uticnica.State == DState.ON)
                    {
                        processingManager.ExecuteWriteCommand(uticnica.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, pidT4.Address, 0);
                    }
                    if (napajanje2.State == DState.OFF)
                    {
                        processingManager.ExecuteWriteCommand(napajanje2.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, pidI2.Address, 1);
                    }
                }

                // Ako se desi da ode u minus, vratimo na minimum
                if (capEGU < cfg.EGU_Min) capEGU = cfg.EGU_Min;
                if (capEGU > cfg.EGU_Max) capEGU = cfg.EGU_Max;

                // Kada se baterija napuni do maksimuma, iskljuci napajanje koje je ukljuceno
                if (Math.Abs(capEGU - cfg.EGU_Max) < 0.0001)
                {
                    if (napajanje1.State == DState.ON)
                        processingManager.ExecuteWriteCommand(napajanje1.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, pidI1.Address, 0);
                    if (napajanje2.State == DState.ON)
                        processingManager.ExecuteWriteCommand(napajanje2.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, pidI2.Address, 0);
                }

                // Pisemo nazad bateriji novi kapacitet
                processingManager.ExecuteWriteCommand(cfg, configuration.GetTransactionId(), configuration.UnitAddress, pidBaterija.Address, (int)capEGU);

                // Delay izmedju komandi (1 sekunda)
                for (int ms = 0; ms < delayBetweenCommands; ms += 1000)
                {
                    if (stopRequested) break;
                    automationTrigger.WaitOne();
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    stopRequested = true;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Start(int delayBetweenCommands)
        {
            this.delayBetweenCommands = delayBetweenCommands * 1000;
            InitializeAndStartThreads();
        }

        public void Stop()
        {
            stopRequested = true;
            Dispose();
        }
        #endregion
    }
}
