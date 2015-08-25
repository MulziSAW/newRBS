﻿using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Threading;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Command;
using Microsoft.Practices.ServiceLocation;
using newRBS.ViewModels.Utils;
using Microsoft.Win32;

namespace newRBS.ViewModels
{
    public class MyMeasurement : INotifyPropertyChanged
    {
        private bool _Selected;
        public bool Selected
        {
            get { return _Selected; }
            set
            {
                if (_Selected != value)
                {
                    _Selected = value;
                    OnPropertyChanged();
                }
            }
        }

        private Models.Measurement _Measurement;
        public Models.Measurement Measurement
        {
            get { return _Measurement; }
            set
            {
                _Measurement = value;
                OnPropertyChanged();
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }

    public class MeasurementListViewModel
    {
        private Models.DataSpectra dataSpectra { get; set; }

        public delegate void EventHandlerMeasurementID(int SpectrumID);
        public event EventHandlerMeasurementID EventMeasurementToPlot, EventMeasurementNotToPlot;

        public List<MyMeasurement> ModifiedItems { get; set; }
        public AsyncObservableCollection<MyMeasurement> MeasurementList { get; set; }

        public CollectionViewSource MeasurementListViewSource { get; set; }

        private Filter lastFilter;

        public MeasurementListViewModel()
        {
            dataSpectra = SimpleIoc.Default.GetInstance<Models.DataSpectra>();

            // Hooking up to events from DataSpectra
            dataSpectra.EventMeasurementNew += new Models.DataSpectra.EventHandlerMeasurement(MeasurementNew);
            dataSpectra.EventMeasurementRemove += new Models.DataSpectra.EventHandlerMeasurementID(MeasurementRemove);
            dataSpectra.EventMeasurementUpdate += new Models.DataSpectra.EventHandlerMeasurement(MeasurementUpdate);

            // Hooking up to events from SpectraFilter
            SimpleIoc.Default.GetInstance<MeasurementFilterViewModel>().EventNewFilter += new MeasurementFilterViewModel.EventHandlerFilter(ChangeFilter);

            ModifiedItems = new List<MyMeasurement>();
            MeasurementList = new AsyncObservableCollection<MyMeasurement>();
            MeasurementList.CollectionChanged += OnCollectionChanged;

            MeasurementListViewSource = new CollectionViewSource();
            MeasurementListViewSource.Source = MeasurementList;
            MeasurementListViewSource.SortDescriptions.Add(new SortDescription("Measurement.StartTime", ListSortDirection.Descending));

            ChangeFilter(new Filter() { Name = "Today", Type = "Date", SubType = "Today" });
        }

        public void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                if (e.NewItems != null)
                {
                    foreach (MyMeasurement newItem in e.NewItems)
                    {
                        ModifiedItems.Add(newItem);
                        newItem.PropertyChanged += this.OnItemPropertyChanged;
                    }
                }

            if (e.Action == NotifyCollectionChangedAction.Remove)
                if (e.OldItems != null)
                {
                    foreach (MyMeasurement oldItem in e.OldItems)
                    {
                        oldItem.PropertyChanged -= this.OnItemPropertyChanged;
                        ModifiedItems.Remove(oldItem);
                    }
                }

            if (e.Action == NotifyCollectionChangedAction.Replace)
                if (e.NewItems != null && e.OldItems != null)
                {
                    foreach (MyMeasurement newItem in e.NewItems)
                    {
                        ModifiedItems.Add(newItem);
                        newItem.PropertyChanged += this.OnItemPropertyChanged;
                    }
                    foreach (MyMeasurement oldItem in e.OldItems)
                    {
                        oldItem.PropertyChanged -= this.OnItemPropertyChanged;
                        ModifiedItems.Remove(oldItem);
                    }
                }
        }

        void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            MyMeasurement myMeasurement = sender as MyMeasurement;

            if (e.PropertyName == "Measurement") return;

            if (myMeasurement.Selected == true)
            { if (EventMeasurementToPlot != null) EventMeasurementToPlot(myMeasurement.Measurement.MeasurementID); }
            else
            { if (EventMeasurementNotToPlot != null) EventMeasurementNotToPlot(myMeasurement.Measurement.MeasurementID); }
        }

        public void ChangeFilter(Filter selectedFilter)
        {
            MeasurementList.Clear();
            Console.WriteLine("FilterType: {0}", selectedFilter.Type);

            using (Models.DatabaseDataContext db = new Models.DatabaseDataContext(MyGlobals.ConString))
            {
                List<Models.Measurement> MeasurementList = new List<Models.Measurement>();

                switch (selectedFilter.Type)
                {
                    case "All":
                        { MeasurementList = db.Measurements.ToList(); break; }

                    case "Date":
                        {
                            switch (selectedFilter.SubType)
                            {
                                case "Today":
                                    { MeasurementList = db.Measurements.Where(x => x.StartTime.Date == DateTime.Today).ToList(); break; }

                                case "ThisWeek":
                                    {
                                        int DayOfWeek = (int)DateTime.Today.DayOfWeek;
                                        MeasurementList = db.Measurements.Where(x => x.StartTime.DayOfYear > (DateTime.Today.DayOfYear - DayOfWeek) && x.StartTime.DayOfYear < (DateTime.Today.DayOfYear - DayOfWeek + 7)).ToList(); //Todo!!!
                                        break;
                                    }

                                case "ThisMonth":
                                    { MeasurementList = db.Measurements.Where(x => x.StartTime.Date.Month == DateTime.Now.Month).ToList(); break; }

                                case "ThisYear":
                                    { MeasurementList = db.Measurements.Where(x => x.StartTime.Date.Year == DateTime.Now.Year).ToList(); break; }

                                case "Year":
                                    { MeasurementList = db.Measurements.Where(x => x.StartTime.Date.Year == selectedFilter.year).ToList(); break; }

                                case "Month":
                                    { MeasurementList = db.Measurements.Where(x => x.StartTime.Date.Year == selectedFilter.year && x.StartTime.Date.Month == selectedFilter.month).ToList(); break; }

                                case "Day":
                                    { MeasurementList = db.Measurements.Where(x => x.StartTime.Date.Year == selectedFilter.year && x.StartTime.Date.Month == selectedFilter.month && x.StartTime.Date.Day == selectedFilter.day).ToList(); break; }
                            }
                        }
                        break;

                    case "Sample":
                        { break; }

                    case "Channel":
                        { MeasurementList = db.Measurements.Where(x => x.Channel == selectedFilter.channel).ToList(); break; }
                }

                Models.Sample tempSample;

                foreach (Models.Measurement measurement in MeasurementList)
                {
                    tempSample = measurement.Sample;
                    // The view will access MeasurementList.Sample, but the Sample will only load when needed and the DataContext doesn't extend to the view
                    this.MeasurementList.Add(new MyMeasurement() { Selected = false, Measurement = measurement });
                }
            }

            MeasurementListViewSource.View.Refresh();

            lastFilter = selectedFilter;
        }


        private void MeasurementNew(Models.Measurement measurement)
        {
            Console.WriteLine("SpectrumNew");
            MeasurementList.Add(new MyMeasurement() { Selected = true, Measurement = measurement });
            if (EventMeasurementToPlot != null) EventMeasurementToPlot(measurement.MeasurementID);
        }

        private void MeasurementRemove(int spectrumID)
        {
            Console.WriteLine("SpectrumRemove");
            MyMeasurement delSpectra = MeasurementList.Where(x => x.Measurement.MeasurementID == spectrumID).First();

            if (delSpectra != null)
                MeasurementList.Remove(delSpectra);
        }

        private void MeasurementUpdate(Models.Measurement spectrum)
        {
            var item = MeasurementList.Where(x => x.Measurement.MeasurementID == spectrum.MeasurementID).First();

            if (item != null)
            {
                int index = MeasurementList.IndexOf(item);
                MeasurementList[index].Measurement = spectrum;
            }
        }

        public void DeleteSelectedSpectra()
        {
            List<int> selectedSpectra = MeasurementList.Where(x => x.Selected == true).Select(y => y.Measurement.MeasurementID).ToList();

            MessageBoxResult rsltMessageBox = MessageBox.Show("Are you shure to delete the selected spectra?", "Confirm deletion", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (rsltMessageBox == MessageBoxResult.Yes)
                foreach (int ID in selectedSpectra)
                    dataSpectra.DeleteSpectra(selectedSpectra);
        }
    }
}