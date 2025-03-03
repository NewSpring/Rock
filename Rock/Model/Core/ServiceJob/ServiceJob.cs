﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using CronExpressionDescriptor;
using Rock.Data;
using Rock.Lava;

namespace Rock.Model
{
    /// <summary>
    /// Represents a scheduled job/routine in Rock. A job class can have multiple ServiceJob instances associated with it in the event that it has different attributes or 
    /// has multiple schedules.  For more information on how to create a job see https://github.com/SparkDevNetwork/Rock/wiki/Rock-Jobs
    /// </summary>
    [RockDomain( "Core" )]
    [Table( "ServiceJob" )]
    [DataContract]
    [CodeGenerateRest( DisableEntitySecurity = true )]
    [Rock.SystemGuid.EntityTypeGuid( Rock.SystemGuid.EntityType.SERVICE_JOB )]
    public partial class ServiceJob : Model<ServiceJob>
    {
        #region Constants

        /// <summary>
        /// The never scheduled cron expression. This will only fire the job in the year 2200. This is useful for jobs
        /// that should be run only on demand, such as rebuilding Streak data.
        /// </summary>
        public static string NeverScheduledCronExpression = "0 0 0 1 1 ? 2200";

        #endregion Constants

        #region Entity Properties

        /// <summary>
        /// Gets or sets a flag indicating if this Job is part of the Rock core system/framework
        /// </summary>
        /// <value>
        /// A <see cref="System.Boolean"/> value that is <c>true</c> if the Job is part of the Rock core system/framework;
        /// otherwise <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsSystem { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if the Job is active.
        /// </summary>
        /// <value>
        /// A <see cref="System.Boolean"/> value that is <c>true</c> if the Job is active; otherwise <c>false</c>.
        /// </value>
        [DataMember]
        public bool? IsActive { get; set; }

        /// <summary>
        /// Gets or sets the friendly Name of the Job. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> containing the friendly Name of the Job.
        /// </value>
        [Required]
        [MaxLength( 100 )]
        [DataMember( IsRequired = true )]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a user defined description of the Job.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the description of the Job.
        /// </value>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the Assembly name of the .dll file that contains the job class.
        /// Set this to null to have Rock figure out the Assembly automatically.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> that contains the Assembly name of the .dll file that contains the job class.
        /// </value>
        [MaxLength( 260 )]
        [DataMember]
        public string Assembly { get; set; }

        /// <summary>
        /// Gets or sets the fully qualified class name with Namespace of the Job class. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> containing the fully qualified class name with Namespace of the Job class.
        /// </value>
        [Required]
        [MaxLength( 100 )]
        [DataMember( IsRequired = true )]
        [EnableAttributeQualification]
        public string Class { get; set; }

        /// <summary>
        /// Gets or sets the Cron Expression that is used to schedule the Job. This property is required.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> that contains the Cron expression that is used to determine the schedule for the job.
        /// </value>
        /// <remarks>
        /// See  http://www.quartz-scheduler.org/documentation/quartz-1.x/tutorials/crontrigger for the syntax.
        /// </remarks>
        [Required]
        [MaxLength( 120 )]
        [DataMember( IsRequired = true )]
        public string CronExpression { get; set; }

        /// <summary>
        /// Gets or sets the date and time that the Job last completed successfully.
        /// </summary>
        /// <value>
        /// A <see cref="System.DateTime"/> representing the date and time of the last time that the Job completed successfully
        /// </value>
        [DataMember]
        public DateTime? LastSuccessfulRunDateTime { get; set; }

        /// <summary>
        /// Gets or sets the date and time that the job last ran.
        /// </summary>
        /// <value>
        /// A <see cref="System.DateTime"/> that represents the last time that the job ran.
        /// </value>
        [DataMember]
        public DateTime? LastRunDateTime { get; set; }

        /// <summary>
        /// Gets or set the amount of time, in seconds, that it took the job to run the last time that it ran.
        /// </summary>
        /// <value>
        /// A <see cref="System.Int32"/> representing the amount of time, in seconds, that it took the job to run the last time that it ran.
        /// </value>
        [DataMember]
        public int? LastRunDurationSeconds { get; set; }

        /// <summary>
        /// Gets or sets the completion status that was returned by the Job the last time that it ran.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> containing the status that was returned by the Job the last time that it ran.
        /// </value>
        [MaxLength( 50 )]
        [DataMember]
        public string LastStatus { get; set; }

        /// <summary>
        /// Gets or sets the status message that was returned the last time that the job was run. In most cases this will be used
        /// in the event of an exception to return the exception message.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the Status Message that returned the last time that the job ran.
        /// </value>
        [DataMember]
        public string LastStatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the name of the scheduler that the job ran under the last time that it ran. In most cases this 
        /// is used to determine if the was run by the IIS or Windows service.
        /// </summary>
        /// <value>
        /// A <see cref="System.String"/> representing the name of the Scheduler that the job ran under the last time that it was run.
        /// </value>
        [MaxLength( 40 )]
        [DataMember]
        public string LastRunSchedulerName { get; set; }

        /// <summary>
        /// Gets or sets a comma delimited list of email address that should receive notification emails for this job. Notification
        /// emails are sent to these email addresses based on the completion status of the Job and the <see cref="NotificationStatus"/>
        /// property of this job.
        /// </summary>
        /// <value>
        /// A <see cref="System.String" /> representing a list of email addresses that should receive notifications for this job.
        /// </value>
        [MaxLength( 1000 )]
        [DataMember]
        public string NotificationEmails { get; set; }

        /// <summary>
        /// Gets or sets the NotificationStatus for this job, this property determines when notification emails should be sent to the <see cref="NotificationEmails"/>
        /// that are associated with this Job
        /// </summary>
        /// <value>
        /// An <see cref="Rock.Model.JobNotificationStatus"/> that indicates when notification emails should be sent for this job. 
        /// When this value is <c>JobNotificationStatus.All</c> a notification email will be sent when the Job completes with any completion status.
        /// When this value is <c>JobNotificationStatus.Success</c> a notification email will be sent when the Job has completed successfully.
        /// When this value is <c>JobNotificationStatus.Error</c> a notification email will be sent when the Job completes with an error status.
        /// When this value is <c>JobNotificationStatus.None</c> notifications will not be sent when the Job completes with any status.
        /// </value>
        [Required]
        [DataMember( IsRequired = true )]
        public JobNotificationStatus NotificationStatus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether jobs should be logged in ServiceJobHistory
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable history]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool EnableHistory { get; set; } = false;


        /// <summary>
        /// Gets or sets the history count per job.
        /// </summary>
        /// <value>
        /// The history count per job.
        /// </value>
        [DataMember]
        public int HistoryCount { get; set; } = 500;

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Gets the last status message as HTML.
        /// </summary>
        /// <value>
        /// The last status message as HTML.
        /// </value>
        [LavaVisible]
        [NotMapped]
        public virtual string LastStatusMessageAsHtml
        {
            get
            {
                return LastStatusMessage.ConvertCrLfToHtmlBr();
            }
        }

        /// <summary>
        /// Gets the cron description.
        /// </summary>
        /// <value>
        /// The cron description.
        /// </value>
        [LavaVisible]
        [NotMapped]
        public virtual string CronDescription
        {
            get
            {
                return ExpressionDescriptor.GetDescription( this.CronExpression, new Options { ThrowExceptionOnParseError = false } );
            }
        }

        /// <summary>
        /// Gets or sets the a list of previous values that this attribute value had (If ServiceJob.EnableHistory is enabled)
        /// </summary>
        /// <value>
        /// The history of service jobs.
        /// </value>
        [DataMember]
        [LavaHidden]
        public virtual ICollection<ServiceJobHistory> ServiceJobHistory { get; set; } = new Collection<ServiceJobHistory>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this Job.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this Job.
        /// </returns>
        public override string ToString()
        {
            return this.Name;
        }

        #endregion

    }

    #region Entity Configuration

    /// <summary>
    /// Job Configuration class.
    /// </summary>
    public partial class ServiceJobConfiguration : EntityTypeConfiguration<ServiceJob>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceJobConfiguration"/> class.
        /// </summary>
        public ServiceJobConfiguration()
        {
        }
    }

    #endregion
}
