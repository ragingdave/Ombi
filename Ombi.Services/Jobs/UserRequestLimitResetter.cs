﻿#region Copyright
// /************************************************************************
//    Copyright (c) 2016 Jamie Rees
//    File: UserRequestLimitResetter.cs
//    Created By: Jamie Rees
//   
//    Permission is hereby granted, free of charge, to any person obtaining
//    a copy of this software and associated documentation files (the
//    "Software"), to deal in the Software without restriction, including
//    without limitation the rights to use, copy, modify, merge, publish,
//    distribute, sublicense, and/or sell copies of the Software, and to
//    permit persons to whom the Software is furnished to do so, subject to
//    the following conditions:
//   
//    The above copyright notice and this permission notice shall be
//    included in all copies or substantial portions of the Software.
//   
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//  ************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Ombi.Core;
using Ombi.Core.SettingModels;
using Ombi.Services.Interfaces;
using Ombi.Store;
using Ombi.Store.Models;
using Ombi.Store.Repository;
using Quartz;

namespace Ombi.Services.Jobs
{
    public class UserRequestLimitResetter : IJob, IUserRequestLimitResetter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public UserRequestLimitResetter(IJobRecord record, IRepository<RequestLimit> repo, ISettingsService<PlexRequestSettings> settings)
        {
            Record = record;
            Repo = repo;
            Settings = settings;
        }

        private IJobRecord Record { get; }
        private IRepository<RequestLimit> Repo { get; }
        private ISettingsService<PlexRequestSettings> Settings { get; }

        public void AlbumLimit(PlexRequestSettings s, IEnumerable<RequestLimit> allUsers)
        {
            if (s.AlbumWeeklyRequestLimit == 0)
            {
                return; // The limit has not been set
            }
            CheckAndDelete(allUsers, RequestType.Album);
        }

        public void MovieLimit(PlexRequestSettings s, IEnumerable<RequestLimit> allUsers)
        {
            if (s.MovieWeeklyRequestLimit == 0)
            {
                return; // The limit has not been set
            }
            CheckAndDelete(allUsers, RequestType.Movie);
        }

        public void TvLimit(PlexRequestSettings s, IEnumerable<RequestLimit> allUsers)
        {
            if (s.TvWeeklyRequestLimit == 0)
            {
                return; // The limit has not been set
            }
            CheckAndDelete(allUsers, RequestType.TvShow);
        }

        private void CheckAndDelete(IEnumerable<RequestLimit> allUsers, RequestType type)
        {
            var users = allUsers.Where(x => x.RequestType == type);
            foreach (var u in users)
            {
                var daysDiff = (u.FirstRequestDate - DateTime.UtcNow.AddDays(-7)).TotalDays;
                if (daysDiff <= 0)
                {
                    Repo.Delete(u);
                }
            }
        }


        public void Start()
        {
            Record.SetRunning(true, JobNames.CpCacher);
            try
            {
                var settings = Settings.GetSettings();
                var users = Repo.GetAll();
                var requestLimits = users as RequestLimit[] ?? users.ToArray();

                MovieLimit(settings, requestLimits);
                TvLimit(settings, requestLimits);
                AlbumLimit(settings, requestLimits);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                Record.Record(JobNames.RequestLimitReset);
                Record.SetRunning(false, JobNames.CpCacher);
            }
        }

        public void Execute(IJobExecutionContext context)
        {
            Record.SetRunning(true, JobNames.CpCacher);
            try
            {
                var settings = Settings.GetSettings();
                var users = Repo.GetAll();
                var requestLimits = users as RequestLimit[] ?? users.ToArray();

                MovieLimit(settings, requestLimits);
                TvLimit(settings, requestLimits);
                AlbumLimit(settings, requestLimits);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                Record.Record(JobNames.RequestLimitReset);
                Record.SetRunning(false, JobNames.CpCacher);
            }
        }
    }
}