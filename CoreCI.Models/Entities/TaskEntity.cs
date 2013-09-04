/*
 * Copyright (C) 2013 Christian Hoffmeister
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see {http://www.gnu.org/licenses/}.
 */
using System;
using System.Collections.Generic;

namespace CoreCI.Models
{
    public class TaskEntity : IEntity
    {
        public Guid Id { get; set; }

        public TaskState State { get; set; }

        public Guid? WorkerId { get; set; }

        public TaskConfiguration Configuration { get; set; }

        public int? ExitCode { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? DispatchedAt { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public TimeSpan? Runtime
        {
            get
            {
                if (this.StartedAt.HasValue && this.FinishedAt.HasValue)
                {
                    return this.FinishedAt.Value.Subtract(this.StartedAt.Value);
                }

                return null;
            }
        }

        public string Commit { get; set; }

        public string CommitUrl { get; set; }

        public string CommitMessage { get; set; }
    }

    public enum TaskState
    {
        Pending,
        Running,
        Succeeded,
        Failed
    }
}
