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
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Bus.Message;
using Rock.Data;
using Rock.Financial;
using Rock.Model;
using Rock.Utility;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Finance
{
    #region Block Attributes

    /// <summary>
    /// Edit an existing scheduled transaction.
    /// This is the *Public* block for editing scheduled transactions
    /// </summary>
    [DisplayName( "Scheduled Transaction Edit" )]
    [Category( "Finance" )]
    [Description( "Edit an existing scheduled transaction." )]

    [BooleanField(
        name: "Impersonation",
        trueText: "Allow (only use on an internal page used by staff)",
        falseText: "Don't Allow",
        description: "Should the current user be able to view and edit other people's transactions?  IMPORTANT: This should only be enabled on an internal page that is secured to trusted users",
        defaultValue: false,
        key: AttributeKey.Impersonation )]

    [BooleanField(
        name: "Impersonator can see saved accounts",
        trueText: "Allow (only use on an internal page used by staff)",
        falseText: "Don't Allow",
        description: "Should the current user be able to view other people's saved accounts?  IMPORTANT: This should only be enabled on an internal page that is secured to trusted users",
        defaultValue: false,
        key: AttributeKey.ImpersonatorCanSeeSavedAccounts )]

    [AccountsField( "Accounts", "The accounts to display.  By default all active accounts with a Public Name will be displayed", false, "", "", 1 )]
    [BooleanField( "Additional Accounts", "Display option for selecting additional accounts", "Don't display option",
        "Should users be allowed to select additional accounts?  If so, any active account with a Public Name value will be available", true, "", 2 )]
    [CustomDropdownListField( "Layout Style", "How the sections of this page should be displayed", "Vertical,Fluid", false, "Vertical", "", 3 )]

    // Text Options

    [TextField( "Panel Title", "The text to display in panel heading", false, "Scheduled Transaction", "Text Options", 4 )]
    [TextField( "Contribution Info Title", "The text to display as heading of section for selecting account and amount.", false, "Contribution Information", "Text Options", 5 )]
    [TextField( "Add Account Text", "The button text to display for adding an additional account", false, "Add Another Account", "Text Options", 6 )]
    [TextField( "Payment Info Title", "The text to display as heading of section for entering credit card or bank account information.", false, "Payment Information", "Text Options", 7 )]
    [TextField( "Confirmation Title", "The text to display as heading of section for confirming information entered.", false, "Confirm Information", "Text Options", 8 )]
    [CodeEditorField( "Confirmation Header", "The text (HTML) to display at the top of the confirmation section.",
        CodeEditorMode.Html, CodeEditorTheme.Rock, 200, true, @"
<p>
Please confirm the information below. Once you have confirmed that the information is accurate click the 'Finish' button to complete your transaction.
</p>
", "Text Options", 9 )]
    [CodeEditorField( "Confirmation Footer", "The text (HTML) to display at the bottom of the confirmation section.",
        CodeEditorMode.Html, CodeEditorTheme.Rock, 200, true, @"
<div class='alert alert-info'>
By clicking the 'finish' button below I agree to allow {{ OrganizationName }} to debit the amount above from my account. I acknowledge that I may
update the transaction information at any time by returning to this website. Please call the Finance Office if you have any additional questions.
</div>
", "Text Options", 10 )]
    [CodeEditorField( "Success Header", "The text (HTML) to display at the top of the success section.",
        CodeEditorMode.Html, CodeEditorTheme.Rock, 200, true, @"
<p>
Thank you for your generous contribution.  Your support is helping {{ OrganizationName }} actively
achieve our mission.  We are so grateful for your commitment.
</p>
", "Text Options", 11 )]
    [CodeEditorField( "Success Footer", "The text (HTML) to display at the bottom of the success section.",
        CodeEditorMode.Html, CodeEditorTheme.Rock, 200, false, @"", "Text Options", 12 )]

    [WorkflowTypeField(
        name: "Workflow Trigger",
        description: "Workflow types to trigger when an edit is submitted for a schedule.",
        allowMultiple: true,
        required: false,
        order: 13,
        key: AttributeKey.WorkflowType )]

    [BooleanField(
        "Enable End Date",
        Description = "When enabled, this setting allows an individual to specify an optional end date for their recurring scheduled gifts.",
        Key = AttributeKey.EnableEndDate,
        DefaultBooleanValue = false,
        Order = 14 )]

    #endregion

    [Rock.SystemGuid.BlockTypeGuid( "5171C4E5-7698-453E-9CC8-088D362296DE" )]
    public partial class ScheduledTransactionEdit : RockBlock
    {
        /// <summary>
        /// Attribute Keys
        /// </summary>
        private static class AttributeKey
        {
            /// <summary>
            /// The workflow type
            /// </summary>
            public const string WorkflowType = "WorkflowType";

            /// <summary>
            /// Allow impersonation
            /// </summary>
            public const string Impersonation = "Impersonation";

            /// <summary>
            /// The impersonator can see saved accounts
            /// </summary>
            public const string ImpersonatorCanSeeSavedAccounts = "ImpersonatorCanSeeSavedAccounts";

            /// <summary>
            /// Determines whether End Date should be displayed.
            /// </summary>
            public const string EnableEndDate = "EnableEndDate";
        }

        private static class PageParameterKey
        {
            [RockObsolete( "1.13.1" )]
            [Obsolete( "Pass the GUID instead using the key ScheduledTransactionGuid.")]
            public const string ScheduledTransactionId = "ScheduledTransactionId";
            public const string ScheduledTransactionGuid = "ScheduledTransactionGuid";
        }

        #region Fields

        protected bool FluidLayout { get; set; }

        #endregion

        #region Properties

        protected int ForeignCurrencyCodeDefinedValueId { get; set; }

        private RockCurrencyCodeInfo _currencyInfo = null;

        protected RockCurrencyCodeInfo CurrencyCodeInfo
        {
            get
            {
                if ( _currencyInfo == null || _currencyInfo.CurrencyCodeDefinedValueId != ForeignCurrencyCodeDefinedValueId )
                {
                    _currencyInfo = new RockCurrencyCodeInfo( ForeignCurrencyCodeDefinedValueId );
                }
                return _currencyInfo;
            }
        }

        /// <summary>
        /// Gets or sets the gateway.
        /// </summary>
        protected GatewayComponent Gateway
        {
            get
            {
                if ( _gateway == null && _gatewayGuid.HasValue )
                {
                    _gateway = GatewayContainer.GetComponent( _gatewayGuid.ToString() );
                }

                return _gateway;
            }

            set
            {
                _gateway = value;
                _gatewayGuid = _gateway.TypeGuid;
            }
        }

        private GatewayComponent _gateway;
        private Guid? _gatewayGuid;

        /// <summary>
        /// Gets or sets the accounts that are available for user to add to the list.
        /// </summary>
        protected List<AccountItem> AvailableAccounts
        {
            get
            {
                if ( _availableAccounts == null )
                {
                    _availableAccounts = new List<AccountItem>();
                }

                return _availableAccounts;
            }

            set
            {
                _availableAccounts = value;
            }
        }

        private List<AccountItem> _availableAccounts;

        /// <summary>
        /// Gets or sets the accounts that are currently displayed to the user
        /// </summary>
        protected List<AccountItem> SelectedAccounts
        {
            get
            {
                if ( _selectedAccounts == null )
                {
                    _selectedAccounts = new List<AccountItem>();
                }

                return _selectedAccounts;
            }

            set
            {
                _selectedAccounts = value;
            }
        }

        private List<AccountItem> _selectedAccounts;

        /// <summary>
        /// Gets or sets the target person identifier.
        /// </summary>
        protected int? TargetPersonId { get; set; }

        /// <summary>
        /// Gets or sets the payment scheduled transaction Id.
        /// </summary>
        protected int? ScheduledTransactionId { get; set; }

        /// <summary>
        /// Gets or sets the payment transaction code.
        /// </summary>
        protected string TransactionCode { get; set; }

        /// <summary>
        /// Gets or sets the payment schedule id.
        /// </summary>
        protected string ScheduleId { get; set; }

        /// <summary>
        /// Gets or sets the End Date of a scheduled transaction.
        /// </summary>
        protected DateTime? EndDate { get; set; }

        #endregion

        #region base control methods

        protected override object SaveViewState()
        {
            ViewState["Gateway"] = _gatewayGuid;
            ViewState["AvailableAccounts"] = AvailableAccounts;
            ViewState["SelectedAccounts"] = SelectedAccounts;
            ViewState["TargetPersonId"] = TargetPersonId;
            ViewState["ScheduledTransactionId"] = ScheduledTransactionId;
            ViewState["TransactionCode"] = TransactionCode;
            ViewState["ScheduleId"] = ScheduleId;
            ViewState["ForeignCurrencyCodeDefinedValueId"] = ForeignCurrencyCodeDefinedValueId;
            ViewState["EndDate"] = EndDate;
            return base.SaveViewState();
        }

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            _gatewayGuid = ViewState["Gateway"] as Guid?;
            AvailableAccounts = ViewState["AvailableAccounts"] as List<AccountItem>;
            SelectedAccounts = ViewState["SelectedAccounts"] as List<AccountItem>;
            TargetPersonId = ViewState["TargetPersonId"] as int?;
            ScheduledTransactionId = ViewState["ScheduledTransactionId"] as int?;
            TransactionCode = ViewState["TransactionCode"] as string ?? string.Empty;
            ScheduleId = ViewState["ScheduleId"] as string ?? string.Empty;
            ForeignCurrencyCodeDefinedValueId = ( int ) ViewState["ForeignCurrencyCodeDefinedValueId"];
            EndDate = ViewState["EndDate"] as DateTime?;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( !Page.IsPostBack )
            {
                dvpForeignCurrencyCode.DefinedTypeId = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.FINANCIAL_CURRENCY_CODE.AsGuid() ).Id;

                lPanelTitle.Text = GetAttributeValue( "PanelTitle" );
                lContributionInfoTitle.Text = GetAttributeValue( "ContributionInfoTitle" );
                lPaymentInfoTitle.Text = GetAttributeValue( "PaymentInfoTitle" );
                lConfirmationTitle.Text = GetAttributeValue( "ConfirmationTitle" );

                var scheduledTransaction = GetScheduledTransaction( true );

                if ( scheduledTransaction != null )
                {
                    Gateway = scheduledTransaction.FinancialGateway.GetGatewayComponent();

                    ForeignCurrencyCodeDefinedValueId = scheduledTransaction.ForeignCurrencyCodeValueId ?? 0;

                    GetAccounts( scheduledTransaction );
                    SetFrequency( scheduledTransaction );
                    SetSavedAccounts( scheduledTransaction );

                    dtpStartDate.SelectedDate = scheduledTransaction.NextPaymentDate;
                    EndDate = scheduledTransaction.EndDate;
                    dtpEndDate.SelectedDate = scheduledTransaction.EndDate;
                    tbComments.Text = scheduledTransaction.Summary;

                    int oneTimeFrequencyId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME.AsGuid() ) ?? 0;
                    int selectedScheduleFrequencyId = btnFrequency.SelectedValue.AsInteger();
                    bool isOneTime = selectedScheduleFrequencyId == oneTimeFrequencyId;

                    if ( !GetAttributeValue( AttributeKey.EnableEndDate ).AsBoolean() || isOneTime )
                    {
                        dtpEndDate.Visible = false;
                    }

                    dvpForeignCurrencyCode.SelectedDefinedValueId = scheduledTransaction.ForeignCurrencyCodeValueId;
                    dvpForeignCurrencyCode.Visible = !new RockCurrencyCodeInfo( scheduledTransaction.ForeignCurrencyCodeValueId ).IsOrganizationCurrency;

                    hfCurrentPage.Value = "1";
                    RockPage page = Page as RockPage;
                    if ( page != null )
                    {
                        page.PageNavigate += page_PageNavigate;
                        page.AddScriptLink( "~/Scripts/moment-with-locales.min.js" );
                    }

                    FluidLayout = GetAttributeValue( "LayoutStyle" ) == "Fluid";

                    btnAddAccount.Title = GetAttributeValue( "AddAccountText" );

                    RegisterScript();

                    // Resolve the text field merge fields
                    var configValues = new Dictionary<string, object>();
                    lConfirmationHeader.Text = GetAttributeValue( "ConfirmationHeader" ).ResolveMergeFields( configValues );
                    lConfirmationFooter.Text = GetAttributeValue( "ConfirmationFooter" ).ResolveMergeFields( configValues );
                    lSuccessHeader.Text = GetAttributeValue( "SuccessHeader" ).ResolveMergeFields( configValues );
                    lSuccessFooter.Text = ( GetAttributeValue( "SuccessFooter" ) ?? string.Empty ).ResolveMergeFields( configValues );

                    hfPaymentTab.Value = "None";

                    //// Temp values for testing...
                    /*
                    txtCreditCard.Text = "5105105105105100";
                    txtCVV.Text = "023";

                    txtRoutingNumber.Text = "111111118";
                    txtAccountNumber.Text = "1111111111";
                     */
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            // Hide the error box on every postback
            nbMessage.Visible = false;
            pnlDupWarning.Visible = false;

            if ( !ScheduledTransactionId.HasValue )
            {
                SetPage( 0 );
                ShowMessage( NotificationBoxType.Danger, "Invalid Transaction", "The transaction you've selected either does not exist or is not valid." );
                base.OnLoad( e );
                return;
            }

            var hostedGatewayComponent = this.Gateway as IHostedGatewayComponent;
            bool isHostedGateway = false;
            if ( hostedGatewayComponent != null )
            {
                var scheduledTransaction = GetScheduledTransaction( false );
                if ( scheduledTransaction != null )
                {
                    isHostedGateway = hostedGatewayComponent.GetSupportedHostedGatewayModes( scheduledTransaction.FinancialGateway ).Contains( HostedGatewayMode.Hosted );
                }
            }

            if ( isHostedGateway )
            {
                SetPage( 0 );
                ShowMessage( NotificationBoxType.Danger, "Configuration", "This page is not configured to allow edits for the payment gateway associated with the selected transaction." );
                base.OnLoad( e );
                return;
            }

            // Save amounts from controls to the viewstate list
            foreach ( RepeaterItem item in rptAccountList.Items )
            {
                var hfAccountId = item.FindControl( "hfAccountId" ) as HiddenField;
                var txtAccountAmount = item.FindControl( "txtAccountAmount" ) as CurrencyBox;
                if ( hfAccountId != null && txtAccountAmount != null )
                {
                    var selectedAccount = SelectedAccounts.FirstOrDefault( a => a.Id == hfAccountId.ValueAsInt() );
                    if ( selectedAccount != null )
                    {
                        selectedAccount.Amount = txtAccountAmount.Value ?? 0.0m;
                    }
                }
            }

            // Update the total amount
            lblTotalAmount.Text = SelectedAccounts.Sum( f => f.Amount ).FormatAsCurrency( ForeignCurrencyCodeDefinedValueId );

            liNone.RemoveCssClass( "active" );
            liCreditCard.RemoveCssClass( "active" );
            liACH.RemoveCssClass( "active" );
            divNonePaymentInfo.RemoveCssClass( "active" );
            divCCPaymentInfo.RemoveCssClass( "active" );
            divACHPaymentInfo.RemoveCssClass( "active" );

            if ( !Gateway.IsUpdatingSchedulePaymentMethodSupported || Gateway is IThreeStepGatewayComponent )
            {
                // This block doesn't support ThreeStepGateway payment entry, but the "No Change" option is OK
                divPaymentMethodModification.Visible = false;
            }

            switch ( hfPaymentTab.Value )
            {
                case "ACH":
                    {
                        liACH.AddCssClass( "active" );
                        divACHPaymentInfo.AddCssClass( "active" );
                        break;
                    }

                case "CreditCard":
                    {
                        liCreditCard.AddCssClass( "active" );
                        divCCPaymentInfo.AddCssClass( "active" );
                        break;
                    }

                default:
                    {
                        liNone.AddCssClass( "active" );
                        divNonePaymentInfo.AddCssClass( "active" );
                        break;
                    }
            }

            // Show or Hide the new payment entry panels based on if a saved account exists and it's selected or not.
            var showNewCard = Gateway.SupportsStandardRockPaymentEntryForm &&
                ( rblSavedCC.Items.Count == 0 || rblSavedCC.Items[rblSavedCC.Items.Count - 1].Selected );
            divNewCard.Style[HtmlTextWriterStyle.Display] = showNewCard ? "block" : "none";

            var showNewAch = Gateway.SupportsStandardRockPaymentEntryForm &&
                ( rblSavedAch.Items.Count == 0 || rblSavedAch.Items[rblSavedAch.Items.Count - 1].Selected );
            divNewBank.Style[HtmlTextWriterStyle.Display] = showNewAch ? "block" : "none";

            if ( !Page.IsPostBack )
            {
                SetPage( 1 );

                // Get the list of accounts that can be used
                BindAccounts();
            }

            base.OnLoad( e );
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the SelectionChanged event of the btnAddAccount control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnAddAccount_SelectionChanged( object sender, EventArgs e )
        {
            var selected = AvailableAccounts.Where( a => a.Id == ( btnAddAccount.SelectedValueAsId() ?? 0 ) ).ToList();
            AvailableAccounts = AvailableAccounts.Except( selected ).ToList();
            SelectedAccounts.AddRange( selected );

            BindAccounts();
        }

        /// <summary>
        /// Handles the Click event of the btnNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnNext_Click( object sender, EventArgs e )
        {
            string errorMessage = string.Empty;

            switch ( hfCurrentPage.Value.AsInteger() )
            {
                case 1:

                    if ( ProcessPaymentInfo( out errorMessage ) )
                    {
                        this.AddHistory( "GivingDetail", "1", null );
                        SetPage( 2 );
                    }
                    else
                    {
                        ShowMessage( NotificationBoxType.Danger, "Oops!", errorMessage );
                    }

                    break;

                case 2:

                    if ( ProcessConfirmation( out errorMessage ) )
                    {
                        this.AddHistory( "GivingDetail", "2", null );
                        SetPage( 3 );
                    }
                    else
                    {
                        ShowMessage( NotificationBoxType.Danger, "Payment Error", errorMessage );
                    }

                    break;
            }
        }

        /// <summary>
        /// Handles the Click event of the btnPrev control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnPrev_Click( object sender, EventArgs e )
        {
            // Previous should only be enabled on the confirmation page (2)
            switch ( hfCurrentPage.Value.AsInteger() )
            {
                case 2:
                    SetPage( 1 );
                    break;
            }
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            var qryParams = new Dictionary<string, string>();

            var scheduledTransactionGuid = GetScheduledTransactionGuidFromUrl();
            if ( scheduledTransactionGuid.HasValue )
            {
                qryParams.Add( PageParameterKey.ScheduledTransactionGuid, scheduledTransactionGuid.ToString() );
            }

            NavigateToParentPage( qryParams );
        }

        /// <summary>
        /// Handles the Click event of the btnConfirm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnConfirm_Click( object sender, EventArgs e )
        {
            TransactionCode = string.Empty;

            string errorMessage = string.Empty;
            if ( ProcessConfirmation( out errorMessage ) )
            {
                SetPage( 3 );
            }
            else
            {
                ShowMessage( NotificationBoxType.Danger, "Payment Error", errorMessage );
            }
        }

        /// <summary>
        /// Handles the PageNavigate event of the page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="HistoryEventArgs"/> instance containing the event data.</param>
        protected void page_PageNavigate( object sender, HistoryEventArgs e )
        {
            int pageId = e.State["GivingDetail"].AsInteger();
            if ( pageId > 0 )
            {
                SetPage( pageId );
            }
        }

        #endregion

        #region  Methods

        #region Initialization

        /// <summary>
        /// Gets the scheduled transaction Guid based on what is specified in the URL
        /// </summary>
        /// <param name="refresh">if set to <c>true</c> [refresh].</param>
        /// <returns></returns>
        private Guid? GetScheduledTransactionGuidFromUrl()
        {
            var financialScheduledTransactionGuid = PageParameter( PageParameterKey.ScheduledTransactionGuid ).AsGuidOrNull();

#pragma warning disable CS0618
            var financialScheduledTransactionId = PageParameter( PageParameterKey.ScheduledTransactionId ).AsIntegerOrNull();
#pragma warning restore CS0618

            if ( financialScheduledTransactionGuid.HasValue )
            {
                return financialScheduledTransactionGuid.Value;
            }

            if ( financialScheduledTransactionId.HasValue )
            {
                return new FinancialScheduledTransactionService( new RockContext() ).GetGuid( financialScheduledTransactionId.Value );
            }

            return null;
        }

        /// <summary>
        /// Gets the scheduled transaction.
        /// </summary>
        /// <param name="refresh">if set to <c>true</c> [refresh].</param>
        /// <returns></returns>
        private FinancialScheduledTransaction GetScheduledTransaction( bool refresh = false )
        {
            // Default target to the current person
            Person targetPerson = CurrentPerson;

            using ( var rockContext = new RockContext() )
            {
                var financialScheduledTransactionGuid = GetScheduledTransactionGuidFromUrl();

                if ( !financialScheduledTransactionGuid.HasValue )
                {
                    return null;
                }

                var financialScheduledTransactionService = new FinancialScheduledTransactionService( rockContext );
                var scheduledTransactionQuery = financialScheduledTransactionService
                    .Queryable( "AuthorizedPersonAlias.Person,ScheduledTransactionDetails,FinancialGateway,FinancialPaymentDetail.CurrencyTypeValue,FinancialPaymentDetail.CreditCardTypeValue" )
                    .Where( t => t.Guid == financialScheduledTransactionGuid.Value );

                // If the block allows impersonation then just get the scheduled transaction
                if ( !GetAttributeValue( AttributeKey.Impersonation ).AsBoolean() )
                {
                    var personService = new PersonService( rockContext );

                    var validGivingIds = new List<string> { targetPerson.GivingId };
                    validGivingIds.AddRange( personService.GetBusinesses( targetPerson.Id ).Select( b => b.GivingId ) );

                    scheduledTransactionQuery = scheduledTransactionQuery
                        .Where( t => t.AuthorizedPersonAlias != null
                            && t.AuthorizedPersonAlias.Person != null
                            && validGivingIds.Contains( t.AuthorizedPersonAlias.Person.GivingId ) );
                }

                var scheduledTransaction = scheduledTransactionQuery.FirstOrDefault();

                if ( scheduledTransaction != null )
                {
                    if ( scheduledTransaction.AuthorizedPersonAlias != null )
                    {
                        TargetPersonId = scheduledTransaction.AuthorizedPersonAlias.PersonId;
                    }

                    ScheduledTransactionId = scheduledTransaction.Id;

                    if ( scheduledTransaction.FinancialGateway != null )
                    {
                        scheduledTransaction.FinancialGateway.LoadAttributes( rockContext );
                    }

                    if ( refresh )
                    {
                        string errorMessages = string.Empty;
                        financialScheduledTransactionService.GetStatus( scheduledTransaction, out errorMessages );
                        rockContext.SaveChanges();
                    }

                    return scheduledTransaction;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the accounts.
        /// </summary>
        /// <param name="scheduledTransaction">The scheduled transaction.</param>
        private void GetAccounts( FinancialScheduledTransaction scheduledTransaction )
        {
            var selectedGuids = GetAttributeValues( "Accounts" ).Select( Guid.Parse ).ToList();
            bool showAll = !selectedGuids.Any();

            bool additionalAccounts = GetAttributeValue( "AdditionalAccounts" ).AsBoolean( true );

            SelectedAccounts = new List<AccountItem>();
            AvailableAccounts = new List<AccountItem>();

            // Enumerate through all active accounts that are public
            foreach ( var account in new FinancialAccountService( new RockContext() ).Queryable()
                .Where( f =>
                    f.IsActive &&
                    f.IsPublic.HasValue &&
                    f.IsPublic.Value &&
                    ( f.StartDate == null || f.StartDate <= RockDateTime.Today ) &&
                    ( f.EndDate == null || f.EndDate >= RockDateTime.Today ) )
                .OrderBy( f => f.Order ) )
            {
                var accountItem = new AccountItem( account.Id, account.Order, account.Name, account.CampusId, account.PublicName );
                if ( showAll )
                {
                    SelectedAccounts.Add( accountItem );
                }
                else
                {
                    if ( selectedGuids.Contains( account.Guid ) )
                    {
                        SelectedAccounts.Add( accountItem );
                    }
                    else
                    {
                        if ( additionalAccounts )
                        {
                            AvailableAccounts.Add( accountItem );
                        }
                    }
                }
            }

            foreach ( var txnDetail in scheduledTransaction.ScheduledTransactionDetails )
            {
                var selectedAccount = SelectedAccounts.Where( a => a.Id == txnDetail.AccountId ).FirstOrDefault();
                if ( selectedAccount != null )
                {
                    selectedAccount.Amount = txnDetail.Amount;
                }
                else
                {
                    var selected = AvailableAccounts.Where( a => a.Id == txnDetail.AccountId ).ToList();
                    if ( selected != null )
                    {
                        selected.ForEach( a => a.Amount = txnDetail.Amount );
                        AvailableAccounts = AvailableAccounts.Except( selected ).ToList();
                        SelectedAccounts.AddRange( selected );
                    }
                }
            }

            BindAccounts();
        }

        /// <summary>
        /// Sets the frequency.
        /// </summary>
        /// <param name="scheduledTransaction">The scheduled transaction.</param>
        private void SetFrequency( FinancialScheduledTransaction scheduledTransaction )
        {
            if ( scheduledTransaction == null || Gateway == null || !Gateway.SupportedPaymentSchedules.Any() )
            {
                return;
            }

            var oneTimeFrequency = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME );
            divRepeatingPayments.Visible = true;

            btnFrequency.DataSource = Gateway.SupportedPaymentSchedules;
            btnFrequency.DataBind();

            btnFrequency.SelectedValue = scheduledTransaction.TransactionFrequencyValueId.ToString();
        }

        /// <summary>
        /// Binds the saved accounts.
        /// </summary>
        /// <param name="scheduledTransaction"></param>
        private void SetSavedAccounts( FinancialScheduledTransaction scheduledTransaction )
        {
            if ( scheduledTransaction == null || Gateway == null || scheduledTransaction.FinancialPaymentDetail == null )
            {
                return;
            }

            BindCurrencyTypeTab(
                scheduledTransaction,
                DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD ),
                liCreditCard,
                divCCPaymentInfo,
                divNewCard,
                rblSavedCC );

            BindCurrencyTypeTab(
                scheduledTransaction,
                DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_ACH ),
                liACH,
                divACHPaymentInfo,
                divNewBank,
                rblSavedAch );
        }

        /// <summary>
        /// Binds the currency type (ACH or credit card) tab.
        /// </summary>
        /// <param name="scheduledTransaction">The scheduled transaction.</param>
        /// <param name="currencyType">Type of the currency.</param>
        /// <param name="liTab">The li tab.</param>
        /// <param name="divTabContent">Content of the div tab.</param>
        /// <param name="divNewForm">The div new form.</param>
        /// <param name="rblSavedAccounts">The RBL saved accounts.</param>
        private void BindCurrencyTypeTab( FinancialScheduledTransaction scheduledTransaction, DefinedValueCache currencyType,
            HtmlGenericControl liTab, HtmlGenericControl divTabContent, HtmlGenericControl divNewForm, RadioButtonList rblSavedAccounts )
        {
            var isCard = currencyType.Guid == Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD.AsGuid();
            var term = isCard ? "card" : "bank account";

            var isCurrentCurrency = scheduledTransaction.FinancialPaymentDetail.CurrencyTypeValueId == currencyType.Id;
            var tabEnabled = isCurrentCurrency || Gateway.SupportsScheduleCurrencyChange;
            var newFormEnabled = tabEnabled && Gateway.SupportsStandardRockPaymentEntryForm;

            liTab.Visible = tabEnabled;
            divTabContent.Visible = tabEnabled;

            if ( !tabEnabled )
            {
                // This tab will be hidden, so nothing else to do
                return;
            }

            divTabContent.AddCssClass( "tab-pane" );
            rblSavedAccounts.Items.Clear();

            var isSelf = TargetPersonId.HasValue && CurrentPerson != null && TargetPersonId == CurrentPerson.Id;
            var canSeeSavedAccounts = isSelf || CanImpersonatorSeeSavedAccounts();
            var savedAccountViewModels = new List<SavedAccountViewModel>();

            if ( canSeeSavedAccounts && Gateway.SupportsSavedAccount( true ) && Gateway.SupportsSavedAccount( currencyType ) )
            {
                // Get the saved accounts for the target person
                var rockContext = new RockContext();
                var service = new FinancialPersonSavedAccountService( rockContext );

                savedAccountViewModels = service
                    .GetByPersonId( TargetPersonId.Value )
                    .Where( a =>
                        a.FinancialGateway.EntityTypeId == Gateway.TypeId &&
                        a.FinancialPaymentDetail.CurrencyTypeValueId == currencyType.Id )
                    .Select( a => new SavedAccountViewModel
                    {
                        Id = a.Id,
                        SavedAccountName = a.Name,
                        FinancialPaymentDetail = a.FinancialPaymentDetail,
                        GatewayPersonIdentifier = a.GatewayPersonIdentifier,
                        ReferenceNumber = a.ReferenceNumber,
                        TransactionCode = a.TransactionCode,
                        IsCard = isCard
                    } )
                    .ToList()
                    .OrderBy( a => a.SavedAccountName )
                    .ToList();

                rblSavedAccounts.DataSource = savedAccountViewModels;
                rblSavedAccounts.DataBind();
            }

            if ( savedAccountViewModels.Any() )
            {
                // Show the saved account list. The new form is initially hidden, but shows if that radio option is selected
                rblSavedAccounts.Visible = true;
                divNewForm.Style[HtmlTextWriterStyle.Display] = "none";

                if ( newFormEnabled )
                {
                    rblSavedAccounts.Items.Add( new ListItem( "Use a different " + term, "0" ) );
                }

                // Try to select the currently used card
                var likelyCurrentSavedAccount = savedAccountViewModels.FirstOrDefault( sa =>
                    sa.IsCard &&
                    sa.ReferenceNumber == scheduledTransaction.TransactionCode ||
                    sa.TransactionCode == scheduledTransaction.TransactionCode ||
                    sa.GatewayPersonIdentifier == scheduledTransaction.TransactionCode );

                if ( likelyCurrentSavedAccount != null )
                {
                    rblSavedAccounts.SetValue( likelyCurrentSavedAccount.Id );
                }
                else
                {
                    rblSavedAccounts.Items[0].Selected = true;
                }
            }
            else if ( newFormEnabled )
            {
                // The form is enabled and there are no saved accounts, so make the form visible immediately when selecting this tab
                divNewForm.Style[HtmlTextWriterStyle.Display] = "block";

                // The tab will be visible, but only show the new form (no saved account list)
                rblSavedAccounts.Visible = false;
            }
            else
            {
                // The tab could be visible, but it would be blank, so hide it
                liTab.Visible = false;
                divTabContent.Visible = false;
                return;
            }

            // Setup the new form according to the gateway specs if it will be visible
            if ( newFormEnabled && isCard )
            {
                SetupNewCreditCardForm( scheduledTransaction );
            }
        }

        /// <summary>
        /// Sets the new credit card form according to the Gateway's specifications.
        /// </summary>
        /// <param name="scheduledTransaction">The scheduled transaction.</param>
        private void SetupNewCreditCardForm( FinancialScheduledTransaction scheduledTransaction )
        {
            txtCardFirstName.Visible = Gateway.SplitNameOnCard;
            var authorizedPerson = scheduledTransaction.AuthorizedPersonAlias.Person;
            txtCardFirstName.Text = authorizedPerson.FirstName;
            txtCardLastName.Visible = Gateway.SplitNameOnCard;
            txtCardLastName.Text = authorizedPerson.LastName;
            txtCardName.Visible = !Gateway.SplitNameOnCard;
            txtCardName.Text = authorizedPerson.FullName;

            var groupLocation = new PersonService( new RockContext() ).GetFirstLocation(
                authorizedPerson.Id, DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() ).Id );
            if ( groupLocation != null )
            {
                acBillingAddress.SetValues( groupLocation.Location );
            }
            else
            {
                acBillingAddress.SetValues( null );
            }

            mypExpiration.MinimumYear = RockDateTime.Now.Year;
        }

        #endregion

        #region Process User Actions

        /// <summary>
        /// Processes the payment information.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        private bool ProcessPaymentInfo( out string errorMessage )
        {
            var rockContext = new RockContext();
            errorMessage = string.Empty;

            var errorMessages = new List<string>();

            // Validate that an amount was entered
            if ( SelectedAccounts.Sum( a => a.Amount ) <= 0 )
            {
                errorMessages.Add( "Make sure you've entered an amount for at least one account" );
            }

            // Validate that no negative amounts were entered
            if ( SelectedAccounts.Any( a => a.Amount < 0 ) )
            {
                errorMessages.Add( "Make sure the amount you've entered for each account is a positive amount" );
            }

            string howOften = DefinedValueCache.Get( btnFrequency.SelectedValueAsId().Value ).Value;

            // Make sure a repeating payment starts in the future
            if ( !dtpStartDate.SelectedDate.HasValue || dtpStartDate.SelectedDate <= RockDateTime.Today )
            {
                errorMessages.Add( "Make sure the Next Gift date is in the future (after today)" );
            }

            if ( dtpEndDate.SelectedDate < dtpStartDate.SelectedDate )
            {
                errorMessages.Add( "Make sure the End Date is not before the Next Gift date" );
            }

            if ( hfPaymentTab.Value == "ACH" )
            {
                // Validate ach options
                if ( rblSavedAch.Items.Count > 0 && ( rblSavedAch.SelectedValueAsInt() ?? 0 ) > 0 )
                {
                    // TODO: Find saved account
                }
                else
                {
                    if ( string.IsNullOrWhiteSpace( txtRoutingNumber.Text ) )
                    {
                        errorMessages.Add( "Make sure to enter a valid routing number" );
                    }

                    if ( string.IsNullOrWhiteSpace( txtAccountNumber.Text ) )
                    {
                        errorMessages.Add( "Make sure to enter a valid account number" );
                    }
                }
            }
            else if ( hfPaymentTab.Value == "CreditCard" )
            {
                // validate cc options
                if ( rblSavedCC.Items.Count > 0 && ( rblSavedCC.SelectedValueAsInt() ?? 0 ) > 0 )
                {
                    // TODO: Find saved card
                }
                else
                {
                    if ( Gateway.SplitNameOnCard )
                    {
                        if ( string.IsNullOrWhiteSpace( txtCardFirstName.Text ) || string.IsNullOrWhiteSpace( txtCardLastName.Text ) )
                        {
                            errorMessages.Add( "Make sure to enter a valid first and last name as it appears on your credit card" );
                        }
                    }
                    else
                    {
                        if ( string.IsNullOrWhiteSpace( txtCardName.Text ) )
                        {
                            errorMessages.Add( "Make sure to enter a valid name as it appears on your credit card" );
                        }
                    }

                    if ( string.IsNullOrWhiteSpace( txtCreditCard.Text ) )
                    {
                        errorMessages.Add( "Make sure to enter a valid credit card number" );
                    }

                    var currentMonth = RockDateTime.Today;
                    currentMonth = new DateTime( currentMonth.Year, currentMonth.Month, 1 );
                    if ( !mypExpiration.SelectedDate.HasValue || mypExpiration.SelectedDate.Value.CompareTo( currentMonth ) < 0 )
                    {
                        errorMessages.Add( "Make sure to enter a valid credit card expiration date" );
                    }

                    if ( string.IsNullOrWhiteSpace( txtCVV.Text ) )
                    {
                        errorMessages.Add( "Make sure to enter a valid credit card security code" );
                    }
                }
            }

            if ( errorMessages.Any() )
            {
                errorMessage = errorMessages.AsDelimited( "<br/>" );
                return false;
            }

            FinancialScheduledTransaction scheduledTransaction = null;

            if ( ScheduledTransactionId.HasValue )
            {
                scheduledTransaction = new FinancialScheduledTransactionService( rockContext )
                    .Queryable( "AuthorizedPersonAlias.Person" ).FirstOrDefault( s => s.Id == ScheduledTransactionId.Value );
            }

            if ( scheduledTransaction == null )
            {
                errorMessage = "There was a problem getting the transaction information";
                return false;
            }

            if ( scheduledTransaction.AuthorizedPersonAlias == null || scheduledTransaction.AuthorizedPersonAlias.Person == null )
            {
                errorMessage = "There was a problem determining the person associated with the transaction";
                return false;
            }

            PaymentInfo paymentInfo = GetPaymentInfo( new PersonService( rockContext ), scheduledTransaction );
            if ( paymentInfo != null )
            {
                tdName.Description = paymentInfo.FullName;
                tdTotal.Description = paymentInfo.Amount.FormatAsCurrency( ForeignCurrencyCodeDefinedValueId );

                if ( paymentInfo.CurrencyTypeValue != null )
                {
                    tdPaymentMethod.Description = paymentInfo.CurrencyTypeValue.Description;
                    tdPaymentMethod.Visible = true;
                }
                else
                {
                    tdPaymentMethod.Visible = false;
                }

                if ( string.IsNullOrWhiteSpace( paymentInfo.MaskedNumber ) )
                {
                    tdAccountNumber.Visible = false;
                }
                else
                {
                    tdAccountNumber.Visible = true;
                    tdAccountNumber.Description = paymentInfo.MaskedNumber;
                }
            }

            rptAccountListConfirmation.DataSource = SelectedAccounts.Where( a => a.Amount != 0 );
            rptAccountListConfirmation.DataBind();

            string nextDate = dtpStartDate.SelectedDate.HasValue ? dtpStartDate.SelectedDate.Value.ToShortDateString() : "?";
            string endDate = dtpEndDate.SelectedDate.HasValue ? dtpEndDate.SelectedDate.Value.ToShortDateString() : "";
            string frequency = DefinedValueCache.Get( btnFrequency.SelectedValueAsInt() ?? 0 ).Description;
            if ( dtpEndDate.SelectedDate.HasValue )
            {
                tdWhen.Description = frequency + " starting on " + nextDate + " and ending on " + endDate;
            }
            else
            {
                tdWhen.Description = frequency + " starting on " + nextDate;
            }

            return true;
        }

        /// <summary>
        /// Processes the confirmation.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        private bool ProcessConfirmation( out string errorMessage )
        {
            var rockContext = new RockContext();
            errorMessage = string.Empty;

            if ( string.IsNullOrWhiteSpace( TransactionCode ) )
            {
                if ( Gateway == null )
                {
                    errorMessage = "There was a problem creating the payment gateway information";
                    return false;
                }

                var personService = new PersonService( rockContext );
                var transactionService = new FinancialScheduledTransactionService( rockContext );
                var transactionDetailService = new FinancialScheduledTransactionDetailService( rockContext );

                FinancialScheduledTransaction scheduledTransaction = null;

                if ( ScheduledTransactionId.HasValue )
                {
                    scheduledTransaction = transactionService
                        .Queryable( "AuthorizedPersonAlias.Person,FinancialGateway" )
                        .FirstOrDefault( s => s.Id == ScheduledTransactionId.Value );
                }

                if ( scheduledTransaction == null )
                {
                    errorMessage = "There was a problem getting the transaction information";
                    return false;
                }

                if ( scheduledTransaction.FinancialPaymentDetail == null )
                {
                    scheduledTransaction.FinancialPaymentDetail = new FinancialPaymentDetail();
                }

                if ( scheduledTransaction.FinancialGateway != null )
                {
                    scheduledTransaction.FinancialGateway.LoadAttributes();
                }

                if ( scheduledTransaction.AuthorizedPersonAlias == null || scheduledTransaction.AuthorizedPersonAlias.Person == null )
                {
                    errorMessage = "There was a problem determining the person associated with the transaction";
                    return false;
                }

                // Get the payment schedule
                scheduledTransaction.TransactionFrequencyValueId = btnFrequency.SelectedValueAsId().Value;

                // ProcessPaymentInfo ensures that dtpStartDate.SelectedDate has a value and is after today
                scheduledTransaction.StartDate = dtpStartDate.SelectedDate.Value;
                scheduledTransaction.NextPaymentDate = Gateway.CalculateNextPaymentDate( scheduledTransaction, null );

                if ( dtpEndDate.SelectedDate.HasValue )
                {
                    scheduledTransaction.EndDate = dtpEndDate.SelectedDate.Value;
                }
                else
                {
                    scheduledTransaction.EndDate = null;
                }

                scheduledTransaction.ForeignCurrencyCodeValueId = dvpForeignCurrencyCode.SelectedDefinedValueId;

                PaymentInfo paymentInfo = GetPaymentInfo( personService, scheduledTransaction );
                if ( paymentInfo == null )
                {
                    errorMessage = "There was a problem creating the payment information";
                    return false;
                }

                // If transaction is not active, attempt to re-activate it first
                if ( !scheduledTransaction.IsActive )
                {
                    if ( !transactionService.Reactivate( scheduledTransaction, out errorMessage ) )
                    {
                        return false;
                    }
                }

                if ( hfPaymentTab.Value == "CreditCard" || hfPaymentTab.Value == "ACH" )
                {
                    // if using a new CC or ACH, clear the payment info and let the gateway set the payment details in
                    // Gateway.UpdateScheduledPayment, then fill in any missing details with SetFromPaymentInfo
                    scheduledTransaction.FinancialPaymentDetail.ClearPaymentInfo();
                }

                if ( Gateway.UpdateScheduledPayment( scheduledTransaction, paymentInfo, out errorMessage ) )
                {
                    if ( hfPaymentTab.Value == "CreditCard" || hfPaymentTab.Value == "ACH" )
                    {
                        // if using a new form of payment, update FinancialPaymentDetail
                        // with anything the Gateway didn't set in UpdateScheduledPayment
                        scheduledTransaction.FinancialPaymentDetail.SetFromPaymentInfo( paymentInfo, Gateway, rockContext );
                    }

                    var selectedAccountIds = SelectedAccounts
                        .Where( a => a.Amount > 0 )
                        .Select( a => a.Id ).ToList();

                    var deletedAccounts = scheduledTransaction.ScheduledTransactionDetails
                        .Where( a => !selectedAccountIds.Contains( a.AccountId ) ).ToList();

                    foreach ( var deletedAccount in deletedAccounts )
                    {
                        scheduledTransaction.ScheduledTransactionDetails.Remove( deletedAccount );
                        transactionDetailService.Delete( deletedAccount );
                    }

                    foreach ( var account in SelectedAccounts
                        .Where( a => a.Amount > 0 ) )
                    {
                        var detail = scheduledTransaction.ScheduledTransactionDetails
                            .Where( d => d.AccountId == account.Id ).FirstOrDefault();
                        if ( detail == null )
                        {
                            detail = new FinancialScheduledTransactionDetail();
                            detail.AccountId = account.Id;
                            scheduledTransaction.ScheduledTransactionDetails.Add( detail );
                        }

                        detail.Amount = account.Amount;
                    }

                    scheduledTransaction.Summary = tbComments.Text;

                    rockContext.SaveChanges();
                    Task.Run( () => ScheduledGiftWasModifiedMessage.PublishScheduledTransactionEvent( scheduledTransaction.Id, ScheduledGiftEventTypes.ScheduledGiftUpdated ) );

                    ScheduleId = scheduledTransaction.GatewayScheduleId;
                    TransactionCode = scheduledTransaction.TransactionCode;

                    if ( transactionService.GetStatus( scheduledTransaction, out errorMessage ) )
                    {
                        rockContext.SaveChanges();
                    }
                }
                else
                {
                    return false;
                }

                tdTransactionCode.Description = TransactionCode;
                tdTransactionCode.Visible = !string.IsNullOrWhiteSpace( TransactionCode );

                tdScheduleId.Description = ScheduleId;
                tdScheduleId.Visible = !string.IsNullOrWhiteSpace( ScheduleId );

                TriggerWorkflows( scheduledTransaction );

                return true;
            }
            else
            {
                pnlDupWarning.Visible = true;
                return false;
            }
        }

        #endregion

        #region Build PaymentInfo

        /// <summary>
        /// Gets the payment information.
        /// </summary>
        /// <returns></returns>
        private PaymentInfo GetPaymentInfo( PersonService personService, FinancialScheduledTransaction scheduledTransaction )
        {
            PaymentInfo paymentInfo = null;
            if ( hfPaymentTab.Value == "ACH" )
            {
                if ( rblSavedAch.Items.Count > 0 && ( rblSavedAch.SelectedValueAsId() ?? 0 ) > 0 )
                {
                    paymentInfo = GetReferenceInfo( rblSavedAch.SelectedValueAsId().Value );
                }
                else
                {
                    paymentInfo = GetACHInfo();
                }
            }
            else if ( hfPaymentTab.Value == "CreditCard" )
            {
                if ( rblSavedCC.Items.Count > 0 && ( rblSavedCC.SelectedValueAsId() ?? 0 ) > 0 )
                {
                    paymentInfo = GetReferenceInfo( rblSavedCC.SelectedValueAsId().Value );
                }
                else
                {
                    paymentInfo = GetCCInfo();
                }
            }
            else
            {
                // no change, so use the reference info from the existing transaction
                paymentInfo = GetReferenceInfoFromTransaction( scheduledTransaction );
            }

            if ( paymentInfo != null )
            {
                paymentInfo.Amount = SelectedAccounts.Sum( a => a.Amount );
                var authorizedPerson = scheduledTransaction.AuthorizedPersonAlias.Person;
                paymentInfo.FirstName = authorizedPerson.FirstName;
                paymentInfo.LastName = authorizedPerson.LastName;
                paymentInfo.Email = authorizedPerson.Email;

                bool displayPhone = GetAttributeValue( "DisplayPhone" ).AsBoolean();
                if ( displayPhone )
                {
                    var phoneNumber = personService.GetPhoneNumber( authorizedPerson, DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME ) ) );
                    paymentInfo.Phone = phoneNumber != null ? phoneNumber.ToString() : string.Empty;
                }

                Guid addressTypeGuid = Guid.Empty;
                if ( !Guid.TryParse( GetAttributeValue( "AddressType" ), out addressTypeGuid ) )
                {
                    addressTypeGuid = new Guid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME );
                }

                var groupLocation = personService.GetFirstLocation( authorizedPerson.Id, DefinedValueCache.Get( addressTypeGuid ).Id );
                if ( groupLocation != null && groupLocation.Location != null )
                {
                    paymentInfo.Street1 = groupLocation.Location.Street1;
                    paymentInfo.Street2 = groupLocation.Location.Street2;
                    paymentInfo.City = groupLocation.Location.City;
                    paymentInfo.State = groupLocation.Location.State;
                    paymentInfo.PostalCode = groupLocation.Location.PostalCode;
                    paymentInfo.Country = groupLocation.Location.Country;
                }
            }

            return paymentInfo;
        }

        /// <summary>
        /// Gets the credit card information.
        /// </summary>
        /// <returns></returns>
        private CreditCardPaymentInfo GetCCInfo()
        {
            var cc = new CreditCardPaymentInfo( txtCreditCard.Text, txtCVV.Text, mypExpiration.SelectedDate.Value );
            cc.NameOnCard = Gateway.SplitNameOnCard ? txtCardFirstName.Text : txtCardName.Text;
            cc.LastNameOnCard = txtCardLastName.Text;
            cc.BillingStreet1 = acBillingAddress.Street1;
            cc.BillingStreet2 = acBillingAddress.Street2;
            cc.BillingCity = acBillingAddress.City;
            cc.BillingState = acBillingAddress.State;
            cc.BillingPostalCode = acBillingAddress.PostalCode;
            cc.BillingCountry = acBillingAddress.Country;

            return cc;
        }

        /// <summary>
        /// Gets the ACH information.
        /// </summary>
        /// <returns></returns>
        private ACHPaymentInfo GetACHInfo()
        {
            var ach = new ACHPaymentInfo( txtAccountNumber.Text, txtRoutingNumber.Text, rblAccountType.SelectedValue == "Savings" ? BankAccountType.Savings : BankAccountType.Checking );
            return ach;
        }

        /// <summary>
        /// Gets the reference information.
        /// </summary>
        /// <param name="savedAccountId">The saved account unique identifier.</param>
        /// <returns></returns>
        private ReferencePaymentInfo GetReferenceInfo( int savedAccountId )
        {
            var savedAccount = new FinancialPersonSavedAccountService( new RockContext() ).Get( savedAccountId );
            if ( savedAccount != null )
            {
                return savedAccount.GetReferencePayment();
            }

            return null;
        }

        /// <summary>
        /// Gets the reference information from the specified scheduled transaction
        /// </summary>
        /// <param name="scheduledTransaction">The scheduled transaction.</param>
        /// <returns></returns>
        private ReferencePaymentInfo GetReferenceInfoFromTransaction( FinancialScheduledTransaction scheduledTransaction )
        {
            ReferencePaymentInfo referencePaymentInfo;
            if ( scheduledTransaction.FinancialPaymentDetail != null )
            {
                if ( scheduledTransaction.FinancialPaymentDetail.FinancialPersonSavedAccount != null )
                {
                    // If we have FinancialPersonSavedAccount for this, get the reference info from that
                    referencePaymentInfo = scheduledTransaction.FinancialPaymentDetail.FinancialPersonSavedAccount.GetReferencePayment();
                }
                else
                {
                    // just in case the transaction doesn't have a FinancialPersonSavedAccount, get as much as we can from scheduledTransaction.FinancialPaymentDetail
                    referencePaymentInfo = new ReferencePaymentInfo();

                    // if we know the original CurrencyType, set it
                    if ( scheduledTransaction.FinancialPaymentDetail.CurrencyTypeValueId.HasValue )
                    {
                        referencePaymentInfo.InitialCurrencyTypeValue = DefinedValueCache.Get( scheduledTransaction.FinancialPaymentDetail.CurrencyTypeValueId.Value );
                    }

                    if ( scheduledTransaction.FinancialPaymentDetail.CreditCardTypeValueId.HasValue )
                    {
                        referencePaymentInfo.InitialCreditCardTypeValue = DefinedValueCache.Get( scheduledTransaction.FinancialPaymentDetail.CreditCardTypeValueId.Value );
                    }

                    referencePaymentInfo.GatewayPersonIdentifier = scheduledTransaction.FinancialPaymentDetail.GatewayPersonIdentifier;
                }
            }
            else
            {
                // For extra safety, if don't have a scheduledTransaction.FinancialPaymentDetail for this transaction, assume it is a credit card/visa
                referencePaymentInfo = new ReferencePaymentInfo();
            }

            if ( referencePaymentInfo.InitialCurrencyTypeValue == null )
            {
                // if we weren't able to figure out InitialCurrencyTypeValue yet, assume it is credit card
                referencePaymentInfo.InitialCurrencyTypeValue = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD.AsGuid() );
            }

            if ( referencePaymentInfo.InitialCreditCardTypeValue == null )
            {
                // if we weren't able to figure out InitialCreditCardTypeValue yet, assume it is Visa
                referencePaymentInfo.InitialCreditCardTypeValue = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CREDITCARD_TYPE_VISA.AsGuid() );
            }

            if ( referencePaymentInfo.ReferenceNumber.IsNullOrWhiteSpace() )
            {
                string errorMessage;
                referencePaymentInfo.ReferenceNumber = Gateway.GetReferenceNumber( scheduledTransaction, out errorMessage );
            }

            return referencePaymentInfo;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Determines whether the impersonator can see saved accounts.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance [can impersonator can see saved accounts]; otherwise, <c>false</c>.
        /// </returns>
        private bool CanImpersonatorSeeSavedAccounts()
        {
            return GetAttributeValue( AttributeKey.ImpersonatorCanSeeSavedAccounts ).AsBoolean();
        }

        /// <summary>
        /// Binds the accounts.
        /// </summary>
        private void BindAccounts()
        {
            rptAccountList.DataSource = SelectedAccounts;
            rptAccountList.DataBind();

            btnAddAccount.Visible = AvailableAccounts.Any();
            btnAddAccount.DataSource = AvailableAccounts;
            btnAddAccount.DataBind();
        }

        /// <summary>
        /// Sets the page.
        /// </summary>
        /// <param name="page">The page.</param>
        private void SetPage( int page )
        {
            //// Page 1 = Payment Info
            //// Page 2 = Confirmation
            //// Page 3 = Success
            //// Page 0 = Only message box is displayed

            pnlPaymentInfo.Visible = page == 1;
            pnlConfirmation.Visible = page == 2;
            pnlSuccess.Visible = page == 3;
            divActions.Visible = page > 0;

            btnPrev.Visible = page == 2;
            btnNext.Visible = page < 3;
            btnNext.Text = page > 1 ? "Finish" : "Next";
            btnCancel.Text = page == 3 ? "Back" : "Cancel";

            hfCurrentPage.Value = page.ToString();
        }

        /// <summary>
        /// Shows the message.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="title">The title.</param>
        /// <param name="text">The text.</param>
        private void ShowMessage( NotificationBoxType type, string title, string text )
        {
            if ( !string.IsNullOrWhiteSpace( text ) )
            {
                nbMessage.Text = text;
                nbMessage.Title = title;
                nbMessage.NotificationBoxType = type;
                nbMessage.Visible = true;
            }
        }

        /// <summary>
        /// Formats the value as currency (called from markup)
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public string FormatValueAsCurrency( decimal value )
        {
            return value.FormatAsCurrency( ForeignCurrencyCodeDefinedValueId );
        }

        /// <summary>
        /// Registers the startup script.
        /// </summary>
        private void RegisterScript()
        {
            RockPage.AddCSSLink( "~/Styles/Blocks/Shared/CardSprites.css", true );
            RockPage.AddScriptLink( "~/Scripts/jquery.creditCardTypeDetector.js" );

            int oneTimeFrequencyId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME ).Id;

            string scriptFormat = @"
    Sys.Application.add_load(function () {{
        // As amounts are entered, validate that they are numeric and recalc total
        $('.account-amount').on('change', function() {{
            var totalAmt = Number(0);
            var symbol = $('#hfCurrencySymbol').val();
            var decimalPlaces = $('#hfCurrencyDecimals').val();

            $('.account-amount .form-control').each(function (index) {{
                var itemValue = $(this).val();
                if (itemValue != null && itemValue != '') {{
                    if (isNaN(itemValue)) {{
                        $(this).parents('div.input-group').addClass('has-error');
                    }}
                    else {{
                        $(this).parents('div.input-group').removeClass('has-error');
                        var num = Number(itemValue);
                        $(this).val(num.toFixed(decimalPlaces));
                        totalAmt = totalAmt + num;
                    }}
                }}
                else {{
                    $(this).parents('div.input-group').removeClass('has-error');
                }}
            }});
            $('.total-amount').html(symbol + totalAmt.toLocaleString(undefined, {{ minimumFractionDigits: decimalPlaces, maximumFractionDigits: decimalPlaces }}));
            return false;
        }});

        // Set the date prompt based on the frequency value entered
        $('#ButtonDropDown_btnFrequency .dropdown-menu a').on('click', function () {{
            var $when = $(this).parents('div.form-group').first().next();
            if ($(this).attr('data-id') == '{3}') {{
                $when.find('label').first().html('When');
            }} else {{
                $when.find('label').first().html('First Gift');

                // Set date to tomorrow if it is equal or less than today's date
                var $dateInput = $when.find('input');
                var locale = window.navigator.userLanguage || window.navigator.language;
                moment.locale(locale);
                var dt = moment($dateInput.val(), 'l');
                var curr = moment();
                if ( (dt-curr) <= 0 ) {{
                    curr = curr.add(1, 'day');

                    $dateInput.val(curr.format('l'));
                    //$dateInput.data('datePicker').value(curr.format('l'));
                }}
            }};
        }});

        // Save the state of the selected payment type pill to a hidden field so that state can
        // be preserved through postback
        $('a[data-toggle=""pill""]').on('shown.bs.tab', function (e) {{
            var tabHref = $(e.target).attr(""href"");
            if (tabHref == '#{0}') {{
                $('#{2}').val('CreditCard');
            }} else if (tabHref == '#{1}') {{
                $('#{2}').val('ACH');
            }} else {{
                $('#{2}').val('None');
            }}
        }});

        // Detect credit card type
        $('.credit-card').creditCardTypeDetector({{ 'credit_card_logos': '.card-logos' }});

        // Toggle credit card display if saved card option is available
        $('div.radio-content').prev('.form-group').find('input:radio').unbind('click').on('click', function () {{
            var $content = $(this).parents('div.form-group').first().next('.radio-content')
            var radioDisplay = $content.css('display');
            if ($(this).val() == 0 && radioDisplay == 'none') {{
                $content.slideToggle();
            }}
            else if ($(this).val() != 0 && radioDisplay != 'none') {{
                $content.slideToggle();
            }}
        }});

        // Hide or show a div based on selection of checkbox
        $('input:checkbox.toggle-input').unbind('click').on('click', function () {{
            $(this).parents('.checkbox').next('.toggle-content').slideToggle();
        }});

        // Disable the submit button as soon as it's clicked to prevent double-clicking
        $('a[id$=""btnNext""]').on('click', function() {{
			$(this).addClass('disabled');
			$(this).unbind('click');
			$(this).on('click', function () {{
				return false;
			}});
        }});
    }});

";
            string script = string.Format(
                scriptFormat,
                divCCPaymentInfo.ClientID,  // {0}
                divACHPaymentInfo.ClientID, // {1}
                hfPaymentTab.ClientID,      // {2}
                oneTimeFrequencyId         // {3}
                );

            ScriptManager.RegisterStartupScript( upPayment, this.GetType(), "giving-profile", script, true );
        }

        /// <summary>
        /// Trigger an instance of each active workflow type selected in the block attributes
        /// </summary>
        private void TriggerWorkflows( FinancialScheduledTransaction schedule )
        {
            if ( schedule == null )
            {
                return;
            }

            var workflowTypeGuids = GetAttributeValues( AttributeKey.WorkflowType ).AsGuidList();

            if ( workflowTypeGuids.Any() )
            {
                // Make sure the workflow types are active and then trigger an instance of each
                var rockContext = new RockContext();
                var service = new WorkflowTypeService( rockContext );
                var workflowTypes = service.Queryable()
                    .AsNoTracking()
                    .Where( wt => wt.IsActive == true && workflowTypeGuids.Contains( wt.Guid ) )
                    .ToList();

                foreach ( var workflowType in workflowTypes )
                {
                    schedule.LaunchWorkflow( workflowType.Guid, string.Empty, null, null );
                }
            }
        }

        #endregion

        #endregion

        #region Helper Classes

        /// <summary>
        /// Lightweight object for each contribution item
        /// </summary>
        [Serializable]
        protected class AccountItem
        {
            public int Id { get; set; }

            public int Order { get; set; }

            public string Name { get; set; }

            public int? CampusId { get; set; }

            public decimal Amount { get; set; }

            public string PublicName { get; set; }

            public AccountItem( int id, int order, string name, int? campusId, string publicName )
            {
                Id = id;
                Order = order;
                Name = name;
                CampusId = campusId;
                PublicName = publicName;
            }
        }

        /// <summary>
        /// Saved Account View Model
        /// </summary>
        private class SavedAccountViewModel
        {
            /// <summary>
            /// Id
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Gets the display name.
            /// </summary>
            /// <value>
            /// The display name.
            /// </value>
            public string DisplayName
            {
                get
                {
                    if ( FinancialPaymentDetail == null )
                    {
                        return null;
                    }

                    if ( FinancialPaymentDetail.ExpirationDate.IsNotNullOrWhiteSpace() )
                    {
                        return $"Use {SavedAccountName} ( {FinancialPaymentDetail.AccountNumberMasked} Expires {FinancialPaymentDetail.ExpirationDate})";
                    }
                    else
                    {
                        return $"Use {SavedAccountName} ( {FinancialPaymentDetail.AccountNumberMasked} )";
                    }
                }
            }

            /// <summary>
            /// Reference Number
            /// </summary>
            public string ReferenceNumber { get; set; }

            /// <summary>
            /// Transaction Code
            /// </summary>
            public string TransactionCode { get; set; }

            /// <summary>
            /// Gets or sets the name of the saved account.
            /// </summary>
            /// <value>
            /// The name of the saved account.
            /// </value>
            public string SavedAccountName { get; internal set; }

            /// <summary>
            /// Gateway Person Identifier
            /// </summary>
            public string GatewayPersonIdentifier { get; set; }

            /// <summary>
            /// Is this a card?
            /// </summary>
            public bool IsCard { get; set; }

            /// <summary>
            /// Gets or sets the financial payment detail.
            /// </summary>
            /// <value>
            /// The financial payment detail.
            /// </value>
            public FinancialPaymentDetail FinancialPaymentDetail { get; internal set; }
        }

        #endregion

        protected void dvpForeignCurrencyCode_SelectedIndexChanged( object sender, EventArgs e )
        {
            ForeignCurrencyCodeDefinedValueId = dvpForeignCurrencyCode.SelectedDefinedValueId ?? 0;
            BindAccounts();
            lblTotalAmount.Text = SelectedAccounts.Sum( f => f.Amount ).FormatAsCurrency( ForeignCurrencyCodeDefinedValueId );
        }

        /// <summary>
        /// Handles the SelectionChanged event of the btnFrequency control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
        protected void btnFrequency_SelectedIndexChanged( object sender, EventArgs e )
        {
            int oneTimeFrequencyId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME.AsGuid() ) ?? 0;
            int selectedScheduleFrequencyId = btnFrequency.SelectedValue.AsInteger();
            bool isOneTime = selectedScheduleFrequencyId == oneTimeFrequencyId;

            if ( isOneTime )
            {
                EndDate = dtpEndDate.SelectedDate;
                dtpEndDate.SelectedDate = null;
                dtpEndDate.Visible = false;
            }
            else if ( GetAttributeValue( AttributeKey.EnableEndDate ).AsBoolean() )
            {
                if (!dtpEndDate.SelectedDate.HasValue)
                {
                    dtpEndDate.SelectedDate = EndDate;
                }
                dtpEndDate.Visible = true;
            }
        }
    }
}