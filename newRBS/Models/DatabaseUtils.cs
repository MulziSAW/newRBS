﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Globalization;
using Microsoft.Win32;
using System.Windows;
using System.Xml.Serialization;
using OxyPlot.Wpf;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Command;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using OxyPlot.Series;

namespace newRBS.Models
{
    /// <summary>
    /// Class responsible for managing a spectrum dictionary (<see cref="spectra"/>) with items \<ID, <see cref="DataSpectrum"/>\>.
    /// </summary>
    public static class DatabaseUtils
    {
        public delegate void EventHandlerMeasurement(Measurement measurement);
        public static event EventHandlerMeasurement EventMeasurementNew, EventMeasurementUpdate, EventMeasurementFinished, EventMeasurementRemove;

        private static TraceSource trace = new TraceSource("DatabaseUtils");

        public static void SendMeasurementNewEvent(Measurement measurement)
        {
            if (EventMeasurementNew != null)
            {
                EventMeasurementNew(measurement);
            }
        }

        public static void SendMeasurementUpdateEvent(Measurement measurement)
        {
            if (EventMeasurementUpdate != null)
            {
                EventMeasurementUpdate(measurement);
            }
        }

        public static void SendMeasurementRemoveEvent(Measurement measurement)
        {
            if (EventMeasurementRemove != null)
            {
                EventMeasurementRemove(measurement);
            }
        }

        public static int? AddNewSample()
        {
            Views.Utils.InputDialog inputDialog = new Views.Utils.InputDialog("Enter new sample name:", "");
            if (inputDialog.ShowDialog() == true)
            {
                Console.WriteLine(inputDialog.Answer);
                if (inputDialog.Answer == "")
                    return null;

                using (DatabaseDataContext Database = new DatabaseDataContext(MyGlobals.ConString))
                {
                    Sample sample = Database.Samples.FirstOrDefault(x => x.SampleName == inputDialog.Answer);

                    if (sample != null)
                    {
                        Console.WriteLine("Sample already exists!");

                        MessageBoxResult result = MessageBox.Show("Sample already exists in database!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                        return sample.SampleID;
                    }

                    // New sample
                    Console.WriteLine("new sample");

                    Sample newSample = new Sample();
                    newSample.SampleName = inputDialog.Answer;
                    newSample.MaterialID = 1;

                    Database.Samples.InsertOnSubmit(newSample);
                    Database.SubmitChanges();

                    return newSample.SampleID;
                }
            }
            else return null;
        }

        /// <summary>
        /// Function that saves spectra to a file
        /// </summary>
        /// <param name="measurementIDs">Array of IDs of the spectra to save.</param>
        /// <param name="FileName">Filename of the file to save the spectra to.</param>
        public static void ExportMeasurements(List<int> measurementIDs, string FileName)
        {
            using (DatabaseDataContext Database = new DatabaseDataContext(MyGlobals.ConString))
            {
                switch (Path.GetExtension(FileName))
                {
                    case ".xml":
                        using (TextWriter WriteFileStream = new StreamWriter(FileName))
                        {
                            XmlAttributeOverrides xOver = new XmlAttributeOverrides();

                            XmlAttributes attrs = new XmlAttributes();
                            attrs.XmlIgnore = true;

                            xOver.Add(typeof(Measurement), "Sample", attrs);
                            xOver.Add(typeof(Measurement), "SampleID", attrs);
                            xOver.Add(typeof(Measurement), "SampleRemark", attrs);
                            xOver.Add(typeof(Measurement), "SpectrumYByte", attrs);
                            xOver.Add(typeof(Measurement), "SpectrumYCalculatedByte", attrs);

                            XmlSerializer SerializerObj = new XmlSerializer(typeof(List<Measurement>), xOver);
                            SerializerObj.Serialize(WriteFileStream, Database.Measurements.Where(x => measurementIDs.Contains(x.MeasurementID)).ToList());
                            break;
                        }
                    case ".dat":
                        using (TextWriter tw = new StreamWriter(FileName))
                        {

                            // Header
                            string strName = "";
                            string strData = "Date";
                            string strRemark = "Remark";
                            string strProjectile = "Projectile";
                            string strEnergy = "Energy";
                            string strScatteringAngle = "Scattering angle";
                            string strIncidentAngle = "Incident angle";
                            string strExitAngle = "Exit angle";
                            string strEnergyChannel = "Energy / Channel";
                            string strOffset = "Offset";
                            string strSolidAngle = "Solid angle";
                            string strCharge = "Charge";
                            string strRealTime = "Real time";
                            string strLiveTime = "Live time";
                            string strFWHM = "FWHM";
                            string strChannel = "Channel";

                            List<Measurement> MeasurementsToExport = Database.Measurements.Where(x => measurementIDs.Contains(x.MeasurementID)).ToList();

                            if (!MeasurementsToExport.Any())
                            { trace.TraceEvent(TraceEventType.Warning, 0, "Can't save Measurement: MeasurementIDs not found"); tw.Close(); return; }

                            NumberFormatInfo point = new NumberFormatInfo();
                            point.NumberDecimalSeparator = ".";

                            foreach (var measurement in MeasurementsToExport)
                            {
                                strName += "\t" + measurement.MeasurementName;
                                strData += "\t" + String.Format("{0:dd.MM.yyyy HH:mm}", measurement.StartTime); ;
                                strRemark += "\t" + measurement.Sample.SampleName;
                                strProjectile += "\t" + measurement.IncomingIonAtomicNumber.ToString(point);
                                strEnergy += "\t" + measurement.IncomingIonEnergy.ToString(point);
                                strScatteringAngle += "\t" + measurement.OutcomingIonAngle.ToString(point);
                                strIncidentAngle += "\t 0.00";
                                strExitAngle += "\t" + (180 - measurement.OutcomingIonAngle).ToString(point);
                                strEnergyChannel += "\t" + measurement.EnergyCalSlope.ToString(point);
                                strOffset += "\t" + measurement.EnergyCalOffset.ToString(point);
                                strSolidAngle += "\t" + measurement.SolidAngle.ToString(point);
                                strCharge += "\t" + measurement.StopValue.ToString(point);
                                strRealTime += "\t" + (measurement.Duration - new DateTime(2000, 01, 01)).TotalSeconds.ToString(point);
                                strLiveTime += "\t";
                                strFWHM += "\t";
                                strChannel += "\t" + measurement.MeasurementName;
                            }

                            tw.WriteLine(strName);
                            tw.WriteLine(strData);
                            tw.WriteLine(strRemark);
                            tw.WriteLine(strProjectile);
                            tw.WriteLine(strEnergy);
                            tw.WriteLine(strScatteringAngle);
                            tw.WriteLine(strIncidentAngle);
                            tw.WriteLine(strExitAngle);
                            tw.WriteLine(strEnergyChannel);
                            tw.WriteLine(strOffset);
                            tw.WriteLine(strSolidAngle);
                            tw.WriteLine(strCharge);
                            tw.WriteLine(strRealTime);
                            tw.WriteLine(strLiveTime);
                            tw.WriteLine(strFWHM);
                            tw.WriteLine(strChannel);

                            List<int[]> spectrumYList = MeasurementsToExport.Select(x => x.SpectrumY).ToList();

                            int maxChannelNumber = spectrumYList.Select(x => x.Count()).ToList().Max();
                            string dataLine;

                            for (int i = 0; i < maxChannelNumber; i++)
                            {
                                dataLine = i.ToString();
                                foreach (int[] spectrumY in spectrumYList)
                                {
                                    if (i < (spectrumY.Count()))
                                        dataLine += "\t" + spectrumY[i].ToString();
                                    else
                                        dataLine += "\t";
                                }
                                tw.WriteLine(dataLine);
                            }

                            break;
                        }
                }
            }
        }

        public static List<Measurement> LoadMeasurementsFromFile(string FileName)
        {
            if (!File.Exists(FileName)) return null;

            List<Measurement> newMeasurements = new List<Measurement>();
            List<List<int>> spectraY = new List<List<int>>();

            switch (Path.GetExtension(FileName))
            {
                case ".xml":
                    {
                        XmlAttributeOverrides xOver = new XmlAttributeOverrides();

                        XmlAttributes attrs = new XmlAttributes();
                        attrs.XmlIgnore = true;

                        xOver.Add(typeof(Measurement), "Sample", attrs);
                        xOver.Add(typeof(Measurement), "SampleID", attrs);
                        xOver.Add(typeof(Measurement), "SampleRemark", attrs);
                        xOver.Add(typeof(Measurement), "SpectrumYByte", attrs);
                        xOver.Add(typeof(Measurement), "SpectrumYCalculatedByte", attrs);

                        XmlSerializer SerializerObj = new XmlSerializer(typeof(List<Measurement>), xOver);
                        using (FileStream ReadFileStream = new FileStream(FileName, FileMode.Open))
                        {
                            newMeasurements = (List<Measurement>)SerializerObj.Deserialize(ReadFileStream);
                        }
                        break;
                    }
                case ".dat":
                    {
                        using (TextReader textReader = new StreamReader(FileName))
                        {
                            string line;

                            string[] lineParts = textReader.ReadLine().Split('\t');

                            int numSpectra = lineParts.Count() - 1;
                            Console.WriteLine("Number of spectra: {0}", numSpectra);

                            for (int i = 0; i < numSpectra; i++)
                            {
                                newMeasurements.Add(new Measurement());
                                spectraY.Add(new List<int>());
                                newMeasurements[i].MeasurementName = lineParts[i + 1];
                            }

                            while ((line = textReader.ReadLine()) != null)
                            {
                                lineParts = line.Split('\t');

                                for (int i = 0; i < numSpectra; i++)
                                {
                                    switch (lineParts[0])
                                    {
                                        case "Date":
                                            { newMeasurements[i].StartTime = DateTime.ParseExact(lineParts[i + 1], "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture); break; }
                                        case "Remark":
                                            { newMeasurements[i].SampleRemark = lineParts[i + 1]; break; }
                                        case "Projectile":
                                            { newMeasurements[i].IncomingIonAtomicNumber = Int32.Parse(lineParts[i + 1]); break; }
                                        case "Energy":
                                            { newMeasurements[i].IncomingIonEnergy = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                                        case "Scattering angle":
                                            { newMeasurements[i].OutcomingIonAngle = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                                        case "Incident angle":
                                            { newMeasurements[i].IncomingIonAngle = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                                        case "Exit angle":
                                            { break; }
                                        case "Energy / Channel":
                                            { newMeasurements[i].EnergyCalSlope = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                                        case "Offset":
                                            { newMeasurements[i].EnergyCalOffset = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                                        case "Solid angle":
                                            { newMeasurements[i].SolidAngle = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                                        case "Charge":
                                            { break; }
                                        case "Real time":
                                            { newMeasurements[i].Duration = new DateTime(2000, 01, 01) + TimeSpan.FromSeconds(Convert.ToDouble(lineParts[i + 1].Replace(".", ","))); break; }
                                        case "Live time":
                                            { break; }
                                        case "FWHM":
                                            { break; }
                                        case "Channel":
                                            { break; }
                                        case "":
                                            { break; }
                                        default:
                                            { spectraY[i].Add(Int32.Parse(lineParts[i + 1].Replace(" ", ""))); break; }
                                    }
                                }
                            }

                            for (int i = 0; i < numSpectra; i++)
                            {
                                newMeasurements[i].SpectrumY = spectraY[i].ToArray();
                                newMeasurements[i].NumOfChannels = spectraY[i].Count();
                                newMeasurements[i].Orientation = "(undefined)";
                                newMeasurements[i].StopType = "(undefined)";
                                newMeasurements[i].StopTime = newMeasurements[i].StartTime + (newMeasurements[i].Duration - new DateTime(2000, 01, 01));
                                newMeasurements[i].Chamber = "(undefined)";
                            }
                        }
                        break;
                    }
            }
            return newMeasurements;
        }

        public static void DeleteMeasurements(List<int> MeasurementIDs)
        {
            if (MeasurementIDs.Count() == 0) return;

            using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
            {
                Database.Measurements.DeleteAllOnSubmit(Database.Measurements.Where(x => MeasurementIDs.Contains(x.MeasurementID)));
                Database.SubmitChanges();
            }
        }

        public static void SaveMeasurementImage(int MeasurementID, string FileName)
        {
            using (FileStream fileStream = File.Create(FileName))
            {
                var plotModel = SimpleIoc.Default.GetInstance<ViewModels.MeasurementPlotViewModel>().plotModel;
                switch (Path.GetExtension(FileName))
                {
                    case ".png":
                        {
                            PngExporter.Export(plotModel, fileStream, 1000, 600, OxyColors.White);
                            break;
                        }
                    case ".pdf":
                        {
                            PdfExporter.Export(plotModel, fileStream, 1000, 600);
                            break;
                        }
                    case ".svg":
                        {
                            OxyPlot.SvgExporter.Export(plotModel, fileStream, 1000, 600, true);
                            break;
                        }
                    case ".dat":
                        {
                            if (!plotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left).Title.Contains("interval"))
                            { MessageBox.Show("Only data sets with data binning cas be saved this way. Use export instead!", "Error"); return; }

                            using (StreamWriter streamWriter = new StreamWriter(fileStream))
                            {
                                List<List<DataPoint>> pointsList = new List<List<DataPoint>>();

                                // Header
                                string LongName = "Energy";
                                string Unit = "keV";
                                string Comment = "";

                                foreach (var s in plotModel.Series)
                                {
                                    LongName += "\t Counts";
                                    Unit += "\t" + (plotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left).Title).Replace("Counts per ", "");
                                    Comment += "\t" + s.Title;

                                    pointsList.Add(((OxyPlot.Series.AreaSeries)s).Points);
                                }
                                streamWriter.WriteLine(LongName);
                                streamWriter.WriteLine(Unit);
                                streamWriter.WriteLine(Comment);

                                // Data
                                List<int> xAxis = new List<int>();

                                foreach (List<DataPoint> dataPointList in pointsList)
                                    foreach (DataPoint dataPoint in dataPointList)
                                        xAxis.Add((int)dataPoint.X);

                                xAxis = xAxis.Distinct().OrderBy(x => x).ToList();

                                string dataString;
                                NumberFormatInfo point = new NumberFormatInfo();
                                point.NumberDecimalSeparator = ".";

                                foreach (int x in xAxis)
                                {
                                    dataString = x.ToString();
                                    foreach (List<DataPoint> dataPointList in pointsList)
                                    {
                                        DataPoint d = dataPointList.FirstOrDefault(i => i.X == x);
                                        if (d.X != 0)
                                            dataString += "\t" + d.Y.ToString(point);
                                        else
                                            dataString += "\t";
                                    }
                                    streamWriter.WriteLine(dataString);
                                }
                                streamWriter.Flush(); // Added
                            }
                            break;
                        }
                }
            }
        }
    }
}
