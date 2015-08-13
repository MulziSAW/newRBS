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
using newRBS.ViewModelUtils;

namespace newRBS.ViewModels
{
    public class TreeViewModel : ViewModelBase
    {
        public AsyncObservableCollection<Filter> Items { get; set; }
    }

    public class Filter : ViewModelBase
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public int year { get; set; }
        public int month { get; set; }
        public int day { get; set; }
        public int channel { get; set; }
        public AsyncObservableCollection<Filter> Children { get; set; }
    }

    public class SpectraFilterViewModel : INotifyPropertyChanged
    {
        private Models.DataSpectra dataSpectra { get; set; }
        //private SpectraListViewModel spectraListViewModel;

        public delegate void EventHandlerFilter(Filter newFilter);
        public event EventHandlerFilter EventNewFilter;

        public ICommand ExpandFilterList { get; set; }
        public ICommand TestButtonClick { get; set; }

        private bool _spectraFilterPanelVis = true;
        public bool spectraFilterPanelVis
        {
            get { return _spectraFilterPanelVis; }
            set { _spectraFilterPanelVis = value; OnPropertyChanged("spectraFilterPanelVis"); }
        }

        public AsyncObservableCollection<string> filterTypeList { get; set; }

        private int _filterTypeIndex;
        public int filterTypeIndex
        {
            get
            { return _filterTypeIndex; }
            set
            {
                _filterTypeIndex = value;
                Console.WriteLine("new filter type {0}", filterTypeList[value]);
                FillFilterList(filterTypeList[value]);
            }
        }

        private Filter _selectedFilter;
        public Filter selectedFilter
        {
            get
            { return _selectedFilter; }
            set
            {
                Console.WriteLine("new selectedFilter");
                Console.WriteLine(value.Type);
                _selectedFilter = value;

                // Send event (to SpectraListView...)
                if (EventNewFilter != null) EventNewFilter(value);
            }
        }

        public object CurrSelItem { get; set; }

        public RelayCommand<ViewModelUtils.TreeViewHelper.DependencyPropertyEventArgs> MySelItemChgCmd { get; set; }

        private void TreeViewItemSelectedChangedCallBack(ViewModelUtils.TreeViewHelper.DependencyPropertyEventArgs e)
        {
            if (e != null && e.DependencyPropertyChangedEventArgs.NewValue != null)
            {
                Filter temp = (Filter)e.DependencyPropertyChangedEventArgs.NewValue;
                Console.WriteLine(temp.Name);
                selectedFilter = temp;
            }
        }

        public TreeViewModel filterTree { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public SpectraFilterViewModel()
        {
            dataSpectra = SimpleIoc.Default.GetInstance<Models.DataSpectra>();

            ExpandFilterList = new RelayCommand(() => _ExpandFilterList(), () => true);
            TestButtonClick = new RelayCommand(() => _TestButtonClick(), () => true);

            filterTypeList = new AsyncObservableCollection<string>();
            filterTree = new TreeViewModel();
            filterTree.Items = new AsyncObservableCollection<Filter>();
            selectedFilter = new Filter() { Name = "All", Type = "All" };

            MySelItemChgCmd = new RelayCommand<ViewModelUtils.TreeViewHelper.DependencyPropertyEventArgs>(TreeViewItemSelectedChangedCallBack);
            CurrSelItem = new object();

            filterTypeList.Add("Date");
            filterTypeList.Add("Sample");
            filterTypeList.Add("Channel");

            filterTypeIndex = 0;
        }

        private void _ExpandFilterList()
        {
            Console.WriteLine("Expand");
            spectraFilterPanelVis = !spectraFilterPanelVis;
        }

        private void _TestButtonClick()
        {
            Console.WriteLine("TestButtionClick-SpectraFilterViewModel");
        }

        private void FillFilterList(string filterType)
        {
            Console.WriteLine("Update adf filter list with filterType: {0}", filterType);
            //filterTree.Items.Clear();
            Console.WriteLine(filterTree.Items.Count());
            if (filterTree.Items.Count() > 0)
            {
                foreach (Filter n in filterTree.Items)
                    Console.WriteLine(n.Name);
                while (filterTree.Items.Count > 0)
                    filterTree.Items.RemoveAt(0);
                foreach (Filter n in filterTree.Items)
                    Console.WriteLine(n.Name);
                //Console.WriteLine(filterTree.Items[0].Name);
            }
            Console.WriteLine("clear done");
            switch (filterType)
            {
                case "Date":
                    filterTree.Items.Add(new Filter() { Name = "All", Type = "All" });
                    filterTree.Items.Add(new Filter() { Name = "Today", Type = "Date", SubType = "Today" });
                    filterTree.Items.Add(new Filter() { Name = "This Week", Type = "Date", SubType = "ThisWeek" });
                    filterTree.Items.Add(new Filter() { Name = "This Month", Type = "Date", SubType = "ThisMonth" });
                    filterTree.Items.Add(new Filter() { Name = "This Year", Type = "Date", SubType = "ThisYear" });

                    List<int> allYears = dataSpectra.GetAllYears();
                    foreach (int Year in allYears)
                    {
                        Filter newYearNode = new Filter() { Name = Year.ToString(), Type = "Date", SubType = "Year", year = Year };

                        List<int> allMonths = dataSpectra.GetAllMonths(Year);
                        if (allMonths.Count > 0)
                        {
                            newYearNode.Children = new AsyncObservableCollection<Filter>();
                            foreach (int Month in allMonths)
                            {
                                Filter newMonthNode = new Filter() { Name = Month.ToString("D2"), Type = "Date", SubType = "Month", year = Year, month = Month };

                                List<int> allDays = dataSpectra.GetAllDays(Year, Month);
                                if (allDays.Count > 0)
                                {
                                    newMonthNode.Children = new AsyncObservableCollection<Filter>();
                                    foreach (int Day in allDays)
                                    {
                                        Filter newDayNode = new Filter() { Name = Day.ToString("D2"), Type = "Date", SubType = "Day", year = Year, month = Month, day = Day };
                                        newMonthNode.Children.Add(newDayNode);
                                    }
                                }
                                newYearNode.Children.Add(newMonthNode);
                            }
                        }
                        filterTree.Items.Add(newYearNode);
                    }

                    break;
                case "Channel":
                    Console.WriteLine("Channel");
                    filterTree.Items.Add(new Filter() { Name = "All", Type = "All" });
                    List<int> allChannels = dataSpectra.GetAllChannels();
                    Console.WriteLine("NumChannels {0}", allChannels.Count());
                    foreach (int Channel in allChannels)
                    {
                        Console.WriteLine(Channel);
                        filterTree.Items.Add(new Filter() { Name = Channel.ToString(), Type = "Channel", channel = Channel });
                    }
                    break;
                default:
                    Console.WriteLine("No action found for filterType: {0}", filterType);
                    break;
            }
        }
    }
}
