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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Bus.Message;
using Rock.Communication;
using Rock.Crm.RecordSource;
using Rock.Data;
using Rock.Financial;
using Rock.Lava;
using Rock.Model;
using Rock.Tasks;
using Rock.Utility;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Finance
{
    /// <summary>
    /// Version 2 of the Transaction Entry Block
    /// </summary>
    [DisplayName( "Transaction Entry (V2)" )]
    [Category( "Finance" )]
    [Description( "Creates a new financial transaction or scheduled transaction." )]

    #region Block Attributes

    [FinancialGatewayField(
        "Financial Gateway",
        Key = AttributeKey.FinancialGateway,
        Description = "The payment gateway to use for Credit Card and ACH transactions.",
        Category = AttributeCategory.None,
        Order = 0 )]

    [BooleanField(
        "Enable ACH",
        Key = AttributeKey.EnableACH,
        DefaultBooleanValue = false,
        Category = AttributeCategory.None,
        Order = 1 )]

    [BooleanField(
        "Enable Credit Card",
        Key = AttributeKey.EnableCreditCard,
        DefaultBooleanValue = true,
        Category = AttributeCategory.None,
        Order = 2 )]

    [TextField(
        "Batch Name Prefix",
        Key = AttributeKey.BatchNamePrefix,
        Description = "The batch prefix name to use when creating a new batch.",
        DefaultValue = "Online Giving",
        Category = AttributeCategory.None,
        Order = 3 )]

    [DefinedValueField(
        "Source",
        Key = AttributeKey.FinancialSourceType,
        Description = "The Financial Source Type to use when creating transactions.",
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE,
        DefaultValue = Rock.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_WEBSITE,
        Category = AttributeCategory.None,
        Order = 4 )]

    [AccountsField(
        "Accounts",
        Key = AttributeKey.AccountsToDisplay,
        Description = "The accounts to display. If the account has a child account for the selected campus, the child account for that campus will be used.",
        Category = AttributeCategory.None,
        Order = 5 )]

    [BooleanField(
        "Ask for Campus if Known",
        Key = AttributeKey.AskForCampusIfKnown,
        Description = "If the campus for the person is already known, should the campus still be prompted for?",
        DefaultBooleanValue = true,
        Category = AttributeCategory.None,
        Order = 10 )]

    [BooleanField(
        "Include Inactive Campuses",
        Key = AttributeKey.IncludeInactiveCampuses,
        Description = "Set this to true to include inactive campuses",
        DefaultBooleanValue = false,
        Category = AttributeCategory.None,
        Order = 10 )]

    [DefinedValueField(
        "Campus Types",
        Key = AttributeKey.IncludedCampusTypes,
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.CAMPUS_TYPE,
        AllowMultiple = true,
        IsRequired = false,
        Description = "Set this to limit campuses by campus type.",
        Category = AttributeCategory.None,
        Order = 11 )]

    [DefinedValueField(
        "Campus Statuses",
        Key = AttributeKey.IncludedCampusStatuses,
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.CAMPUS_STATUS,
        AllowMultiple = true,
        IsRequired = false,
        Description = "Set this to limit campuses by campus status.",
        Category = AttributeCategory.None,
        Order = 12 )]

    [BooleanField(
        "Enable Multi-Account",
        Key = AttributeKey.EnableMultiAccount,
        Description = "Should the person be able specify amounts for more than one account?",
        DefaultBooleanValue = true,
        Category = AttributeCategory.None,
        Order = 13 )]

    [DefinedValueField(
        "Financial Source Type",
        Key = AttributeKey.FinancialSourceType,
        Description = "The Financial Source Type to use when creating transactions",
        IsRequired = false,
        AllowMultiple = false,
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE,
        DefaultValue = Rock.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_WEBSITE,
        Category = AttributeCategory.None,
        Order = 19 )]

    [BooleanField(
        "Enable Anonymous Giving",
        Key = AttributeKey.EnableAnonymousGiving,
        Description = "Should the option to give anonymously be displayed. Giving anonymously will display the transaction as 'Anonymous' in places where it is shown publicly, for example, on a list of fund-raising contributors.",
        DefaultBooleanValue = false,
        Category = AttributeCategory.None,
        Order = 24 )]

    [TextField(
        "Anonymous Giving Tool-tip",
        Key = AttributeKey.AnonymousGivingTooltip,
        IsRequired = false,
        Description = "The tool-tip for the 'Give Anonymously' check box.",
        Category = AttributeCategory.None,
        Order = 25 )]

    [BooleanField(
        "Enable Business Giving",
        Key = AttributeKey.EnableBusinessGiving,
        Description = "Should the option to give as a business be displayed.",
        DefaultBooleanValue = true,
        Category = AttributeCategory.None,
        Order = 26 )]

    [BooleanField(
        "Enable Fee Coverage",
        Description = "Determines if the fee coverage feature is enabled or not.",
        Key = AttributeKey.EnableFeeCoverage,
        DefaultBooleanValue = false,
        Order = 27 )]

    [BooleanField(
        "Fee Coverage Default State",
        Description = "Determines if checkbox for 'Cover the fee' defaults to checked.",
        Key = AttributeKey.FeeCoverageDefaultState,
        DefaultBooleanValue = false,
        Order = 28 )]

    [CodeEditorField(
        "Fee Coverage Message",
        Description = "The Lava template to use to provide the cover the fees prompt to the individual. <span class='tip tip-lava'></span>",
        EditorMode = CodeEditorMode.Lava,
        Key = AttributeKey.FeeCoverageMessage,
        DefaultValue = "Make my gift go further. Please increase my gift by {%if IsPercentage %} {{ Percentage }}% ({{ AmountHTML }}) {% else %} {{ AmountHTML }} {% endif %} to help cover the electronic transaction fees.",
        Order = 28 )]

    [BooleanField(
        "Disable Captcha Support",
        Description = "If set to 'Yes' the CAPTCHA verification step will not be performed.",
        Key = AttributeKey.DisableCaptchaSupport,
        DefaultBooleanValue = false,
        Order = 29
        )]

    [BooleanField(
        "Enable End Date",
        Description = "When enabled, this setting allows an individual to specify an optional end date for their recurring scheduled gifts.",
        Key = AttributeKey.EnableEndDate,
        DefaultBooleanValue = false,
        Order = 30 )]

    #region Scheduled Transactions

    [BooleanField(
        "Scheduled Transactions",
        Key = AttributeKey.AllowScheduledTransactions,
        Description = "If the selected gateway(s) allow scheduled transactions, should that option be provided to user.",
        TrueText = "Allow",
        FalseText = "Don't Allow",
        DefaultBooleanValue = true,
        Category = AttributeCategory.ScheduleGifts,
        Order = 1 )]

    [BooleanField(
        "Show Scheduled Gifts",
        Key = AttributeKey.ShowScheduledTransactions,
        Description = "If the person has any scheduled gifts, show a summary of their scheduled gifts.",
        DefaultBooleanValue = true,
        Category = AttributeCategory.ScheduleGifts,
        Order = 2 )]

    [CodeEditorField(
        "Scheduled Gifts Template",
        Key = AttributeKey.ScheduledTransactionsTemplate,
        Description = "The Lava Template to use to display Scheduled Gifts.",
        DefaultValue = DefaultScheduledTransactionsTemplate,
        EditorMode = CodeEditorMode.Lava,
        Category = AttributeCategory.ScheduleGifts,
        Order = 3 )]

    [LinkedPage(
        "Scheduled Transaction Edit Page",
        Key = AttributeKey.ScheduledTransactionEditPage,
        Description = "The page to use for editing scheduled transactions.",
        Category = AttributeCategory.ScheduleGifts,
        Order = 4 )]

    #endregion

    #region Payment Comment Options

    [BooleanField(
        "Enable Comment Entry",
        Key = AttributeKey.EnableCommentEntry,
        Description = "Allows the guest to enter the value that's put into the comment field (will be appended to the 'Payment Comment' setting)",
        IsRequired = false,
        Category = AttributeCategory.PaymentComments,
        Order = 1 )]

    [TextField(
        "Comment Entry Label",
        Key = AttributeKey.CommentEntryLabel,
        Description = "The label to use on the comment edit field (e.g. Trip Name to give to a specific trip).",
        DefaultValue = "Comment",
        IsRequired = false,
        Category = AttributeCategory.PaymentComments,
        Order = 2 )]

    [CodeEditorField(
        "Payment Comment Template",
        Key = AttributeKey.PaymentCommentTemplate,
        Description = @"The comment to include with the payment transaction when sending to Gateway. <span class='tip tip-lava'></span>",
        IsRequired = false,
        EditorMode = CodeEditorMode.Lava,
        Category = AttributeCategory.PaymentComments,
        Order = 3 )]

    #endregion Payment Comment Options

    #region Text Options

    [TextField( "Save Account Title",
        Key = AttributeKey.SaveAccountTitle,
        Description = "The text to display as heading of section for saving payment information.",
        IsRequired = false,
        DefaultValue = "Make Giving Even Easier",
        Category = AttributeCategory.TextOptions,
        Order = 1 )]

    [CodeEditorField(
        "Intro Message",
        Key = AttributeKey.IntroMessageTemplate,
        EditorMode = CodeEditorMode.Lava,
        Description = "The text to place at the top of the amount entry. <span class='tip tip-lava'></span>",
        DefaultValue = "<h2>Your Generosity Changes Lives</h2>",
        Category = AttributeCategory.TextOptions,
        Order = 2 )]

    [TextField(
        "Gift Term",
        Key = AttributeKey.GiftTerm,
        DefaultValue = "Gift",
        Category = AttributeCategory.TextOptions,
        Order = 3 )]

    [TextField(
        "Give Button Text - Now ",
        Key = AttributeKey.GiveButtonNowText,
        DefaultValue = "Give Now",
        Category = AttributeCategory.TextOptions,
        Order = 4 )]

    [TextField(
        "Give Button Text - Scheduled",
        Key = AttributeKey.GiveButtonScheduledText,
        DefaultValue = "Schedule Your Gift",
        Category = AttributeCategory.TextOptions,
        Order = 5 )]

    [CodeEditorField( "Account Header Template",
        Key = AttributeKey.AccountHeaderTemplate,
        Description = "The Lava Template to use as the amount input label for each account.",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        EditorHeight = 50,
        IsRequired = true,
        DefaultValue = "{{ Account.PublicName }}",
        Category = AttributeCategory.TextOptions,
        Order = 6 )]

    [CodeEditorField(
        "Amount Summary Template",
        Key = AttributeKey.AmountSummaryTemplate,
        EditorMode = CodeEditorMode.Lava,
        Description = "The text (HTML) to display on the amount summary page. <span class='tip tip-lava'></span>",
        DefaultValue = DefaultAmountSummaryTemplate,
        Category = AttributeCategory.TextOptions,
        Order = 7 )]

    [CodeEditorField(
        "Finish Lava Template",
        Key = AttributeKey.FinishLavaTemplate,
        EditorMode = CodeEditorMode.Lava,
        Description = "The text (HTML) to display on the success page. <span class='tip tip-lava'></span>",
        DefaultValue = DefaultFinishLavaTemplate,
        Category = AttributeCategory.TextOptions,
        Order = 8 )]

    #endregion

    #region Email Templates

    [SystemCommunicationField(
        "Confirm Account Email Template",
        Key = AttributeKey.ConfirmAccountEmailTemplate,
        Description = "The Email Template to use when confirming a new account",
        IsRequired = false,
        DefaultValue = Rock.SystemGuid.SystemCommunication.SECURITY_CONFIRM_ACCOUNT,
        Category = AttributeCategory.EmailTemplates,
        Order = 1 )]

    [SystemCommunicationField(
        "Receipt Email",
        Key = AttributeKey.ReceiptEmail,
        Description = "The system email to use to send the receipt.",
        IsRequired = false,
        Category = AttributeCategory.EmailTemplates,
        Order = 2 )]

    #endregion Email Templates

    #region Person Options

    [BooleanField(
        "Prompt for Phone",
        Key = AttributeKey.PromptForPhone,
        Category = AttributeCategory.PersonOptions,
        Description = "Should the user be prompted for their phone number?",
        DefaultBooleanValue = false,
        Order = 1 )]

    [BooleanField(
        "Prompt for Email",
        Key = AttributeKey.PromptForEmail,
        Category = AttributeCategory.PersonOptions,
        Description = "Should the user be prompted for their email address?",
        DefaultBooleanValue = true,
        Order = 2 )]

    [GroupLocationTypeField(
        "Address Type",
        Key = AttributeKey.PersonAddressType,
        Category = AttributeCategory.PersonOptions,
        Description = "The location type to use for the person's address",
        GroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY,
        DefaultValue = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME,
        IsRequired = false,
        Order = 3 )]

    [DefinedValueField(
        "Connection Status",
        Key = AttributeKey.PersonConnectionStatus,
        Category = AttributeCategory.PersonOptions,
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS,
        Description = "The connection status to use for new individuals (default: 'Prospect').",
        AllowMultiple = false,
        DefaultValue = Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_PROSPECT,
        IsRequired = true,
        Order = 4 )]

    [DefinedValueField(
        "Record Status",
        Key = AttributeKey.PersonRecordStatus,
        Category = AttributeCategory.PersonOptions,
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS,
        Description = "The record status to use for new individuals (default: 'Pending').",
        IsRequired = true,
        AllowMultiple = false,
        DefaultValue = Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING,
        Order = 5 )]

    [DefinedValueField(
        "Record Source",
        Key = AttributeKey.PersonRecordSource,
        Category = AttributeCategory.PersonOptions,
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.RECORD_SOURCE_TYPE,
        Description = "The record source to use for new individuals (default = 'Giving'). If a 'RecordSource' page parameter is found, it will be used instead.",
        IsRequired = true,
        AllowMultiple = false,
        DefaultValue = Rock.SystemGuid.DefinedValue.RECORD_SOURCE_TYPE_GIVING,
        Order = 6 )]

    #endregion Person Options

    #region Advanced options

    [DefinedValueField(
        "Transaction Type",
        Key = AttributeKey.TransactionType,
        Description = "",
        IsRequired = true,
        AllowMultiple = false,
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.FINANCIAL_TRANSACTION_TYPE,
        DefaultValue = Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION,
        Category = AttributeCategory.Advanced,
        Order = 1 )]

    [EntityTypeField(
        "Transaction Entity Type",
        Key = AttributeKey.TransactionEntityType,
        Description = "The Entity Type for the Transaction Detail Record (usually left blank)",
        IsRequired = false,
        Category = AttributeCategory.Advanced,
        Order = 2 )]

    [TextField( "Entity Id Parameter",
        Key = AttributeKey.EntityIdParam,
        Description = "The Page Parameter that will be used to set the EntityId value for the Transaction Detail Record (requires Transaction Entry Type to be configured)",
        IsRequired = false,
        Category = AttributeCategory.Advanced,
        Order = 3 )]

    [AttributeField( "Allowed Transaction Attributes From URL",
        Key = AttributeKey.AllowedTransactionAttributesFromURL,
        EntityTypeGuid = Rock.SystemGuid.EntityType.FINANCIAL_TRANSACTION,
        Description = "Specify any Transaction Attributes that can be populated from the URL.  The URL should be formatted like: ?Attribute_AttributeKey1=hello&Attribute_AttributeKey2=world",
        IsRequired = false,
        AllowMultiple = true,
        Category = AttributeCategory.Advanced,
        Order = 4 )]

    [BooleanField(
        "Allow Account Options In URL",
        Key = AttributeKey.AllowAccountOptionsInURL,
        Description = "Set to true to allow account options to be set via URL. To simply set allowed accounts, the allowed accounts can be specified as a comma-delimited list of AccountIds or AccountGlCodes. Example: ?AccountIds=1,2,3 or ?AccountGlCodes=40100,40110. The default amount for each account and whether it is editable can also be specified. Example:?AccountIds=1^50.00^false,2^25.50^false,3^35.00^true or ?AccountGlCodes=40100^50.00^false,40110^42.25^true",
        IsRequired = false,
        Category = AttributeCategory.Advanced,
        Order = 5 )]

    [BooleanField(
        "Only Public Accounts In URL",
        Key = AttributeKey.OnlyPublicAccountsInURL,
        Description = "Set to true if using the 'Allow Account Options In URL' option to prevent non-public accounts to be specified.",
        DefaultBooleanValue = true,
        Category = AttributeCategory.Advanced,
        Order = 6 )]

    [CodeEditorField(
        "Invalid Account Message",
        Key = AttributeKey.InvalidAccountInURLMessage,
        Description = "Display this text (HTML) as an error alert if an invalid 'account' or 'GL account' is passed through the URL. Leave blank to just ignore the invalid accounts and not show a message.",
        EditorMode = CodeEditorMode.Html,
        EditorTheme = CodeEditorTheme.Rock,
        EditorHeight = 200,
        IsRequired = false,
        DefaultValue = "",
        Category = AttributeCategory.Advanced,
        Order = 7 )]

    [BooleanField( "Enable Initial Back button",
        Key = AttributeKey.EnableInitialBackButton,
        Description = "Show a Back button on the initial page that will navigate to wherever the user was prior to the transaction entry",
        DefaultBooleanValue = false,
        Category = AttributeCategory.Advanced,
        Order = 8 )]

    [BooleanField(
        "Impersonation",
        Key = AttributeKey.AllowImpersonation,
        Description = "Should the current user be able to view and edit other people's transactions? IMPORTANT: This should only be enabled on an internal page that is secured to trusted users.",
        TrueText = "Allow (only use on an internal page used by staff)",
        FalseText = "Don't Allow",
        DefaultBooleanValue = false,
        Category = AttributeCategory.Advanced,
        Order = 9 )]

    #endregion Advanced Options

    #endregion Block Attributes
    [Rock.SystemGuid.BlockTypeGuid( "6316D801-40C0-4EED-A2AD-55C13870664D" )]
    public partial class TransactionEntryV2 : RockBlock
    {
        #region constants

        private const string DefaultAmountSummaryTemplate = @"
{% assign sortedAccounts = Accounts | Sort:'Order,PublicName' %}

<span class='account-names'>{{ sortedAccounts | Map:'PublicName' | Join:', ' | ReplaceLast:',',' and' }}</span>
-
<span class='account-campus'>{{ Campus.Name }}</span>";

        private const string DefaultFinishLavaTemplate = @"
{% if Transaction.ScheduledTransactionDetails %}
    {% assign transactionDetails = Transaction.ScheduledTransactionDetails %}
{% else %}
    {% assign transactionDetails = Transaction.TransactionDetails %}
{% endif %}

<h1>Thank You!</h1>

<p>Your support is helping {{ 'Global' | Attribute:'OrganizationName' }} actively achieve our
mission. We are so grateful for your commitment.</p>

<dl>
    <dt>Confirmation Code</dt>
    <dd>{{ Transaction.TransactionCode }}</dd>
    <dd></dd>

    <dt>Name</dt>
    <dd>{{ Person.FullName }}</dd>
    <dd></dd>
    <dd>{{ Person.Email }}</dd>
    <dd>{{ BillingLocation.Street }} {{ BillingLocation.City }}, {{ BillingLocation.State }} {{ BillingLocation.PostalCode }}</dd>
</dl>

<dl class='dl-horizontal'>
    {% for transactionDetail in transactionDetails %}
        <dt>{{ transactionDetail.Account.PublicName }}</dt>
        <dd>{{ transactionDetail.Amount | Minus: transactionDetail.FeeCoverageAmount | FormatAsCurrency }}</dd>
    {% endfor %}
    {% if Transaction.TotalFeeCoverageAmount %}
        <dt>Fee Coverage</dt>
        <dd>{{ Transaction.TotalFeeCoverageAmount | FormatAsCurrency }}</dd>
    {% endif %}
    <dd></dd>

    <dt>Payment Method</dt>
    <dd>{{ PaymentDetail.CurrencyTypeValue.Description}}</dd>

    {% if PaymentDetail.AccountNumberMasked  != '' %}
        <dt>Account Number</dt>
        <dd>{{ PaymentDetail.AccountNumberMasked }}</dd>
    {% endif %}

    <dt>When<dt>
    <dd>

    {% if Transaction.TransactionFrequencyValue %}
        {{ Transaction.TransactionFrequencyValue.Value }} //- Updated to include EndDate
{% if Transaction.EndDate %}starting on {{ Transaction.NextPaymentDate | Date:'sd' }} and ending on {{ Transaction.EndDate | Date:'sd' }}{% else %}starting on {{ Transaction.NextPaymentDate | Date:'sd' }}{% endif %}
    {% else %}
        Today
    {% endif %}
    </dd>
</dl>
";

        private const string DefaultScheduledTransactionsTemplate = @"
<h4>Scheduled {{ GiftTerm | Pluralize }}</h4>

{% for scheduledTransaction in ScheduledTransactions %}
    <div class='scheduled-transaction js-scheduled-transaction' data-scheduled-transaction-id='{{ scheduledTransaction.Id }}' data-expanded='{{ ExpandedStates[scheduledTransaction.Id] }}'>
        <div class='panel panel-default'>
            <div class='panel-heading'>
                <span class='panel-title h1'>
                    <i class='fa fa-calendar'></i>
                    {{ scheduledTransaction.TransactionFrequencyValue.Value }}
                </span>

                <span class='js-scheduled-totalamount scheduled-totalamount margin-l-md'>
                    {{ scheduledTransaction.TotalAmount | FormatAsCurrency }}
                </span>

                <div class='panel-actions pull-right'>
                    <span class='js-toggle-scheduled-details toggle-scheduled-details clickable fa fa-chevron-down'></span>
                </div>
            </div>

            <div class='js-scheduled-details scheduled-details margin-l-lg'>
                <div class='panel-body'>
                    {% for scheduledTransactionDetail in scheduledTransaction.ScheduledTransactionDetails %}
                        <div class='account-details'>
                            <span class='scheduled-transaction-account control-label'>
                                {{ scheduledTransactionDetail.Account.PublicName }}
                            </span>
                            <br />
                            <span class='scheduled-transaction-amount'>
                                {{ scheduledTransactionDetail.Amount | FormatAsCurrency }}
                            </span>
                        </div>
                    {% endfor %}

                    <br />
                    <span class='scheduled-transaction-payment-detail'>
                        {% assign financialPaymentDetail = scheduledTransaction.FinancialPaymentDetail %}

                        {% if financialPaymentDetail.CurrencyTypeValue.Value != 'Credit Card' %}
                            {{ financialPaymentDetail.CurrencyTypeValue.Value }}
                        {% else %}
                            {{ financialPaymentDetail.CreditCardTypeValue.Value }} {{ financialPaymentDetail.AccountNumberMasked }} Expires: {{ financialPaymentDetail.ExpirationDate }}
                        {% endif %}
                    </span>
                    <br />

                    {% if scheduledTransaction.NextPaymentDate != null %}
                        Next Gift: {{ scheduledTransaction.NextPaymentDate | Date:'sd' }}.
                    {% endif %}


                    <div class='scheduled-details-actions margin-t-md'>
                        {% if LinkedPages.ScheduledTransactionEditPage != '' %}
                            <a href='{{ LinkedPages.ScheduledTransactionEditPage }}?ScheduledTransactionId={{ scheduledTransaction.Id }}'>Edit</a>
                        {% endif %}
                        <a class='margin-l-sm' onclick=""{{ scheduledTransaction.Id | Postback:'DeleteScheduledTransaction' }}"">Delete</a>
                    </div>
                </div>
            </div>
        </div>
    </div>
{% endfor %}


<script type='text/javascript'>

    // Scheduled Transaction JavaScripts
    function setScheduledDetailsVisibility($container, animate) {
        var $scheduledDetails = $container.find('.js-scheduled-details');
        var expanded = $container.attr('data-expanded');
        var $totalAmount = $container.find('.js-scheduled-totalamount');
        var $toggle = $container.find('.js-toggle-scheduled-details');

        if (expanded == 1) {
            if (animate) {
                $scheduledDetails.slideDown();
                $totalAmount.fadeOut();
            } else {
                $scheduledDetails.show();
                $totalAmount.hide();
            }

            $toggle.removeClass('fa-chevron-down').addClass('fa-chevron-up');
        } else {
            if (animate) {
                $scheduledDetails.slideUp();
                $totalAmount.fadeIn();
            } else {
                $scheduledDetails.hide();
                $totalAmount.show();
            }

            $toggle.removeClass('fa-chevron-up').addClass('fa-chevron-down');
        }
    };

    Sys.Application.add_load(function () {
        var $scheduleDetailsContainers = $('.js-scheduled-transaction');

        $scheduleDetailsContainers.each(function (index) {
            setScheduledDetailsVisibility($($scheduleDetailsContainers[index]), false);
        });

        var $toggleScheduledDetails = $('.js-toggle-scheduled-details');
        $toggleScheduledDetails.on('click', function () {
            var $scheduledDetailsContainer = $(this).closest('.js-scheduled-transaction');
            if ($scheduledDetailsContainer.attr('data-expanded') == 1) {
                $scheduledDetailsContainer.attr('data-expanded', 0);
            } else {
                $scheduledDetailsContainer.attr('data-expanded', 1);
            }

            setScheduledDetailsVisibility($scheduledDetailsContainer, true);
        });
    });
</script>
";

        #endregion

        #region Attribute Keys

        /// <summary>
        /// Keys to use for Block Attributes
        /// </summary>
        private static class AttributeKey
        {
            public const string AccountsToDisplay = "AccountsToDisplay";
            public const string AllowImpersonation = "AllowImpersonation";
            public const string AllowScheduledTransactions = "AllowScheduledTransactions";
            public const string BatchNamePrefix = "BatchNamePrefix";
            public const string FinancialGateway = "FinancialGateway";
            public const string EnableACH = "EnableACH";
            public const string EnableCreditCard = "EnableCreditCard";
            public const string EnableCommentEntry = "EnableCommentEntry";
            public const string CommentEntryLabel = "CommentEntryLabel";
            public const string EnableBusinessGiving = "EnableBusinessGiving";
            public const string EnableAnonymousGiving = "EnableAnonymousGiving";
            public const string AnonymousGivingTooltip = "AnonymousGivingTooltip";
            public const string PaymentCommentTemplate = "PaymentCommentTemplate";
            public const string EnableInitialBackButton = "EnableInitialBackButton";
            public const string FinancialSourceType = "FinancialSourceType";
            public const string ShowScheduledTransactions = "ShowScheduledTransactions";
            public const string ScheduledTransactionsTemplate = "ScheduledTransactionsTemplate";
            public const string ScheduledTransactionEditPage = "ScheduledTransactionEditPage";
            public const string GiftTerm = "GiftTerm";
            public const string GiveButtonNowText = "GiveButtonNowText";
            public const string GiveButtonScheduledText = "GiveButtonScheduledText";
            public const string AccountHeaderTemplate = "AccountHeaderTemplate";
            public const string AmountSummaryTemplate = "AmountSummaryTemplate";
            public const string AskForCampusIfKnown = "AskForCampusIfKnown";
            public const string IncludeInactiveCampuses = "IncludeInactiveCampuses";
            public const string IncludedCampusTypes = "IncludedCampusTypes";
            public const string IncludedCampusStatuses = "IncludedCampusStatuses";
            public const string EnableMultiAccount = "EnableMultiAccount";
            public const string IntroMessageTemplate = "IntroMessageTemplate";
            public const string FinishLavaTemplate = "FinishLavaTemplate";
            public const string SaveAccountTitle = "SaveAccountTitle";
            public const string ConfirmAccountEmailTemplate = "ConfirmAccountEmailTemplate";
            public const string TransactionType = "Transaction Type";
            public const string TransactionEntityType = "TransactionEntityType";
            public const string EntityIdParam = "EntityIdParam";
            public const string AllowedTransactionAttributesFromURL = "AllowedTransactionAttributesFromURL";
            public const string AllowAccountOptionsInURL = "AllowAccountOptionsInURL";
            public const string OnlyPublicAccountsInURL = "OnlyPublicAccountsInURL";
            public const string InvalidAccountInURLMessage = "InvalidAccountInURLMessage";
            public const string ReceiptEmail = "ReceiptEmail";
            public const string PromptForPhone = "PromptForPhone";
            public const string PromptForEmail = "PromptForEmail";
            public const string PersonAddressType = "PersonAddressType";
            public const string PersonConnectionStatus = "PersonConnectionStatus";
            public const string PersonRecordStatus = "PersonRecordStatus";
            public const string PersonRecordSource = "PersonRecordSource";
            public const string EnableFeeCoverage = "EnableFeeCoverage";
            public const string FeeCoverageDefaultState = "FeeCoverageDefaultState";
            public const string FeeCoverageMessage = "FeeCoverageMessage";
            public const string DisableCaptchaSupport = "DisableCaptchaSupport";
            public const string EnableEndDate = "EnableEndDate";
        }

        #endregion Attribute Keys

        #region Attribute Categories

        private static class AttributeCategory
        {
            public const string None = "";
            public const string ScheduleGifts = "Scheduled Gifts";
            public const string PaymentComments = "Payment Comments";
            public const string TextOptions = "Text Options";
            public const string Advanced = "Advanced";
            public const string EmailTemplates = "Email Templates";
            public const string PersonOptions = "Person Options";
        }

        #endregion Attribute Categories

        #region MergeFieldKeys

        private static class MergeFieldKey
        {
            public const string AmountHTML = "AmountHTML";
            public const string Percentage = "Percentage";
            public const string IsPercentage = "IsPercentage";
            public const string FixedAmount = "FixedAmount";
            public const string IsFixedAmount = "IsFixedAmount";
            public const string CalculatedAmountJSHook = "CalculatedAmountJSHook";
            public const string IsSavedAccount = "IsSavedAccount";
            public const string CalculatedAmount = "CalculatedAmount";
        }

        #endregion MergeFieldKeys

        #region PageParameterKeys

        private static class PageParameterKey
        {
            public const string Person = "rckid";

            public const string AttributeKeyPrefix = "Attribute_";

            public const string AccountIdsOptions = "AccountIds";

            public const string AccountGlCodesOptions = "AccountGlCodes";

            public const string AmountLimit = "AmountLimit";

            /// <summary>
            /// The frequency options in the form of &Frequency=DefinedValueId^UserEditable
            /// </summary>
            public const string FrequencyOptions = "Frequency";

            public const string StartDate = "StartDate";

            /// <summary>
            /// Overrides how campus is determined (instead of basing on Person, etc).
            /// If CampusId is specified in the URL, that would take precedence for how the campus is determined.
            /// For example, when creating a new person/family, setting the Account Picker's campus, etc
            /// </summary>
            public const string CampusId = "CampusId";

            public const string ScheduledTransactionGuidToTransfer = "ScheduledTransactionGuid";
            public const string Transfer = "Transfer";

            public const string ParticipationMode = "ParticipationMode";
        }

        #endregion

        #region ViewState Keys

        private static class ViewStateKey
        {
            // The Campus Id to use. This is usually based on the current person
            // but could be overridden by setting CampusId in the url.
            public const string SelectedCampusId = "SelectedCampusId";

            public const string HostPaymentInfoSubmitScript = "HostPaymentInfoSubmitScript";
            public const string TransactionCode = "TransactionCode";
            public const string CustomerTokenEncrypted = "CustomerTokenEncrypted";
            public const string TargetPersonGuid = "TargetPersonGuid";
            public const string ScheduledTransactionIdToBeTransferred = "ScheduledTransactionIdToBeTransferred";
        }

        #endregion ViewState Keys

        #region enums

        /// <summary>
        ///
        /// </summary>
        private enum EntryStep
        {
            /// <summary>
            /// prompt for amounts (step 1)
            /// </summary>
            PromptForAmounts,

            /// <summary>
            /// Get payment information (step 2)
            /// </summary>
            GetPaymentInfo,

            /// <summary>
            /// Get/Update personal information (step 3)
            /// </summary>
            GetPersonalInformation,

            /// <summary>
            /// The show transaction summary (step 4)
            /// </summary>
            ShowTransactionSummary
        }

        #endregion enums

        #region fields

        private Control _hostedPaymentInfoControl;

        /// <summary>
        /// use FinancialGateway instead
        /// </summary>
        private Rock.Model.FinancialGateway _financialGateway = null;

        /// <summary>
        /// Gets the financial gateway (model) that is configured for this block
        /// </summary>
        /// <returns></returns>
        private Rock.Model.FinancialGateway FinancialGateway
        {
            get
            {
                if ( _financialGateway == null )
                {
                    RockContext rockContext = new RockContext();
                    var financialGatewayGuid = this.GetAttributeValue( AttributeKey.FinancialGateway ).AsGuid();
                    _financialGateway = new FinancialGatewayService( rockContext ).GetNoTracking( financialGatewayGuid );
                }

                return _financialGateway;
            }
        }

        private IHostedGatewayComponent _financialGatewayComponent = null;

        /// <summary>
        /// Gets the financial gateway component that is configured for this block
        /// </summary>
        /// <returns></returns>
        private IHostedGatewayComponent FinancialGatewayComponent
        {
            get
            {
                if ( _financialGatewayComponent == null )
                {
                    var financialGateway = FinancialGateway;
                    if ( financialGateway != null )
                    {
                        _financialGatewayComponent = financialGateway.GetGatewayComponent() as IHostedGatewayComponent;
                    }
                }

                return _financialGatewayComponent;
            }
        }

        #endregion Fields

        #region helper classes

        /// <summary>
        /// Helper object for account options passed via the request string using <see cref="AttributeKey.AllowAccountOptionsInURL"/>
        /// </summary>
        private class ParameterAccountOption
        {
            public int AccountId { get; set; }

            public decimal? Amount { get; set; }

            public bool Enabled { get; set; }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the payment transaction code.
        /// </summary>
        protected string TransactionCode
        {
            get { return ViewState[ViewStateKey.TransactionCode] as string ?? string.Empty; }
            set { ViewState[ViewStateKey.TransactionCode] = value; }
        }

        /// <summary>
        /// Gets or sets the Customer Token for a newly created customer token from the payment info control.
        /// NOTE: Lets encrypt this since we don't want the ViewState to have an un-encrypted customer token, even though ViewState is already encrypted.
        /// </summary>
        /// <value>
        /// The customer token (encrypted)
        /// </value>
        protected string CustomerTokenEncrypted
        {
            get { return ViewState[ViewStateKey.CustomerTokenEncrypted] as string ?? string.Empty; }
            set { ViewState[ViewStateKey.CustomerTokenEncrypted] = value; }
        }

        protected int? SelectedCampusId
        {
            get { return ViewState[ViewStateKey.SelectedCampusId] as int?; }
            set { ViewState[ViewStateKey.SelectedCampusId] = value; }
        }

        #endregion Properties

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            // Don't use captcha if the block is set to disable (DisableCaptchaSupport==true) it or if is not configured (IsAvailable==false)
            var disableCaptchaSupport = GetAttributeValue( AttributeKey.DisableCaptchaSupport ).AsBoolean() || !cpCaptcha.IsAvailable;
            cpCaptcha.Visible = !disableCaptchaSupport;
            cpCaptcha.TokenReceived += CpCaptcha_TokenReceived;

            var enableACH = this.GetAttributeValue( AttributeKey.EnableACH ).AsBoolean();
            var enableCreditCard = this.GetAttributeValue( AttributeKey.EnableCreditCard ).AsBoolean();
            if ( this.FinancialGatewayComponent != null && this.FinancialGateway != null )
            {
                _hostedPaymentInfoControl = this.FinancialGatewayComponent.GetHostedPaymentInfoControl( this.FinancialGateway, $"_hostedPaymentInfoControl_{this.FinancialGateway.Id}", new HostedPaymentInfoControlOptions { EnableACH = enableACH, EnableCreditCard = enableCreditCard } );
                phHostedPaymentControl.Controls.Add( _hostedPaymentInfoControl );

                if ( disableCaptchaSupport )
                {
                    hfHostPaymentInfoSubmitScript.Value = this.FinancialGatewayComponent.GetHostPaymentInfoSubmitScript( this.FinancialGateway, _hostedPaymentInfoControl );
                }
            }

            if ( _hostedPaymentInfoControl is IHostedGatewayPaymentControlTokenEvent )
            {
                ( _hostedPaymentInfoControl as IHostedGatewayPaymentControlTokenEvent ).TokenReceived += _hostedPaymentInfoControl_TokenReceived;
            }

            if ( this.GetAttributeValue( AttributeKey.EnableFeeCoverage ).AsBoolean() && _hostedPaymentInfoControl is IHostedGatewayPaymentControlCurrencyTypeEvent )
            {
                ( _hostedPaymentInfoControl as IHostedGatewayPaymentControlCurrencyTypeEvent ).CurrencyTypeChange += TransactionEntryV2_HostedPaymentControlCurrencyTypeChange;
            }

            tglIndividualOrBusiness.Visible = this.GetAttributeValue( AttributeKey.EnableBusinessGiving ).AsBoolean();

            cbGiveAnonymouslyIndividual.Visible = this.GetAttributeValue( AttributeKey.EnableAnonymousGiving ).AsBoolean();
            cbGiveAnonymouslyIndividual.ToolTip = this.GetAttributeValue( AttributeKey.AnonymousGivingTooltip );
            cbGiveAnonymouslyBusiness.Visible = this.GetAttributeValue( AttributeKey.EnableAnonymousGiving ).AsBoolean();
            cbGiveAnonymouslyBusiness.ToolTip = this.GetAttributeValue( AttributeKey.AnonymousGivingTooltip );

            // Evaluate if comment entry box should be displayed
            tbCommentEntry.Label = GetAttributeValue( AttributeKey.CommentEntryLabel );
            tbCommentEntry.Visible = GetAttributeValue( AttributeKey.EnableCommentEntry ).AsBoolean();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                // Ensure that there is only one transaction processed by getting a unique guid when this block loads for the first time
                // This will ensure there are no (unintended) duplicate transactions
                hfTransactionGuid.Value = Guid.NewGuid().ToString();
                ShowDetails();
            }
            else
            {
                RouteAction();
            }

            base.OnLoad( e );
        }

        #endregion Base Control Methods

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            // If block options where changed, reload the whole page since changing some of the options (Gateway ACH Control options ) requires a full page reload
            this.NavigateToCurrentPageReference();
        }

        /// <summary>
        /// Handles the TokenReceived event of the _hostedPaymentInfoControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void _hostedPaymentInfoControl_TokenReceived( object sender, HostedGatewayPaymentControlTokenEventArgs e )
        {
            if ( !e.IsValid )
            {
                nbPaymentTokenError.Text = e.ErrorMessage;
                nbPaymentTokenError.Visible = true;
            }
            else
            {
                nbPaymentTokenError.Visible = false;
                btnGetPaymentInfoNext_Click( sender, e );
            }
        }

        /// <summary>
        /// Handles the TokenReceived event of the CpCaptcha control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Captcha.TokenReceivedEventArgs"/> instance containing the event data.</param>
        private void CpCaptcha_TokenReceived( object sender, Captcha.TokenReceivedEventArgs e )
        {
            if ( e.IsValid )
            {
                hfHostPaymentInfoSubmitScript.Value = this.FinancialGatewayComponent.GetHostPaymentInfoSubmitScript( this.FinancialGateway, _hostedPaymentInfoControl );
                cpCaptcha.Visible = false;
            }
        }

        #endregion

        #region Gateway Help Related

        /// <summary>
        /// Handles the ItemDataBound event of the rptInstalledGateways control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptInstalledGateways_ItemDataBound( object sender, System.Web.UI.WebControls.RepeaterItemEventArgs e )
        {
            IHostedGatewayComponent financialGatewayComponent = e.Item.DataItem as IHostedGatewayComponent;
            if ( financialGatewayComponent == null )
            {
                return;
            }

            var gatewayEntityType = EntityTypeCache.Get( financialGatewayComponent.TypeGuid );
            var gatewayEntityTypeType = gatewayEntityType.GetEntityType();

            HiddenField hfGatewayEntityTypeId = e.Item.FindControl( "hfGatewayEntityTypeId" ) as HiddenField;
            hfGatewayEntityTypeId.Value = gatewayEntityType.Id.ToString();

            Literal lGatewayName = e.Item.FindControl( "lGatewayName" ) as Literal;
            Literal lGatewayDescription = e.Item.FindControl( "lGatewayDescription" ) as Literal;

            lGatewayName.Text = Reflection.GetDisplayName( gatewayEntityTypeType );
            lGatewayDescription.Text = Reflection.GetDescription( gatewayEntityTypeType );

            HyperLink aGatewayConfigure = e.Item.FindControl( "aGatewayConfigure" ) as HyperLink;
            HyperLink aGatewayLearnMore = e.Item.FindControl( "aGatewayLearnMore" ) as HyperLink;
            aGatewayConfigure.Visible = financialGatewayComponent.ConfigureURL.IsNotNullOrWhiteSpace();
            aGatewayLearnMore.Visible = financialGatewayComponent.LearnMoreURL.IsNotNullOrWhiteSpace();

            aGatewayConfigure.NavigateUrl = financialGatewayComponent.ConfigureURL;
            aGatewayLearnMore.NavigateUrl = financialGatewayComponent.LearnMoreURL;
        }

        /// <summary>
        /// Loads and Validates the gateways, showing a message if the gateways aren't configured correctly
        /// </summary>
        private bool LoadGatewayOptions()
        {
            if ( this.FinancialGateway == null )
            {
                ShowGatewayHelp();
                return false;
            }
            else
            {
                HideGatewayHelp();
            }

            // get the FinancialGateway's GatewayComponent so we can show a warning if they have an unsupported gateway.
            var hostedGatewayComponent = FinancialGateway.GetGatewayComponent() as IHostedGatewayComponent;

            var testGatewayGuid = Rock.SystemGuid.EntityType.FINANCIAL_GATEWAY_TEST_GATEWAY.AsGuid();

            if ( hostedGatewayComponent == null )
            {
                ShowConfigurationMessage( NotificationBoxType.Warning, "Unsupported Gateway", "This block only supports Gateways that have a hosted payment interface." );
                pnlTransactionEntry.Visible = false;
                return false;
            }
            else if ( this.FinancialGatewayComponent.TypeGuid == testGatewayGuid )
            {
                ShowConfigurationMessage( NotificationBoxType.Warning, "Testing", "You are using the Test Financial Gateway. No actual amounts will be charged to your card or bank account." );
            }
            else
            {
                HideConfigurationMessage();
            }

            bool allowScheduledTransactions = this.GetAttributeValue( AttributeKey.AllowScheduledTransactions ).AsBoolean();
            if ( allowScheduledTransactions )
            {
                SetFrequencyOptions();
            }

            var startDate = PageParameter( PageParameterKey.StartDate ).AsDateTime();
            if ( startDate.HasValue && startDate.Value > RockDateTime.Today )
            {
                dtpStartDate.SelectedDate = startDate.Value;
            }
            else
            {
                dtpStartDate.SelectedDate = RockDateTime.Today;
            }

            pnlScheduledTransactionFrequency.Visible = allowScheduledTransactions;
            pnlScheduledTransactionStartDate.Visible = allowScheduledTransactions;

            return true;
        }

        /// <summary>
        /// Configures the Cover the Fees controls
        /// </summary>
        private void ConfigureCoverTheFees()
        {
            pnlGetPaymentInfoCoverTheFeeCreditCard.Visible = false;
            pnlGetPaymentInfoCoverTheFeeACH.Visible = false;
            hfCoverTheFeeCreditCardPercent.Value = null;
            var totalAmount = caapPromptForAccountAmounts.AccountAmounts.Sum( a => a.Amount ?? 0.00M );

            hfAmountWithoutCoveredFee.Value = totalAmount.FormatAsCurrency();

            pnlGiveNowCoverTheFee.Visible = false;
            var enableFeeCoverage = this.GetAttributeValue( AttributeKey.EnableFeeCoverage ).AsBoolean();
            if ( !enableFeeCoverage )
            {
                // option isn't enabled
                return;
            }

            var feeCoverageGatewayComponent = FinancialGateway.GetGatewayComponent() as IFeeCoverageGatewayComponent;
            if ( feeCoverageGatewayComponent == null )
            {
                // the gateway doesn't have fee coverage options
                return;
            }

            bool feeCoverageDefaultState = this.GetAttributeValue( AttributeKey.FeeCoverageDefaultState ).AsBoolean();
            cbGiveNowCoverTheFee.Checked = feeCoverageDefaultState;
            cbGetPaymentInfoCoverTheFeeACH.Checked = feeCoverageDefaultState;
            cbGetPaymentInfoCoverTheFeeCreditCard.Checked = feeCoverageDefaultState;

            var feeCoverageMessageTemplate = this.GetAttributeValue( AttributeKey.FeeCoverageMessage );
            var feeCoverageMergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage );

            var creditCardFeeCoveragePercentage = feeCoverageGatewayComponent.GetCreditCardFeeCoveragePercentage( FinancialGateway );
            feeCoverageMergeFields.Add( MergeFieldKey.Percentage, creditCardFeeCoveragePercentage );
            feeCoverageMergeFields.Add( MergeFieldKey.IsPercentage, creditCardFeeCoveragePercentage.HasValue );
            var achFeeCoverageAmount = feeCoverageGatewayComponent.GetACHFeeCoverageAmount( FinancialGateway );
            feeCoverageMergeFields.Add( MergeFieldKey.FixedAmount, achFeeCoverageAmount );
            feeCoverageMergeFields.Add( MergeFieldKey.IsFixedAmount, achFeeCoverageAmount.HasValue );
            var calculatedAmountJSHook = "js-coverthefee-checkbox-fee-amount-text";
            feeCoverageMergeFields.Add( MergeFieldKey.CalculatedAmountJSHook, calculatedAmountJSHook );

            if ( creditCardFeeCoveragePercentage > 0 )
            {
                pnlGetPaymentInfoCoverTheFeeCreditCard.Visible = this.GetAttributeValue( AttributeKey.EnableCreditCard ).AsBoolean();

                var creditCardFeeCoverageAmount = decimal.Round( totalAmount * ( creditCardFeeCoveragePercentage.Value / 100.0M ), 2 );
                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.AmountHTML, creditCardFeeCoverageAmount.FormatAsCurrency() );
                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.IsSavedAccount, false );
                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.CalculatedAmount, creditCardFeeCoverageAmount );
                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.IsPercentage, true );
                cbGetPaymentInfoCoverTheFeeCreditCard.Text = feeCoverageMessageTemplate.ResolveMergeFields( feeCoverageMergeFields );
                hfAmountWithCoveredFeeCreditCard.Value = ( totalAmount + creditCardFeeCoverageAmount ).FormatAsCurrency();
            }

            if ( achFeeCoverageAmount > 0 )
            {
                pnlGetPaymentInfoCoverTheFeeACH.Visible = this.GetAttributeValue( AttributeKey.EnableACH ).AsBoolean();
                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.AmountHTML, achFeeCoverageAmount.FormatAsCurrency() );
                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.IsSavedAccount, false );
                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.CalculatedAmount, achFeeCoverageAmount );
                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.IsPercentage, false );
                cbGetPaymentInfoCoverTheFeeACH.Text = feeCoverageMessageTemplate.ResolveMergeFields( feeCoverageMergeFields );
                hfAmountWithCoveredFeeACH.Value = ( totalAmount + achFeeCoverageAmount ).FormatAsCurrency();
            }

            if ( achFeeCoverageAmount > 0 || creditCardFeeCoveragePercentage > 0 )
            {
                if ( _hostedPaymentInfoControl is IHostedGatewayPaymentControlCurrencyTypeEvent )
                {
                    var paymentControlCurrencyTypeValue = ( _hostedPaymentInfoControl as IHostedGatewayPaymentControlCurrencyTypeEvent ).CurrencyTypeValue;
                    var paymentControlCurrencyTypeGuid = paymentControlCurrencyTypeValue != null ? paymentControlCurrencyTypeValue.Guid : ( Guid? ) null;
                    SetControlsForSelectedPaymentCurrencyType( paymentControlCurrencyTypeGuid );
                }
            }

            var financialPersonSavedAccountId = ddlPersonSavedAccount.SelectedValue.AsInteger();
            if ( financialPersonSavedAccountId == 0 )
            {
                // No saved account selected, so don't show the option until the Payment Info step
                pnlGiveNowCoverTheFee.Visible = false;
                return;
            }

            bool isAch = this.UsingACHPersonSavedAccount();

            if ( isAch )
            {
                hfCoverTheFeeCreditCardPercent.Value = null;
                if ( !achFeeCoverageAmount.HasValue || achFeeCoverageAmount.Value == 0.00M )
                {
                    return;
                }

                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.AmountHTML, achFeeCoverageAmount.FormatAsCurrency() );
                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.IsSavedAccount, true );
                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.IsPercentage, false );
                cbGiveNowCoverTheFee.Text = feeCoverageMessageTemplate.ResolveMergeFields( feeCoverageMergeFields );
                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.CalculatedAmount, achFeeCoverageAmount );
            }
            else
            {
                hfCoverTheFeeCreditCardPercent.Value = creditCardFeeCoveragePercentage.ToString();
                if ( !creditCardFeeCoveragePercentage.HasValue || creditCardFeeCoveragePercentage.Value == 0.00M )
                {
                    return;
                }

                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.AmountHTML, $"{RockCurrencyCodeInfo.GetCurrencySymbol()}<span class='{calculatedAmountJSHook}' decimal-places='{RockCurrencyCodeInfo.GetDecimalPlaces()}'></span>" );
                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.IsSavedAccount, true );
                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.IsPercentage, true );
                feeCoverageMergeFields.AddOrReplace( MergeFieldKey.CalculatedAmount, null );

                cbGiveNowCoverTheFee.Text = feeCoverageMessageTemplate.ResolveMergeFields( feeCoverageMergeFields );
            }

            pnlGiveNowCoverTheFee.Visible = true;
        }

        /// <summary>
        /// Handles the HostedPaymentControlCurrencyTypeChange event of the TransactionEntryV2 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="HostedGatewayPaymentControlCurrencyTypeEventArgs"/> instance containing the event data.</param>
        private void TransactionEntryV2_HostedPaymentControlCurrencyTypeChange( object sender, HostedGatewayPaymentControlCurrencyTypeEventArgs e )
        {
            var currencyTypeValueGuid = e.hostedGatewayPaymentControl.CurrencyTypeValue != null ? e.hostedGatewayPaymentControl.CurrencyTypeValue.Guid : ( Guid? ) null;
            SetControlsForSelectedPaymentCurrencyType( currencyTypeValueGuid );
        }

        /// <summary>
        /// Sets the type of the controls for selected payment currency.
        /// </summary>
        /// <param name="currencyTypeValueGuid">The currency type value unique identifier.</param>
        private void SetControlsForSelectedPaymentCurrencyType( Guid? currencyTypeValueGuid )
        {
            var feeCoverageGatewayComponent = FinancialGateway.GetGatewayComponent() as IFeeCoverageGatewayComponent;
            if ( feeCoverageGatewayComponent == null )
            {
                return;
            }

            var achSelected = currencyTypeValueGuid == Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_ACH.AsGuid();
            if ( achSelected )
            {
                var feeCoverageACHAmount = feeCoverageGatewayComponent.GetACHFeeCoverageAmount( this.FinancialGateway );
                pnlGetPaymentInfoCoverTheFeeACH.Visible = feeCoverageACHAmount > 0;
                pnlGetPaymentInfoCoverTheFeeCreditCard.Visible = false;
            }
            else
            {
                var creditCardFeeCoveragePercentage = feeCoverageGatewayComponent.GetCreditCardFeeCoveragePercentage( this.FinancialGateway );
                pnlGetPaymentInfoCoverTheFeeACH.Visible = false;
                pnlGetPaymentInfoCoverTheFeeCreditCard.Visible = creditCardFeeCoveragePercentage > 0;
            }

            UpdateAccountSummaryAmount();
        }

        /// <summary>
        /// Usings the ach person saved account.
        /// </summary>
        /// <returns></returns>
        private bool UsingACHPersonSavedAccount()
        {
            var financialPersonSavedAccountId = ddlPersonSavedAccount.SelectedValue.AsInteger();
            if ( financialPersonSavedAccountId == 0 )
            {
                return false;
            }

            var currencyTypeValueIdACH = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_ACH.AsGuid() );
            var financialPersonSavedAccountCurrencyTypeValueId = new FinancialPersonSavedAccountService( new RockContext() )
                .GetSelect( financialPersonSavedAccountId, s => s.FinancialPaymentDetail.CurrencyTypeValueId );
            var isAch = financialPersonSavedAccountCurrencyTypeValueId.HasValue && financialPersonSavedAccountCurrencyTypeValueId.Value == currencyTypeValueIdACH;
            return isAch;
        }

        /// <summary>
        /// Sets the schedule frequency options.
        /// </summary>
        private void SetFrequencyOptions()
        {
            var supportedFrequencies = this.FinancialGatewayComponent.SupportedPaymentSchedules;
            foreach ( var supportedFrequency in supportedFrequencies )
            {
                ddlFrequency.Items.Add( new ListItem( supportedFrequency.Value, supportedFrequency.Id.ToString() ) );
            }

            // If gateway didn't specifically support one-time, add it anyway for immediate gifts
            var oneTimeFrequency = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME );
            if ( !supportedFrequencies.Where( f => f.Id == oneTimeFrequency.Id ).Any() )
            {
                ddlFrequency.Items.Insert( 0, new ListItem( oneTimeFrequency.Value, oneTimeFrequency.Id.ToString() ) );
            }

            DefinedValueCache pageParameterFrequency = null;
            bool frequencyEditable = true;
            string frequencyParameterValue = this.PageParameter( PageParameterKey.FrequencyOptions );
            if ( frequencyParameterValue.IsNotNullOrWhiteSpace() )
            {
                // if there is a Frequency specified in the Url, set the to the default, and optionally make it ReadOnly
                string[] frequencyOptions = frequencyParameterValue.Split( '^' );
                var defaultFrequencyValueId = frequencyOptions[0].AsIntegerOrNull();
                if ( frequencyOptions.Length >= 2 )
                {
                    frequencyEditable = frequencyOptions[0].AsBooleanOrNull() ?? true;
                }

                if ( defaultFrequencyValueId.HasValue )
                {
                    pageParameterFrequency = DefinedValueCache.Get( defaultFrequencyValueId.Value );
                }
            }

            if ( !frequencyEditable && pageParameterFrequency != null )
            {
                ddlFrequency.Enabled = false;
            }
            else
            {
                ddlFrequency.Enabled = true;
            }

            ddlFrequency.SetValue( pageParameterFrequency ?? oneTimeFrequency );
        }

        /// <summary>
        /// Shows the gateway help
        /// </summary>
        private void ShowGatewayHelp()
        {
            pnlGatewayHelp.Visible = true;
            pnlTransactionEntry.Visible = false;

            var hostedGatewayComponentList = Rock.Financial.GatewayContainer.Instance.Components
                .Select( a => a.Value.Value )
                .Where( a => a is IHostedGatewayComponent && !( a is TestGateway ) )
                .Select( a => a as IHostedGatewayComponent ).ToList();

            rptInstalledGateways.DataSource = hostedGatewayComponentList;
            rptInstalledGateways.DataBind();
        }

        /// <summary>
        /// Hides the gateway help.
        /// </summary>
        private void HideGatewayHelp()
        {
            pnlGatewayHelp.Visible = false;
        }

        /// <summary>
        /// Shows the configuration message.
        /// </summary>
        /// <param name="notificationBoxType">Type of the notification box.</param>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        private void ShowConfigurationMessage( NotificationBoxType notificationBoxType, string title, string message )
        {
            nbConfigurationNotification.NotificationBoxType = notificationBoxType;
            nbConfigurationNotification.Title = title;
            nbConfigurationNotification.Text = message;

            nbConfigurationNotification.Visible = true;
        }

        /// <summary>
        /// Hides the configuration message.
        /// </summary>
        private void HideConfigurationMessage()
        {
            nbConfigurationNotification.Visible = false;
        }

        #endregion Gateway Guide Related

        #region Scheduled Gifts Related

        /// <summary>
        /// if ShowScheduledTransactions is enabled, Loads Scheduled Transactions into Lava Merge Fields for <seealso cref="AttributeKey.ScheduledTransactionsTemplate"/>
        /// </summary>
        private void BindScheduledTransactions()
        {
            if ( !this.GetAttributeValue( AttributeKey.ShowScheduledTransactions ).AsBoolean() )
            {
                HideScheduledTransactionsPanel();
                return;
            }

            var rockContext = new RockContext();
            var targetPerson = GetTargetPerson( rockContext );

            if ( targetPerson == null )
            {
                HideScheduledTransactionsPanel();
                return;
            }

            var mergeFields = LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson, new CommonMergeFieldsOptions() );
            mergeFields.Add( "GiftTerm", this.GetAttributeValue( AttributeKey.GiftTerm ) ?? "Gift" );

            Dictionary<string, object> linkedPages = new Dictionary<string, object>();
            linkedPages.Add( "ScheduledTransactionEditPage", LinkedPageRoute( AttributeKey.ScheduledTransactionEditPage ) );
            mergeFields.Add( "LinkedPages", linkedPages );

            FinancialScheduledTransactionService financialScheduledTransactionService = new FinancialScheduledTransactionService( rockContext );
            FinancialGatewayService financialGatewayService = new FinancialGatewayService( rockContext );

            // get business giving id
            var givingIdList = targetPerson.GetBusinesses( rockContext ).Select( g => g.GivingId ).ToList();

            // Only list scheduled transactions that use a Hosted Gateway
            var hostedGatewayIdList = financialGatewayService.Queryable()
                .Where( a => a.IsActive )
                .AsNoTracking()
                .ToList().Where( a => a.GetGatewayComponent() is IHostedGatewayComponent )
                .Select( a => a.Id )
                .ToList();

            var targetPersonGivingId = targetPerson.GivingId;
            givingIdList.Add( targetPersonGivingId );
            var scheduledTransactionList = financialScheduledTransactionService.Queryable()
                .Where( a => givingIdList.Contains( a.AuthorizedPersonAlias.Person.GivingId ) && a.FinancialGatewayId.HasValue && a.IsActive == true && hostedGatewayIdList.Contains( a.FinancialGatewayId.Value ) )
                .ToList();

            // Refresh the active transactions
            financialScheduledTransactionService.GetStatus( scheduledTransactionList, true );

            // in case .GetStatus set an schedule to IsActive=False, filter the scheduledTransactionList by IsActive=True again
            scheduledTransactionList = scheduledTransactionList.Where( a => a.IsActive ).ToList();

            rockContext.SaveChanges();

            if ( !scheduledTransactionList.Any() )
            {
                HideScheduledTransactionsPanel();
            }
            else
            {
                ShowScheduledTransactionsPanel();
            }

            scheduledTransactionList = scheduledTransactionList.OrderByDescending( a => a.NextPaymentDate ).ToList();

            mergeFields.Add( "ScheduledTransactions", scheduledTransactionList );

            var scheduledTransactionsTemplate = this.GetAttributeValue( AttributeKey.ScheduledTransactionsTemplate );
            lScheduledTransactionsHTML.Text = scheduledTransactionsTemplate.ResolveMergeFields( mergeFields ).ResolveClientIds( upnlContent.ClientID );
        }

        /// <summary>
        /// Deletes the scheduled transaction.
        /// </summary>
        /// <param name="scheduledTransactionId">The scheduled transaction identifier.</param>
        protected void DeleteScheduledTransaction( int scheduledTransactionId )
        {
            using ( var rockContext = new Rock.Data.RockContext() )
            {
                FinancialScheduledTransactionService financialScheduledTransactionService = new FinancialScheduledTransactionService( rockContext );
                var scheduledTransaction = financialScheduledTransactionService.Get( scheduledTransactionId );
                if ( scheduledTransaction == null )
                {
                    return;
                }

                scheduledTransaction.FinancialGateway.LoadAttributes( rockContext );

                string errorMessage = string.Empty;
                if ( financialScheduledTransactionService.Cancel( scheduledTransaction, out errorMessage ) )
                {
                    try
                    {
                        financialScheduledTransactionService.GetStatus( scheduledTransaction, out errorMessage );
                    }
                    catch
                    {
                        // ignore
                    }

                    rockContext.SaveChanges();
                }
                else
                {
                    nbConfigurationNotification.Dismissable = true;
                    nbConfigurationNotification.NotificationBoxType = NotificationBoxType.Danger;
                    nbConfigurationNotification.Text = string.Format( "An error occurred while deleting your scheduled {0}", GetAttributeValue( AttributeKey.GiftTerm ).ToLower() );
                    nbConfigurationNotification.Details = errorMessage;
                    nbConfigurationNotification.Visible = true;
                }
            }

            BindScheduledTransactions();
        }

        #endregion Scheduled Gifts

        #region Transaction Entry Related

        /// <summary>
        /// Updates the Personal/Business info when giving as a business
        /// </summary>
        private void UpdatePersonalInformationFromSelectedBusiness()
        {
            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            int? selectedBusinessPersonId = cblSelectBusiness.SelectedValue.AsIntegerOrNull();
            Person personAsBusiness = null;
            if ( selectedBusinessPersonId.HasValue )
            {
                personAsBusiness = personService.Get( selectedBusinessPersonId.Value );
            }

            if ( personAsBusiness == null )
            {
                tbBusinessName.Text = null;
                acAddressBusiness.SetValues( null );
                tbEmailBusiness.Text = string.Empty;
                pnbPhoneBusiness.Text = string.Empty;
            }
            else
            {
                tbBusinessName.Text = personAsBusiness.LastName;
                tbEmailBusiness.Text = personAsBusiness.Email;

                Guid addressTypeGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK.AsGuid();
                var addressTypeId = DefinedValueCache.GetId( addressTypeGuid );

                GroupLocation businessLocation = null;
                if ( addressTypeId.HasValue )
                {
                    businessLocation = new PersonService( rockContext ).GetFirstLocation( personAsBusiness.Id, addressTypeId.Value );
                }

                if ( businessLocation != null )
                {
                    acAddressBusiness.SetValues( businessLocation.Location );
                }
                else
                {
                    acAddressBusiness.SetValues( null );
                }
            }
        }

        /// <summary>
        /// Handles the CheckedChanged event of the tglIndividualOrBusiness control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void tglIndividualOrBusiness_CheckedChanged( object sender, EventArgs e )
        {
            UpdateGivingAsIndividualOrBusinessControls();
        }

        /// <summary>
        /// Updates the giving as individual or business controls.
        /// </summary>
        private void UpdateGivingAsIndividualOrBusinessControls()
        {
            var givingAsBusiness = GivingAsBusiness();
            pnlPersonInformationAsIndividual.Visible = !givingAsBusiness;
            pnlPersonInformationAsBusiness.Visible = givingAsBusiness;
            UpdatePersonalInformationFromSelectedBusiness();
        }

        /// <summary>
        /// Handles the Click event of the btnSaveAccount control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSaveAccount_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();

            var targetPerson = GetTargetPerson( rockContext );

            if ( pnlCreateLogin.Visible )
            {
                if ( !UserLoginService.IsValidNewUserLogin( tbUserName.Text, tbPassword.Text, tbPasswordConfirm.Text, out string errorTitle, out string errorMessage ) )
                {
                    nbSaveAccountError.Title = errorTitle;
                    nbSaveAccountError.Text = errorMessage;
                    nbSaveAccountError.NotificationBoxType = NotificationBoxType.Validation;
                    nbSaveAccountError.Visible = true;
                    return;
                }

                var userLogin = UserLoginService.Create(
                    rockContext,
                    targetPerson,
                    Rock.Model.AuthenticationServiceType.Internal,
                    EntityTypeCache.Get( Rock.SystemGuid.EntityType.AUTHENTICATION_DATABASE.AsGuid() ).Id,
                    tbUserName.Text,
                    tbPassword.Text,
                    false );

                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeFields.Add( "ConfirmAccountUrl", RootPath + "ConfirmAccount" );
                mergeFields.Add( "Person", targetPerson );
                mergeFields.Add( "User", userLogin );

                var emailMessage = new RockEmailMessage( GetAttributeValue( AttributeKey.ConfirmAccountEmailTemplate ).AsGuid() );
                emailMessage.AddRecipient( new RockEmailMessageRecipient( targetPerson, mergeFields ) );
                emailMessage.AppRoot = ResolveRockUrl( "~/" );
                emailMessage.ThemeRoot = ResolveRockUrl( "~~/" );
                emailMessage.CreateCommunicationRecord = false;
                emailMessage.Send();
            }

            var financialGatewayComponent = this.FinancialGatewayComponent;
            var financialGateway = this.FinancialGateway;

            var financialPaymentDetail = new FinancialTransactionService( rockContext ).GetSelect( hfTransactionGuid.Value.AsGuid(), s => s.FinancialPaymentDetail );
            if ( financialPaymentDetail == null )
            {
                // if this was a ScheduledTransaction, get the FinancialPaymentDetail from that instead
                financialPaymentDetail = new FinancialScheduledTransactionService( rockContext ).GetSelect( hfTransactionGuid.Value.AsGuid(), s => s.FinancialPaymentDetail );
            }

            var gatewayPersonIdentifier = Rock.Security.Encryption.DecryptString( this.CustomerTokenEncrypted );

            var savedAccount = new FinancialPersonSavedAccount();
            var paymentDetail = financialPaymentDetail;

            savedAccount.PersonAliasId = targetPerson.PrimaryAliasId;
            savedAccount.ReferenceNumber = gatewayPersonIdentifier;
            savedAccount.Name = tbSaveAccount.Text;
            savedAccount.TransactionCode = TransactionCode;
            savedAccount.GatewayPersonIdentifier = gatewayPersonIdentifier;
            savedAccount.FinancialGatewayId = financialGateway.Id;
            savedAccount.FinancialPaymentDetail = new FinancialPaymentDetail();
            savedAccount.FinancialPaymentDetail.AccountNumberMasked = paymentDetail.AccountNumberMasked;
            savedAccount.FinancialPaymentDetail.CurrencyTypeValueId = paymentDetail.CurrencyTypeValueId;
            savedAccount.FinancialPaymentDetail.CreditCardTypeValueId = paymentDetail.CreditCardTypeValueId;
            savedAccount.FinancialPaymentDetail.NameOnCard = paymentDetail.NameOnCard;
            savedAccount.FinancialPaymentDetail.ExpirationMonth = paymentDetail.ExpirationMonth;
            savedAccount.FinancialPaymentDetail.ExpirationYear = paymentDetail.ExpirationYear;
            savedAccount.FinancialPaymentDetail.BillingLocationId = paymentDetail.BillingLocationId;

            var savedAccountService = new FinancialPersonSavedAccountService( rockContext );
            savedAccountService.Add( savedAccount );
            rockContext.SaveChanges();

            // If we created a new saved account, update the transaction to say it that is used this saved account.
            paymentDetail.FinancialPersonSavedAccountId = savedAccount.Id;
            rockContext.SaveChanges();

            cbSaveAccount.Visible = false;
            tbSaveAccount.Visible = false;
            pnlCreateLogin.Visible = false;
            divSaveActions.Visible = false;

            nbSaveAccountSuccess.Title = "Success";
            nbSaveAccountSuccess.Text = "The account has been saved for future use";
            nbSaveAccountSuccess.NotificationBoxType = NotificationBoxType.Success;
            nbSaveAccountSuccess.Visible = true;
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cblSelectBusiness control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cblSelectBusiness_SelectedIndexChanged( object sender, EventArgs e )
        {
            UpdatePersonalInformationFromSelectedBusiness();
        }

        /// <summary>
        /// Routes any actions that might have come from <seealso cref="AttributeKey.ScheduledTransactionsTemplate"/>
        /// </summary>
        private void RouteAction()
        {
            if ( this.Page.Request.Form["__EVENTARGUMENT"] != null )
            {
                string[] eventArgs = this.Page.Request.Form["__EVENTARGUMENT"].Split( '^' );

                if ( eventArgs.Length == 2 )
                {
                    string action = eventArgs[0];
                    string parameters = eventArgs[1];
                    int? scheduledTransactionId;

                    switch ( action )
                    {
                        case "DeleteScheduledTransaction":
                            scheduledTransactionId = parameters.AsIntegerOrNull();
                            if ( scheduledTransactionId.HasValue )
                            {
                                DeleteScheduledTransaction( scheduledTransactionId.Value );
                            }

                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Shows the details.
        /// </summary>
        private void ShowDetails()
        {
            if ( !LoadGatewayOptions() )
            {
                return;
            }

            aHistoryBackButton.Visible = false;
            if ( this.GetAttributeValue( AttributeKey.EnableInitialBackButton ).AsBoolean() )
            {
                if ( this.Request.UrlReferrer != null )
                {
                    aHistoryBackButton.HRef = this.Request.UrlReferrer.ToString();
                    aHistoryBackButton.Visible = true;
                }
                else
                {
                    aHistoryBackButton.HRef = "#";
                }
            }

            bool enableACH = this.GetAttributeValue( AttributeKey.EnableACH ).AsBoolean();
            bool enableCreditCard = this.GetAttributeValue( AttributeKey.EnableCreditCard ).AsBoolean();

            if ( enableACH == false && enableCreditCard == false )
            {
                ShowConfigurationMessage( NotificationBoxType.Warning, "Configuration", "Enable ACH and/or Enable Credit Card needs to be enabled." );
                pnlTransactionEntry.Visible = false;
                return;
            }

            if ( !SetInitialTargetPersonControls() )
            {
                return;
            }

            ConfigureCampusAccountAmountPicker();

            // if Gateways are configured, show a warning if no Accounts are configured (we don't want to show an Accounts warning if they haven't configured a gateway yet)
            if ( !caapPromptForAccountAmounts.SelectableAccountIds.Any() )
            {
                ShowConfigurationMessage( NotificationBoxType.Warning, "Configuration", "At least one Financial Account must be selected in the configuration for this block." );
                pnlTransactionEntry.Visible = false;
                return;
            }

            string introMessageTemplate = this.GetAttributeValue( AttributeKey.IntroMessageTemplate );

            Dictionary<string, object> introMessageMergeFields = null;

            IEntity transactionEntity = GetTransactionEntity();

            introMessageMergeFields = LavaHelper.GetCommonMergeFields( this.RockPage );
            if ( transactionEntity != null && LavaHelper.IsLavaTemplate( introMessageTemplate ) )
            {
                var rockContext = new RockContext();
                introMessageMergeFields.Add( "TransactionEntity", transactionEntity );
                var transactionEntityTypeId = transactionEntity.TypeId;

                // Include any Transactions that are associated with the TransactionEntity for Lava
                var transactionEntityTransactions = new FinancialTransactionService( rockContext ).Queryable()
                    .Include( a => a.TransactionDetails )
                    .Where( a => a.TransactionDetails.Any( d => d.EntityTypeId.HasValue && d.EntityTypeId == transactionEntityTypeId && d.EntityId == transactionEntity.Id ) )
                    .ToList();

                var transactionEntityTransactionsTotal = transactionEntityTransactions.SelectMany( d => d.TransactionDetails )
                    .Where( d => d.EntityTypeId.HasValue && d.EntityTypeId == transactionEntityTypeId && d.EntityId == transactionEntity.Id )
                    .Sum( d => ( decimal? ) d.Amount );

                introMessageMergeFields.Add( "TransactionEntityTransactions", transactionEntityTransactions );
                introMessageMergeFields.Add( "TransactionEntityTransactionsTotal", transactionEntityTransactionsTotal );

                // If the transactionEntityTypeId is GroupMember, it will probably be Fundraising related, so add additional merge fields that they
                // might want available if this a Fundraising GroupMember.
                var participationMode = PageParameters().ContainsKey( PageParameterKey.ParticipationMode ) ? PageParameter( PageParameterKey.ParticipationMode ).AsIntegerOrNull() ?? 1 : 1;

                if ( EntityTypeCache.Get( transactionEntityTypeId ).Guid == Rock.SystemGuid.EntityType.GROUP_MEMBER.AsGuid() )
                {
                    var groupMember = new GroupMemberService( rockContext ).Get( transactionEntity.Guid );
                    GroupService groupService = new GroupService( rockContext );
                    if ( participationMode == ( int ) ParticipationType.Family )
                    {
                        var familyMemberGroupMembersInCurrentGroup = groupService.GroupMembersInAnotherGroup( groupMember.Person.GetFamily(), groupMember.Group );
                        decimal groupFundraisingGoal = 0;
                        foreach ( var member in familyMemberGroupMembersInCurrentGroup )
                        {
                            member.LoadAttributes( rockContext );
                            member.Group.LoadAttributes( rockContext );
                            groupFundraisingGoal += member.GetAttributeValue( "IndividualFundraisingGoal" ).AsDecimalOrNull() ?? member.Group.GetAttributeValue( "IndividualFundraisingGoal" ).AsDecimalOrNull() ?? 0;
                        }

                        var contributionTotal = new FinancialTransactionDetailService( rockContext )
                        .GetContributionsForGroupMemberList( transactionEntityTypeId, familyMemberGroupMembersInCurrentGroup.Select( m => m.Id ).ToList() );
                        introMessageMergeFields.Add( "FundraisingGoal", groupFundraisingGoal );
                        introMessageMergeFields.Add( "AmountRaised", contributionTotal );
                    }
                    else
                    {
                        groupMember.LoadAttributes( rockContext );
                        groupMember.Group.LoadAttributes( rockContext );
                        var memberFundraisingGoal = groupMember.GetAttributeValue( "IndividualFundraisingGoal" ).AsDecimalOrNull() ?? groupMember.Group.GetAttributeValue( "IndividualFundraisingGoal" ).AsDecimalOrNull() ?? 0;
                        introMessageMergeFields.Add( "FundraisingGoal", memberFundraisingGoal );
                        introMessageMergeFields.Add( "AmountRaised", transactionEntityTransactionsTotal );
                    }
                }

                introMessageMergeFields.Add( "AmountLimit", this.PageParameter( PageParameterKey.AmountLimit ).AsDecimalOrNull() );
            }

            lIntroMessage.Text = introMessageTemplate.ResolveMergeFields( introMessageMergeFields );
            btnGiveNow.Text = GetAttributeValue( AttributeKey.GiveButtonNowText );

            pnlTransactionEntry.Visible = true;

            if ( this.GetAttributeValue( AttributeKey.ShowScheduledTransactions ).AsBoolean() )
            {
                ShowScheduledTransactionsPanel();
                BindScheduledTransactions();
            }
            else
            {
                HideScheduledTransactionsPanel();
            }

            tbEmailIndividual.Visible = GetAttributeValue( AttributeKey.PromptForEmail ).AsBoolean();
            tbEmailBusiness.Visible = GetAttributeValue( AttributeKey.PromptForEmail ).AsBoolean();
            pnbPhoneIndividual.Visible = GetAttributeValue( AttributeKey.PromptForPhone ).AsBoolean();
            pnbPhoneBusiness.Visible = GetAttributeValue( AttributeKey.PromptForPhone ).AsBoolean();

            UpdateGivingControlsForSelections();
        }

        /// <summary>
        /// Shows the scheduled transactions panel.
        /// </summary>
        public void ShowScheduledTransactionsPanel()
        {
            pnlScheduledTransactions.Visible = true;
            pnlTransactionEntryPanel.RemoveCssClass( "col-sm-12" ).AddCssClass( "col-sm-8" );
        }

        /// <summary>
        /// Hides the scheduled transactions panel.
        /// </summary>
        public void HideScheduledTransactionsPanel()
        {
            pnlScheduledTransactions.Visible = false;
            pnlTransactionEntryPanel.RemoveCssClass( "col-sm-8" ).AddCssClass( "col-sm-12" );
        }

        /// <summary>
        /// Parses the account URL options.
        /// </summary>
        /// <returns></returns>
        private List<ParameterAccountOption> ParseAccountUrlOptions()
        {
            List<ParameterAccountOption> result = new List<ParameterAccountOption>();
            result.AddRange( ParseAccountUrlOptionsParameter( this.PageParameter( PageParameterKey.AccountIdsOptions ), false ) );
            result.AddRange( ParseAccountUrlOptionsParameter( this.PageParameter( PageParameterKey.AccountGlCodesOptions ), true ) );
            return result;
        }

        private void ConfigureCampusAccountAmountPicker()
        {
            var allowAccountsInUrl = this.GetAttributeValue( AttributeKey.AllowAccountOptionsInURL ).AsBoolean();
            var rockContext = new RockContext();
            List<int> selectableAccountIds = FinancialAccountCache.GetByGuids( this.GetAttributeValues( AttributeKey.AccountsToDisplay ).AsGuidList() ).Select( a => a.Id ).ToList();
            CampusAccountAmountPicker.AccountIdAmount[] accountAmounts = null;

            bool enableMultiAccount = this.GetAttributeValue( AttributeKey.EnableMultiAccount ).AsBoolean();
            caapPromptForAccountAmounts.UseAccountCampusMappingLogic = true;
            caapPromptForAccountAmounts.AccountHeaderTemplate = this.GetAttributeValue( AttributeKey.AccountHeaderTemplate );
            if ( enableMultiAccount )
            {
                caapPromptForAccountAmounts.AmountEntryMode = CampusAccountAmountPicker.AccountAmountEntryMode.MultipleAccounts;
            }
            else
            {
                caapPromptForAccountAmounts.AmountEntryMode = CampusAccountAmountPicker.AccountAmountEntryMode.SingleAccount;
            }

            caapPromptForAccountAmounts.AskForCampusIfKnown = this.GetAttributeValue( AttributeKey.AskForCampusIfKnown ).AsBoolean();
            caapPromptForAccountAmounts.IncludeInactiveCampuses = this.GetAttributeValue( AttributeKey.IncludeInactiveCampuses ).AsBoolean();
            var includedCampusStatusIds = this.GetAttributeValues( AttributeKey.IncludedCampusStatuses )
                .ToList()
                .AsGuidList()
                .Select( a => DefinedValueCache.Get( a ) )
                .Where( a => a != null )
                .Select( a => a.Id ).ToArray();

            caapPromptForAccountAmounts.IncludedCampusStatusIds = includedCampusStatusIds;

            var includedCampusTypeIds = this.GetAttributeValues( AttributeKey.IncludedCampusTypes )
                .ToList()
                .AsGuidList()
                .Select( a => DefinedValueCache.Get( a ) )
                .Where( a => a != null )
                .Select( a => a.Id ).ToArray();

            caapPromptForAccountAmounts.IncludedCampusTypeIds = includedCampusTypeIds;

            if ( allowAccountsInUrl )
            {
                List<ParameterAccountOption> parameterAccountOptions = ParseAccountUrlOptions();
                if ( parameterAccountOptions.Any() )
                {
                    selectableAccountIds = parameterAccountOptions.Select( a => a.AccountId ).ToList();
                    string invalidAccountInURLMessage = this.GetAttributeValue( AttributeKey.InvalidAccountInURLMessage );
                    if ( invalidAccountInURLMessage.IsNotNullOrWhiteSpace() )
                    {
                        var validAccountUrlIdsQuery = FinancialAccountCache.GetByIds( selectableAccountIds )
                            .Where( a =>
                                 a.IsActive &&
                                 ( a.StartDate == null || a.StartDate <= RockDateTime.Today ) &&
                                 ( a.EndDate == null || a.EndDate >= RockDateTime.Today ) );

                        if ( this.GetAttributeValue( AttributeKey.OnlyPublicAccountsInURL ).AsBooleanOrNull() ?? true )
                        {
                            validAccountUrlIdsQuery = validAccountUrlIdsQuery.Where( a => a.IsPublic == true );
                        }

                        var validAccountIds = validAccountUrlIdsQuery.Select( a => a.Id ).ToList();

                        if ( selectableAccountIds.Where( a => !validAccountIds.Contains( a ) ).Any() )
                        {
                            nbConfigurationNotification.Text = invalidAccountInURLMessage;
                            nbConfigurationNotification.NotificationBoxType = NotificationBoxType.Validation;
                            nbConfigurationNotification.Visible = true;
                        }
                    }

                    var parameterAccountAmounts = parameterAccountOptions.Select( a => new CampusAccountAmountPicker.AccountIdAmount( a.AccountId, a.Amount ) { ReadOnly = !a.Enabled } );
                    accountAmounts = parameterAccountAmounts.ToArray();
                }
            }

            caapPromptForAccountAmounts.SelectableAccountIds = selectableAccountIds.ToArray();

            // Check if this is a transfer and that the person is the authorized person on the transaction
            if ( !string.IsNullOrWhiteSpace( PageParameter( PageParameterKey.Transfer ) ) && !string.IsNullOrWhiteSpace( PageParameter( PageParameterKey.ScheduledTransactionGuidToTransfer ) ) )
            {
                InitializeTransfer( PageParameter( PageParameterKey.ScheduledTransactionGuidToTransfer ).AsGuidOrNull() );

                if ( _scheduledTransactionIdToBeTransferred.HasValue && selectableAccountIds.Any() )
                {
                    accountAmounts = GetAccountAmountsFromTransferredScheduledTransaction( rockContext, selectableAccountIds );
                }
            }

            if ( accountAmounts != null )
            {
                caapPromptForAccountAmounts.AccountAmounts = accountAmounts;
            }
        }

        /// <summary>
        /// Parses the account URL options parameter.
        /// </summary>
        /// <param name="accountOptionsParameterValue">The account options parameter value.</param>
        /// <param name="parseAsAccountGLCode">if set to <c>true</c> [parse as account gl code].</param>
        /// <returns></returns>
        private List<ParameterAccountOption> ParseAccountUrlOptionsParameter( string accountOptionsParameterValue, bool parseAsAccountGLCode )
        {
            List<ParameterAccountOption> result = new List<ParameterAccountOption>();
            if ( accountOptionsParameterValue.IsNullOrWhiteSpace() )
            {
                return result;
            }

            var onlyPublicAccountsInURL = this.GetAttributeValue( AttributeKey.OnlyPublicAccountsInURL ).AsBoolean();

            var accountOptions = Server.UrlDecode( accountOptionsParameterValue ).Split( ',' );

            foreach ( var accountOption in accountOptions )
            {
                ParameterAccountOption parameterAccountOption = new ParameterAccountOption();
                var accountOptionParts = accountOption.Split( '^' ).ToList();
                if ( accountOptionParts.Count > 0 )
                {
                    while ( accountOptionParts.Count < 3 )
                    {
                        accountOptionParts.Add( null );
                    }

                    if ( parseAsAccountGLCode )
                    {
                        var accountGLCode = accountOptionParts[0];
                        if ( accountGLCode.IsNotNullOrWhiteSpace() )
                        {
                            using ( var rockContext = new RockContext() )
                            {
                                parameterAccountOption.AccountId = new FinancialAccountService( rockContext )
                                    .Queryable()
                                    .Where( a => a.GlCode == accountGLCode &&
                                    a.IsActive &&
                                    ( onlyPublicAccountsInURL ? ( a.IsPublic ?? false ) : true ) &&
                                    ( a.StartDate == null || a.StartDate <= RockDateTime.Today ) &&
                                    ( a.EndDate == null || a.EndDate >= RockDateTime.Today ) )
                                    .Select( a => a.Id ).FirstOrDefault();
                            }
                        }
                    }
                    else
                    {
                        parameterAccountOption.AccountId = accountOptionParts[0].AsInteger();
                    }

                    parameterAccountOption.Amount = accountOptionParts[1].AsDecimalOrNull();
                    parameterAccountOption.Enabled = accountOptionParts[2].AsBooleanOrNull() ?? true;
                    result.Add( parameterAccountOption );
                }
            }

            return result;
        }

        /// <summary>
        /// Sets the target person and Initializes the UI based on the initial target person.
        /// </summary>
        private bool SetInitialTargetPersonControls()
        {
            // If impersonation is allowed, and a valid person key was used, set the target to that person
            Person targetPerson = null;
            var allowImpersonation = GetAttributeValue( AttributeKey.AllowImpersonation ).AsBoolean();
            string personActionId = PageParameter( PageParameterKey.Person );
            pnlTransactionEntry.Visible = true;

            if ( personActionId.IsNotNullOrWhiteSpace() )
            {
                // If a person key was supplied then try to get that person
                var rockContext = new RockContext();
                targetPerson = new PersonService( rockContext ).GetByPersonActionIdentifier( personActionId, "transaction" );

                if ( allowImpersonation )
                {
                    // If impersonation is allowed then ensure the supplied person key was valid
                    if ( targetPerson == null )
                    {
                        nbInvalidPersonWarning.Text = "Invalid or Expired Person Token specified";
                        nbInvalidPersonWarning.NotificationBoxType = NotificationBoxType.Danger;
                        nbInvalidPersonWarning.Visible = true;
                        pnlTransactionEntry.Visible = false;
                        return false;
                    }
                }
                else
                {
                    // If impersonation is not allowed show an error if the target and current user are not the same
                    if ( targetPerson?.Id != CurrentPerson?.Id )
                    {
                        nbInvalidPersonWarning.Text = $"Impersonation is not allowed on this block.";
                        nbInvalidPersonWarning.NotificationBoxType = NotificationBoxType.Danger;
                        nbInvalidPersonWarning.Visible = true;
                        pnlTransactionEntry.Visible = false;
                        return false;
                    }
                }
            }
            else
            {
                // If a person key was not provided then use the Current Person, which may be null
                targetPerson = CurrentPerson;
            }

            if ( targetPerson != null )
            {
                ViewState[ViewStateKey.TargetPersonGuid] = Rock.Security.Encryption.EncryptString( targetPerson.Guid.ToString() );
            }
            else
            {
                ViewState[ViewStateKey.TargetPersonGuid] = string.Empty;
            }

            SetCampus( targetPerson );

            pnlLoggedInNameDisplay.Visible = targetPerson != null;
            if ( targetPerson != null )
            {
                lCurrentPersonFullName.Text = targetPerson.FullName;
                tbFirstName.Text = targetPerson.FirstName;
                tbLastName.Text = targetPerson.LastName;
                tbEmailIndividual.Text = targetPerson.Email;
                var rockContext = new RockContext();
                var addressTypeGuid = GetAttributeValue( AttributeKey.PersonAddressType ).AsGuid();
                var addressTypeId = DefinedValueCache.GetId( addressTypeGuid );

                GroupLocation personGroupLocation = null;
                if ( addressTypeId.HasValue )
                {
                    personGroupLocation = new PersonService( rockContext ).GetFirstLocation( targetPerson.Id, addressTypeId.Value );
                }

                if ( personGroupLocation != null )
                {
                    acAddressIndividual.SetValues( personGroupLocation.Location );
                }
                else
                {
                    acAddressIndividual.SetValues( null );
                }

                var promptForPhone = GetAttributeValue( AttributeKey.PromptForPhone ).AsBoolean();
                if ( promptForPhone )
                {
                    var personPhoneNumber = targetPerson.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() );

                    // If person did not have a home phone number, read the cell phone number (which would then
                    // get saved as a home number also if they don't change it, which is OK ).
                    if ( personPhoneNumber == null || string.IsNullOrWhiteSpace( personPhoneNumber.Number ) || personPhoneNumber.IsUnlisted )
                    {
                        personPhoneNumber = targetPerson.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
                    }

                    PhoneNumberBox pnbPhone = pnbPhoneIndividual;
                    if ( targetPerson.IsBusiness() )
                    {
                        pnbPhone = pnbPhoneBusiness;
                    }

                    if ( personPhoneNumber != null )
                    {
                        if ( !personPhoneNumber.IsUnlisted )
                        {
                            pnbPhone.CountryCode = personPhoneNumber.CountryCode;
                            pnbPhone.Number = personPhoneNumber.ToString();
                        }
                    }
                    else
                    {
                        pnbPhone.CountryCode = PhoneNumber.DefaultCountryCode();
                        pnbPhone.Number = string.Empty;
                    }
                }

                cblSelectBusiness.Items.Clear();

                var personService = new PersonService( rockContext );
                var businesses = personService.GetBusinesses( targetPerson.Id ).Select( a => new
                {
                    a.Id,
                    a.LastName
                } ).ToList();

                if ( businesses.Any() )
                {
                    foreach ( var business in businesses )
                    {
                        cblSelectBusiness.Items.Add( new ListItem( business.LastName, business.Id.ToString() ) );
                    }

                    cblSelectBusiness.Items.Add( new ListItem( "New Business", string.Empty ) );
                    cblSelectBusiness.Visible = true;
                    cblSelectBusiness.SelectedIndex = 0;
                }
                else
                {
                    //// person is associated with any businesses (yet),
                    //// so don't present the 'select business' prompt since they would only have the option to create a new business.
                    cblSelectBusiness.Visible = false;
                }
            }

            pnlNotLoggedInNameEntry.Visible = targetPerson == null;

            // show a prompt for Business Contact on the pnlPersonInformationAsBusiness panel if we don't have a target person so that we can create a person to be associated with the new business
            pnlBusinessContactAnonymous.Visible = targetPerson == null;

            return true;
        }

        /// <summary>
        /// Gets the target person.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private Person GetTargetPerson( RockContext rockContext )
        {
            var targetPersonValue = Rock.Security.Encryption.DecryptString( ViewState[ViewStateKey.TargetPersonGuid] as string );
            string personActionId = PageParameter( PageParameterKey.Person );
            if ( personActionId.IsNullOrWhiteSpace() && targetPersonValue.IsNullOrWhiteSpace() )
            {
                // If there is no person action identifier, just use the currently logged in Person.
                return CurrentPerson;
            }

            var targetPersonGuid = targetPersonValue?.AsGuidOrNull();
            return targetPersonGuid != null ? new PersonService( rockContext ).Get( targetPersonGuid.Value ) : null;
        }

        /// <summary>
        /// Creates the target person from the information collected (Name, Phone, Email, Address), or returns a matching person if they already exist.
        /// NOTE: Use <seealso cref="CreateBusiness"/> to creating a Business(Person) record
        /// </summary>
        /// <param name="paymentInfo">The payment information.</param>
        /// <returns></returns>
        private Person CreateTargetPerson()
        {
            string firstName = tbFirstName.Text;
            string lastName = tbLastName.Text;
            string email = tbEmailIndividual.Text;

            if ( firstName.IsNotNullOrWhiteSpace() && lastName.IsNotNullOrWhiteSpace() && email.IsNotNullOrWhiteSpace() )
            {
                var personQuery = new PersonService.PersonMatchQuery( firstName, lastName, email, pnbPhoneIndividual.Number );
                var matchingPerson = new PersonService( new RockContext() ).FindPerson( personQuery, true );
                if ( matchingPerson != null )
                {
                    return matchingPerson;
                }
            }

            return _createPersonOrBusiness( false, firstName, lastName, email );
        }

        /// <summary>
        /// Creates the business contact person.
        /// </summary>
        /// <returns></returns>
        private Person CreateBusinessContactPerson()
        {
            string firstName = tbBusinessContactFirstName.Text;
            string lastName = tbBusinessContactLastName.Text;
            string email = tbBusinessContactEmail.Text;

            if ( firstName.IsNotNullOrWhiteSpace() && lastName.IsNotNullOrWhiteSpace() && email.IsNotNullOrWhiteSpace() )
            {
                var personQuery = new PersonService.PersonMatchQuery( firstName, lastName, email, pnbPhoneIndividual.Number );
                var matchingPerson = new PersonService( new RockContext() ).FindPerson( personQuery, true );
                if ( matchingPerson != null )
                {
                    return matchingPerson;
                }
            }

            return _createPersonOrBusiness( false, firstName, lastName, email );
        }

        /// <summary>
        /// Creates a business (or returns an existing business if the person already has a business with the same business name)
        /// </summary>
        /// <returns></returns>
        private Person CreateBusiness( Person contactPerson )
        {
            var businessName = tbBusinessName.Text;

            // Try to find existing business for person that has the same name
            var personBusinesses = contactPerson.GetBusinesses()
                .Where( b => b.LastName == businessName )
                .ToList();

            if ( personBusinesses.Count() == 1 )
            {
                return personBusinesses.First();
            }

            string email = tbEmailBusiness.Text;

            var business = _createPersonOrBusiness( true, null, businessName, email );

            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            personService.AddContactToBusiness( business.Id, contactPerson.Id );
            rockContext.SaveChanges();

            return business;
        }

        /// <summary>
        /// Creates the person or business.
        /// </summary>
        /// <param name="createBusiness">if set to <c>true</c> [create business].</param>
        /// <returns></returns>
        private Person _createPersonOrBusiness( bool createBusiness, string firstName, string lastName, string email )
        {
            var rockContext = new RockContext();
            DefinedValueCache dvcConnectionStatus = DefinedValueCache.Get( GetAttributeValue( AttributeKey.PersonConnectionStatus ).AsGuid() );
            DefinedValueCache dvcRecordStatus = DefinedValueCache.Get( GetAttributeValue( AttributeKey.PersonRecordStatus ).AsGuid() );

            // Create Person
            var newPersonOrBusiness = new Person();
            newPersonOrBusiness.FirstName = firstName;
            newPersonOrBusiness.LastName = lastName;

            newPersonOrBusiness.Email = email;
            newPersonOrBusiness.IsEmailActive = true;
            newPersonOrBusiness.EmailPreference = EmailPreference.EmailAllowed;
            if ( createBusiness )
            {
                newPersonOrBusiness.RecordTypeValueId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_BUSINESS.AsGuid() );
            }
            else
            {
                newPersonOrBusiness.RecordTypeValueId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() );
            }

            if ( dvcConnectionStatus != null )
            {
                newPersonOrBusiness.ConnectionStatusValueId = dvcConnectionStatus.Id;
            }

            if ( dvcRecordStatus != null )
            {
                newPersonOrBusiness.RecordStatusValueId = dvcRecordStatus.Id;
            }

            newPersonOrBusiness.RecordSourceValueId = GetRecordSourceValueId();

            int? campusId = this.SelectedCampusId;

            // Create Person and Family, and set their primary campus to the one they gave money to
            Group familyGroup = PersonService.SaveNewPerson( newPersonOrBusiness, rockContext, campusId, false );

            // SaveNewPerson should have already done this, but just in case
            rockContext.SaveChanges();

            return newPersonOrBusiness;
        }

        /// <summary>
        /// Gets the record source to use for new individuals.
        /// </summary>
        /// <returns>
        /// The identifier of the Record Source Type <see cref="DefinedValue"/> to use.
        /// </returns>
        private int? GetRecordSourceValueId()
        {
            return RecordSourceHelper.GetSessionRecordSourceValueId()
                ?? DefinedValueCache.Get( GetAttributeValue( AttributeKey.PersonRecordSource ).AsGuid() )?.Id;
        }

        /// <summary>
        /// Updates the business from the information collected (Phone, Email, Address) and saves changes (if any) to the database.
        /// </summary>
        /// <param name="business">The business.</param>
        private void UpdateBusinessFromInputInformation( Person business )
        {
            _updatePersonOrBusinessFromInputInformation( business, PersonInputSource.Business );
        }

        /// <summary>
        /// Updates the person from input information collected (Phone, Email, Address) and saves changes (if any) to the database..
        /// </summary>
        /// <param name="person">The person.</param>
        private void UpdatePersonFromInputInformation( Person person, PersonInputSource personInputSource )
        {
            _updatePersonOrBusinessFromInputInformation( person, personInputSource );
        }

        private enum PersonInputSource
        {
            Person,
            Business,
            BusinessContact
        }

        /// <summary>
        /// Updates the person/business from the information collected (Phone, Email, Address) and saves changes (if any) to the database.
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="paymentInfo">The payment information.</param>
        private void _updatePersonOrBusinessFromInputInformation( Person personOrBusiness, PersonInputSource personInputSource )
        {
            var promptForEmail = this.GetAttributeValue( AttributeKey.PromptForEmail ).AsBoolean();
            var promptForPhone = this.GetAttributeValue( AttributeKey.PromptForPhone ).AsBoolean();
            PhoneNumberBox pnbPhone;
            EmailBox tbEmail;
            int numberTypeId;
            Guid locationTypeGuid;
            AddressControl acAddress;

            switch ( personInputSource )
            {
                case PersonInputSource.Business:
                    {
                        tbEmail = tbEmailBusiness;
                        pnbPhone = pnbPhoneBusiness;
                        numberTypeId = DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK ) ).Id;
                        locationTypeGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK.AsGuid();
                        acAddress = acAddressBusiness;
                        break;
                    }

                case PersonInputSource.BusinessContact:
                    {
                        tbEmail = tbBusinessContactEmail;
                        pnbPhone = pnbBusinessContactPhone;
                        numberTypeId = DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME ) ).Id;
                        locationTypeGuid = GetAttributeValue( AttributeKey.PersonAddressType ).AsGuid();
                        acAddress = null;
                        break;
                    }

                case PersonInputSource.Person:
                    {
                        // PersonInput
                        tbEmail = tbEmailIndividual;
                        pnbPhone = pnbPhoneIndividual;
                        numberTypeId = DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME ) ).Id;
                        locationTypeGuid = GetAttributeValue( AttributeKey.PersonAddressType ).AsGuid();
                        acAddress = acAddressIndividual;
                        break;
                    }

                default:
                    {
                        throw new Exception( "Unexpected PersonInputSource" );
                    }
            }

            if ( promptForPhone )
            {
                if ( pnbPhone.Number.IsNotNullOrWhiteSpace() )
                {
                    var phone = personOrBusiness.PhoneNumbers.FirstOrDefault( p => p.NumberTypeValueId == numberTypeId );
                    if ( phone == null )
                    {
                        phone = new PhoneNumber();
                        personOrBusiness.PhoneNumbers.Add( phone );
                        phone.NumberTypeValueId = numberTypeId;
                    }

                    phone.CountryCode = PhoneNumber.CleanNumber( pnbPhone.CountryCode );
                    phone.Number = PhoneNumber.CleanNumber( pnbPhone.Number );
                }
            }

            var primaryFamily = personOrBusiness.GetFamily();

            if ( primaryFamily != null )
            {
                var rockContext = new RockContext();

                // fetch primaryFamily using rockContext so that any changes will get saved
                primaryFamily = new GroupService( rockContext ).Get( primaryFamily.Id );

                if ( acAddress != null )
                {
                    GroupService.AddNewGroupAddress(
                        rockContext,
                        primaryFamily,
                        locationTypeGuid.ToString(),
                        acAddress.Street1,
                        acAddress.Street2,
                        acAddress.City,
                        acAddress.State,
                        acAddress.PostalCode,
                        acAddress.Country,
                        true );
                }
            }
        }

        /// <summary>
        /// Binds the person saved accounts that are available for the <paramref name="selectedScheduleFrequencyId" />
        /// </summary>
        private void BindPersonSavedAccounts()
        {
            // Get current selection before updating the drop down list items.
            var currentSavedAccountSelection = ddlPersonSavedAccount.SelectedValue;
            ddlPersonSavedAccount.Visible = false;
            ddlPersonSavedAccount.Items.Clear();
            pnlSavedAccounts.Visible = false;

            var rockContext = new RockContext();
            var targetPerson = GetTargetPerson( rockContext );

            // No person, no accounts
            if ( targetPerson == null )
            {
                return;
            }

            var personSavedAccountsQuery = new FinancialPersonSavedAccountService( rockContext )
                .GetByPersonId( targetPerson.Id )
                .Where( a => !a.IsSystem )
                .AsNoTracking();

            var financialGateway = this.FinancialGateway;
            var financialGatewayComponent = this.FinancialGatewayComponent;
            if ( financialGateway == null || financialGatewayComponent == null )
            {
                return;
            }

            bool enableACH = this.GetAttributeValue( AttributeKey.EnableACH ).AsBoolean();
            bool enableCreditCard = this.GetAttributeValue( AttributeKey.EnableCreditCard ).AsBoolean();
            var creditCardCurrency = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD.AsGuid() );
            var achCurrency = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_ACH.AsGuid() );
            List<DefinedValueCache> allowedCurrencyTypes = new List<DefinedValueCache>();

            if ( enableCreditCard && financialGatewayComponent.SupportsSavedAccount( creditCardCurrency ) )
            {
                allowedCurrencyTypes.Add( creditCardCurrency );
            }

            if ( enableACH && financialGatewayComponent.SupportsSavedAccount( achCurrency ) )
            {
                allowedCurrencyTypes.Add( achCurrency );
            }

            int[] allowedCurrencyTypeIds = allowedCurrencyTypes.Select( a => a.Id ).ToArray();

            personSavedAccountsQuery = personSavedAccountsQuery.Where( a =>
                a.FinancialGatewayId == financialGateway.Id
                && ( a.FinancialPaymentDetail.CurrencyTypeValueId != null )
                && allowedCurrencyTypeIds.Contains( a.FinancialPaymentDetail.CurrencyTypeValueId.Value ) );

            var personSavedAccountList = personSavedAccountsQuery.OrderBy( a => a.Name ).AsNoTracking().Select( a => new
            {
                a.Id,
                a.Name,
                a.FinancialPaymentDetail
            } ).ToList();

            // Only show the SavedAccount picker if there are saved accounts. If there aren't any (or if they choose 'Use a different payment method'), a later step will prompt them to enter Payment Info (CC/ACH fields)
            ddlPersonSavedAccount.Visible = personSavedAccountList.Any();
            pnlSavedAccounts.Visible = personSavedAccountList.Any();

            ddlPersonSavedAccount.Items.Clear();
            foreach ( var personSavedAccount in personSavedAccountList )
            {
                string displayName;
                if ( personSavedAccount.FinancialPaymentDetail.ExpirationDate.IsNotNullOrWhiteSpace() )
                {
                    displayName = $"{personSavedAccount.Name} ({personSavedAccount.FinancialPaymentDetail.AccountNumberMasked} Expires: {personSavedAccount.FinancialPaymentDetail.ExpirationDate})";
                }
                else
                {
                    displayName = $"{personSavedAccount.Name} ({personSavedAccount.FinancialPaymentDetail.AccountNumberMasked})";
                }

                ddlPersonSavedAccount.Items.Add( new ListItem( displayName, personSavedAccount.Id.ToString() ) );
            }

            ddlPersonSavedAccount.Items.Add( new ListItem( "Use a different payment method", 0.ToString() ) );

            if ( currentSavedAccountSelection.IsNotNullOrWhiteSpace() )
            {
                ddlPersonSavedAccount.SetValue( currentSavedAccountSelection );
            }
            else
            {
                ddlPersonSavedAccount.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Sets the selected campus, and account picker campus from person
        /// or from a CampusId url parameter
        /// </summary>
        private void SetCampus( Person person )
        {
            var pageParameterCampusId = this.PageParameter( PageParameterKey.CampusId ).AsIntegerOrNull();

            if ( pageParameterCampusId.HasValue )
            {
                this.SelectedCampusId = pageParameterCampusId.Value;
            }
            else
            {
                if ( person != null )
                {
                    var personCampus = person.GetCampus();
                    if ( personCampus != null )
                    {
                        this.SelectedCampusId = personCampus.Id;
                    }
                }
            }

            caapPromptForAccountAmounts.CampusId = this.SelectedCampusId;
        }

        /// <summary>
        /// Determines if a Person's Saved Account was used as the payment method
        /// </summary>
        /// <returns></returns>
        private bool UsingPersonSavedAccount()
        {
            return ddlPersonSavedAccount.SelectedValue.AsInteger() > 0;
        }

        /// <summary>
        /// Navigates to step.
        /// </summary>
        /// <param name="entryStep">The entry step.</param>
        private void NavigateToStep( EntryStep entryStep )
        {
            pnlPromptForAmounts.Visible = entryStep == EntryStep.PromptForAmounts;

            pnlAmountSummary.Visible = entryStep == EntryStep.GetPersonalInformation
                || entryStep == EntryStep.GetPaymentInfo;

            pnlPersonalInformation.Visible = entryStep == EntryStep.GetPersonalInformation;
            pnlPaymentInfo.Visible = entryStep == EntryStep.GetPaymentInfo;
            pnlTransactionSummary.Visible = entryStep == EntryStep.ShowTransactionSummary;
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlPersonSavedAccount control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlPersonSavedAccount_SelectedIndexChanged( object sender, EventArgs e )
        {
            UpdateGivingControlsForSelections();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the ddlFrequency control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
        protected void ddlFrequency_SelectedIndexChanged( object sender, EventArgs e )
        {
            UpdateGivingControlsForSelections();
        }

        /// <summary>
        /// Updates the giving controls based on what options are selected in the UI
        /// </summary>
        private void UpdateGivingControlsForSelections()
        {
            nbPromptForAmountsWarning.Visible = false;
            BindPersonSavedAccounts();

            bool allowScheduledTransactions = this.GetAttributeValue( AttributeKey.AllowScheduledTransactions ).AsBoolean();
            int oneTimeFrequencyId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME.AsGuid() ) ?? 0;
            int selectedScheduleFrequencyId;

            if ( allowScheduledTransactions )
            {
                selectedScheduleFrequencyId = ddlFrequency.SelectedValue.AsInteger();
            }
            else
            {
                selectedScheduleFrequencyId = oneTimeFrequencyId;
            }

            int firstAndFifteenthFrequencyId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_FIRST_AND_FIFTEENTH.AsGuid() ) ?? 0;
            bool isOneTime = selectedScheduleFrequencyId == oneTimeFrequencyId;
            var giftTerm = this.GetAttributeValue( AttributeKey.GiftTerm );

            if ( isOneTime )
            {
                if ( FinancialGatewayComponent.SupportedPaymentSchedules.Any( a => a.Guid == Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME.AsGuid() ) == false )
                {
                    // Gateway doesn't support OneTime as a Scheduled, so it must be posted today
                    dtpStartDate.SelectedDate = RockDateTime.Now.Date;
                    dtpStartDate.Visible = false;
                }
                else
                {
                    dtpStartDate.Visible = true;
                }

                dtpStartDate.Label = string.Format( "Process {0} On", giftTerm );
                btnGiveNow.Text = this.GetAttributeValue( AttributeKey.GiveButtonNowText );
                pnlScheduledTransactionEndDate.Visible = false;
                dtpEndDate.SelectedDate = null;
            }
            else
            {
                btnGiveNow.Text = this.GetAttributeValue( AttributeKey.GiveButtonScheduledText );
                dtpStartDate.Visible = true;

                if ( _scheduledTransactionIdToBeTransferred.HasValue )
                {
                    dtpStartDate.Label = "Next Gift";
                }
                else
                {
                    dtpStartDate.Label = "Start Giving On";
                }

                if ( this.GetAttributeValue( AttributeKey.EnableEndDate ).AsBoolean() )
                {
                    pnlScheduledTransactionEndDate.Visible = true;
                    dtpEndDate.Label = "End Giving On";
                }
            }

            if ( selectedScheduleFrequencyId == firstAndFifteenthFrequencyId )
            {
                var selectedDate = dtpStartDate.SelectedDate ?? RockDateTime.Now;

                // Get the day of month (Day of Month for 11/9/2020 would be 9)
                var nextBillDayOfMonth = selectedDate.Day;
                if ( nextBillDayOfMonth > 1 && nextBillDayOfMonth < 15 )
                {
                    // they specified a start between the 1st and 15th, so round up the next 15th
                    var nextFifteenth = new DateTime( selectedDate.Year, selectedDate.Month, 15 );
                    dtpStartDate.SelectedDate = nextFifteenth;
                }
                else if ( nextBillDayOfMonth > 15 )
                {
                    // they specified a start after 15th, so round up the next month's 1st
                    var nextFirst = new DateTime( selectedDate.Year, selectedDate.Month, 1 ).AddMonths( 1 );
                    dtpStartDate.SelectedDate = nextFirst;
                }
            }

            var earliestScheduledStartDate = FinancialGatewayComponent.GetEarliestScheduledStartDate( FinancialGateway );

            // if scheduling recurring, it can't start today since the gateway will be taking care of automated giving, it might have already processed today's transaction. So make sure it is no earlier than the gateway's earliest start date.
            if ( !isOneTime && ( !dtpStartDate.SelectedDate.HasValue || dtpStartDate.SelectedDate.Value.Date < earliestScheduledStartDate ) )
            {
                dtpStartDate.SelectedDate = earliestScheduledStartDate;
            }

            ConfigureCoverTheFees();
        }

        /// <summary>
        /// Processes the transaction.
        /// </summary>
        /// <returns></returns>
        protected void ProcessTransaction()
        {
            if ( !tbFirstName.IsValid )
            {
                nbProcessTransactionError.Text = tbFirstName.CustomValidator.ErrorMessage;
                nbProcessTransactionError.Visible = true;
                return;
            }

            var transactionGuid = hfTransactionGuid.Value.AsGuid();
            var rockContext = new RockContext();

            // to make duplicate transactions impossible, make sure that our Transaction hasn't already been processed as a regular or scheduled transaction
            bool transactionAlreadyExists = new FinancialTransactionService( rockContext ).Queryable().Any( a => a.Guid == transactionGuid );
            if ( !transactionAlreadyExists )
            {
                transactionAlreadyExists = new FinancialScheduledTransactionService( rockContext ).Queryable().Any( a => a.Guid == transactionGuid );
            }

            if ( transactionAlreadyExists )
            {
                ShowTransactionSummary();
                return;
            }

            bool givingAsBusiness = this.GivingAsBusiness();
            var financialGatewayComponent = this.FinancialGatewayComponent;
            string errorMessage;
            var paymentInfo = CreatePaymentInfoFromControls( givingAsBusiness );
            nbProcessTransactionError.Visible = false;

            // use the paymentToken as the reference number for creating the customer account
            var savedAccountId = ddlPersonSavedAccount.SelectedValue.AsIntegerOrNull();
            if ( savedAccountId.HasValue && savedAccountId.Value > 0 )
            {
                FinancialPersonSavedAccount financialPersonSavedAccount = new FinancialPersonSavedAccountService( rockContext ).Get( savedAccountId.Value );

                if ( financialPersonSavedAccount != null )
                {
                    if ( financialPersonSavedAccount.ReferenceNumber.IsNotNullOrWhiteSpace() )
                    {
                        paymentInfo.ReferenceNumber = financialPersonSavedAccount.ReferenceNumber;
                    }

                    if ( financialPersonSavedAccount.GatewayPersonIdentifier.IsNotNullOrWhiteSpace() )
                    {
                        paymentInfo.GatewayPersonIdentifier = financialPersonSavedAccount.GatewayPersonIdentifier;
                    }
                    else
                    {
                        // If this is from a SavedAccount, and GatewayPersonIdentifier is unknown, this is probably from an older NMI gateway transaction that only saved the GatewayPersonIdentifier to ReferenceNumber.
                        paymentInfo.GatewayPersonIdentifier = financialPersonSavedAccount.ReferenceNumber;
                    }
                }
            }

            if ( paymentInfo.GatewayPersonIdentifier.IsNullOrWhiteSpace() )
            {
                financialGatewayComponent.UpdatePaymentInfoFromPaymentControl( this.FinancialGateway, _hostedPaymentInfoControl, paymentInfo, out errorMessage );
                var customerToken = financialGatewayComponent.CreateCustomerAccount( this.FinancialGateway, paymentInfo, out errorMessage );
                if ( errorMessage.IsNotNullOrWhiteSpace() || customerToken.IsNullOrWhiteSpace() )
                {
                    nbProcessTransactionError.Text = errorMessage ?? "Unknown Error";
                    nbProcessTransactionError.Visible = true;
                    return;
                }

                paymentInfo.GatewayPersonIdentifier = customerToken;

                // save the customer token in view state since we'll need it in case they create a saved account
                this.CustomerTokenEncrypted = Rock.Security.Encryption.EncryptString( customerToken );
            }

            // determine or create the Person record that this transaction is for
            var targetPerson = this.GetTargetPerson( rockContext );
            int transactionPersonId;

            if ( targetPerson == null )
            {
                if ( givingAsBusiness )
                {
                    targetPerson = this.CreateBusinessContactPerson();
                }
                else
                {
                    targetPerson = this.CreateTargetPerson();
                }

                ViewState[ViewStateKey.TargetPersonGuid] = Rock.Security.Encryption.EncryptString( targetPerson.Guid.ToString() );
            }

            UpdatePersonFromInputInformation( targetPerson, givingAsBusiness ? PersonInputSource.BusinessContact : PersonInputSource.Person );

            if ( givingAsBusiness )
            {
                int? businessId = cblSelectBusiness.SelectedValue.AsInteger();
                var business = new PersonService( rockContext ).Get( businessId.Value );
                if ( business == null )
                {
                    business = CreateBusiness( targetPerson );
                }

                UpdateBusinessFromInputInformation( business );
                transactionPersonId = business.Id;
            }
            else
            {
                transactionPersonId = targetPerson.Id;
            }

            // just in case anything about the person/business was updated (email or phone), save changes
            rockContext.SaveChanges();

            nbProcessTransactionError.Visible = false;

            if ( IsScheduledTransaction() )
            {
                string gatewayScheduleId = null;
                try
                {
                    PaymentSchedule paymentSchedule = new PaymentSchedule
                    {
                        TransactionFrequencyValue = DefinedValueCache.Get( ddlFrequency.SelectedValue.AsInteger() ),
                        StartDate = dtpStartDate.SelectedDate.Value,
                        EndDate = dtpEndDate.SelectedDate.HasValue ? dtpEndDate.SelectedDate.Value : ( DateTime? ) null,
                        PersonId = transactionPersonId
                    };

                    var financialScheduledTransaction = this.FinancialGatewayComponent.AddScheduledPayment( this.FinancialGateway, paymentSchedule, paymentInfo, out errorMessage );
                    if ( financialScheduledTransaction == null )
                    {
                        if ( errorMessage.IsNullOrWhiteSpace() )
                        {
                            errorMessage = "Unknown Error";
                        }

                        nbProcessTransactionError.Text = errorMessage;
                        nbProcessTransactionError.Visible = true;
                        return;
                    }

                    gatewayScheduleId = financialScheduledTransaction.GatewayScheduleId;

                    SaveScheduledTransaction( transactionPersonId, paymentInfo, paymentSchedule, financialScheduledTransaction );
                }
                catch ( Exception ex )
                {
                    if ( gatewayScheduleId.IsNotNullOrWhiteSpace() )
                    {
                        // if we didn't get the gatewayScheduleId from AddScheduledPayment, see if the gateway paymentInfo.TransactionCode before the exception occurred
                        gatewayScheduleId = paymentInfo.TransactionCode;
                    }

                    // if an exception occurred, it is possible that an orphaned subscription might be on the Gateway server. Some gateway components will clean-up when there is exception, but log it just in case it needs to be resolved by a human
                    throw new Exception( string.Format( "Error occurred when saving financial scheduled transaction for gateway scheduled payment with a gatewayScheduleId of {0} and FinancialScheduledTransaction with Guid of {1}.", gatewayScheduleId, transactionGuid ), ex );
                }
            }
            else
            {
                string transactionCode = null;
                try
                {
                    FinancialTransaction financialTransaction = this.FinancialGatewayComponent.Charge( this.FinancialGateway, paymentInfo, out errorMessage );
                    if ( financialTransaction == null )
                    {
                        if ( errorMessage.IsNullOrWhiteSpace() )
                        {
                            errorMessage = "Unknown Error";
                        }

                        nbProcessTransactionError.Text = errorMessage;
                        nbProcessTransactionError.Visible = true;
                        return;
                    }

                    transactionCode = financialTransaction.TransactionCode;

                    SaveTransaction( transactionPersonId, paymentInfo, financialTransaction );
                }
                catch ( Exception ex )
                {
                    throw new Exception( string.Format( "Error occurred when saving financial transaction for gateway payment with a transactionCode of {0} and FinancialTransaction with Guid of {1}.", transactionCode, transactionGuid ), ex );
                }
            }

            ShowTransactionSummary();
        }

        /// <summary>
        /// Giving as business.
        /// </summary>
        /// <returns></returns>
        private bool GivingAsBusiness()
        {
            return tglIndividualOrBusiness.Checked;
        }

        /// <summary>
        /// Determines whether a scheduled giving frequency was selected
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is scheduled transaction]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsScheduledTransaction()
        {
            bool allowScheduledTransactions = this.GetAttributeValue( AttributeKey.AllowScheduledTransactions ).AsBoolean();
            if ( !allowScheduledTransactions )
            {
                return false;
            }

            int oneTimeFrequencyId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME.AsGuid() ) ?? 0;
            if ( ddlFrequency.SelectedValue.AsInteger() != oneTimeFrequencyId )
            {
                return true;
            }
            else
            {
                return dtpStartDate.SelectedDate > RockDateTime.Now.Date;
            }
        }

        /// <summary>
        /// Creates a PaymentInfo object from the information collected in the UI
        /// </summary>
        /// <param name="givingAsBusiness">if set to <c>true</c> [giving as business].</param>
        /// <returns></returns>
        private ReferencePaymentInfo CreatePaymentInfoFromControls( bool givingAsBusiness )
        {
            var acAddress = givingAsBusiness ? acAddressBusiness : acAddressIndividual;
            var tbEmail = givingAsBusiness ? tbEmailBusiness : tbEmailIndividual;
            var pnbPhone = givingAsBusiness ? pnbPhoneBusiness : pnbPhoneIndividual;

            var paymentInfo = new ReferencePaymentInfo
            {
                Email = tbEmail.Text,
                Phone = PhoneNumber.FormattedNumber( pnbPhone.CountryCode, pnbPhone.Number, true )
            };

            paymentInfo.UpdateAddressFieldsFromAddressControl( acAddress );

            if ( givingAsBusiness )
            {
                if ( pnlBusinessContactAnonymous.Visible )
                {
                    paymentInfo.FirstName = tbBusinessContactFirstName.Text;
                    paymentInfo.LastName = tbBusinessContactLastName.Text;
                }
                else
                {
                    paymentInfo.FirstName = tbFirstName.Text;
                    paymentInfo.LastName = tbLastName.Text;
                }

                paymentInfo.BusinessName = tbBusinessName.Text;
            }
            else
            {
                paymentInfo.FirstName = tbFirstName.Text;
                paymentInfo.LastName = tbLastName.Text;
            }

            paymentInfo.IPAddress = GetClientIpAddress();

            paymentInfo.FinancialPersonSavedAccountId = ddlPersonSavedAccount.SelectedValueAsId();

            var commentTransactionAccountDetails = new List<FinancialTransactionDetail>();
            PopulateTransactionDetails( commentTransactionAccountDetails );

            SetPaymentComment( paymentInfo, commentTransactionAccountDetails, tbCommentEntry.Text );

            paymentInfo.Amount = commentTransactionAccountDetails.Sum( a => a.Amount );
            var totalFeeCoverageAmounts = commentTransactionAccountDetails.Where( a => a.FeeCoverageAmount.HasValue ).Select( a => a.FeeCoverageAmount.Value );
            if ( totalFeeCoverageAmounts.Any() )
            {
                paymentInfo.FeeCoverageAmount = totalFeeCoverageAmounts.Sum();
            }

            var transactionType = DefinedValueCache.Get( this.GetAttributeValue( AttributeKey.TransactionType ).AsGuidOrNull() ?? Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION.AsGuid() );
            paymentInfo.TransactionTypeValueId = transactionType.Id;

            return paymentInfo;
        }

        /// <summary>
        /// Shows the transaction summary.
        /// </summary>
        protected void ShowTransactionSummary()
        {
            var rockContext = new RockContext();
            var transactionGuid = hfTransactionGuid.Value.AsGuid();

            var mergeFields = LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson, new CommonMergeFieldsOptions() );
            var finishLavaTemplate = this.GetAttributeValue( AttributeKey.FinishLavaTemplate );
            IEntity transactionEntity = GetTransactionEntity();
            mergeFields.Add( "TransactionEntity", transactionEntity );

            // the transactionGuid is either for a FinancialTransaction or a FinancialScheduledTransaction
            int? financialPaymentDetailId;
            FinancialPaymentDetail financialPaymentDetail;
            FinancialTransaction financialTransaction = new FinancialTransactionService( rockContext ).Get( transactionGuid );
            if ( financialTransaction != null )
            {
                mergeFields.Add( "Transaction", financialTransaction );
                mergeFields.Add( "Person", financialTransaction.AuthorizedPersonAlias.Person );
                financialPaymentDetail = financialTransaction.FinancialPaymentDetail;
                financialPaymentDetailId = financialTransaction.FinancialGatewayId;
            }
            else
            {
                FinancialScheduledTransaction financialScheduledTransaction = new FinancialScheduledTransactionService( rockContext ).Get( transactionGuid );
                mergeFields.Add( "Transaction", financialScheduledTransaction );
                mergeFields.Add( "Person", financialScheduledTransaction.AuthorizedPersonAlias.Person );
                financialPaymentDetail = financialScheduledTransaction.FinancialPaymentDetail;
                financialPaymentDetailId = financialScheduledTransaction.FinancialGatewayId;
            }

            if ( financialPaymentDetail != null || financialPaymentDetailId.HasValue )
            {
                financialPaymentDetail = financialPaymentDetail ?? new FinancialPaymentDetailService( rockContext ).GetNoTracking( financialPaymentDetailId.Value );
                mergeFields.Add( "PaymentDetail", financialPaymentDetail );

                if ( financialPaymentDetail.BillingLocation != null || financialPaymentDetail.BillingLocationId.HasValue )
                {
                    var billingLocation = financialPaymentDetail.BillingLocation ?? new LocationService( rockContext ).GetNoTracking( financialPaymentDetail.BillingLocationId.Value );
                    mergeFields.Add( "BillingLocation", billingLocation );
                }
            }

            lTransactionSummaryHTML.Text = finishLavaTemplate.ResolveMergeFields( mergeFields );

            if ( !UsingPersonSavedAccount() )
            {
                lSaveAccountTitle.Text = GetAttributeValue( AttributeKey.SaveAccountTitle );
                pnlSaveAccountPrompt.Visible = true;

                // Show save account info based on if checkbox is checked
                pnlSaveAccountEntry.Style[HtmlTextWriterStyle.Display] = cbSaveAccount.Checked ? "block" : "none";
            }

            // If target person does not have a login, have them create a UserName and password
            var targetPerson = GetTargetPerson( rockContext );
            var hasUserLogin = targetPerson != null ? new UserLoginService( rockContext ).GetByPersonId( targetPerson.Id ).Any() : false;
            pnlCreateLogin.Visible = !hasUserLogin;

            NavigateToStep( EntryStep.ShowTransactionSummary );
        }

        /// <summary>
        /// Saves the transaction.
        /// </summary>
        /// <param name="personId">The person identifier.</param>
        /// <param name="paymentInfo">The payment information.</param>
        /// <param name="transaction">The transaction.</param>
        private void SaveTransaction( int personId, PaymentInfo paymentInfo, FinancialTransaction transaction )
        {
            FinancialGateway financialGateway = this.FinancialGateway;
            IHostedGatewayComponent gateway = this.FinancialGatewayComponent;
            var rockContext = new RockContext();

            // manually assign the Guid that we generated at the beginning of the transaction UI entry to help make duplicate transactions impossible
            transaction.Guid = hfTransactionGuid.Value.AsGuid();

            transaction.AuthorizedPersonAliasId = new PersonAliasService( rockContext ).GetPrimaryAliasId( personId );
            if ( this.GivingAsBusiness() )
            {
                transaction.ShowAsAnonymous = cbGiveAnonymouslyBusiness.Checked;
            }
            else
            {
                transaction.ShowAsAnonymous = cbGiveAnonymouslyIndividual.Checked;
            }

            transaction.TransactionDateTime = RockDateTime.Now;
            transaction.FinancialGatewayId = financialGateway.Id;

            var transactionType = DefinedValueCache.Get( this.GetAttributeValue( AttributeKey.TransactionType ).AsGuidOrNull() ?? Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION.AsGuid() );
            transaction.TransactionTypeValueId = transactionType.Id;

            transaction.Summary = paymentInfo.Comment1;

            if ( transaction.FinancialPaymentDetail == null )
            {
                transaction.FinancialPaymentDetail = new FinancialPaymentDetail();
            }

            /* 02/17/2022 MDP

            Note that after the transaction, the HostedGateway knows more about the FinancialPaymentDetail than Rock does
            since it is the gateway that collects the payment info. But just in case paymentInfo has information the the gateway hasn't set,
            we'll fill in any missing details.

            But then we'll want to use FinancialPaymentDetail as the most accurate values for the payment info. 
            */

            transaction.FinancialPaymentDetail.SetFromPaymentInfo( paymentInfo, gateway as GatewayComponent, rockContext );

            Guid? sourceGuid = GetAttributeValue( AttributeKey.FinancialSourceType ).AsGuidOrNull();
            if ( sourceGuid.HasValue )
            {
                transaction.SourceTypeValueId = DefinedValueCache.GetId( sourceGuid.Value );
            }

            PopulateTransactionDetails( transaction.TransactionDetails );

            var batchService = new FinancialBatchService( rockContext );

            var currencyTypeValue = transaction.FinancialPaymentDetail?.CurrencyTypeValueId != null
                ? DefinedValueCache.Get( transaction.FinancialPaymentDetail.CurrencyTypeValueId.Value )
                : null;

            var creditCardTypeValue = transaction.FinancialPaymentDetail?.CreditCardTypeValueId != null
                ? DefinedValueCache.Get( transaction.FinancialPaymentDetail.CreditCardTypeValueId.Value )
                : null;

            // Get the batch
            var batch = batchService.GetForNewTransaction( transaction, GetAttributeValue( AttributeKey.BatchNamePrefix ) );

            var batchChanges = new History.HistoryChangeList();
            FinancialBatchService.EvaluateNewBatchHistory( batch, batchChanges );

            transaction.LoadAttributes( rockContext );

            var allowedTransactionAttributes = GetAttributeValue( AttributeKey.AllowedTransactionAttributesFromURL ).Split( ',' ).AsGuidList().Select( x => AttributeCache.Get( x ).Key );

            foreach ( KeyValuePair<string, AttributeValueCache> attr in transaction.AttributeValues )
            {
                if ( PageParameters().ContainsKey( PageParameterKey.AttributeKeyPrefix + attr.Key ) && allowedTransactionAttributes.Contains( attr.Key ) )
                {
                    attr.Value.Value = Server.UrlDecode( PageParameter( PageParameterKey.AttributeKeyPrefix + attr.Key ) );
                }
            }

            var financialTransactionService = new FinancialTransactionService( rockContext );

            // If this is a new Batch, SaveChanges so that we can get the Batch.Id
            if ( batch.Id == 0 )
            {
                rockContext.SaveChanges();
            }

            transaction.BatchId = batch.Id;

            // use the financialTransactionService to add the transaction instead of batch.Transactions to avoid lazy-loading the transactions already associated with the batch
            financialTransactionService.Add( transaction );
            rockContext.SaveChanges();
            transaction.SaveAttributeValues();

            batchService.IncrementControlAmount( batch.Id, transaction.TotalAmount, batchChanges );
            rockContext.SaveChanges();

            Task.Run( () => GiftWasGivenMessage.PublishTransactionEvent( transaction.Id, GiftEventTypes.GiftSuccess ) );

            HistoryService.SaveChanges(
                rockContext,
                typeof( FinancialBatch ),
                Rock.SystemGuid.Category.HISTORY_FINANCIAL_BATCH.AsGuid(),
                batch.Id,
                batchChanges );

            SendReceipt( transaction.Id );

            TransactionCode = transaction.TransactionCode;
        }

        /// <summary>
        /// Populates the transaction details for a FinancialTransaction or ScheduledFinancialTransaction
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="transactionDetails">The transaction details.</param>
        private void PopulateTransactionDetails<T>( ICollection<T> transactionDetails ) where T : ITransactionDetail, new()
        {
            var transactionEntity = this.GetTransactionEntity();
            var selectedAccountAmounts = caapPromptForAccountAmounts.AccountAmounts.Where( a => a.Amount.HasValue && a.Amount != 0 ).ToArray();

            var totalFeeCoverageAmount = GetSelectedFeeCoverageAmount();
            var totalSelectedAmounts = selectedAccountAmounts.Sum( a => a.Amount.Value );

            foreach ( var selectedAccountAmount in selectedAccountAmounts )
            {
                var transactionDetail = new T();

                transactionDetail.AccountId = selectedAccountAmount.AccountId;
                if ( totalFeeCoverageAmount > 0 )
                {
                    decimal portionOfTotalAmount = decimal.Divide( selectedAccountAmount.Amount.Value, totalSelectedAmounts );
                    decimal feeCoverageAmountForAccount = decimal.Round( portionOfTotalAmount * totalFeeCoverageAmount, 2 );
                    transactionDetail.Amount = selectedAccountAmount.Amount.Value + feeCoverageAmountForAccount;
                    transactionDetail.FeeCoverageAmount = feeCoverageAmountForAccount;
                }
                else
                {
                    transactionDetail.Amount = selectedAccountAmount.Amount.Value;
                }

                if ( transactionEntity != null )
                {
                    transactionDetail.EntityTypeId = transactionEntity.TypeId;
                    transactionDetail.EntityId = transactionEntity.Id;
                }

                transactionDetails.Add( transactionDetail );
            }
        }

        /// <summary>
        /// Gets the selected fee coverage amount that the person opted to include
        /// </summary>
        /// <returns></returns>
        private decimal GetSelectedFeeCoverageAmount()
        {
            decimal totalFeeCoverageAmount = 0.00M;
            var feeCoverageGatewayComponent = FinancialGateway.GetGatewayComponent() as IFeeCoverageGatewayComponent;
            if ( !this.GetAttributeValue( AttributeKey.EnableFeeCoverage ).AsBoolean() || feeCoverageGatewayComponent == null )
            {
                return 0.00M;
            }

            decimal? feeCoverageCreditCardPercent = null;
            decimal? feeCoverageACHAmount = null;

            if ( UsingPersonSavedAccount() )
            {
                if ( cbGiveNowCoverTheFee.Checked )
                {
                    if ( this.UsingACHPersonSavedAccount() )
                    {
                        feeCoverageACHAmount = feeCoverageGatewayComponent.GetACHFeeCoverageAmount( this.FinancialGateway );
                    }
                    else
                    {
                        feeCoverageCreditCardPercent = feeCoverageGatewayComponent.GetCreditCardFeeCoveragePercentage( this.FinancialGateway );
                    }
                }
            }
            else
            {
                bool isAch = false;
                if ( _hostedPaymentInfoControl is IHostedGatewayPaymentControlCurrencyTypeEvent )
                {
                    IHostedGatewayPaymentControlCurrencyTypeEvent hostedGatewayPaymentControlCurrencyTypeEvent = _hostedPaymentInfoControl as IHostedGatewayPaymentControlCurrencyTypeEvent;
                    isAch = hostedGatewayPaymentControlCurrencyTypeEvent.CurrencyTypeValue != null
                        && hostedGatewayPaymentControlCurrencyTypeEvent.CurrencyTypeValue.Guid == Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_ACH.AsGuid();
                }

                if ( isAch && cbGetPaymentInfoCoverTheFeeACH.Checked )
                {
                    feeCoverageACHAmount = feeCoverageGatewayComponent.GetACHFeeCoverageAmount( this.FinancialGateway );
                }
                else if ( !isAch && cbGetPaymentInfoCoverTheFeeCreditCard.Checked )
                {
                    feeCoverageCreditCardPercent = feeCoverageGatewayComponent.GetCreditCardFeeCoveragePercentage( this.FinancialGateway );
                }
            }

            var selectedAccountAmounts = caapPromptForAccountAmounts.AccountAmounts.Where( a => a.Amount.HasValue && a.Amount != 0 ).ToArray();

            decimal totalAmount = selectedAccountAmounts.Sum( a => a.Amount.Value );
            if ( feeCoverageACHAmount.HasValue && feeCoverageACHAmount > 0.00M )
            {
                totalFeeCoverageAmount = feeCoverageACHAmount.Value;
            }
            else if ( feeCoverageCreditCardPercent.HasValue && feeCoverageCreditCardPercent > 0.00M )
            {
                totalFeeCoverageAmount = totalAmount * ( feeCoverageCreditCardPercent.Value / 100.0M );
                totalFeeCoverageAmount = decimal.Round( totalFeeCoverageAmount, 2 );
            }

            return totalFeeCoverageAmount;
        }

        /// <summary>
        /// Saves the scheduled transaction.
        /// </summary>
        /// <param name="personId">The person identifier.</param>
        /// <param name="paymentInfo">The payment information.</param>
        /// <param name="schedule">The schedule.</param>
        /// <param name="scheduledTransaction">The scheduled transaction.</param>
        private void SaveScheduledTransaction( int personId, PaymentInfo paymentInfo, PaymentSchedule schedule, FinancialScheduledTransaction scheduledTransaction )
        {
            FinancialGateway financialGateway = this.FinancialGateway;
            IHostedGatewayComponent gateway = this.FinancialGatewayComponent;
            var rockContext = new RockContext();

            // manually assign the Guid that we generated at the beginning of the transaction UI entry to help make duplicate transactions impossible
            scheduledTransaction.Guid = hfTransactionGuid.Value.AsGuid();

            scheduledTransaction.TransactionFrequencyValueId = schedule.TransactionFrequencyValue.Id;
            scheduledTransaction.StartDate = schedule.StartDate;
            scheduledTransaction.EndDate = schedule.EndDate;
            scheduledTransaction.AuthorizedPersonAliasId = new PersonAliasService( rockContext ).GetPrimaryAliasId( personId ).Value;
            scheduledTransaction.FinancialGatewayId = financialGateway.Id;

            var transactionType = DefinedValueCache.Get( this.GetAttributeValue( AttributeKey.TransactionType ).AsGuidOrNull() ?? Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION.AsGuid() );
            scheduledTransaction.TransactionTypeValueId = transactionType.Id;

            scheduledTransaction.Summary = paymentInfo.Comment1;

            if ( scheduledTransaction.FinancialPaymentDetail == null )
            {
                scheduledTransaction.FinancialPaymentDetail = new FinancialPaymentDetail();
            }

            scheduledTransaction.FinancialPaymentDetail.SetFromPaymentInfo( paymentInfo, gateway as GatewayComponent, rockContext );

            Guid? sourceGuid = GetAttributeValue( AttributeKey.FinancialSourceType ).AsGuidOrNull();
            if ( sourceGuid.HasValue )
            {
                scheduledTransaction.SourceTypeValueId = DefinedValueCache.GetId( sourceGuid.Value );
            }

            PopulateTransactionDetails( scheduledTransaction.ScheduledTransactionDetails );

            var financialScheduledTransactionService = new FinancialScheduledTransactionService( rockContext );
            financialScheduledTransactionService.Add( scheduledTransaction );
            rockContext.SaveChanges();

            // If this is a transfer, now we can delete the old transaction
            if ( _scheduledTransactionIdToBeTransferred.HasValue )
            {
                DeleteTransferredScheduledTransaction( _scheduledTransactionIdToBeTransferred.Value );
            }

            Task.Run( () => ScheduledGiftWasModifiedMessage.PublishScheduledTransactionEvent( scheduledTransaction.Id, ScheduledGiftEventTypes.ScheduledGiftCreated ) );

            BindScheduledTransactions();
        }

        /// <summary>
        /// Gets the transaction entity.
        /// </summary>
        /// <returns></returns>
        private IEntity GetTransactionEntity()
        {
            IEntity transactionEntity = null;
            Guid? transactionEntityTypeGuid = GetAttributeValue( AttributeKey.TransactionEntityType ).AsGuidOrNull();
            if ( transactionEntityTypeGuid.HasValue )
            {
                var transactionEntityType = EntityTypeCache.Get( transactionEntityTypeGuid.Value );
                if ( transactionEntityType != null )
                {
                    var entityId = this.PageParameter( this.GetAttributeValue( AttributeKey.EntityIdParam ) ).AsIntegerOrNull();
                    if ( entityId.HasValue )
                    {
                        transactionEntity = Reflection.GetIEntityForEntityType( transactionEntityType.GetEntityType(), entityId.Value );
                    }
                }
            }

            return transactionEntity;
        }

        /// <summary>
        /// Sends the receipt.
        /// </summary>
        /// <param name="transactionId">The transaction identifier.</param>
        private void SendReceipt( int transactionId )
        {
            Guid? receiptEmail = GetAttributeValue( AttributeKey.ReceiptEmail ).AsGuidOrNull();
            if ( receiptEmail.HasValue )
            {
                // Queue a bus message to send receipts
                var sendPaymentReceiptsTask = new ProcessSendPaymentReceiptEmails.Message
                {
                    SystemEmailGuid = receiptEmail.Value,
                    TransactionId = transactionId
                };

                sendPaymentReceiptsTask.Send();
            }
        }

        /// <summary>
        /// Sets the comment field for a payment, incorporating the Lava template specified in the block settings if appropriate.
        /// </summary>
        /// <param name="paymentInfo"></param>
        /// <param name="userComment"></param>
        private void SetPaymentComment( PaymentInfo paymentInfo, List<FinancialTransactionDetail> commentTransactionAccountDetails, string userComment )
        {
            // Create a payment comment using the Lava template specified in this block.
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            mergeFields.Add( "TransactionDateTime", RockDateTime.Now );

            if ( paymentInfo != null )
            {
                mergeFields.Add( "CurrencyType", paymentInfo.CurrencyTypeValue );
            }

            mergeFields.Add( "TransactionAccountDetails", commentTransactionAccountDetails.Where( a => a.Amount != 0 ).ToList() );

            var paymentComment = GetAttributeValue( AttributeKey.PaymentCommentTemplate ).ResolveMergeFields( mergeFields );

            if ( GetAttributeValue( AttributeKey.EnableCommentEntry ).AsBoolean() )
            {
                if ( paymentComment.IsNotNullOrWhiteSpace() )
                {
                    // Append user comments to the block-specified payment comment.
                    paymentInfo.Comment1 = string.Format( "{0}: {1}", paymentComment, userComment );
                }
                else
                {
                    paymentInfo.Comment1 = userComment;
                }
            }
            else
            {
                paymentInfo.Comment1 = paymentComment;
            }
        }

        #endregion Transaction Entry Related

        #region Navigation

        /// <summary>
        /// Handles the Click event of the btnGiveNow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnGiveNow_Click( object sender, EventArgs e )
        {
            // Don't use captcha if the control is not visible
            if ( cpCaptcha.Visible )
            {
                nbPromptForAmountsWarning.Visible = true;
                nbPromptForAmountsWarning.Text = "There was an issue processing your request. Please try again. If the issue persists please contact us.";
                return;
            }

            var giftTerm = this.GetAttributeValue( AttributeKey.GiftTerm );

            nbProcessTransactionError.Visible = false;
            nbPaymentTokenError.Visible = false;

            if ( this.IsScheduledTransaction() )
            {
                var earliestScheduledStartDate = FinancialGatewayComponent.GetEarliestScheduledStartDate( FinancialGateway );
                if ( dtpStartDate.SelectedDate < earliestScheduledStartDate || !dtpStartDate.SelectedDate.HasValue )
                {
                    nbPromptForAmountsWarning.Visible = true;

                    nbPromptForAmountsWarning.Text = string.Format( "When scheduling a {0}, the minimum start date is {1}", giftTerm.ToLower(), earliestScheduledStartDate.ToShortDateString() );
                    return;
                }

                int oneTimeFrequencyId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME.AsGuid() ) ?? 0;
                if ( this.GetAttributeValue( AttributeKey.EnableEndDate ).AsBoolean() && ddlFrequency.SelectedValue.AsInteger() != oneTimeFrequencyId )
                {
                    if ( dtpEndDate.SelectedDate < dtpStartDate.SelectedDate )
                    {
                        nbPromptForAmountsWarning.Visible = true;

                        nbPromptForAmountsWarning.Text = string.Format( "When scheduling a {0}, the minimum end date is {1}", giftTerm.ToLower(), dtpStartDate.SelectedDate.ToShortDateString() );
                        return;
                    }
                }
            }
            else
            {
                if ( dtpStartDate.SelectedDate < RockDateTime.Today )
                {
                    nbPromptForAmountsWarning.Visible = true;
                    nbPromptForAmountsWarning.Text = string.Format( "Make sure the process {0} date is not in the past", giftTerm );
                    return;
                }
            }

            if ( caapPromptForAccountAmounts.IsValidAmountSelected() )
            {
                nbPromptForAmountsWarning.Visible = false;
                pnlPersonalInformation.Visible = false;

                // get the accountId(s) that have an amount specified
                var amountAccountIds = caapPromptForAccountAmounts.AccountAmounts
                    .Where( a => a.Amount.HasValue && a.Amount != 0.00M ).Select( a => a.AccountId )
                    .ToList();

                var accounts = new FinancialAccountService( new RockContext() ).GetByIds( amountAccountIds ).ToList();
                var amountSummaryMergeFields = LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                amountSummaryMergeFields.Add( "Accounts", accounts );

                if ( caapPromptForAccountAmounts.CampusId.HasValue )
                {
                    amountSummaryMergeFields.Add( "Campus", CampusCache.Get( caapPromptForAccountAmounts.CampusId.Value ) );
                }

                lAmountSummaryText.Text = GetAttributeValue( AttributeKey.AmountSummaryTemplate ).ResolveMergeFields( amountSummaryMergeFields );

                UpdateAccountSummaryAmount();

                if ( UsingPersonSavedAccount() )
                {
                    NavigateToStep( EntryStep.GetPersonalInformation );
                }
                else
                {
                    ConfigureCoverTheFees();
                    NavigateToStep( EntryStep.GetPaymentInfo );
                }
            }
            else
            {
                nbPromptForAmountsWarning.Visible = true;
                nbPromptForAmountsWarning.Text = "Please specify an amount";
            }
        }

        /// <summary>
        /// Updates the account summary amount.
        /// </summary>
        private void UpdateAccountSummaryAmount()
        {
            var totalPreFeeAmount = caapPromptForAccountAmounts.AccountAmounts.Sum( a => a.Amount ?? 0.00M );
            var coverTheFeeAmount = GetSelectedFeeCoverageAmount();
            var totalAmount = totalPreFeeAmount + coverTheFeeAmount;
            lAmountSummaryAmount.Text = totalAmount.FormatAsCurrency();
        }

        /// <summary>
        /// Handles the Click event of the btnGetPaymentInfoBack control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnGetPaymentInfoBack_Click( object sender, EventArgs e )
        {
            NavigateToStep( EntryStep.PromptForAmounts );
        }

        /// <summary>
        /// Handles the Click event of the btnGetPaymentInfoNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnGetPaymentInfoNext_Click( object sender, EventArgs e )
        {
            //// NOTE: the btnGetPaymentInfoNext button tells _hostedPaymentInfoControl to get a token via JavaScript
            //// When _hostedPaymentInfoControl gets a token response, the _hostedPaymentInfoControl_TokenReceived event will be triggered
            //// If _hostedPaymentInfoControl_TokenReceived gets a valid token, it will call btnGetPaymentInfoNext_Click

            UpdateAccountSummaryAmount();
            nbProcessTransactionError.Visible = false;
            NavigateToStep( EntryStep.GetPersonalInformation );
        }

        /// <summary>
        /// Handles the Click event of the btnPersonalInformationBack control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnPersonalInformationBack_Click( object sender, EventArgs e )
        {
            if ( UsingPersonSavedAccount() )
            {
                NavigateToStep( EntryStep.PromptForAmounts );
            }
            else
            {
                NavigateToStep( EntryStep.GetPaymentInfo );
            }
        }

        /// <summary>
        /// Handles the Click event of the btnPersonalInformationNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnPersonalInformationNext_Click( object sender, EventArgs e )
        {
            ProcessTransaction();
        }

        #endregion navigation

        #region ScheduledTransaction Transfer

        /// <summary>
        /// The scheduled transaction to be transferred.  This will get set if the
        /// page parameter "transfer" and the "ScheduledTransactionId" are passed in.
        /// </summary>
        private int? _scheduledTransactionIdToBeTransferred
        {
            get { return ViewState[ViewStateKey.ScheduledTransactionIdToBeTransferred] as int?; }
            set { ViewState[ViewStateKey.ScheduledTransactionIdToBeTransferred] = value; }
        }

        /// <summary>
        /// Fetches the old (to be transferred) scheduled transaction and verifies
        /// that the target person is the same on the scheduled transaction.  Then
        /// it puts it into the _scheduledTransactionToBeTransferred private field
        /// for use throughout the entry process so that its values can be used on
        /// the form for the new transaction.
        /// </summary>
        /// <param name="scheduledTransactionId">The scheduled transaction identifier.</param>
        private void InitializeTransfer( Guid? scheduledTransactionGuid )
        {
            if ( scheduledTransactionGuid == null )
            {
                return;
            }

            RockContext rockContext = new RockContext();
            var scheduledTransaction = new FinancialScheduledTransactionService( rockContext ).GetInclude( scheduledTransactionGuid.Value, s => s.AuthorizedPersonAlias.Person );
            var personService = new PersonService( rockContext );

            var targetPerson = GetTargetPerson( rockContext );

            // get business giving id
            var givingIds = personService.GetBusinesses( targetPerson.Id ).Select( g => g.GivingId ).ToList();

            // add the person's regular giving id
            givingIds.Add( targetPerson.GivingId );

            // Make sure the current person is the authorized person or one of the authorized person's businesses, otherwise return
            if ( scheduledTransaction == null || !givingIds.Contains( scheduledTransaction.AuthorizedPersonAlias.Person.GivingId ) )
            {
                return;
            }

            if ( scheduledTransaction.AuthorizedPersonAlias.Person.IsBusiness() )
            {
                tglIndividualOrBusiness.Checked = true;
                cblSelectBusiness.SetValue( scheduledTransaction.AuthorizedPersonAlias.PersonId );
                UpdateGivingAsIndividualOrBusinessControls();
            }

            _scheduledTransactionIdToBeTransferred = scheduledTransaction?.Id;

            // Set the frequency to be the same on the initial page build
            if ( !IsPostBack )
            {
                ddlFrequency.SelectedValue = scheduledTransaction.TransactionFrequencyValueId.ToString();
                dtpStartDate.SelectedDate = scheduledTransaction.NextPaymentDate.HasValue
                    ? scheduledTransaction.NextPaymentDate
                    : RockDateTime.Today.AddDays( 1 );
            }
        }

        /// <summary>
        /// Gets the account amounts from transferred scheduled transaction.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="selectableAccountIds">The selectable account ids.</param>
        /// <returns>CampusAccountAmountPicker.AccountIdAmount[].</returns>
        private CampusAccountAmountPicker.AccountIdAmount[] GetAccountAmountsFromTransferredScheduledTransaction( RockContext rockContext, List<int> selectableAccountIds )
        {
            CampusAccountAmountPicker.AccountIdAmount[] accountAmounts;
            var scheduledTransactionIdToBeTransferredAccountAmounts = new FinancialScheduledTransactionDetailService( rockContext )
.Queryable()
.Where( a => a.ScheduledTransactionId == _scheduledTransactionIdToBeTransferred.Value )
.Select( a => new
{
    a.AccountId,
    a.Amount
} ).ToList();

            accountAmounts = selectableAccountIds.Select( a => new CampusAccountAmountPicker.AccountIdAmount( a, 0.00M ) ).ToArray();
            var firstSelectableAccountAmount = accountAmounts[0];

            foreach ( var scheduledTransactionIdToBeTransferredAccountAmount in scheduledTransactionIdToBeTransferredAccountAmounts )
            {
                var accountAmount = accountAmounts.Where( a => a.AccountId == scheduledTransactionIdToBeTransferredAccountAmount.AccountId ).FirstOrDefault();
                if ( accountAmount == null )
                {
                    accountAmount = firstSelectableAccountAmount;
                }

                accountAmount.Amount += scheduledTransactionIdToBeTransferredAccountAmount.Amount;
            }

            accountAmounts = accountAmounts.Where( a => a.Amount != 0.00M ).ToArray();
            return accountAmounts;
        }

        /// <summary>
        /// Deletes the transferred scheduled transaction.
        /// </summary>
        /// <param name="scheduledTransactionId">The scheduled transaction identifier.</param>
        private void DeleteTransferredScheduledTransaction( int scheduledTransactionId )
        {
            using ( var rockContext = new Rock.Data.RockContext() )
            {
                FinancialScheduledTransactionService fstService = new FinancialScheduledTransactionService( rockContext );
                var currentTransaction = fstService.Get( scheduledTransactionId );
                if ( currentTransaction != null && currentTransaction.FinancialGateway != null )
                {
                    currentTransaction.FinancialGateway.LoadAttributes( rockContext );
                }

                string errorMessage = string.Empty;
                if ( fstService.Cancel( currentTransaction, out errorMessage ) )
                {
                    try
                    {
                        fstService.GetStatus( currentTransaction, out errorMessage );
                    }
                    catch ( Exception ex )
                    {
                        // if it was successfully cancelled, but we got an errorMessage or exception getting the status, that is OK.
                        ExceptionLogService.LogException( ex );
                    }

                    rockContext.SaveChanges();
                }
                else
                {
                    ExceptionLogService.LogException( new Exception( $"Transaction Entry V2 got an error when cancelling a transferred scheduled transaction: {errorMessage}" ) );
                    nbConfigurationNotification.Dismissable = true;
                    nbConfigurationNotification.NotificationBoxType = NotificationBoxType.Danger;
                    nbConfigurationNotification.Text = string.Format( "An error occurred while remove the tranferred scheduled {0}", GetAttributeValue( AttributeKey.GiftTerm ).ToLower() );
                    nbConfigurationNotification.Details = errorMessage;
                    nbConfigurationNotification.Visible = true;
                }
            }
        }

        #endregion
    }
}