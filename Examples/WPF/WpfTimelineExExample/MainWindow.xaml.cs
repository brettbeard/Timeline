﻿/* 
 * Copyright (c) 2010, Andriy Syrov
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided 
 * that the following conditions are met:
 * 
 * Redistributions of source code must retain the above copyright notice, this list of conditions and the 
 * following disclaimer.
 * 
 * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and 
 * the following disclaimer in the documentation and/or other materials provided with the distribution.
 *
 * Neither the name of Andriy Syrov nor the names of his contributors may be used to endorse or promote 
 * products derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED 
 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
 * PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY 
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED 
 * TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN 
 * IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
 *   
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using TimelineLibrary;
using TimelineEx;
using System.IO;

namespace WpfTimelineExExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MediaElement                                    m_playing;
        private List<TimelineEvent>                     m_events = new List<TimelineEvent>();
        private bool                                    m_init = true;


        public MainWindow()
        {
            InitializeComponent();

            timeline.OnEventDeleted += OnEventDeleted;
            timeline.OnEventCreated += OnEventCreated;
            timeline.EditClick += OnEditClick;

            timeline.TimelineDoubleClick += OnTimelineDoubleClick;
            timeline.TimelineReady += OnTimelineReady;

        }

        private void OnTimelineReady(
            object                                      sender, 
            EventArgs                                   e
        )
        {
            if (m_init)
            {
                var monetXml = this.GetResourceTextFile("Monet.xml");
                timeline.ResetEvents(monetXml);
                m_init = false;
            }
        }

        void OnEditClick(
            TimelineEventEx                             obj
        )
        {
            EditEventWindow                             edit;

            edit = new EditEventWindow();

            edit.Tray = timeline;
            edit.Event = obj;

            edit.Show();
        }

        void OnTimelineDoubleClick(
            DateTime                                    date, 
            Point                                       point
        )
        {
            EditEventWindow                             edit;

            if (timeline.EditMode)
            {
                edit = new EditEventWindow();
                edit.Tray = timeline;
                edit.AddNew = true;

                edit.EditEvent = new TimelineEventEx()
                {
                    StartDate = date,
                    EndDate = date,
                    TopOverride = point.Y + timeline.EventCanvasOffset
                };

                edit.Show();
            }
        }

        /// 
        /// <summary>
        /// This event fires when event is about to be displayed on the screen,
        /// we attach event handlers to stop/play mediaplayer link here</summary>
        /// 
        void OnEventCreated(
            FrameworkElement                            element, 
            TimelineDisplayEvent                        de
        )
        {
            MediaElement                                me;
            TimelineLibrary.Hyperlink                   link;

            me = element.FindName("MediaPlayer") as MediaElement;
            link = element.FindName("StopPlayButton") as TimelineLibrary.Hyperlink;

            if (me != null && link != null)
            {
                link.Tag = me;
                link.Click += new RoutedEventHandler(OnButtonClick);
                me.MediaEnded += new RoutedEventHandler(OnMediaEnded);
                me.Tag = false;
            }
        }

        void OnMediaEnded(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            (sender as MediaElement).Tag = false;
        }

        /// 
        /// <summary>
        /// Once visual element is deleted we mark that there are no currently 
        /// playing videos</summary>
        /// 
        private void OnEventDeleted(
            FrameworkElement                            element, 
            TimelineLibrary.TimelineDisplayEvent        de
        )
        {
            MediaElement                                me;

            me = element.FindName("MediaPlayer") as MediaElement;

            if (me == m_playing && me != null)
            {
                m_playing.Stop();
                m_playing = null;
            }
        }

        /// 
        /// <summary>
        /// Stop/play link clicked, we start/stop playing current video
        /// and stop previous video if any</summary>
        /// 
        private void OnButtonClick(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            MediaElement                                me;

            me = (sender as TimelineLibrary.Hyperlink).Tag as MediaElement;

            if (m_playing != null && m_playing != me)
            {
                m_playing.Stop();
                m_playing.Tag = false;
                m_playing = null;
            }

            if (me.Tag != null && (bool) me.Tag)
            {
                m_playing = null;
                me.Stop();
                me.Tag = false;
            }
            else
            {
                m_playing = me;
                me.Play();
                me.Tag = true;
            }
        }

        /// 
        /// <summary>
        /// This checkbox handler allows to switch timelineex mode from
        /// edit to readonly and back</summary>
        /// 
        private void EditModeClick(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            timeline.EditMode = (sender as CheckBox).IsChecked.GetValueOrDefault(false);
        }

        /// 
        /// <summary>
        /// Read events again from XML file</summary>
        /// 
        private void OnReloadClick(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            timeline.Reload();
        }

        /// 
        /// <summary>
        /// Copy events with all changes (size, position, date, ranges) to them into local 
        /// collection</summary>
        /// 
        private void OnSaveClick(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            m_events.Clear();

            timeline.TimelineEvents.ForEach((ev) => m_events.Add(ev));
        }

        /// 
        /// <summary>
        /// Remove all events from timeline</summary>
        /// 
        private void OnClearClick(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            timeline.ClearEvents();
        }

        /// 
        /// <summary>
        /// Load events saved by OnSaveClick</summary>
        /// 
        private void OnLoadSavedEventsClick(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            timeline.ClearEvents();
            timeline.ResetEvents(m_events);
        }

        private void AllowChangeDatesClick(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            timeline.AllowEditDates = (sender as CheckBox).IsChecked.GetValueOrDefault(false);
        }

        private void OnAddNewEventClick(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            EditEventWindow                             edit;

            edit = new EditEventWindow();
            edit.Tray = timeline;

            edit.AddNew = true;

            edit.Show();
        }

        /// <summary>
        /// Gets the resource text file.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns></returns>
        private string GetResourceTextFile(string filename)
        {
            string result = string.Empty;

            using (Stream stream = this.GetType().Assembly.
                       GetManifestResourceStream("WpfTimelineExExample." + filename))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    result = sr.ReadToEnd();
                }
            }
            return result;
        }
    }
}
