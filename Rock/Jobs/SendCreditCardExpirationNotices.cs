// <copyright>
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
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Quartz;

using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Security;
using System.Web;

namespace Rock.Jobs
{
    /// <summary>
    /// Determines if a credit card is going to expire and notifies the person.
    /// </summary>
    [SystemEmailField( "Expiring Credit Card Email", "The system email template to use for the credit card expiration notice. The merge fields 'Person', 'Card' (the last four digits of the credit card), and 'Expiring' (the MM/YYYY of expiration) will be available to the email template.", required: true, order: 0 )]
    [WorkflowTypeField( "Workflow", "The Workflow to launch for person whose credit card is expiring. The attributes 'Person', 'Card' (the last four digits of the credit card), and 'Expiring' (the MM/YYYY of expiration) will be passed to the workflow as attributes.", false, required: false, order: 1 )]
    [DisallowConcurrentExecution]
    public class SendCreditCardExpirationNotices : IJob
    {
        /// <summary>
        /// Empty constructor for job initialization
        /// <para>
        /// Jobs require a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public SendCreditCardExpirationNotices()
        {
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {
            var rockContext = new RockContext();
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            // Get the details for the email that we'll be sending out.
            Guid? systemEmailGuid = dataMap.GetString( "ExpiringCreditCardEmail" ).AsGuidOrNull();
            SystemEmailService emailService = new SystemEmailService( rockContext );
            SystemEmail systemEmail = null;

            if ( systemEmailGuid.HasValue )
            {
                systemEmail = emailService.Get( systemEmailGuid.Value );
            }

            if (systemEmail == null)
            {
                throw new Exception( "Expiring credit card email is missing." );
            }

            // Fetch the configured Workflow once if one was set, we'll use it later.
            Guid? workflowGuid = dataMap.GetString( "Workflow" ).AsGuidOrNull();
            WorkflowTypeCache workflowType = null;
            var workflowService = new WorkflowService( rockContext );

            if ( workflowGuid != null )
            {
                workflowType = WorkflowTypeCache.Get( workflowGuid.Value );
            }

            var qry = new FinancialScheduledTransactionService( rockContext )
                .Queryable( "ScheduledTransactionDetails,FinancialPaymentDetail.CurrencyTypeValue,FinancialPaymentDetail.CreditCardTypeValue" )
                .Where( t => t.IsActive && t.FinancialPaymentDetail.ExpirationMonthEncrypted != null
                && ( t.EndDate == null || t.EndDate > DateTime.Now ) )
                .AsNoTracking();

            // Get the current month and year
            DateTime now = DateTime.Now;
            int month = now.Month;
            int year = now.Year;
            int counter = 0;
            var errors = new List<string>();

            foreach ( var transaction in qry )
            {
                // This checks to see if the expiration is saved in plain text or if it is encrypted. Will return an integer for either case.
                // NOTE: not necessary if all data is encrypted in the database, may want to revert later
                int? expirationMonthDecrypted = null;
                int? expirationYearDecrypted = null;

                if ( transaction.FinancialPaymentDetail.ExpirationMonthEncrypted.Length == 2 )
                {
                  expirationMonthDecrypted = transaction.FinancialPaymentDetail.ExpirationMonthEncrypted.AsIntegerOrNull();
                } else {
                  expirationMonthDecrypted = Encryption.DecryptString( transaction.FinancialPaymentDetail.ExpirationMonthEncrypted ).AsIntegerOrNull();
                }
                if ( transaction.FinancialPaymentDetail.ExpirationYearEncrypted.Length == 2 )
                {
                  expirationYearDecrypted = transaction.FinancialPaymentDetail.ExpirationYearEncrypted.AsIntegerOrNull();
                } else {
                  expirationYearDecrypted = Encryption.DecryptString( transaction.FinancialPaymentDetail.ExpirationYearEncrypted ).AsIntegerOrNull();
                }


                if ( expirationMonthDecrypted.HasValue && expirationMonthDecrypted.HasValue )
                {
                    string acctNum = string.Empty;

                    if ( !string.IsNullOrEmpty( transaction.FinancialPaymentDetail.AccountNumberMasked ) && transaction.FinancialPaymentDetail.AccountNumberMasked.Length >= 4 )
                    {
                        acctNum = transaction.FinancialPaymentDetail.AccountNumberMasked.Substring( transaction.FinancialPaymentDetail.AccountNumberMasked.Length - 4 );
                    }

                    int expYear = expirationYearDecrypted.Value;
                    int expMonth = expirationMonthDecrypted.Value - 1;
                    if ( expMonth == 0 )
                    {
                        expYear -= 1;
                        expMonth = 12;
                    }

                    // this is going to take the years and strip them down to always being two digit years
                    string expDate = expMonth.ToString() + expYear.ToString().Substring( expYear.ToString().Length - 2, 2);
                    string currentDate = month.ToString() + year.ToString().Substring( year.ToString().Length - 2, 2 );

                    if ( expDate == currentDate )
                    {
                        // as per ISO7813 https://en.wikipedia.org/wiki/ISO/IEC_7813
                        var expDateFormatted = string.Format( "{0:D2}/{1:D2}", expirationMonthDecrypted, expirationYearDecrypted );

                        var recipients = new List<RecipientData>();
                        var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );
                        var person = transaction.AuthorizedPersonAlias.Person;

                        if ( !person.IsEmailActive || person.Email.IsNullOrWhiteSpace() || person.EmailPreference == EmailPreference.DoNotEmail )
                        {
                            continue;
                        }

                        mergeFields.Add( "Person", person );
                        mergeFields.Add( "Card", acctNum );
                        mergeFields.Add( "Expiring", expDateFormatted );
                        recipients.Add( new RecipientData( person.Email, mergeFields ) );

                        var emailMessage = new RockEmailMessage( systemEmail.Guid );
                        emailMessage.SetRecipients( recipients );

                        var emailErrors = new List<string>();
                        
                        emailMessage.Send(out emailErrors);
                        errors.AddRange( emailErrors );

                        // Start workflow for this person
                        if ( workflowType != null )
                        {
                            Dictionary<string, string> attributes = new Dictionary<string, string>();
                            attributes.Add( "Person", transaction.AuthorizedPersonAlias.Guid.ToString() );
                            attributes.Add( "Card", acctNum );
                            attributes.Add( "Expiring", expDateFormatted );
                            StartWorkflow( workflowService, workflowType, attributes, string.Format( "{0} (scheduled transaction Id: {1})", person.FullName, transaction.Id ) );
                        }
                        

                        counter++;
                    }
                }
            }

            context.Result = string.Format( "{0} scheduled credit card transactions were examined with {1} notice(s) sent.", qry.Count(), counter );

            if ( errors.Any() )
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine();
                sb.Append( string.Format( "{0} Errors: ", errors.Count() ) );
                errors.ForEach( e => { sb.AppendLine(); sb.Append( e ); } );
                string errorMessage = sb.ToString();
                context.Result += errorMessage;
                var exception = new Exception( errorMessage );
                HttpContext context2 = HttpContext.Current;
                ExceptionLogService.LogException( exception, context2 );
                throw exception;
            }
        }

        /// <summary>
        /// Starts the workflow.
        /// </summary>
        /// <param name="workflowService">The workflow service.</param>
        /// <param name="workflowType">Type of the workflow.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="workflowNameSuffix">The workflow instance name suffix (the part that is tacked onto the end fo the name to distinguish one instance from another).</param>
        protected void StartWorkflow( WorkflowService workflowService, WorkflowTypeCache workflowType, Dictionary<string, string> attributes, string workflowNameSuffix )
        {
            // launch workflow if configured
            if ( workflowType != null && ( workflowType.IsActive ?? true ) )
            {
                var workflow = Rock.Model.Workflow.Activate( workflowType, "SendCreditCardExpiration " + workflowNameSuffix );

                // set attributes
                foreach ( KeyValuePair<string, string> attribute in attributes )
                {
                    workflow.SetAttributeValue( attribute.Key, attribute.Value );
                }

                // launch workflow
                List<string> workflowErrors;
                workflowService.Process( workflow, out workflowErrors );
            }
        }
    }
}
