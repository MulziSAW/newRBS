﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using GalaSoft.MvvmLight.Ioc;
using System.Data.Linq;
using System.Text;
using System.Threading.Tasks;

namespace newRBS.Models
{
    /// <summary>
    /// Class responsible for simultaneous measurements of spectra on several channels. 
    /// </summary>
    public class MeasureSpectra
    {
        private CAEN_x730 myCAEN_x730;
        private DataSpectra myDataSpectra;

        /// <summary>
        /// Variable that holds the selected  channels.
        /// </summary>

        public ExpDetails expDetails = new ExpDetails { ion = EnumIon.He, ionEnergy = 1400, theta = 170 };
        public EnergyCalibration[] energyCalibration = new EnergyCalibration[8] { new EnergyCalibration { energyCalOffset = 0, energyCalSlope = 1 }, new EnergyCalibration { energyCalOffset = 0, energyCalSlope = 1 }, new EnergyCalibration { energyCalOffset = 0, energyCalSlope = 1 }, new EnergyCalibration { energyCalOffset = 0, energyCalSlope = 1 }, new EnergyCalibration { energyCalOffset = 0, energyCalSlope = 1 }, new EnergyCalibration { energyCalOffset = 0, energyCalSlope = 1 }, new EnergyCalibration { energyCalOffset = 0, energyCalSlope = 1 }, new EnergyCalibration { energyCalOffset = 0, energyCalSlope = 1 } };
        public Stop stop = new Stop { type = "Manual", value = 0 };

        private Timer[] spectraMeasurementTimer = new Timer[8] { new Timer(500), new Timer(500), new Timer(500), new Timer(500), new Timer(500), new Timer(500), new Timer(500), new Timer(500) };

        private Dictionary<int, int> activeChannels = new Dictionary<int, int>();

        /// <summary>
        /// Initializes the class and stores the handled instances of <see cref="CAEN_x730"/> and <see cref="DataSpectra"/>
        /// </summary>
        /// <param name="cAEN_x730">Instance of the class responsible for the CAEN N6730</param>
        /// <param name="dataSpectra">Instance of the class responsible for storing the measured spectra</param>
        public MeasureSpectra(CAEN_x730 cAEN_x730, DataSpectra dataSpectra)
        {
            myCAEN_x730 = SimpleIoc.Default.GetInstance<Models.CAEN_x730>();
            myDataSpectra = SimpleIoc.Default.GetInstance<Models.DataSpectra>();
        }


        /// <summary>
        /// Starts the measurement for the selected channels (<see cref="selectedChannels"/>).
        /// </summary>
        /// <returns>A list containing the IDs of the started measurements</returns>
        public List<int> StartMeasurements(List<int> selectedChannels)
        {
            List<int> IDs = new List<int>();
            Console.WriteLine("Measurement will be starte");
            foreach (int channel in selectedChannels)
            {
                myCAEN_x730.StartAcquisition(channel);
                int ID = myDataSpectra.NewSpectrum(channel, expDetails, energyCalibration[channel], "Manual", 0, true);
                IDs.Add(ID);
                activeChannels.Add(channel, ID);

                spectraMeasurementTimer[channel].Elapsed += delegate { SpectraMeasurementWorker(ID, channel); };
                spectraMeasurementTimer[channel].Start();
            }
            return IDs;
        }

        /// <summary>
        /// Stops the measurement for the selected channels (<see cref="selectedChannels"/>).
        /// </summary>
        public void StopMeasurements(List<int> selectedChannels)
        {
            foreach (int channel in selectedChannels)
            {
                if (!activeChannels.ContainsKey(channel))
                    continue;

                myCAEN_x730.StopAcquisition(channel);

                spectraMeasurementTimer[channel].Stop();

                myDataSpectra.StopSpectrum(activeChannels[channel]);

                activeChannels.Remove(channel);

            }
        }

        /// <summary>
        /// Function that is called by the spectraMeasurementTimer. It reads a spectrum sends it to the <see cref="DataSpectra"/> class.
        /// </summary>
        /// <param name="ID">ID of the measurement where the spectra will be send to.</param>
        /// <param name="channel">Channel to read the spectrum from.</param>
        private void SpectraMeasurementWorker(int ID, int channel)
        {
            int[] newSpectrumY = myCAEN_x730.GetHistogram(channel);
            myDataSpectra.SetSpectrumY(ID, newSpectrumY);
        }
    }
}
