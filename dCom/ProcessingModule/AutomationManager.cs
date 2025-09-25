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

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationManager"/> class.
        /// </summary>
        /// <param name="storage">The storage.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="automationTrigger">The automation trigger.</param>
        /// <param name="configuration">The configuration.</param>
        public AutomationManager(IStorage storage, IProcessingManager processingManager, AutoResetEvent automationTrigger, IConfiguration configuration)
		{
			this.storage = storage;
			this.processingManager = processingManager;
            this.configuration = configuration;
            this.automationTrigger = automationTrigger;
        }

        /// <summary>
        /// Initializes and starts the threads.
        /// </summary>
		private void InitializeAndStartThreads()
		{
			InitializeAutomationWorkerThread();
			StartAutomationWorkerThread();
		}

        /// <summary>
        /// Initializes the automation worker thread.
        /// </summary>
		private void InitializeAutomationWorkerThread()
		{
			automationWorker = new Thread(AutomationWorker_DoWork);
			automationWorker.Name = "Aumation Thread";
		}

        /// <summary>
        /// Starts the automation worker thread.
        /// </summary>
		private void StartAutomationWorkerThread()
		{
			automationWorker.Start();
		}


		private void AutomationWorker_DoWork()
		{
			EGUConverter eguConverter = new EGUConverter();
			PointIdentifier KapacitetBaterije = new PointIdentifier(PointType.ANALOG_OUTPUT, 2000); //stavili na pocetak jer je lakse za pracenje
			PointIdentifier USB1 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 1000);
			PointIdentifier USB2 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 1001);
			PointIdentifier USB3 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 1002);
			PointIdentifier Uticnica = new PointIdentifier(PointType.DIGITAL_OUTPUT, 1003);
			PointIdentifier Napajanje1 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 3000);
			PointIdentifier Napajanje2 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 3001);

            List<PointIdentifier> signali = new List<PointIdentifier> { KapacitetBaterije, USB1, USB2, USB3, Uticnica, Napajanje1, Napajanje2 };
            ushort tempCap = 0;
            {
                List<IPoint> tacke = storage.GetPoints(signali);
                IConfigItem configElement = tacke[0].ConfigItem;

				ushort kapacitet = (ushort)eguConverter.ConvertToEGU(configElement.ScaleFactor, configElement.Deviation, tacke[0].RawValue);

                //logika-------------------------------------------------------------


                //1. uticnica-----------------------------------------------------

                if (tacke[1].RawValue == 1) //ako je povezana 1. uticnica
				{
					if ((tempCap -= 1) >= 0) //ako moze da se smanji za 1 posto, smanji ga
					{
						kapacitet -= 1;
						if (kapacitet == configElement.MinValue) //isto kao == 0
						{
							tempCap = 0;
							processingManager.ExecuteWriteCommand(tacke[1].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, USB1.Address, 0); //gasimo 1. uticnicu
							automationTrigger.WaitOne();
						}
						else
						{
							processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, KapacitetBaterije.Address, kapacitet);
						}
					}
                    else //ako ne moze da se smanji, gasimo uticnicu
					{
						tempCap = 0;
						processingManager.ExecuteWriteCommand(tacke[1].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, USB1.Address, 0); //gasimo 1. uticnicu
						automationTrigger.WaitOne();
                    }
                }


                //2. uticnica-----------------------------------------------------

                if (tacke[2].RawValue == 1) //ako je povezana 2. uticnica
                {
					if((tempCap-=1) >= 0) //ako moze da se smanji za 1 posto, smanji ga
					{
						kapacitet-=1;
						if (kapacitet ==configElement.MinValue) //isto kao ==0
						{
                            tempCap = 0;
                            processingManager.ExecuteWriteCommand(tacke[2].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, USB2.Address, 0); //gasimo 2. uticnicu
                            automationTrigger.WaitOne();
                        }
						else
						{
							processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, KapacitetBaterije.Address,kapacitet);
						}
                    }
                    else //ako ne moze da se smanji, gasimo uticnicu
					{
						tempCap = 0;
						processingManager.ExecuteWriteCommand(tacke[2].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, USB2.Address, 0); //gasimo 2. uticnicu
						automationTrigger.WaitOne();
                    }
                }



                //3. uticnica-----------------------------------------------------

                if (tacke[3].RawValue == 1) //ako je povezana 3. uticnica
				{
					if((tempCap-=1) >= 0) //ako moze da se smanji za 1 posto, smanji ga
					{
						kapacitet-=1;
						if (kapacitet ==configElement.MinValue) //isto kao ==0
						{
                            tempCap = 0;
                            processingManager.ExecuteWriteCommand(tacke[3].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, USB3.Address, 0); //gasimo 1. uticnicu
                            automationTrigger.WaitOne();
                        }
						else
						{
							processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, KapacitetBaterije.Address,kapacitet);
						}
                    }
					else //ako ne moze da se smanji, gasimo uticnicu
					{
						tempCap = 0;
						processingManager.ExecuteWriteCommand(tacke[3].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, USB3.Address, 0); //gasimo 3. uticnicu
						automationTrigger.WaitOne();
					}
                }

                //4. uticinica-----------------------------------------------------

				if (tacke[4].RawValue == 1) //ako je povezana uticnica
				{
                    if ((tempCap -= 3) >= 0) //ako moze da se smanji za 1 posto, smanji ga
                    {
                        kapacitet -= 3;
                        if (kapacitet == configElement.MinValue) //isto kao ==0
                        {
                            tempCap = 0;
                            processingManager.ExecuteWriteCommand(tacke[4].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, Uticnica.Address, 0); //gasimo I4 uticnicu
                            automationTrigger.WaitOne();
                            //palimo napajanje I2
                            processingManager.ExecuteWriteCommand(tacke[6].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, Napajanje2.Address, 1);
                        }
                        else
                        {
                            processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, KapacitetBaterije.Address, kapacitet);
                        }
                    }
                    else //ako ne moze da se smanji, gasimo uticnicu
                    {
                        tempCap = 0;
                        processingManager.ExecuteWriteCommand(tacke[4].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, Uticnica.Address, 0); //gasimo I4 uticnicu
                        automationTrigger.WaitOne();
                        //palimo napajanje I2
                        processingManager.ExecuteWriteCommand(tacke[6].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, Napajanje2.Address, 1);

                    }
                }

                //napajanje I2-----------------------------------------------------

                if (tacke[6].RawValue == 1) //ako je ukljuceno napajanje I2
				{
					//da li dodati iskljucivanje I1
					if ((tempCap += 3) <= 100)
					{
						kapacitet += 3;
						if (kapacitet == configElement.MaxValue) //isto kao ==100
						{
							tempCap = 100;
							processingManager.ExecuteWriteCommand(tacke[6].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, Napajanje2.Address, 0); //gasimo napajanje I2
							automationTrigger.WaitOne();
							processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, KapacitetBaterije.Address, kapacitet); //azuriramo kapacitet

						}
						else
						{
							processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, KapacitetBaterije.Address, kapacitet);
						}
					}
					else //ako ne moze da se poveća, gasimo napajanje I2
					{
						tempCap = 100;
						processingManager.ExecuteWriteCommand(tacke[6].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, Napajanje2.Address, 0); //gasimo napajanje I2
					}
				}


                //napajanje I1-----------------------------------------------------

                if (tacke[5].RawValue == 1) //ako je ukljuceno napajanje I2
                {
                    //da li dodati iskljucivanje I2, ako je I2 ukljucen
                    if ((tempCap += 2) <= 100)
                    {
                        kapacitet +=2;
                        if (kapacitet == configElement.MaxValue) //isto kao ==100
                        {
                            tempCap = 100;
                            processingManager.ExecuteWriteCommand(tacke[5].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, Napajanje1.Address, 0); //gasimo napajanje I2
                            automationTrigger.WaitOne();
                            processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, KapacitetBaterije.Address, kapacitet); //azuriramo kapacitet

                        }
                        else
                        {
                            processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, KapacitetBaterije.Address, kapacitet);
                        }
                    }
                    else //ako ne moze da se poveća, gasimo napajanje I1
                    {
                        tempCap = 100;
                        processingManager.ExecuteWriteCommand(tacke[5].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, Napajanje2.Address, 0); //gasimo napajanje I2
                    }
                }

                //kapacitet pao ispod 20%
                //ako je ukljucena uticnica T4, gasimo je i palimo napajanje I2
                if (kapacitet < 20  && tacke[4].RawValue == 1)
				{
                    processingManager.ExecuteWriteCommand(tacke[5].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, Uticnica.Address, 0); //gasimo uticnicu T4
                    automationTrigger.WaitOne();
					processingManager.ExecuteWriteCommand(tacke[6].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, Napajanje2.Address, 1); //palimo napajanje I2
                }
				else if( kapacitet<20)
				{
					if (tacke[6].RawValue == 0) //ako nije ukljuceno napajanje I2, palimo ga
                        processingManager.ExecuteWriteCommand(tacke[6].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, Napajanje2.Address, 1); //palimo napajanje I2
                }
                //---------------------------------------------------------------------

                //rucno podesavanje-----------------------------------------------------
                //Korisnik moze rucno da ukljuci uredjaje na t1-t4 akko je kapacitet veci od 20 (LowAlarm)
                if (kapacitet>configElement.LowLimit) // >20
				{
					if (tacke[1].RawValue == 1) //ako je povezana 1. uticnica
                    {
						kapacitet -= 1;
						processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, KapacitetBaterije.Address, kapacitet);
                    }
					
					if(tacke[2].RawValue == 1) //ako je povezana 2. uticnica
					{
						kapacitet -= 1;
						processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, KapacitetBaterije.Address, kapacitet);
                    }

					if(tacke[3].RawValue == 1) //ako je povezana 3. uticnica
                    {
						kapacitet -= 1;
						processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, KapacitetBaterije.Address, kapacitet);
                    }

                    if(tacke[4].RawValue == 1) //ako je povezana T4 uticnica
                    {
						kapacitet -= 3;
						processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, KapacitetBaterije.Address, kapacitet);
                    }
                }
                for (int i = 0; i < delayBetweenCommands; i += 1000)
                    automationTrigger.WaitOne();
            }
        }

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls


        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Indication if managed objects should be disposed.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
				}
				disposedValue = true;
			}
		}


		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// GC.SuppressFinalize(this);
		}

        /// <inheritdoc />
        public void Start(int delayBetweenCommands)
		{
			this.delayBetweenCommands = delayBetweenCommands*1000;
            InitializeAndStartThreads();
		}

        /// <inheritdoc />
        public void Stop()
		{
			Dispose();
		}
		#endregion
	}
}
