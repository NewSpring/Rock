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

using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Lava;
using Rock.Logging;
using Rock.Model;
using Rock.Web.Cache;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Rock.Utility
{
    /// <summary>
    /// Manages the import of well-known sample data sets into a Rock database.
    /// </summary>
    public class SampleDataManager
    {
        #region Constructors

        /// <summary>
        /// Create a new instance.
        /// </summary>
        public SampleDataManager() : this( ( ILogger ) null )
        {
        }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        public SampleDataManager( ILogger logger )
        {
            _taskLog = logger ?? new RockLoggerMemoryBuffer();
        }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        public SampleDataManager( ILavaEngine lavaEngine, ILogger logger = null )
        {
            _taskLog = logger ?? new RockLoggerMemoryBuffer();

            _lavaEngine = lavaEngine;
        }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        [Obsolete( "This is not used and will be removed in the future." )]
        [RockObsolete( "17.0" )]
        public SampleDataManager( IRockLogger logDevice )
            : this( ( ILogger ) null )
        {
        }

        #endregion

        #region Fields

        private SampleDataImportActionArgs _args = new SampleDataImportActionArgs();

        private readonly ILogger _taskLog;
        private readonly ILavaEngine _lavaEngine;

        /// <summary>
        /// Stopwatch used to measure time during certain operations.
        /// </summary>
        private readonly Stopwatch _stopwatch = new Stopwatch();

        /// <summary>
        /// StringBuilder is used to build the stopwatch trace for certain operations.
        /// </summary>
        private readonly StringBuilder _sb = new StringBuilder();

        /// <summary>
        /// Holds the Person Image binary file type.
        /// </summary>
        private readonly static BinaryFileType _personImageBinaryFileType = new BinaryFileTypeService( new RockContext() ).Get( Rock.SystemGuid.BinaryFiletype.PERSON_IMAGE.AsGuid() );

        /// <summary>
        /// Holds the Person Image binary file type.
        /// </summary>
        private readonly static BinaryFileType _checkImageBinaryFileType = new BinaryFileTypeService( new RockContext() ).Get( Rock.SystemGuid.BinaryFiletype.CONTRIBUTION_IMAGE.AsGuid() );

        /// <summary>
        /// The Person image binary file type settings
        /// </summary>
        private string _personImageBinaryFileTypeSettings = string.Empty;

        /// <summary>
        /// The check image binary file type settings
        /// </summary>
        private string _checkImageBinaryFileTypeSettings = string.Empty;

        /// <summary>
        /// The id for the "child" role of a family.
        /// </summary>
        private readonly static int _childRoleId = new GroupTypeRoleService( new RockContext() ).Get( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() ).Id;

        /// <summary>
        /// The id for the "adult" role of a family.
        /// </summary>
        private readonly static int _adultRoleId = new GroupTypeRoleService( new RockContext() ).Get( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ).Id;

        /// <summary>
        /// The Entity Type Id for the Person entities.
        /// </summary>
        private readonly static int _personEntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.Person ) ).Id;

        /// <summary>
        /// The Authentication Database entity type.
        /// </summary>
        private readonly static int _authenticationDatabaseEntityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.AUTHENTICATION_DATABASE.AsGuid() ).Id;

        /// <summary>
        /// Percent of additional time someone tends to NOT attend during the summer months (7-9)
        /// </summary>
        private readonly int summerPercentFactor = 30;

        /// <summary>
        /// A random number generator for use when calculating random attendance data and financial giving frequency (skipping).
        /// </summary>
        private static Random _random = new Random( ( int ) RockDateTime.Now.Ticks );

        /// <summary>
        /// The number of characters (length) that security codes should be.
        /// </summary>
        private readonly static int _securityCodeLength = 5;

        /// <summary>
        /// A little lookup list for finding a group/location appropriate for the child's attendance data
        /// </summary>
        private readonly static List<ClassGroupLocation> _classes = new List<ClassGroupLocation>
        {
            new ClassGroupLocation { GroupId = 25, LocationId = 4, MinAge = 0.0, MaxAge = 3.0, Name = "Nursery - Bunnies Room" },
            new ClassGroupLocation { GroupId = 26, LocationId = 5, MinAge = 0.0, MaxAge = 3.99, Name = "Crawlers/Walkers - Kittens Room" },
            new ClassGroupLocation { GroupId = 27, LocationId = 6, MinAge = 0.0, MaxAge = 5.99, Name = "Preschool - Puppies Room" },
            new ClassGroupLocation { GroupId = 28, LocationId = 7, MinAge = 4.75, MaxAge = 8.75, Name = "Grades K-1 - Bears Room" },
            new ClassGroupLocation { GroupId = 29, LocationId = 8, MinAge = 6.0, MaxAge = 10.99, Name = "Grades 2-3 - Bobcats Room" },
            new ClassGroupLocation { GroupId = 30, LocationId = 9, MinAge = 8.0, MaxAge = 13.99, Name = "Grades 4-6 - Outpost Room" },
            new ClassGroupLocation { GroupId = 31, LocationId = 10, MinAge = 12.0, MaxAge = 15.0, Name = "Grades 7-8 - Warehouse" },
            new ClassGroupLocation { GroupId = 32, LocationId = 11, MinAge = 13.0, MaxAge = 19.0, Name = "Grades 9-12 - Garage" },
        };

        /// <summary>
        /// Holds a cached copy of the "start time" DateTime for any scheduleIds this block encounters.
        /// </summary>
        private readonly Dictionary<int, DateTime> _scheduleTimes = new Dictionary<int, DateTime>();

        /// <summary>
        /// Holds a cached copy of the Id for each person Guid
        /// </summary>
        private readonly Dictionary<Guid, int> _peopleDictionary = new Dictionary<Guid, int>();

        /// <summary>
        /// Holds a cached copy of the Id for each group Guid
        /// </summary>
        private readonly Dictionary<Guid, int> _groupDictionary = new Dictionary<Guid, int>();

        /// <summary>
        /// Holds a cached copy of the attribute Id for each person Guid
        /// </summary>
        private readonly Dictionary<Guid, int> _peopleAliasDictionary = new Dictionary<Guid, int>();

        /// <summary>
        /// Holds a cache of the person object
        /// </summary>
        private readonly Dictionary<Guid, Person> _personCache = new Dictionary<Guid, Person>();

        /// <summary>
        /// Holds a cached copy of the location Id for each family Guid
        /// </summary>
        private readonly Dictionary<Guid, int> _familyLocationDictionary = new Dictionary<Guid, int>();

        /// <summary>
        /// A dictionary of a Person's login user names.
        /// </summary>
        private readonly Dictionary<Person, List<string>> _peopleLoginsDictionary = new Dictionary<Person, List<string>>();

        /// <summary>
        /// Holds the dictionary of person GUIDs and a dictionary of their attribute names
        /// and values from the family section of the XML.
        /// </summary>
        private readonly Dictionary<Guid, bool> _personWithAttributes = new Dictionary<Guid, bool>();

        /// <summary>
        /// Holds the dictionary contribution FinancialBatches for each week/date time.
        /// </summary>
        private readonly Dictionary<DateTime, FinancialBatch> _contributionBatches = new Dictionary<DateTime, FinancialBatch>();

        /// <summary>
        /// The contribution transaction type id
        /// </summary>
        private readonly static int _transactionTypeContributionId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION.AsGuid() ).Id;

        /// <summary>
        /// Magic kiosk Id used for attendance data.
        /// </summary>
        private readonly static int _kioskDeviceId = 2;

        /// <summary>
        /// The marital status DefinedType
        /// </summary>
        private DefinedTypeCache _maritalStatusDefinedType
        {
            get
            {
                return DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_MARITAL_STATUS.AsGuid() );
            }
        }

        /// <summary>
        /// The small group topic DefinedType
        /// </summary>
        private DefinedTypeCache _smallGroupTopicDefinedType
        {
            get
            {
                return DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.SMALL_GROUP_TOPIC.AsGuid() );
            }
        }

        /// <summary>
        /// The record status reason DefinedType
        /// </summary>
        private DefinedTypeCache _recordStatusReasonDefinedType
        {
            get
            {
                return DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS_REASON.AsGuid() );
            }
        }

        /// <summary>
        /// The suffix DefinedType
        /// </summary>
        private DefinedTypeCache _suffixDefinedType
        {
            get
            {
                return DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_SUFFIX.AsGuid() );
            }
        }

        #endregion Fields

        #region Public Properties and Methods

        /// <summary>
        /// The log device used to record processing details.
        /// </summary>
        [Obsolete( "This is not used and will be removed in the future." )]
        [RockObsolete( "17.0" )]
        public IRockLogger LogDevice => null;

        /// <summary>
        /// Gets the logger associated with this instance.
        /// </summary>
        /// <value>The logger.</value>
        public ILogger Logger => _taskLog;

        /// <summary>
        /// Flag to indicate if this is running as Unit Tests.
        /// When false, some things can be run in a separate thread (such as the RunNow job).
        /// </summary>
        public bool IsUnitTest { get; set; } = true;

        /// <summary>
        /// Process all the data in the XML file; deleting stuff and then adding stuff.
        /// as per https://github.com/SparkDevNetwork/Rock/wiki/z.-Rock-Solid-Demo-Church-Specification#wiki-xml-data
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="args"></param>
        public void CreateFromXmlDocumentFile( string filePath, SampleDataImportActionArgs args )
        {
            var xdoc = XDocument.Load( filePath );

            this.ProcessXmlDocument( xdoc, args );
        }

        /// <summary>
        /// Process all the data in the XML file; deleting stuff and then adding stuff.
        /// as per https://github.com/SparkDevNetwork/Rock/wiki/z.-Rock-Solid-Demo-Church-Specification#wiki-xml-data
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="args"></param>
        public void CreateFromXmlDocumentText( string xml, SampleDataImportActionArgs args )
        {
            var xdoc = XDocument.Parse( xml.ToStringSafe().Trim() );

            this.ProcessXmlDocument( xdoc, args );
        }

        private void VerifyEnvironment()
        {
            if ( _lavaEngine == null && LavaService.GetCurrentEngine() == null )
            {
                throw new Exception( "SampleDataManager processing failed. The LavaService must be initialized to process sample data correctly." );
            }
        }

        /// <summary>
        /// Resolves the merge fields using either the lava engine we were
        /// initialized with or the default environment engine.
        /// </summary>
        /// <param name="template">The template to be resolved.</param>
        /// <param name="mergeFields">The merge fields.</param>
        /// <returns>The rendered lava output.</returns>
        private string ResolveMergeFields( string template, Dictionary<string, object> mergeFields )
        {
            if ( _lavaEngine != null )
            {
                return _lavaEngine.RenderTemplate( template, new LavaDataDictionary( mergeFields ) ).Text;
            }
            else
            {
                return template.ResolveMergeFields( mergeFields );
            }
        }

        /// <summary>
        /// Process all the data in the XML file; deleting stuff and then adding stuff.
        /// as per https://github.com/SparkDevNetwork/Rock/wiki/z.-Rock-Solid-Demo-Church-Specification#wiki-xml-data
        /// </summary>
        /// <param name="xdoc"></param>
        /// <param name="args"></param>
        internal void ProcessXmlDocument( XDocument xdoc, SampleDataImportActionArgs args )
        {
            // Prior to processing, verify the environment.
            VerifyEnvironment();

            _args = args ?? new SampleDataImportActionArgs();

            // Re-seed the randomizer with the given seed if it's non-0.
            var seed = args.RandomizerSeed.GetValueOrDefault( 0 );
            if ( seed > 0 )
            {
                _random = new Random( seed );
            }

            var rockContext = GetConfiguredDataContext();
            try
            {
                rockContext.Configuration.AutoDetectChangesEnabled = false;

                var elemFamilies = xdoc.Element( "data" ).Element( "families" );
                var elemGroups = xdoc.Element( "data" ).Element( "groups" );
                var elemLocations = xdoc.Element( "data" ).Element( "locations" );
                var elemCampuses = xdoc.Element( "data" ).Element( "campuses" );
                var elemRelationships = xdoc.Element( "data" ).Element( "relationships" );
                var elemConnections = xdoc.Element( "data" ).Element( "connections" );
                var elemFollowing = xdoc.Element( "data" ).Element( "following" );
                var elemSecurityGroups = xdoc.Element( "data" ).Element( "securityRoles" );
                var elemRegistrationTemplates = xdoc.Element( "data" ).Element( "registrationTemplates" );
                var elemRegistrationInstances = xdoc.Element( "data" ).Element( "registrationInstances" );
                var financialGateways = xdoc.Element( "data" ).Element( "financialGateways" );

                // load studyTopics into DefinedType
                foreach ( var elemGroup in elemGroups.Elements( "group" ) )
                {
                    if ( elemGroup.Attribute( "studyTopic" ) != null )
                    {
                        var topic = elemGroup.Attribute( "studyTopic" ).Value;
                        DefinedValueCache smallGroupTopicDefinedValue = _smallGroupTopicDefinedType.DefinedValues.FirstOrDefault( a => a.Value == topic );

                        // add it as new if we didn't find it.
                        if ( smallGroupTopicDefinedValue == null )
                        {
                            smallGroupTopicDefinedValue = AddDefinedTypeValue( topic, _smallGroupTopicDefinedType );
                        }
                    }
                }

                // If DeleteDataFirst is enabled...
                // delete any sample data that might exist already
                // using RockContext in case there are multiple saves (like Attributes)
                rockContext.WrapTransaction( () =>
                {
                    _stopwatch.Start();
                    AppendFormat( "00:00.00 started <br/>" );

                    if ( _args.DeleteExistingData )
                    {
                        // Delete this stuff that might have people attached to it
                        DeleteRegistrationTemplates( elemRegistrationTemplates, rockContext );

                        // Now we'll clean up by deleting any previously created data such as
                        // families, addresses, people, photos, attendance data, etc.
                        DeleteExistingGroups( elemGroups, rockContext );
                        DeleteExistingFamilyData( elemFamilies, rockContext );

                        LogElapsed( "data deleted" );
                    }
                } );

                // make sure the database auth MEF component is initialized in case it hasn't done its first Load/Save Attributes yet (prevents possible lockup)
                var authenticationComponent = Rock.Security.AuthenticationContainer.GetComponent( EntityTypeCache.Get( _authenticationDatabaseEntityTypeId ).Name );

                // Import the sample data
                // using RockContext in case there are multiple saves (like Attributes)
                if ( _args.ProcessOnlyGivingData )
                {
                    rockContext.WrapTransaction( () =>
                    {
                        AddAllFamilyGiving( elemFamilies, rockContext );
                    } );
                }
                else
                {
                    rockContext.WrapTransaction( () =>
                    {
                        // Now we can add the families (and people) and then groups.... etc.
                        AddFamilies( elemFamilies, rockContext );
                        LogElapsed( "families added" );

                        AddRelationships( elemRelationships, rockContext );
                        LogElapsed( "relationships added" );

                        AddLocations( elemLocations, rockContext );
                        LogElapsed( "locations added" );

                        AddCampuses( elemCampuses, rockContext );
                        LogElapsed( "campuses added" );

                        AddGroups( elemGroups, rockContext );
                        LogElapsed( "groups added" );

                        AddConnections( elemConnections, rockContext );
                        LogElapsed( "people connection requests added" );

                        AddFollowing( elemFollowing, rockContext );
                        LogElapsed( "people following added" );

                        AddToSecurityGroups( elemSecurityGroups, rockContext );
                        LogElapsed( "people added to security roles" );

                        AddFinancialGateways( financialGateways, rockContext );
                        LogElapsed( "financialGateways added" );

                        AddRegistrationTemplates( elemRegistrationTemplates, rockContext );
                        LogElapsed( "registration templates added" );

                        AddRegistrationInstances( elemRegistrationInstances, rockContext );
                        LogElapsed( "registration instances added" );

                        rockContext.ChangeTracker.DetectChanges();
                        rockContext.SaveChanges( disablePrePostProcessing: true );
                        LogElapsed( "...changes saved" );

                        // Add Person Notes
                        AddPersonNotes( elemFamilies, rockContext );
                        rockContext.SaveChanges( disablePrePostProcessing: true );
                        LogElapsed( "notes added" );

                        // Add Person Previous LastNames
                        AddPeoplesPreviousNames( elemFamilies, rockContext );
                        rockContext.SaveChanges( disablePrePostProcessing: true );
                        LogElapsed( "previous names added" );
                    } );

                    // Add logins, but only if a password has been supplied.
                    // Note that this cannot be done in the same transaction as creating the Person records,
                    // or it will result in a database deadlock.
                    if ( !string.IsNullOrEmpty( _args.Password ) )
                    {
                        AddPersonLogins( rockContext );
                        LogElapsed( "person logins added" );
                    }
                }

                // since some PostSaveChanges was disabled, call these cleanup tasks
                using ( var personRockContext = new Rock.Data.RockContext() )
                {
                    // these should all be pretty quick, but just in case
                    personRockContext.Database.CommandTimeout = 180;
                    PersonService.UpdatePrimaryFamilyAll( personRockContext );
                    PersonService.UpdateGivingLeaderIdAll( personRockContext );
                    PersonService.UpdateGivingIdAll( personRockContext );
                    PersonService.UpdatePersonAgeClassificationAll( rockContext );
                    PersonService.UpdateBirthDateAll( personRockContext );

                    DbService.ExecuteCommand( @"UPDATE [PhoneNumber] SET [FullNumber] = CONCAT([CountryCode], [Number]) where [FullNumber] is null OR [FullNumber] != CONCAT([CountryCode], [Number])" );
                }

                // PA: Run the Update Persisted Attribute Value Job to populate the Field Type, like [ValueByDateTime], columns of the attributes table
                if ( IsUnitTest )
                {
                    var serviceJobService = new ServiceJobService( rockContext );
                    var updatePersistedAttributeValueJob = serviceJobService.Get( Rock.SystemGuid.ServiceJob.UPDATE_PERSISTED_ATTRIBUTE_VALUE );
                    serviceJobService.RunNow( updatePersistedAttributeValueJob );
                }
                else
                {
                    // Fire-and-forget in background to prevent blocking
                    Task.Run( () =>
                    {
                        using ( var backgroundContext = new RockContext() )
                        {
                            var serviceJobService = new ServiceJobService( backgroundContext );
                            var job = serviceJobService.Get( Rock.SystemGuid.ServiceJob.UPDATE_PERSISTED_ATTRIBUTE_VALUE );
                            serviceJobService.RunNow( job );
                        }
                    } );
                }

                // done.
                LogElapsed( "done" );

                if ( _args.EnableStopwatch )
                {
                    Logger.LogDebug( _sb.ToString() );
                }

                // Clear the static objects that contains all security roles and auth rules (so that it will be refreshed)
                foreach ( var role in RoleCache.AllRoles() )
                {
                    RoleCache.Remove( role.Id );
                }

                Rock.Security.Authorization.Clear();
            }
            finally
            {
                rockContext.Configuration.AutoDetectChangesEnabled = true;
            }
        }

        #endregion

        #region Private Properties and Methods

        /// <summary>
        /// Send the elapsed time and message to the output log.
        /// </summary>
        /// <param name="message"></param>
        private void LogElapsed( string message )
        {
            var ts = _stopwatch.Elapsed;
            AppendFormat( "{0:00}:{1:00}.{2:00} {3}<br/>", ts.Minutes, ts.Seconds, ts.Milliseconds / 10, message );
        }

        /// <summary>
        /// Append the formatted content to the client hub (log).
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        private void AppendFormat( string format, params object[] args )
        {
            if ( _args.EnableStopwatch )
            {
                var x = string.Format( format, args );
                _sb.Append( x );
                Logger.LogDebug( x );
            }
        }

        /// <summary>
        /// Adds any registration templates given in the XML file.
        /// </summary>
        /// <param name="elemRegistrationTemplates"></param>
        /// <param name="rockContext"></param>
        private void AddRegistrationTemplates( XElement elemRegistrationTemplates, RockContext rockContext )
        {
            if ( elemRegistrationTemplates == null )
            {
                return;
            }

            // Get attribute values from RegistrationTemplateDetail block
            // Get instance of the attribute.
            string defaultConfirmationEmail = _args.RegistrationConfirmationEmailTemplate;
            string defaultReminderEmail = _args.RegistrationReminderEmailTemplate;
            string defaultPaymentReminderEmail = _args.RegistrationPaymentReminderTemplate;
            string defaultSuccessText = _args.RegistrationSuccessText;

            RegistrationTemplateService registrationTemplateService = new RegistrationTemplateService( rockContext );
            RegistrationTemplatePlacementService registrationTemplatePlacementService = new RegistrationTemplatePlacementService( rockContext );

            // Add a template for each...
            foreach ( var element in elemRegistrationTemplates.Elements( "registrationTemplate" ) )
            {
                // skip any illegally formatted items
                if ( element.Attribute( "guid" ) == null )
                {
                    continue;
                }

                int categoryId = CategoryCache.Get( element.Attribute( "categoryGuid" ).Value.Trim().AsGuid() ).Id;

                // Find the group type and 
                var groupType = GroupTypeCache.Get( element.Attribute( "groupTypeGuid" ).Value.Trim().AsGuid() );

                RegistrantsSameFamily registrantsSameFamily;
                if ( element.Attribute( "registrantsInSameFamily" ) != null )
                {
                    Enum.TryParse( element.Attribute( "registrantsInSameFamily" ).Value.Trim(), out registrantsSameFamily );
                }
                else
                {
                    registrantsSameFamily = RegistrantsSameFamily.Ask;
                }

                bool setCostOnInstance = true;
                if ( element.Attribute( "setCostOn" ).Value.Trim() == "template" )
                {
                    setCostOnInstance = false;
                }

                RegistrationNotify notify = RegistrationNotify.None;
                RegistrationNotify matchNotify;
                foreach ( string item in element.Attribute( "notify" ).Value.SplitDelimitedValues( whitespace: false ) )
                {
                    if ( Enum.TryParse( item.Replace( " ", string.Empty ), out matchNotify ) )
                    {
                        notify = notify | matchNotify;
                    }
                }

                // Now find the matching financial gateway
                FinancialGatewayService financialGatewayService = new FinancialGatewayService( rockContext );
                string gatewayName = element.Attribute( "financialGateway" ) != null ? element.Attribute( "financialGateway" ).Value : "Test Gateway";
                var financialGateway = financialGatewayService.Queryable()
                    .Where( g => g.Name == gatewayName )
                    .FirstOrDefault();

                RegistrationTemplate registrationTemplate = new RegistrationTemplate()
                {
                    Guid = element.Attribute( "guid" ).Value.Trim().AsGuid(),
                    Name = element.Attribute( "name" ).Value.Trim(),
                    Description = element.Attribute( "description" ) != null ? element.Attribute( "description" ).Value.Trim() : string.Empty,
                    IsActive = true,
                    CategoryId = categoryId,
                    GroupTypeId = groupType.Id,
                    GroupMemberRoleId = groupType.DefaultGroupRoleId,
                    GroupMemberStatus = GroupMemberStatus.Active,
                    Notify = notify,
                    AddPersonNote = element.Attribute( "addPersonNote" ) != null ? element.Attribute( "addPersonNote" ).Value.AsBoolean() : false,
                    LoginRequired = element.Attribute( "loginRequired" ) != null ? element.Attribute( "loginRequired" ).Value.AsBoolean() : false,
                    AllowExternalRegistrationUpdates = element.Attribute( "allowExternalUpdatesToSavedRegistrations" ) != null ? element.Attribute( "allowExternalUpdatesToSavedRegistrations" ).Value.AsBoolean() : false,
                    ////AllowGroupPlacement = element.Attribute( "allowGroupPlacement" ) != null ? element.Attribute( "allowGroupPlacement" ).Value.AsBoolean() : false,
                    AllowMultipleRegistrants = element.Attribute( "allowMultipleRegistrants" ) != null ? element.Attribute( "allowMultipleRegistrants" ).Value.AsBoolean() : false,
                    MaxRegistrants = element.Attribute( "maxRegistrants" ).Value.AsInteger(),
                    RegistrantsSameFamily = registrantsSameFamily,
                    SetCostOnInstance = setCostOnInstance,
                    FinancialGatewayId = financialGateway.Id,
                    BatchNamePrefix = element.Attribute( "batchNamePrefix" ) != null ? element.Attribute( "batchNamePrefix" ).Value.Trim() : string.Empty,
                    Cost = element.Attribute( "cost" ).Value.AsDecimal(),
                    MinimumInitialPayment = element.Attribute( "minInitialPayment" ).Value.AsDecimal(),
                    RegistrationTerm = element.Attribute( "registrationTerm" ) != null ? element.Attribute( "registrationTerm" ).Value.Trim() : "Registration",
                    RegistrantTerm = element.Attribute( "registrantTerm" ) != null ? element.Attribute( "registrantTerm" ).Value.Trim() : "Registrant",
                    FeeTerm = element.Attribute( "feeTerm" ) != null ? element.Attribute( "feeTerm" ).Value.Trim() : "Additional Options",
                    DiscountCodeTerm = element.Attribute( "discountCodeTerm" ) != null ? element.Attribute( "discountCodeTerm" ).Value.Trim() : "Discount Code",
                    ConfirmationFromName = "{{ RegistrationInstance.ContactPersonAlias.Person.FullName }}",
                    ConfirmationFromEmail = "{{ RegistrationInstance.ContactEmail }}",
                    ConfirmationSubject = "{{ RegistrationInstance.Name }} Confirmation",
                    ConfirmationEmailTemplate = defaultConfirmationEmail,
                    ReminderFromName = "{{ RegistrationInstance.ContactPersonAlias.Person.FullName }}",
                    ReminderFromEmail = "{{ RegistrationInstance.ContactEmail }}",
                    ReminderSubject = "{{ RegistrationInstance.Name }} Reminder",
                    ReminderEmailTemplate = defaultReminderEmail,
                    SuccessTitle = "Congratulations {{ Registration.FirstName }}",
                    SuccessText = defaultSuccessText,
                    PaymentReminderEmailTemplate = defaultPaymentReminderEmail,
                    PaymentReminderFromEmail = "{{ RegistrationInstance.ContactEmail }}",
                    PaymentReminderFromName = "{{ RegistrationInstance.ContactPersonAlias.Person.FullName }}",
                    PaymentReminderSubject = "{{ RegistrationInstance.Name }} Payment Reminder",
                    PaymentReminderTimeSpan = element.Attribute( "paymentReminderTimeSpan" ) != null ? element.Attribute( "paymentReminderTimeSpan" ).Value.AsInteger() : 0,
                    CreatedDateTime = RockDateTime.Now,
                    ModifiedDateTime = RockDateTime.Now,
                    WaitListEnabled = GetBooleanValueSafe( element, "waitListEnabled" )
                };

                registrationTemplateService.Add( registrationTemplate );

                rockContext.SaveChanges();

                string name = element.Attribute( "name" ).Value.Trim();
                bool allowExternalUpdatesToSavedRegistrations = element.Attribute( "allowExternalUpdatesToSavedRegistrations" ).Value.AsBoolean();
                bool addPersonNote = element.Attribute( "addPersonNote" ).Value.AsBoolean();
                bool loginRequired = element.Attribute( "loginRequired" ).Value.AsBoolean();
                Guid guid = element.Attribute( "guid" ).Value.Trim().AsGuid();

                // Find any Form elements and add them to the template
                int formOrder = 0;
                var registrantAttributeQualifierColumn = "RegistrationTemplateId";
                int? registrationRegistrantEntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.RegistrationRegistrant ) ).Id;
                if ( element.Elements( "forms" ).Count() > 0 )
                {
                    foreach ( var formElement in element.Elements( "forms" ).Elements( "form" ) )
                    {
                        formOrder++;
                        var form = new RegistrationTemplateForm();
                        form.Guid = formElement.Attribute( "guid" ).Value.Trim().AsGuid();
                        registrationTemplate.Forms.Add( form );
                        form.Name = formElement.Attribute( "name" ).Value.Trim();
                        form.Order = formOrder;

                        int ffOrder = 0;
                        if ( formElement.Elements( "formFields" ).Count() > 0 )
                        {
                            foreach ( var formFieldElement in formElement.Elements( "formFields" ).Elements( "field" ) )
                            {
                                ffOrder++;
                                var formField = new RegistrationTemplateFormField();
                                formField.Guid = Guid.NewGuid();
                                formField.CreatedDateTime = RockDateTime.Now;

                                form.Fields.Add( formField );

                                switch ( formFieldElement.Attribute( "source" ).Value.Trim().ToLowerInvariant() )
                                {
                                    case "person field":
                                        formField.FieldSource = RegistrationFieldSource.PersonField;
                                        break;
                                    case "person attribute":
                                        formField.FieldSource = RegistrationFieldSource.PersonAttribute;
                                        break;
                                    case "group member attribute":
                                        formField.FieldSource = RegistrationFieldSource.GroupMemberAttribute;
                                        break;
                                    case "registrant attribute":
                                    case "registration attribute":
                                        // note this was renamed from 'registration attribute' to 'registrant attribute', but the sample data might still call it 'registration attribute'
                                        formField.FieldSource = RegistrationFieldSource.RegistrantAttribute;

                                        var attrState = new Rock.Model.Attribute();

                                        attrState.Guid = formFieldElement.Attribute( "guid" ).Value.AsGuid();
                                        attrState.Name = formFieldElement.Attribute( "name" ).Value.Trim();
                                        attrState.Key = attrState.Name.RemoveSpecialCharacters().Replace( " ", string.Empty );
                                        var type = formFieldElement.Attribute( "type" ).Value.Trim();
                                        var fieldType = FieldTypeCache.All().Where( f => f.Name == type ).FirstOrDefault();
                                        if ( fieldType != null )
                                        {
                                            attrState.FieldTypeId = fieldType.Id;
                                            var attribute = Helper.SaveAttributeEdits( attrState, registrationRegistrantEntityTypeId, registrantAttributeQualifierColumn, registrationTemplate.Id.ToString(), rockContext );

                                            rockContext.SaveChanges( disablePrePostProcessing: true );

                                            // update AttributeCache manually since saved changes with disablePrePostProcessing = true
                                            attribute.FieldTypeId = fieldType.Id;
                                            AttributeCache.Get( attribute );

                                            formField.Attribute = attribute;
                                        }
                                        else
                                        {
                                            throw new Exception( "Unable to find FieldType for attribute" );
                                        }

                                        break;
                                    default:
                                        throw new NotSupportedException( string.Format( "unknown form field source: {0}", formFieldElement.Attribute( "source" ).Value ) );
                                }

                                formField.AttributeId = null;
                                if ( !formField.AttributeId.HasValue &&
                                    formField.FieldSource == RegistrationFieldSource.RegistrantAttribute &&
                                    formField.Attribute != null )
                                {
                                    var attr = AttributeCache.Get( formField.Attribute.Guid, rockContext );
                                    if ( attr != null )
                                    {
                                        formField.AttributeId = attr.Id;
                                    }
                                }

                                RegistrationPersonFieldType registrationPersonFieldType;
                                if ( formField.FieldSource == RegistrationFieldSource.PersonField && formFieldElement.Attribute( "name" ) != null &&
                                    Enum.TryParse( formFieldElement.Attribute( "name" ).Value.Replace( " ", string.Empty ).Trim(), out registrationPersonFieldType ) )
                                {
                                    formField.PersonFieldType = registrationPersonFieldType;
                                }

                                if ( formField.FieldSource == RegistrationFieldSource.PersonAttribute && formFieldElement.Attribute( "attributeGuid" ) != null )
                                {
                                    var attributeGuid = formFieldElement.Attribute( "attributeGuid" ).Value.AsGuid();
                                    var attr = AttributeCache.Get( attributeGuid, rockContext );
                                    if ( attr != null )
                                    {
                                        formField.AttributeId = attr.Id;
                                    }
                                }

                                formField.IsInternal = formFieldElement.Attribute( "isInternal" ) != null ? formFieldElement.Attribute( "isInternal" ).Value.AsBoolean() : false;
                                formField.IsSharedValue = formFieldElement.Attribute( "isCommon" ) != null ? formFieldElement.Attribute( "isCommon" ).Value.AsBoolean() : false;
                                formField.ShowCurrentValue = formFieldElement.Attribute( "showCurrentValue" ) != null ? formFieldElement.Attribute( "showCurrentValue" ).Value.AsBoolean() : false;
                                formField.PreText = formFieldElement.Attribute( "preText" ) != null ? formFieldElement.Attribute( "preText" ).Value : string.Empty;
                                formField.PostText = formFieldElement.Attribute( "postText" ) != null ? formFieldElement.Attribute( "postText" ).Value : string.Empty;
                                formField.IsGridField = formFieldElement.Attribute( "showOnGrid" ) != null ? formFieldElement.Attribute( "showOnGrid" ).Value.AsBoolean() : false;
                                formField.ShowOnWaitlist = formFieldElement.Attribute( "showOnWaitList" ) != null ? formFieldElement.Attribute( "showOnWaitList" ).Value.AsBoolean() : false;
                                formField.IsRequired = formFieldElement.Attribute( "isRequired" ) != null ? formFieldElement.Attribute( "isRequired" ).Value.AsBoolean() : false;
                                formField.Order = ffOrder;
                                formField.CreatedDateTime = RockDateTime.Now;
                            }
                        }
                    }
                }

                // Discounts
                int discountOrder = 0;
                if ( element.Elements( "discounts" ) != null )
                {
                    foreach ( var discountElement in element.Elements( "discounts" ).Elements( "discount" ) )
                    {
                        discountOrder++;
                        var discount = new RegistrationTemplateDiscount();
                        discount.Guid = Guid.NewGuid();
                        discount.MaxRegistrants = GetNullableIntegerValueSafe( discountElement, "maxRegistrants" );
                        discount.MaxUsage = GetNullableIntegerValueSafe( discountElement, "maxUsage" );
                        discount.MinRegistrants = GetNullableIntegerValueSafe( discountElement, "minRegistrants" );
                        registrationTemplate.Discounts.Add( discount );

                        discount.Code = discountElement.Attribute( "code" ).Value;

                        switch ( discountElement.Attribute( "type" ).Value.Trim().ToLowerInvariant() )
                        {
                            case "percentage":
                                discount.DiscountPercentage = discountElement.Attribute( "value" ).Value.Trim().AsDecimal() * 0.01m;
                                discount.DiscountAmount = 0.0m;
                                break;
                            case "amount":
                                discount.DiscountPercentage = 0.0m;
                                discount.DiscountAmount = discountElement.Attribute( "value" ).Value.Trim().AsDecimal();
                                break;
                            default:
                                throw new NotSupportedException( string.Format( "unknown discount type: {0}", discountElement.Attribute( "type" ).Value ) );
                        }

                        discount.Order = discountOrder;
                    }
                }

                // Fees
                int feeOrder = 0;
                if ( element.Elements( "fees" ) != null )
                {
                    foreach ( var feeElement in element.Elements( "fees" ).Elements( "fee" ) )
                    {
                        feeOrder++;
                        var fee = new RegistrationTemplateFee();
                        fee.Guid = Guid.NewGuid();
                        fee.Name = feeElement.Attribute( "name" ).Value.Trim();
                        fee.DiscountApplies = GetBooleanValueSafe( feeElement, "discountApplies" );
                        fee.IsRequired = GetBooleanValueSafe( feeElement, "isRequired" );
                        fee.HideWhenNoneRemaining = GetBooleanValueSafe( feeElement, "hideWhenNoneRemaining" );
                        fee.IsActive = GetBooleanValueSafe( feeElement, "isActive", true );
                        registrationTemplate.Fees.Add( fee );

                        switch ( feeElement.Attribute( "type" ).Value.Trim().ToLowerInvariant() )
                        {
                            case "multiple":
                                fee.FeeType = RegistrationFeeType.Multiple;
                                fee.FeeItems = new List<RegistrationTemplateFeeItem>();
                                foreach ( XElement option in feeElement.Elements( "option" ) )
                                {
                                    fee.FeeItems.Add( new RegistrationTemplateFeeItem
                                    {
                                        Name = option.Attribute( "name" ).Value,
                                        Cost = option.Attribute( "cost" ).Value.AsDecimal()
                                    } );
                                }

                                break;

                            case "single":
                                fee.FeeType = RegistrationFeeType.Single;
                                fee.FeeItems = new List<RegistrationTemplateFeeItem>();
                                fee.FeeItems.Add( new RegistrationTemplateFeeItem
                                {
                                    Name = fee.Name,
                                    Cost = feeElement.Attribute( "cost" ).Value.AsDecimal()
                                } );

                                break;

                            default:
                                throw new NotSupportedException( string.Format( "unknown fee type: {0}", feeElement.Attribute( "type" ).Value ) );
                        }

                        fee.DiscountApplies = feeElement.Attribute( "discountApplies" ).Value.AsBoolean();
                        fee.AllowMultiple = feeElement.Attribute( "enableQuantity" ).Value.AsBoolean();
                        fee.Order = feeOrder;
                    }
                }

                // placement groups
                int placementGroupOrder = 0;
                if ( element.Elements( "placementGroups" ) != null )
                {
                    foreach ( var placemenetGroupElement in element.Elements( "placementGroups" ).Elements( "placementGroup" ) )
                    {
                        placementGroupOrder++;

                        // Find the group type and 
                        var placementGroupType = GroupTypeCache.Get( placemenetGroupElement.Attribute( "groupTypeGuid" ).Value.Trim().AsGuid() );

                        var registrationTemplatePlacement = new RegistrationTemplatePlacement();
                        registrationTemplatePlacement.Guid = Guid.NewGuid();
                        registrationTemplatePlacement.RegistrationTemplateId = registrationTemplate.Id;
                        registrationTemplatePlacementService.Add( registrationTemplatePlacement );
                        registrationTemplatePlacement.Name = placemenetGroupElement.Attribute( "name" ).Value;
                        registrationTemplatePlacement.Order = placementGroupOrder;
                        registrationTemplatePlacement.GroupTypeId = placementGroupType.Id;
                        registrationTemplatePlacement.IconCssClass = placemenetGroupElement.Attribute( "iconCssClass" ).Value;
                        registrationTemplatePlacement.AllowMultiplePlacements = placemenetGroupElement.Attribute( "allowMultiple" ).Value.AsBoolean();
                        rockContext.SaveChanges( disablePrePostProcessing: true );

                        if ( placemenetGroupElement.Elements( "sharedGroups" ) != null )
                        {
                            var sharedPlacementGroupGuids = new List<Guid>();
                            foreach ( var sharedGroupElement in placemenetGroupElement.Elements( "sharedGroups" ).Elements( "sharedGroup" ) )
                            {
                                sharedPlacementGroupGuids.Add( sharedGroupElement.Attribute( "groupGuid" ).Value.Trim().AsGuid() );
                            }

                            if ( sharedPlacementGroupGuids.Any() )
                            {
                                GroupService groupService = new GroupService( rockContext );
                                var sharedPlacementGroups = groupService.GetByGuids( sharedPlacementGroupGuids ).ToList();
                                registrationTemplatePlacementService.SetRegistrationTemplatePlacementPlacementGroups( registrationTemplatePlacement, sharedPlacementGroups );
                                rockContext.SaveChanges( disablePrePostProcessing: true );
                            }
                        }
                    }
                }

                if ( element.Elements( "registrationAttributes" ) != null )
                {
                    foreach ( var registrationAttribute in element.Elements( "registrationAttributes" ).Elements( "registrationAttribute" ) )
                    {
                        var type = registrationAttribute.Attribute( "type" ).Value.Trim();
                        var fieldType = FieldTypeCache.All().FirstOrDefault( f => f.Name == type );
                        var categoryGuids = registrationAttribute.Attribute( "categoryGuids" ) != null ? registrationAttribute.Attribute( "categoryGuids" ).Value.Trim().SplitDelimitedValues( "," ) : new string[] { };
                        var attributeState = new Rock.Model.Attribute()
                        {
                            Name = registrationAttribute.Attribute( "name" )?.Value,
                            Guid = registrationAttribute.Attribute( "guid" ).Value.AsGuid(),
                            FieldTypeId = fieldType.Id
                        };

                        attributeState.Key = attributeState.Name.RemoveSpecialCharacters().Replace( " ", string.Empty );

                        new CategoryService( new RockContext() ).Queryable().Where( c => categoryGuids.Contains( c.Guid.ToString() ) ).ToList().ForEach( c => attributeState.Categories.Add( c ) );

                        var attribute = Helper.SaveAttributeEdits( attributeState, new Registration().TypeId, "RegistrationTemplateId", registrationTemplate.Id.ToString(), rockContext );

                        rockContext.SaveChanges( disablePrePostProcessing: true );

                        // update AttributeCache manually since saved changes with disablePrePostProcessing = true
                        attribute.FieldTypeId = fieldType.Id;
                        AttributeCache.Get( attribute );
                    }
                }


            }
        }

        /// <summary>
        /// Adds any registration instances given in the XML file.
        /// </summary>
        /// <param name="elemRegistrationInstances"></param>
        /// <param name="rockContext"></param>
        private void AddRegistrationInstances( XElement elemRegistrationInstances, RockContext rockContext )
        {
            if ( elemRegistrationInstances == null )
            {
                return;
            }

            foreach ( var element in elemRegistrationInstances.Elements( "registrationInstance" ) )
            {
                // skip any illegally formatted items
                if ( element.Attribute( "templateGuid" ) == null )
                {
                    continue;
                }

                // Now find the matching registration template
                RegistrationInstanceService registrationInstanceService = new RegistrationInstanceService( rockContext );

                RegistrationTemplateService registrationTemplateService = new RegistrationTemplateService( rockContext );
                Guid templateGuid = element.Attribute( "templateGuid" ).Value.AsGuid();
                var registrationTemplate = registrationTemplateService.Queryable()
                    .Where( g => g.Guid == templateGuid )
                    .FirstOrDefault();

                if ( registrationTemplate == null )
                {
                    throw new NotSupportedException( string.Format( "unknown registration template: {0}", templateGuid ) );
                }

                // Merge lava fields
                // LAVA additionalReminderDetails
                var mergeFields = new Dictionary<string, object>();
                DateTime? registrationStartsDate = null;
                DateTime? registrationEndsDate = null;
                DateTime? sendReminderDate = null;
                var additionalReminderDetails = string.Empty;
                var additionalConfirmationDetails = string.Empty;

                if ( element.Attribute( "registrationStarts" ) != null )
                {
                    var renderResult = ResolveMergeFields( element.Attribute( "registrationStarts" ).Value, mergeFields );
                    registrationStartsDate = DateTime.Parse( renderResult );
                }

                if ( element.Attribute( "registrationEnds" ) != null )
                {
                    var renderResult = ResolveMergeFields( element.Attribute( "registrationEnds" ).Value, mergeFields );
                    registrationEndsDate = DateTime.Parse( renderResult );
                }

                if ( element.Attribute( "sendReminderDate" ) != null )
                {
                    var renderResult = ResolveMergeFields( element.Attribute( "sendReminderDate" ).Value, mergeFields );
                    sendReminderDate = DateTime.Parse( renderResult );
                }

                if ( element.Attribute( "additionalReminderDetails" ) != null )
                {
                    var renderResult = ResolveMergeFields( element.Attribute( "additionalReminderDetails" ).Value, mergeFields );
                    additionalReminderDetails = renderResult;
                }

                if ( element.Attribute( "additionalConfirmationDetails" ) != null )
                {
                    var renderResult = ResolveMergeFields( element.Attribute( "additionalConfirmationDetails" ).Value, mergeFields );
                    additionalConfirmationDetails = renderResult;
                }

                // Get the contact info
                int? contactPersonAliasId = null;
                if ( element.Attribute( "contactPersonGuid" ) != null )
                {
                    var guid = element.Attribute( "contactPersonGuid" ).Value.AsGuid();
                    if ( _peopleAliasDictionary.ContainsKey( guid ) )
                    {
                        contactPersonAliasId = _peopleAliasDictionary[element.Attribute( "contactPersonGuid" ).Value.AsGuid()];
                    }
                }

                // Find the matching account
                FinancialAccountService financialGatewayService = new FinancialAccountService( rockContext );
                string accountName = element.Attribute( "account" ) != null ? element.Attribute( "account" ).Value : string.Empty;
                var account = financialGatewayService.Queryable()
                    .Where( g => g.Name == accountName )
                    .FirstOrDefault();

                RegistrationInstance registrationInstance = new RegistrationInstance()
                {
                    Guid = ( element.Attribute( "guid" ) != null ) ? element.Attribute( "guid" ).Value.Trim().AsGuid() : Guid.NewGuid(),
                    Name = ( element.Attribute( "name" ) != null ) ? element.Attribute( "name" ).Value.Trim() : "New " + registrationTemplate.Name,
                    IsActive = true,
                    RegistrationTemplateId = registrationTemplate.Id,
                    StartDateTime = registrationStartsDate,
                    EndDateTime = registrationEndsDate,
                    MaxAttendees = element.Attribute( "maxAttendees" ) != null ? element.Attribute( "maxAttendees" ).Value.AsInteger() : 0,
                    SendReminderDateTime = sendReminderDate,
                    ContactPersonAliasId = contactPersonAliasId,
                    ContactPhone = element.Attribute( "contactPhone" ) != null ? element.Attribute( "contactPhone" ).Value : string.Empty,
                    ContactEmail = element.Attribute( "contactEmail" ) != null ? element.Attribute( "contactEmail" ).Value : string.Empty,
                    AccountId = ( account != null ) ? ( int? ) account.Id : null,
                    AdditionalReminderDetails = HttpUtility.HtmlDecode( additionalReminderDetails ),
                    AdditionalConfirmationDetails = HttpUtility.HtmlDecode( additionalConfirmationDetails ),
                    CreatedDateTime = RockDateTime.Now,
                    ModifiedDateTime = RockDateTime.Now,
                };

                registrationInstanceService.Add( registrationInstance );
            }
        }

        private bool GetBooleanValueSafe( XElement element, string name, bool defaultValue = false)
        {
            return  element?.Attribute( name )?.Value?.AsBoolean() ?? defaultValue;
        }

        private int? GetNullableIntegerValueSafe( XElement element, string name )
        {
            return element.Attribute( name ) != null ? element.Attribute( name ).Value.AsIntegerOrNull() : null;
        }

        /// <summary>
        /// Adds any notes for any people given in the XML file.
        /// </summary>
        /// <param name="elemFamilies"></param>
        /// <param name="rockContext"></param>
        private void AddPersonNotes( XElement elemFamilies, RockContext rockContext )
        {
            var peopleWithNotes = from n in elemFamilies.Elements( "family" ).Elements( "members" ).Elements( "person" ).Elements( "notes" ).Elements( "note" )
                                  select new
                                  {
                                      PersonGuid = n.Parent.Parent.Attribute( "guid" ).Value,
                                      Type = n.Attribute( "type" ).Value,
                                      Text = n.Attribute( "text" ).Value,
                                      IsPrivate = n.Attribute( "isPrivate" ) != null ? n.Attribute( "isPrivate" ).Value : "false",
                                      IsAlert = n.Attribute( "isAlert" ) != null ? n.Attribute( "isAlert" ).Value : "false",
                                      ByPersonGuid = n.Attribute( "byGuid" ) != null ? n.Attribute( "byGuid" ).Value : null,
                                      Date = n.Attribute( "date" ) != null ? n.Attribute( "date" ).Value : null
                                  };

            foreach ( var r in peopleWithNotes )
            {
                int personId = _peopleDictionary[r.PersonGuid.AsGuid()];
                AddNote( personId, r.Type, r.Text, r.Date, r.ByPersonGuid, r.IsPrivate, r.IsAlert, rockContext );
            }
        }

        /// <summary>
        /// Adds the peoples previous names.
        /// </summary>
        /// <param name="elemFamilies">The elem families.</param>
        /// <param name="rockContext">The rock context.</param>
        private void AddPeoplesPreviousNames( XElement elemFamilies, RockContext rockContext )
        {
            var previousNames = from n in elemFamilies.Elements( "family" ).Elements( "members" ).Elements( "person" ).Elements( "previousNames" ).Elements( "name" )
                                select new
                                {
                                    PersonGuid = n.Parent.Parent.Attribute( "guid" ).Value,
                                    LastName = n.Attribute( "lastName" ).Value,
                                };

            foreach ( var r in previousNames )
            {
                int personId = _peopleDictionary[r.PersonGuid.AsGuid()];
                int personAliasId = _peopleAliasDictionary[r.PersonGuid.AsGuid()];
                AddPreviousName( personAliasId, r.LastName, rockContext );
            }
        }

        /// <summary>
        /// Adds the name of the previous.
        /// </summary>
        /// <param name="personAliasId">The person alias identifier.</param>
        /// <param name="previousLastName">Last name of the previous.</param>
        /// <param name="rockContext">The rock context.</param>
        private void AddPreviousName( int personAliasId, string previousLastName, RockContext rockContext )
        {
            var personPreviousNameService = new PersonPreviousNameService( rockContext );
            var previousName = new PersonPreviousName()
            {
                LastName = previousLastName,
                PersonAliasId = personAliasId
            };
            personPreviousNameService.Add( previousName );
        }

        /// <summary>
        /// Adds a KnownRelationship record between the two supplied GUIDs with the given 'is' relationship type:
        ///     
        ///     Role / inverse Role
        ///     ================================
        ///     step-parent     / step-child
        ///     grandparent     / grandchild
        ///     previous-spouse / previous-spouse
        ///     can-check-in    / allow-check-in-by
        ///     parent          / child
        ///     sibling         / sibling
        ///     invited         / invited-by
        ///     related         / related
        ///     
        /// ...for XML such as:
        /// <relationships>
        ///     <relationship a="Ben" personGuid="3C402382-3BD2-4337-A996-9E62F1BAB09D"
        ///     has="step-parent" forGuid="3D7F6605-3666-4AB5-9F4E-D7FEBF93278E" name="Brian" />
        ///  </relationships>
        ///  
        /// </summary>
        /// <param name="elemRelationships"></param>
        /// <param name="rockContext">The rock context.</param>
        private void AddRelationships( XElement elemRelationships, RockContext rockContext )
        {
            if ( elemRelationships == null )
            {
                return;
            }

            Guid ownerRoleGuid = Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_OWNER.AsGuid();
            Guid knownRelationshipsGroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS.AsGuid();
            var memberService = new GroupMemberService( rockContext );

            var groupTypeRoles = GroupTypeCache.Get( knownRelationshipsGroupTypeGuid, rockContext ).Roles;

            //// We have to create (or fetch existing) two groups for each relationship, adding the
            //// other person as a member of that group with the appropriate GroupTypeRole (GTR):
            ////   * a group with person as owner (GTR) and forPerson as type/role (GTR) 
            ////   * a group with forPerson as owner (GTR) and person as inverse-type/role (GTR)

            foreach ( var elemRelationship in elemRelationships.Elements( "relationship" ) )
            {
                // skip any illegally formatted items
                if ( elemRelationship.Attribute( "personGuid" ) == null || elemRelationship.Attribute( "forGuid" ) == null ||
                    elemRelationship.Attribute( "has" ) == null )
                {
                    continue;
                }

                Guid personGuid = elemRelationship.Attribute( "personGuid" ).Value.Trim().AsGuid();
                Guid forGuid = elemRelationship.Attribute( "forGuid" ).Value.Trim().AsGuid();
                int ownerPersonId = _peopleDictionary[personGuid];
                int forPersonId = _peopleDictionary[forGuid];

                string relationshipType = elemRelationship.Attribute( "has" ).Value.Trim();

                int roleId = -1;

                switch ( relationshipType )
                {
                    case "step-parent":
                        roleId = groupTypeRoles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_STEP_PARENT.AsGuid() )
                            .Select( r => r.Id ).FirstOrDefault();
                        break;

                    case "step-child":
                        roleId = groupTypeRoles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_STEP_CHILD.AsGuid() )
                            .Select( r => r.Id ).FirstOrDefault();
                        break;

                    case "can-check-in":
                        roleId = groupTypeRoles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_CAN_CHECK_IN.AsGuid() )
                            .Select( r => r.Id ).FirstOrDefault();
                        break;

                    case "allow-check-in-by":
                        roleId = groupTypeRoles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_ALLOW_CHECK_IN_BY.AsGuid() )
                            .Select( r => r.Id ).FirstOrDefault();
                        break;

                    case "grandparent":
                        roleId = groupTypeRoles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_GRANDPARENT.AsGuid() )
                            .Select( r => r.Id ).FirstOrDefault();
                        break;

                    case "grandchild":
                        roleId = groupTypeRoles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_GRANDCHILD.AsGuid() )
                            .Select( r => r.Id ).FirstOrDefault();
                        break;

                    case "invited":
                        roleId = groupTypeRoles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_INVITED.AsGuid() )
                            .Select( r => r.Id ).FirstOrDefault();
                        break;

                    case "invited-by":
                        roleId = groupTypeRoles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_INVITED_BY.AsGuid() )
                            .Select( r => r.Id ).FirstOrDefault();
                        break;

                    case "previous-spouse":
                        roleId = groupTypeRoles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_PREVIOUS_SPOUSE.AsGuid() )
                            .Select( r => r.Id ).FirstOrDefault();
                        break;

                    case "sibling":
                        roleId = groupTypeRoles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_SIBLING.AsGuid() )
                            .Select( r => r.Id ).FirstOrDefault();
                        break;

                    case "parent":
                        roleId = groupTypeRoles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_PARENT.AsGuid() )
                            .Select( r => r.Id ).FirstOrDefault();
                        break;

                    case "child":
                        roleId = groupTypeRoles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_CHILD.AsGuid() )
                            .Select( r => r.Id ).FirstOrDefault();
                        break;

                    case "related":
                        roleId = groupTypeRoles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_PEER_NETWORK_RELATED.AsGuid() )
                            .Select( r => r.Id ).FirstOrDefault();
                        break;

                    case "business":
                        roleId = groupTypeRoles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_BUSINESS.AsGuid() )
                            .Select( r => r.Id ).FirstOrDefault();
                        break;

                    default:
                        //// throw new NotSupportedException( string.Format( "unknown relationship type {0}", elemRelationship.Attribute( "has" ).Value ) );
                        // just skip unknown relationship types
                        continue;
                }

                // find the person's KnownRelationship "owner" group
                var knownRelationshipGroup = memberService.Queryable()
                    .Where( m =>
                        m.PersonId == ownerPersonId &&
                        m.GroupRole.Guid == ownerRoleGuid )
                    .Select( m => m.Group )
                    .FirstOrDefault();

                // create it if it does not yet exist
                if ( knownRelationshipGroup == null )
                {
                    var ownerRole = new GroupTypeRoleService( rockContext ).Get( ownerRoleGuid );
                    if ( ownerRole != null && ownerRole.GroupTypeId.HasValue )
                    {
                        var ownerGroupMember = new GroupMember();
                        ownerGroupMember.PersonId = ownerPersonId;
                        ownerGroupMember.GroupRoleId = ownerRole.Id;
                        ownerGroupMember.GroupTypeId = ownerRole.GroupTypeId.Value;

                        knownRelationshipGroup = new Group();
                        knownRelationshipGroup.Name = ownerRole.GroupType.Name;
                        knownRelationshipGroup.GroupTypeId = ownerRole.GroupTypeId.Value;
                        knownRelationshipGroup.Members.Add( ownerGroupMember );

                        var groupService = new GroupService( rockContext );
                        groupService.Add( knownRelationshipGroup );

                        rockContext.SaveChanges( disablePrePostProcessing: true );

                        knownRelationshipGroup = groupService.Get( knownRelationshipGroup.Id );
                    }
                }

                // Now find (and add if not found) the forPerson as a member with the "has" role-type
                var groupMember = memberService.Queryable()
                    .Where( m =>
                        m.GroupId == knownRelationshipGroup.Id &&
                        m.PersonId == forPersonId &&
                        m.GroupRoleId == roleId )
                    .FirstOrDefault();

                if ( groupMember == null )
                {
                    groupMember = new GroupMember()
                    {
                        GroupId = knownRelationshipGroup.Id,
                        PersonId = forPersonId,
                        GroupRoleId = roleId,
                        GroupTypeId = knownRelationshipGroup.GroupTypeId
                    };

                    new GroupMemberService( rockContext ).Add( groupMember );
                }

                // Now create thee inverse relationship.
                //
                // (NOTE: Don't panic if your VS tooling complains that there is
                // an unused variable here.  There is no need to do anything with the
                // inverseGroupMember relationship because it was already added to the
                // context.  All we have to do below is save the changes to the context
                // when we're ready.)
                var inverseGroupMember = memberService.GetInverseRelationship( groupMember, createGroup: true );
            }
        }

        /// <summary>
        /// Handles adding families from the given XML element snippet
        /// </summary>
        /// <param name="elemFamilies">The XML element containing all the families.</param>
        /// <param name="rockContext">The rock context.</param>
        private void AddFamilies( XElement elemFamilies, RockContext rockContext )
        {
            if ( elemFamilies == null )
            {
                return;
            }

            LogElapsed( "adding families..." );

            // Persist the storage type's settings specific to the photo binary file type
            var settings = new Dictionary<string, string>();
            if ( _personImageBinaryFileType.Attributes == null )
            {
                _personImageBinaryFileType.LoadAttributes();
            }

            foreach ( var attributeValue in _personImageBinaryFileType.AttributeValues )
            {
                settings.Add( attributeValue.Key, attributeValue.Value.Value );
            }

            _personImageBinaryFileTypeSettings = settings.ToJson();

            GroupService groupService = new GroupService( rockContext );
            var attributeValueService = new AttributeValueService( rockContext );

            List<Group> allGroups = new List<Group>();
            var attendanceData = new Dictionary<Guid, List<Attendance>>();

            // Next create the family along with its members and related data
            foreach ( var elemFamily in elemFamilies.Elements( "family" ) )
            {
                Guid guid = elemFamily.Attribute( "guid" ).Value.Trim().AsGuid();
                var familyMembers = BuildFamilyMembersFromXml( elemFamily.Element( "members" ), rockContext );

                // Call replica of groupService's SaveNewFamily method in an attempt to speed things up
                Group family = CreateNewFamily( familyMembers, campusId: 1 );
                family.Guid = guid;

                // add the family to the context's list of groups
                groupService.Add( family );

                // add the families address(es)
                AddFamilyAddresses( groupService, family, elemFamily.Element( "addresses" ), rockContext );

                // add their attendance data
                if ( _args.FabricateAttendance )
                {
                    AddFamilyAttendance( family, elemFamily, rockContext, attendanceData );
                }

                allGroups.Add( family );

                _stopwatch.Stop();
                AppendFormat( "{0:00}:{1:00}.{2:00} added {3}<br/>", _stopwatch.Elapsed.Minutes, _stopwatch.Elapsed.Seconds, _stopwatch.Elapsed.Milliseconds / 10, family.Name );
                _stopwatch.Start();
            }

            rockContext.ChangeTracker.DetectChanges();
            rockContext.SaveChanges( disablePrePostProcessing: true );

            // Now save each person's attribute values (who had them defined in the XML)
            // and add each person's ID to a dictionary for use later.
            _stopwatch.Stop();
            AppendFormat( "{0:00}:{1:00}.{2:00} saving attributes for everyone...<br/>", _stopwatch.Elapsed.Minutes, _stopwatch.Elapsed.Seconds, _stopwatch.Elapsed.Milliseconds / 10 );
            _stopwatch.Start();

            foreach ( var gm in allGroups.SelectMany( g => g.Members ) )
            {
                // Put the person's id into the people dictionary for later use.
                if ( !_peopleDictionary.ContainsKey( gm.Person.Guid ) )
                {
                    _peopleDictionary.Add( gm.Person.Guid, gm.Person.Id );
                }

                // Only save if the person had attributes, otherwise it will error.
                if ( _personWithAttributes.ContainsKey( gm.Person.Guid ) )
                {
                    foreach ( var attributeCache in gm.Person.Attributes.Select( a => a.Value ) )
                    {
                        var newValue = gm.Person.AttributeValues[attributeCache.Key];
                        if ( newValue != null )
                        {
                            var attributeValue = new AttributeValue();
                            attributeValue.AttributeId = newValue.AttributeId;
                            attributeValue.EntityId = gm.Person.Id;
                            attributeValue.Value = newValue.Value;
                            // PA: setting the dirty bit so that the Update Persisted Attribute Values job can populate the field related columns like ValueAsDateTime
                            attributeValue.IsPersistedValueDirty = true;
                            attributeValueService.Add( attributeValue );
                        }
                    }
                }
            }

            rockContext.ChangeTracker.DetectChanges();
            rockContext.SaveChanges( disablePrePostProcessing: true );

            _stopwatch.Stop();
            AppendFormat( "{0:00}:{1:00}.{2:00} attributes saved<br/>", _stopwatch.Elapsed.Minutes, _stopwatch.Elapsed.Seconds, _stopwatch.Elapsed.Milliseconds / 10 );
            _stopwatch.Start();

            // Create person alias records for each person manually since we set disablePrePostProcessing=true on save
            PersonService personService = new PersonService( rockContext );
            foreach ( var person in personService.Queryable( true ).Include( a => a.Aliases )
                .Where( p =>
                    _peopleDictionary.Keys.Contains( p.Guid ) &&
                    !p.Aliases.Any() ) )
            {
                person.Aliases.Add( new PersonAlias { AliasPersonId = person.Id, AliasPersonGuid = person.Guid } );
            }

            rockContext.ChangeTracker.DetectChanges();
            rockContext.SaveChanges( disablePrePostProcessing: true );

            _stopwatch.Stop();
            AppendFormat( "{0:00}:{1:00}.{2:00} added person aliases<br/>", _stopwatch.Elapsed.Minutes, _stopwatch.Elapsed.Seconds, _stopwatch.Elapsed.Milliseconds / 10 );
            _stopwatch.Start();

            // Put the person alias ids into the people alias dictionary for later use.
            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            foreach ( var personAlias in personAliasService.Queryable( "Person" )
                .Where( a =>
                    _peopleDictionary.Keys.Contains( a.Person.Guid ) &&
                    a.PersonId == a.AliasPersonId ) )
            {
                _peopleAliasDictionary.Add( personAlias.Person.Guid, personAlias.Id );
            }

            // Now that person aliases have been saved, save the attendance records
            var attendanceService = new AttendanceService( rockContext );
            var attendanceGuids = attendanceData.Select( a => a.Key ).ToList();
            foreach ( var aliasKeyValue in _peopleAliasDictionary
                .Where( a => attendanceGuids.Contains( a.Key ) ) )
            {
                foreach ( var attendance in attendanceData[aliasKeyValue.Key] )
                {
                    attendance.PersonAliasId = aliasKeyValue.Value;

                    // make sure the attendance has a unique guid
                    attendance.Guid = Guid.NewGuid();

                    attendanceService.Add( attendance );
                }
            }

            rockContext.ChangeTracker.DetectChanges();
            rockContext.SaveChanges( disablePrePostProcessing: true );

            _stopwatch.Stop();
            AppendFormat( "{0:00}:{1:00}.{2:00} added attendance records<br/>", _stopwatch.Elapsed.Minutes, _stopwatch.Elapsed.Seconds, _stopwatch.Elapsed.Milliseconds / 10 );
            _stopwatch.Start();

            // Now re-process the family section looking for any giving data.
            // We do this last because we need the personAliases that were just added.
            // Persist the storage type's settings specific to the contribution binary file type
            AddAllFamilyGiving( elemFamilies, rockContext );

            rockContext.ChangeTracker.DetectChanges();
            rockContext.SaveChanges( disablePrePostProcessing: true );
        }

        /// <summary>
        /// This method will add all family giving data.  It can be used stand-alone to only process/add giving data.
        /// </summary>
        /// <param name="elemFamilies"></param>
        /// <param name="rockContext"></param>
        private void AddAllFamilyGiving( XElement elemFamilies, RockContext rockContext )
        {
            // Now re-process the family section looking for any giving data.
            // We do this last because we need the personAliases that were just added.
            // Persist the storage type's settings specific to the contribution binary file type
            var settings = new Dictionary<string, string>();
            if ( _checkImageBinaryFileType.Attributes == null )
            {
                _checkImageBinaryFileType.LoadAttributes();
            }

            foreach ( var attributeValue in _checkImageBinaryFileType.AttributeValues )
            {
                settings.Add( attributeValue.Key, attributeValue.Value.Value );
            }

            _checkImageBinaryFileTypeSettings = settings.ToJson();

            foreach ( var elemFamily in elemFamilies.Elements( "family" ) )
            {
                // add the families giving data
                if ( _args.EnableGiving )
                {
                    // Support multiple giving elements per family
                    foreach ( var elementGiving in elemFamily.Elements( "giving" ) )
                    {
                        AddFamilyGiving( elementGiving, elemFamily.Attribute( "name" ).Value, rockContext );
                    }
                }
            }

            if ( _args.EnableGiving )
            {
                // Now add the batches to the service to be persisted
                var financialBatchService = new FinancialBatchService( rockContext );
                foreach ( var financialBatch in _contributionBatches )
                {
                    financialBatchService.Add( financialBatch.Value );
                }
            }

            rockContext.ChangeTracker.DetectChanges();
            rockContext.SaveChanges( disablePrePostProcessing: true );
        }

        /// <summary>
        /// Handles adding locations from the given XML element snippet.
        /// </summary>
        /// <param name="elemLocations"></param>
        /// <param name="rockContext"></param>
        private void AddLocations( XElement elemLocations, RockContext rockContext )
        {
            if ( elemLocations == null )
            {
                return;
            }

            var allLocations = from n in elemLocations.Elements( "location" )
                               select new
                               {
                                   Type = n.Attribute( "type" ).Value,
                                   Name = n.Attribute( "name" ).Value,
                                   Guid = n.Attribute( "guid" ).Value.AsGuid(),
                                   IsActive = n.Attribute( "isActive" ) == null || n.Attribute( "isActive" ).Value.AsBoolean( true ),
                                   ParentLocationGuid = n.Attribute( "parentLocationGuid" ) != null ? n.Attribute( "parentLocationGuid" ).Value : null,
                               };

            foreach ( var l in allLocations )
            {
                AddLocation( l.ParentLocationGuid, l.Guid, l.Type, l.Name, l.IsActive, rockContext );
            }
        }

        /// <summary>
        /// Adds a location if the guid does not already exist.
        /// </summary>
        /// <param name="parentLocationGuid"></param>
        /// <param name="locationGuid"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="isActive"></param>
        /// <param name="rockContext"></param>
        private void AddLocation( string parentLocationGuid, Guid locationGuid, string type, string name, bool isActive, RockContext rockContext )
        {
            var service = new LocationService( rockContext );

            var existingLocation = service.GetNoTracking( locationGuid );

            // Don't re-add an existing location
            if ( existingLocation != null )
            {
                return;
            }

            Guid locationTypeGuid = new Guid();

            switch ( type )
            {
                case "room":
                    locationTypeGuid = Rock.SystemGuid.DefinedValue.LOCATION_TYPE_ROOM.AsGuid();
                    break;

                case "building":
                    locationTypeGuid = Rock.SystemGuid.DefinedValue.LOCATION_TYPE_BUILDING.AsGuid();
                    break;

                case "campus":
                    locationTypeGuid = Rock.SystemGuid.DefinedValue.LOCATION_TYPE_CAMPUS.AsGuid();
                    break;

                default:
                    locationTypeGuid = Rock.SystemGuid.DefinedValue.LOCATION_TYPE_ROOM.AsGuid();
                    break;
            }

            var locationTypeValueId = DefinedValueCache.Get( locationTypeGuid ).Id;

            var location = new Location()
            {
                Name = name,
                Guid = locationGuid,
                LocationTypeValueId = locationTypeValueId,
                IsActive = isActive,
                CreatedDateTime = RockDateTime.Now,
                ModifiedDateTime = RockDateTime.Now
            };

            // Set the location's parent location if given
            if ( !string.IsNullOrEmpty( parentLocationGuid ) )
            {
                // save changes in case the location was just added prior.
                rockContext.SaveChanges();

                // The given parent location guid must be valid.
                location.ParentLocation = service.Get( parentLocationGuid.AsGuid() );
            }

            service.Add( location );
        }

        /// <summary>
        /// Adds the campuses if the GUID does not already exist.
        /// </summary>
        /// <param name="eleCampuses">The ele campuses.</param>
        /// <param name="rockContext">The rock context.</param>
        private void AddCampuses( XElement eleCampuses, RockContext rockContext )
        {
            if ( eleCampuses == null )
            {
                return;
            }

            var campusService = new CampusService( rockContext );

            foreach ( var campusElement in eleCampuses.Elements( "campus" ) )
            {
                Guid? campusGuid = campusElement.Attribute( "guid" ).Value.AsGuidOrNull();
                if ( CampusCache.Get( campusGuid.Value ) != null )
                {
                    continue;
                }

                var campus = new Campus
                {
                    Name = campusElement.Attribute( "name" ).Value ?? "New Campus",
                    ShortCode = campusElement.Attribute( "shortCode" ).Value,
                    Guid = campusGuid ?? Guid.NewGuid(),
                    PhoneNumber = campusElement.Attribute( "phoneNumber" ).Value,
                    Description = campusElement.Attribute( "description" ).Value,
                    IsActive = campusElement.Attribute( "isActive" ).Value.AsBoolean( true ),
                    Url = campusElement.Attribute( "url" ).Value,
                    CampusStatusValueId = DefinedValueCache.GetId( campusElement.Attribute( "campusStatusValueGuid" ).Value.AsGuidOrNull() ?? new Guid() ),
                    CampusTypeValueId = DefinedValueCache.GetId( campusElement.Attribute( "campusTypeValueGuid" ).Value.AsGuidOrNull() ?? new Guid() )
                };

                var locationElement = campusElement.Element( "location" );
                if ( locationElement != null )
                {
                    campus.Location = new Location
                    {
                        LocationTypeValueId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.LOCATION_TYPE_CAMPUS.AsGuid() ),
                        Name = locationElement.Attribute( "name" ).Value ?? $"{campus.Name} - Location",
                        Guid = locationElement.Attribute( "guid" ).Value.AsGuidOrNull() ?? Guid.NewGuid(),
                        IsActive = locationElement.Attribute( "isActive" ).Value.AsBoolean( true )
                    };
                }

                campusService.Add( campus );
            }
        }

        /// <summary>
        /// Handles adding groups from the given XML element snippet.
        /// </summary>
        /// <param name="elemGroups">The elem groups.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        private void AddGroups( XElement elemGroups, RockContext rockContext )
        {
            // Add groups
            if ( elemGroups == null )
            {
                return;
            }

            GroupService groupService = new GroupService( rockContext );

            // Next create the group along with its members.
            foreach ( var elemGroup in elemGroups.Elements( "group" ) )
            {
                Guid guid = elemGroup.Attribute( "guid" ).Value.Trim().AsGuid();
                string type = elemGroup.Attribute( "type" ).Value;
                Group group = new Group()
                {
                    Guid = guid,
                    Name = elemGroup.Attribute( "name" ).Value.Trim(),
                    IsActive = true,
                    IsPublic = true
                };

                // skip any where there is no group type given -- they are invalid entries.
                if ( string.IsNullOrEmpty( elemGroup.Attribute( "type" ).Value.Trim() ) )
                {
                    return;
                }

                int? roleId;
                GroupTypeCache groupType;
                switch ( elemGroup.Attribute( "type" ).Value.Trim() )
                {
                    case "serving":
                        groupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_SERVING_TEAM.AsGuid() );
                        group.GroupTypeId = groupType.Id;
                        roleId = groupType.DefaultGroupRoleId;
                        break;
                    case "smallgroup":
                        groupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_SMALL_GROUP.AsGuid() );
                        group.GroupTypeId = groupType.Id;
                        roleId = groupType.DefaultGroupRoleId;
                        break;
                    case "generalgroup":
                        groupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_GENERAL.AsGuid() );
                        group.GroupTypeId = groupType.Id;
                        roleId = groupType.DefaultGroupRoleId;
                        break;
                    default:
                        throw new NotSupportedException( string.Format( "unknown group type {0}", elemGroup.Attribute( "type" ).Value.Trim() ) );
                }

                if ( elemGroup.Attribute( "description" ) != null )
                {
                    group.Description = elemGroup.Attribute( "description" ).Value;
                }

                if ( elemGroup.Attribute( "parentGroupGuid" ) != null )
                {
                    var parentGroup = groupService.Get( elemGroup.Attribute( "parentGroupGuid" ).Value.AsGuid() );
                    if ( parentGroup != null )
                    {
                        group.ParentGroupId = parentGroup.Id;
                    }
                }

                // Set the group's meeting location
                if ( elemGroup.Attribute( "meetsAtHomeOfFamily" ) != null )
                {
                    int meetingLocationValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_MEETING_LOCATION.AsGuid() ).Id;
                    var groupLocation = new GroupLocation()
                    {
                        IsMappedLocation = false,
                        IsMailingLocation = false,
                        GroupLocationTypeValueId = meetingLocationValueId,
                        LocationId = _familyLocationDictionary[elemGroup.Attribute( "meetsAtHomeOfFamily" ).Value.AsGuid()],
                    };

                    // Set the group location's GroupMemberPersonId if given (required?)
                    if ( elemGroup.Attribute( "meetsAtHomeOfPerson" ) != null )
                    {
                        groupLocation.GroupMemberPersonAliasId = _peopleAliasDictionary[elemGroup.Attribute( "meetsAtHomeOfPerson" ).Value.AsGuid()];
                    }

                    group.GroupLocations.Add( groupLocation );
                }

                group.LoadAttributes( rockContext );

                // Set the study topic
                if ( elemGroup.Attribute( "studyTopic" ) != null )
                {
                    var topic = elemGroup.Attribute( "studyTopic" ).Value;
                    DefinedValueCache smallGroupTopicDefinedValue = _smallGroupTopicDefinedType.DefinedValues.FirstOrDefault( a => a.Value == topic );

                    // add it as new if we didn't find it.
                    if ( smallGroupTopicDefinedValue == null )
                    {
                        smallGroupTopicDefinedValue = AddDefinedTypeValue( topic, _smallGroupTopicDefinedType );
                    }

                    group.SetAttributeValue( "Topic", smallGroupTopicDefinedValue.Guid.ToString() );
                }

                // Set the schedule and meeting time
                if ( elemGroup.Attribute( "groupSchedule" ) != null )
                {
                    string[] schedule = elemGroup.Attribute( "groupSchedule" ).Value.SplitDelimitedValues( whitespace: false );

                    if ( schedule[0] == "weekly" )
                    {
                        var dow = schedule[1];
                        var time = schedule[2];
                        AddWeeklySchedule( group, dow, time );
                    }
                }

                // Add each person as a member
                foreach ( var elemPerson in elemGroup.Elements( "person" ) )
                {
                    Guid personGuid = elemPerson.Attribute( "guid" ).Value.Trim().AsGuid();

                    GroupMember groupMember = new GroupMember();
                    groupMember.GroupMemberStatus = GroupMemberStatus.Active;
                    groupMember.GroupTypeId = group.GroupTypeId;

                    if ( elemPerson.Attribute( "isLeader" ) != null )
                    {
                        bool isLeader = elemPerson.Attribute( "isLeader" ).Value.Trim().AsBoolean();
                        if ( isLeader )
                        {
                            var gtLeaderRole = groupType.Roles.Where( r => r.IsLeader ).FirstOrDefault();
                            if ( gtLeaderRole != null )
                            {
                                groupMember.GroupRoleId = gtLeaderRole.Id;
                            }
                        }
                    }
                    else
                    {
                        groupMember.GroupRoleId = roleId ?? -1;
                    }

                    // Normally, a personGuid will be in the dictionary, but in some cases it
                    // might be an existing well known Guid (i.e., the 'Admin Admin' record) so
                    // in that case, we'll try to fetch the ID from the db.
                    if ( _peopleDictionary.ContainsKey( personGuid ) )
                    {
                        groupMember.PersonId = _peopleDictionary[personGuid];
                    }
                    else
                    {
                        var existingPerson = new PersonService( rockContext ).Get( personGuid );
                        if ( existingPerson == null )
                        {
                            // skip this group member record if it could not be found.
                            continue;
                        }

                        _peopleDictionary.Add( personGuid, existingPerson.Id );
                        groupMember.PersonId = existingPerson.Id;
                    }

                    group.Members.Add( groupMember );
                }

                if ( elemGroup.Attribute( "groupCapacity" ) != null )
                {
                    group.GroupCapacity = elemGroup.Attribute( "groupCapacity" ).Value.AsIntegerOrNull();
                }

                groupService.Add( group );

                // Now we have to save changes in order for the attributes to be saved correctly.
                rockContext.SaveChanges();
                group.SaveAttributeValues( rockContext );

                if ( !_groupDictionary.ContainsKey( group.Guid ) )
                {
                    _groupDictionary.Add( group.Guid, group.Id );
                }

                // Now add any group location schedules
                LocationService locationService = new LocationService( rockContext );
                ScheduleService scheduleService = new ScheduleService( rockContext );
                Guid locationTypeMeetingLocationGuid = new Guid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_MEETING_LOCATION );
                var locationTypeMeetingLocationId = DefinedValueCache.Get( locationTypeMeetingLocationGuid ).Id;

                foreach ( var elemLocation in elemGroup.Elements( "location" ) )
                {
                    Guid locationGuid = elemLocation.Attribute( "guid" ).Value.Trim().AsGuid();
                    Location location = locationService.Get( locationGuid );
                    GroupLocation groupLocation = new GroupLocation();
                    groupLocation.Location = location;
                    groupLocation.GroupLocationTypeValueId = locationTypeMeetingLocationId;
                    group.GroupLocations.Add( groupLocation );

                    foreach ( var elemSchedule in elemLocation.Elements( "schedule" ) )
                    {
                        try
                        {
                            Guid scheduleGuid = elemSchedule.Attribute( "guid" ).Value.Trim().AsGuid();
                            Schedule schedule = scheduleService.Get( scheduleGuid );
                            groupLocation.Schedules.Add( schedule );

                            //// TODO -- once Group Scheduling is in develop, add the GroupLocationScheduleConfig
                            //// data (minimumCapacity, desiredCapacity, maximumCapacity) if any was given.
                        }
                        catch
                        {
                        }
                    }

                    LogElapsed( "group location schedules added" );
                }
            }
        }

        /// <summary>
        /// Adds a Weekly schedule to the given group.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="dayOfWeekName"></param>
        /// <param name="time"></param>
        private void AddWeeklySchedule( Group group, string dayOfWeekName, string time )
        {
            group.Schedule = new Schedule();

            DayOfWeek dow = ( DayOfWeek ) Enum.Parse( typeof( DayOfWeek ), dayOfWeekName, true );

            group.Schedule.iCalendarContent = null;
            group.Schedule.WeeklyDayOfWeek = dow;

            TimeSpan timespan;
            if ( TimeSpan.TryParse( time, out timespan ) )
            {
                group.Schedule.WeeklyTimeOfDay = timespan;
            }
        }

        /// <summary>
        /// Adds a new defined value to a given DefinedType.
        /// </summary>
        /// <param name="stringValue">the string value of the new defined value</param>
        /// <param name="definedType">a defined type to which the defined value will be added.</param>
        /// <returns></returns>
        private DefinedValueCache AddDefinedTypeValue( string stringValue, DefinedTypeCache definedType )
        {
            using ( var rockContext = GetConfiguredDataContext() )
            {
                DefinedValueService definedValueService = new DefinedValueService( rockContext );

                DefinedValue definedValue = new DefinedValue
                {
                    Id = 0,
                    IsSystem = false,
                    Value = stringValue,
                    Description = string.Empty,
                    CreatedDateTime = RockDateTime.Now,
                    DefinedTypeId = definedType.Id
                };

                definedValueService.Add( definedValue );
                rockContext.SaveChanges();

                return DefinedValueCache.Get( definedValue.Id, rockContext );
            }
        }

        /// <summary>
        /// Adds the following records from the given XML element.
        /// </summary>
        /// <example>
        ///   &lt;following&gt;
        ///       &lt;follows personGuid="1dfff821-e97c-4324-9883-cf59b5c5bdd6" followsGuid="1dfff821-e97c-4324-9883-cf59b5c5bdd6" type="person" /&gt;
        ///   &lt;/connections&gt;
        /// </example>
        /// <param name="elemFollowing">The element with the following XML fragment.</param>
        /// <param name="rockContext">The rock context.</param>
        private void AddFollowing( XElement elemFollowing, RockContext rockContext )
        {
            if ( elemFollowing == null )
            {
                return;
            }

            FollowingService followingService = new FollowingService( rockContext );

            int entityTypeId;
            int entityId;

            // Find the type and it's corresponding opportunity and then add a connection request for the given person.
            foreach ( var element in elemFollowing.Elements( "follows" ) )
            {
                Guid personGuid = element.Attribute( "personGuid" ).Value.Trim().AsGuid();
                Guid entityGuid = element.Attribute( "followsGuid" ).Value.Trim().AsGuid();

                string entityTypeName = element.Attribute( "type" ).Value.Trim();

                // only person (person aliases) are supported now.
                if ( entityTypeName.ToLower() == "person" )
                {
                    entityTypeId = EntityTypeCache.Get( typeof( Rock.Model.PersonAlias ) ).Id;
                    entityId = _peopleAliasDictionary[entityGuid];
                }
                else if ( entityTypeName.ToLower() == "group" )
                {
                    entityTypeId = EntityTypeCache.Get( typeof( Rock.Model.Group ) ).Id;
                    entityId = _groupDictionary[entityGuid];
                }
                else
                {
                    // only person (person aliases) are supported as of now.
                    continue;
                }

                Following following = new Following()
                {
                    PersonAliasId = _peopleAliasDictionary[personGuid],
                    EntityTypeId = entityTypeId,
                    EntityId = entityId,
                    CreatedByPersonAliasId = _peopleAliasDictionary[personGuid],
                    CreatedDateTime = RockDateTime.Now,
                    ModifiedDateTime = RockDateTime.Now,
                    ModifiedByPersonAliasId = _peopleAliasDictionary[personGuid]
                };

                followingService.Add( following );
            }
        }

        /// <summary>
        /// Adds the connections requests to the system from the given XML element.
        /// </summary>
        /// <example>
        ///   &lt;connections&gt;
        ///       &lt;connection type="Involvement" opportunity="Children's" comment="I would love to help teach kids about Jesus." date="2015-10-11T00:00:00" personGuid="1dfff821-e97c-4324-9883-cf59b5c5bdd6" /&gt;
        ///   &lt;/connections&gt;
        /// </example>
        /// <param name="elemConnections">The elem connections.</param>
        /// <param name="rockContext">The rock context.</param>
        private void AddConnections( XElement elemConnections, RockContext rockContext )
        {
            if ( elemConnections == null )
            {
                return;
            }

            ConnectionRequestService crService = new ConnectionRequestService( rockContext );
            ConnectionOpportunityService coService = new ConnectionOpportunityService( rockContext );
            ConnectionTypeService typeService = new ConnectionTypeService( rockContext );
            ConnectionStatusService connectionStatusService = new ConnectionStatusService( rockContext );
            ConnectionStatus noContact = connectionStatusService.Get( "901e1a6a-0e91-4f42-880f-47c061c24e0c".AsGuid() );

            // Find the type and it's corresponding opportunity and then add a connection request for the given person.
            foreach ( var element in elemConnections.Elements( "connection" ) )
            {
                string connectionTypeName = element.Attribute( "type" ).Value.Trim();
                string opportunityName = element.Attribute( "opportunity" ).Value.Trim();
                string comment = element.Attribute( "comment" ).Value.Trim();
                DateTime date = DateTime.Parse( element.Attribute( "date" ).Value.Trim(), new CultureInfo( "en-US" ) );
                Guid personGuid = element.Attribute( "personGuid" ).Value.Trim().AsGuid();

                var connectionOpportunity = coService.Queryable( "ConnectionType" ).AsNoTracking().Where( co => co.ConnectionType.Name == connectionTypeName && co.Name == opportunityName ).FirstOrDefault();

                // make sure we found a matching connection opportunity
                if ( connectionOpportunity != null )
                {
                    ConnectionRequest connectionRequest = new ConnectionRequest()
                    {
                        ConnectionOpportunityId = connectionOpportunity.Id,
                        ConnectionTypeId = connectionOpportunity.ConnectionTypeId,
                        PersonAliasId = _peopleAliasDictionary[personGuid],
                        Comments = comment,
                        ConnectionStatus = noContact,
                        ConnectionState = global::ConnectionState.Active,
                        CreatedDateTime = date
                    };

                    crService.Add( connectionRequest );
                }
            }
        }

        /// <summary>
        /// Handles adding people to the security groups from the given XML element snippet.
        /// </summary>
        /// <param name="elemSecurityGroups">The elem security groups.</param>
        /// <param name="rockContext">The rock context.</param>
        private void AddToSecurityGroups( XElement elemSecurityGroups, RockContext rockContext )
        {
            if ( elemSecurityGroups == null )
            {
                return;
            }

            GroupService groupService = new GroupService( rockContext );

            // Next find each group and add its members
            foreach ( var elemGroup in elemSecurityGroups.Elements( "group" ) )
            {
                int membersAdded = 0;
                Guid guid = elemGroup.Attribute( "guid" ).Value.Trim().AsGuid();
                Group securityGroup = groupService.GetByGuid( guid );
                if ( securityGroup == null )
                {
                    continue;
                }

                // Add each person as a member of the group
                foreach ( var elemPerson in elemGroup.Elements( "members" ).Elements( "person" ) )
                {
                    Guid personGuid = elemPerson.Attribute( "guid" ).Value.Trim().AsGuid();
                    int personId = _peopleDictionary[personGuid];

                    // Don't add if already in the group...
                    if ( securityGroup.Members.Where( p => p.PersonId == personId ).Count() > 0 )
                    {
                        continue;
                    }

                    membersAdded++;
                    GroupMember groupMember = new GroupMember();
                    groupMember.GroupMemberStatus = GroupMemberStatus.Active;
                    groupMember.GroupRoleId = securityGroup.GroupType.DefaultGroupRoleId.Value;
                    groupMember.PersonId = personId;
                    groupMember.GroupTypeId = securityGroup.GroupTypeId;
                    securityGroup.Members.Add( groupMember );
                }
            }
        }

        /// <summary>
        /// Deletes the family's addresses, phone numbers, photos, viewed records, and people.
        /// TODO: delete attendance codes for attendance data that's about to be deleted when
        /// we delete the person record.
        /// </summary>
        /// <param name="families">The families.</param>
        /// <param name="rockContext">The rock context.</param>
        private void DeleteExistingFamilyData( XElement families, RockContext rockContext )
        {
            PersonService personService = new PersonService( rockContext );
            PhoneNumberService phoneNumberService = new PhoneNumberService( rockContext );
            PersonViewedService personViewedService = new PersonViewedService( rockContext );
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            PersonDuplicateService personDuplicateService = new PersonDuplicateService( rockContext );
            NoteService noteService = new NoteService( rockContext );
            AuthService authService = new AuthService( rockContext );
            CommunicationService communicationService = new CommunicationService( rockContext );
            CommunicationRecipientService communicationRecipientService = new CommunicationRecipientService( rockContext );
            FinancialBatchService financialBatchService = new FinancialBatchService( rockContext );
            FinancialTransactionService financialTransactionService = new FinancialTransactionService( rockContext );
            PersonPreviousNameService personPreviousNameService = new PersonPreviousNameService( rockContext );
            ConnectionRequestService connectionRequestService = new ConnectionRequestService( rockContext );
            ConnectionRequestActivityService connectionRequestActivityService = new ConnectionRequestActivityService( rockContext );

            // delete the batch data
            List<int> imageIds = new List<int>();
            foreach ( var batch in financialBatchService.Queryable().Where( b => b.Name.StartsWith( "SampleData" ) ) )
            {
                imageIds.AddRange( batch.Transactions.SelectMany( t => t.Images ).Select( i => i.BinaryFileId ).ToList() );
                financialTransactionService.DeleteRange( batch.Transactions );
                financialBatchService.Delete( batch );
            }

            // delete all transaction images
            foreach ( var image in binaryFileService.GetByIds( imageIds ) )
            {
                binaryFileService.Delete( image );
            }

            foreach ( var elemFamily in families.Elements( "family" ) )
            {
                Guid guid = elemFamily.Attribute( "guid" ).Value.Trim().AsGuid();

                GroupService groupService = new GroupService( rockContext );
                Group family = groupService.Get( guid );
                if ( family != null )
                {
                    var groupMemberService = new GroupMemberService( rockContext );
                    var members = groupMemberService.GetByGroupId( family.Id, true );

                    // delete the people records
                    string errorMessage;
                    List<int> photoIds = members.Select( m => m.Person ).Where( p => p.PhotoId != null ).Select( a => ( int ) a.PhotoId ).ToList();

                    foreach ( var person in members.Select( m => m.Person ) )
                    {
                        person.GivingGroup = null;
                        person.GivingGroupId = null;
                        person.PhotoId = null;

                        // delete phone numbers
                        foreach ( var phone in phoneNumberService.GetByPersonId( person.Id ) )
                        {
                            if ( phone != null )
                            {
                                phoneNumberService.Delete( phone );
                            }
                        }

                        // delete communication recipient
                        foreach ( var recipient in communicationRecipientService.Queryable().Where( r => r.PersonAlias.PersonId == person.Id ) )
                        {
                            communicationRecipientService.Delete( recipient );
                        }

                        // delete communication
                        foreach ( var communication in communicationService.Queryable().Where( c => c.SenderPersonAliasId == person.PrimaryAlias.Id ) )
                        {
                            communicationService.Delete( communication );
                        }

                        // delete person viewed records
                        foreach ( var view in personViewedService.GetByTargetPersonId( person.Id ) )
                        {
                            personViewedService.Delete( view );
                        }

                        // delete notes created by them or on their record.
                        foreach ( var note in noteService.Queryable().Where( n => n.CreatedByPersonAlias.PersonId == person.Id
                           || ( n.NoteType.EntityTypeId == _personEntityTypeId && n.EntityId == person.Id ) ) )
                        {
                            noteService.Delete( note );
                        }

                        // delete previous names on their records
                        foreach ( var previousName in personPreviousNameService.Queryable().Where( r => r.PersonAlias.PersonId == person.Id ) )
                        {
                            personPreviousNameService.Delete( previousName );
                        }

                        // delete any GroupMember records they have
                        foreach ( var groupMember in groupMemberService.Queryable().Where( gm => gm.PersonId == person.Id ) )
                        {
                            groupMemberService.Delete( groupMember );
                        }

                        // delete their aliases
                        foreach ( var alias in personAliasService.Queryable().Where( a => a.PersonId == person.Id ) )
                        {
                            foreach ( var duplicate in personDuplicateService.Queryable().Where( d => d.DuplicatePersonAliasId == alias.Id ) )
                            {
                                personDuplicateService.Delete( duplicate );
                            }

                            personAliasService.Delete( alias );
                        }

                        // delete any connection requests tied to them
                        foreach ( var request in connectionRequestService.Queryable().Where( r => r.PersonAlias.PersonId == person.Id || r.ConnectorPersonAlias.PersonId == person.Id ) )
                        {
                            connectionRequestActivityService.DeleteRange( request.ConnectionRequestActivities );
                            connectionRequestService.Delete( request );
                        }

                        // Save these changes so the CanDelete passes the check...
                        rockContext.SaveChanges( disablePrePostProcessing: true );

                        if ( personService.CanDelete( person, out errorMessage ) )
                        {
                            personService.Delete( person );
                        }
                        else
                        {
                            throw new Exception( string.Format( "Trying to delete {0}, but: {1}", person.FullName, errorMessage ) );
                        }
                    }

                    rockContext.SaveChanges( disablePrePostProcessing: true );

                    // delete all member photos
                    foreach ( var photo in binaryFileService.GetByIds( photoIds ) )
                    {
                        binaryFileService.Delete( photo );
                    }

                    DeleteGroupAndMemberData( family, rockContext );
                }
            }
        }

        /// <summary>
        /// Generic method to delete the members of a group and then the group.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <exception cref="System.InvalidOperationException">Unable to delete group:  + group.Name</exception>
        private void DeleteGroupAndMemberData( Group group, RockContext rockContext )
        {
            GroupService groupService = new GroupService( rockContext );

            // delete addresses
            GroupLocationService groupLocationService = new GroupLocationService( rockContext );
            if ( group.GroupLocations.Count > 0 )
            {
                foreach ( var groupLocations in group.GroupLocations.ToList() )
                {
                    group.GroupLocations.Remove( groupLocations );
                    groupLocationService.Delete( groupLocations );
                }
            }

            // delete members
            var groupMemberService = new GroupMemberService( rockContext );
            var members = group.Members;
            foreach ( var member in members.ToList() )
            {
                group.Members.Remove( member );
                groupMemberService.Delete( member );
            }

            // delete attribute values
            group.LoadAttributes( rockContext );
            if ( group.AttributeValues != null )
            {
                var attributeValueService = new AttributeValueService( rockContext );
                foreach ( var entry in group.AttributeValues )
                {
                    var attributeValue = attributeValueService.GetByAttributeIdAndEntityId( entry.Value.AttributeId, group.Id );
                    if ( attributeValue != null )
                    {
                        attributeValueService.Delete( attributeValue );
                    }
                }
            }

            if ( group.Groups.Any() )
            {
                groupService.DeleteRange( group.Groups );
                rockContext.SaveChanges();
            }

            // now delete the group
            if ( groupService.Delete( group ) )
            {
                // OK
            }
            else
            {
                throw new InvalidOperationException( "Unable to delete group: " + group.Name );
            }
        }

        /// <summary>
        /// Delete all groups found in the given XML.
        /// </summary>
        /// <param name="elemGroups">The elem groups.</param>
        /// <param name="rockContext">The rock context.</param>
        private void DeleteExistingGroups( XElement elemGroups, RockContext rockContext )
        {
            if ( elemGroups == null )
            {
                return;
            }

            GroupService groupService = new GroupService( rockContext );
            foreach ( var elemGroup in elemGroups.Elements( "group" ) )
            {
                Guid guid = elemGroup.Attribute( "guid" ).Value.Trim().AsGuid();
                Group group = groupService.Get( guid );
                if ( group != null )
                {
                    DeleteGroupAndMemberData( group, rockContext );
                    rockContext.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Deletes the registration templates.
        /// </summary>
        /// <param name="elemRegistrationTemplates">The elem registration template.</param>
        /// <param name="rockContext">The rock context.</param>
        private void DeleteRegistrationTemplates( XElement elemRegistrationTemplates, RockContext rockContext )
        {
            if ( elemRegistrationTemplates == null )
            {
                return;
            }

            var service = new RegistrationTemplateService( rockContext );

            foreach ( var elemRegistrationTemplate in elemRegistrationTemplates.Elements( "registrationTemplate" ) )
            {
                Guid guid = elemRegistrationTemplate.Attribute( "guid" ).Value.Trim().AsGuid();
                var registrationTemplate = service.Get( guid );

                rockContext.WrapTransaction( () =>
                {
                    if ( registrationTemplate != null )
                    {
                        if ( registrationTemplate.Instances != null )
                        {
                            AttributeService attributeService = new AttributeService( rockContext );
                            if ( registrationTemplate.Forms != null )
                            {
                                foreach ( var id in registrationTemplate.Forms.SelectMany( f => f.Fields ).Where( ff => ff.FieldSource == RegistrationFieldSource.RegistrantAttribute ).Select( f => f.AttributeId ) )
                                {
                                    if ( id != null )
                                    {
                                        Rock.Model.Attribute attribute = attributeService.Get( id ?? -1 );
                                        if ( attribute != null )
                                        {
                                            attributeService.Delete( attribute );
                                        }
                                    }
                                }
                            }

                            var registrations = registrationTemplate.Instances.SelectMany( i => i.Registrations );
                            new RegistrationService( rockContext ).DeleteRange( registrations );
                            new RegistrationTemplatePlacementService( rockContext ).DeleteRange( registrationTemplate.Placements );
                            new RegistrationInstanceService( rockContext ).DeleteRange( registrationTemplate.Instances );
                        }

                        service.Delete( registrationTemplate );
                        rockContext.SaveChanges();
                    }
                } );
            }
        }

        /// <summary>
        /// Adds the family giving records.
        /// </summary>
        /// <param name="elemGiving">The giving element.</param>
        /// <param name="familyName">The family name.</param>
        /// <param name="rockContext">The rock context.</param>
        private void AddFamilyGiving( XElement elemGiving, string familyName, RockContext rockContext )
        {
            // return from here if there's not a startGiving date, account amount details or a person Guid.
            if ( elemGiving == null || elemGiving.Attribute( "startGiving" ) == null || elemGiving.Attribute( "accountAmount" ) == null || elemGiving.Attribute( "personGuid" ) == null )
            {
                return;
            }

            // get some variables we'll need to create the giving records
            DateTime startingDate = DateTime.Parse( elemGiving.Attribute( "startGiving" ).Value.Trim(), new CultureInfo( "en-US" ) );
            DateTime endDate = RockDateTime.Now;

            if ( elemGiving.Attribute( "endGiving" ) != null )
            {
                DateTime.TryParse( elemGiving.Attribute( "endGiving" ).Value.Trim(), out endDate );
            }
            else if ( elemGiving.Attribute( "endingGivingWeeksAgo" ) != null )
            {
                int endingWeeksAgo = 0;
                int.TryParse( elemGiving.Attribute( "endingGivingWeeksAgo" ).Value.Trim(), out endingWeeksAgo );
                endDate = RockDateTime.Now.AddDays( -7 * endingWeeksAgo );
            }

            int percentGive = 100;
            if ( elemGiving.Attribute( "percentGive" ) != null )
            {
                int.TryParse( elemGiving.Attribute( "percentGive" ).Value.Trim(), out percentGive );
            }

            int growRatePercent = 0;
            if ( elemGiving.Attribute( "growRatePercent" ) != null )
            {
                int.TryParse( elemGiving.Attribute( "growRatePercent" ).Value.Trim(), out growRatePercent );
            }

            int growFrequencyWeeks = 0;
            if ( elemGiving.Attribute( "growFrequencyWeeks" ) != null )
            {
                int.TryParse( elemGiving.Attribute( "growFrequencyWeeks" ).Value.Trim(), out growFrequencyWeeks );
            }

            int specialGiftPercent = 0;
            if ( elemGiving.Attribute( "specialGiftPercent" ) != null )
            {
                int.TryParse( elemGiving.Attribute( "specialGiftPercent" ).Value.Trim(), out specialGiftPercent );
            }

            Frequency frequency;
            if ( elemGiving.Attribute( "frequency" ) != null )
            {
                Enum.TryParse( elemGiving.Attribute( "frequency" ).Value.Trim(), out frequency );
            }
            else
            {
                frequency = Frequency.weekly;
            }

            Guid personGuid = elemGiving.Attribute( "personGuid" ).Value.Trim().AsGuid();

            // Build a dictionary of FinancialAccount Ids and the amount to give to that account.
            Dictionary<int, decimal> accountAmountDict = new Dictionary<int, decimal>();
            FinancialAccountService financialAccountService = new FinancialAccountService( rockContext );
            var allAccountAmount = elemGiving.Attribute( "accountAmount" ).Value.Trim().Split( ',' );
            foreach ( var item in allAccountAmount )
            {
                var accountAmount = item.Split( ':' );
                decimal amount;
                if ( !decimal.TryParse( accountAmount[1], out amount ) )
                {
                    continue; // skip if not a valid decimal
                }

                var accountName = accountAmount[0].ToLower();
                var financialAccount = financialAccountService.Queryable().AsNoTracking().Where( a => a.Name.ToLower() == accountName ).FirstOrDefault();
                if ( financialAccount != null )
                {
                    accountAmountDict.Add( financialAccount.Id, amount );
                }
                else
                {
                    financialAccount = financialAccountService.Queryable().AsNoTracking().First();
                }
            }

            // Build a circular linked list of photos to use for the fake contribution check images
            var circularImageList = new LinkedList<string>();
            if ( elemGiving.Attribute( "imageUrls" ) != null )
            {
                var allImageUrls = elemGiving.Attribute( "imageUrls" ).Value.Trim().Split( ',' );
                foreach ( var item in allImageUrls )
                {
                    circularImageList.AddLast( item );
                }
            }

            // Now create the giving data for this recipe set
            CreateGiving( personGuid, startingDate, endDate, frequency, percentGive, growRatePercent, growFrequencyWeeks, specialGiftPercent, accountAmountDict, circularImageList, rockContext );
            AppendFormat( "{0:00}:{1:00}.{2:00} added giving data {3} ({4})<br/>", _stopwatch.Elapsed.Minutes, _stopwatch.Elapsed.Seconds, _stopwatch.Elapsed.Milliseconds / 10, familyName, frequency );
        }

        /// <summary>
        /// Creates the giving records for the given parameters.
        /// </summary>
        /// <param name="personGuid">The person unique identifier.</param>
        /// <param name="startingDate">The starting date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="frequency">The frequency (onetime, weekly, monthly).</param>
        /// <param name="percentGive">The percent give.</param>
        /// <param name="growRatePercent">The grow rate percent.</param>
        /// <param name="growFrequencyWeeks">The grow frequency weeks.</param>
        /// <param name="specialGiftPercent">The special gift percent.</param>
        /// <param name="accountAmountDict">The account amount dictionary.</param>
        /// <param name="circularImageList">A circular linked list of imageUrls to use for the fake contribution checks.</param>
        /// <param name="rockContext">A rock context.</param>
        private void CreateGiving( Guid personGuid, DateTime startingDate, DateTime endDate, Frequency frequency, int percentGive, int growRatePercent, int growFrequencyWeeks, int specialGiftPercent, Dictionary<int, decimal> accountAmountDict, LinkedList<string> circularImageList, RockContext rockContext )
        {
            int weekNumber = 0;
            DateTime monthly = startingDate;

            var currencyTypeCheck = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CHECK.AsGuid() );

            var imageUrlNode = circularImageList.First ?? null;

            // for each weekend or monthly between the starting and ending date...
            for ( DateTime date = startingDate; date <= endDate; date = frequency == Frequency.weekly ? date.AddDays( 7 ) : frequency == Frequency.monthly ? date.AddMonths( 1 ) : endDate.AddDays( 1 ) )
            {
                weekNumber = ( int ) ( date - startingDate ).TotalDays / 7;

                // increase by growRatePercent every growFrequencyWeeks
                if ( growFrequencyWeeks != 0 && growRatePercent != 0 && weekNumber != 0 && weekNumber % growFrequencyWeeks == 0 )
                {
                    var copy = accountAmountDict.ToDictionary( entry => entry.Key, entry => entry.Value );
                    foreach ( var item in accountAmountDict )
                    {
                        decimal amount = Math.Round( ( item.Value * 0.01M ) + item.Value, 0 );
                        copy[item.Key] = amount;
                    }

                    accountAmountDict = copy;
                }

                // randomized skip/missed weeks
                int summerFactor = ( date.Month >= 7 && date.Month <= 9 ) ? summerPercentFactor : 0;
                if ( _random.Next( 0, 100 ) > percentGive - summerFactor )
                {
                    continue; // skip this week
                }

                FinancialBatch batch;
                if ( _contributionBatches.ContainsKey( date ) )
                {
                    batch = _contributionBatches[date];
                }
                else
                {
                    batch = new FinancialBatch
                    {
                        Id = 0,
                        Guid = Guid.NewGuid(),
                        BatchStartDateTime = date,
                        BatchEndDateTime = date,
                        Status = BatchStatus.Closed,
                        ControlAmount = 0,
                        Name = string.Format( "SampleData{0}", date.ToJavascriptMilliseconds() ),
                        CreatedByPersonAliasId = _args.CreatorPersonAliasId
                    };
                    _contributionBatches.Add( date, batch );
                }

                // Find the person alias via the dictrionary or look them up and add to dictionary:
                int? personAliasId;
                if ( _peopleAliasDictionary.ContainsKey( personGuid ) )
                {
                    personAliasId = _peopleAliasDictionary[personGuid];
                }
                else
                {
                    personAliasId = new PersonService( rockContext ).Get( personGuid ).PrimaryAliasId;
                    if ( personAliasId == null )
                    {
                        // this should not happen
                        continue;
                    }
                    _peopleAliasDictionary.Add( personGuid, personAliasId.Value );
                }

                // Set up the new transaction
                FinancialTransaction financialTransaction = new FinancialTransaction
                {
                    TransactionTypeValueId = _transactionTypeContributionId,
                    Guid = Guid.NewGuid(),
                    TransactionDateTime = date,
                    AuthorizedPersonAliasId = personAliasId
                };

                financialTransaction.FinancialPaymentDetail = new FinancialPaymentDetail();
                financialTransaction.FinancialPaymentDetail.CurrencyTypeValueId = currencyTypeCheck.Id;
                financialTransaction.FinancialPaymentDetail.Guid = Guid.NewGuid();

                // Add a transaction detail record for each account they're donating to
                foreach ( var item in accountAmountDict )
                {
                    FinancialTransactionDetail transactionDetail = new FinancialTransactionDetail
                    {
                        AccountId = item.Key,
                        Amount = item.Value,
                        Guid = Guid.NewGuid()
                    };

                    financialTransaction.TransactionDetails.Add( transactionDetail );
                }

                // Add the image to the transaction (if any)
                if ( imageUrlNode != null )
                {
                    FinancialTransactionImage transactionImage = new FinancialTransactionImage
                    {
                        BinaryFile = SaveImage( imageUrlNode.Value, _checkImageBinaryFileType, _checkImageBinaryFileTypeSettings, rockContext ),
                        Guid = Guid.NewGuid(),
                    };
                    financialTransaction.Images.Add( transactionImage );
                    imageUrlNode = imageUrlNode.Next ?? imageUrlNode.List.First;
                }

                // Update the batch with the new control amount
                batch.ControlAmount += financialTransaction.TotalAmount;
                batch.Transactions.Add( financialTransaction );
            }
        }

        /// <summary>
        /// Grabs the necessary parameters from the XML and then calls the CreateAttendance() method
        /// to generate all the attendance data for the family.
        /// </summary>
        /// <param name="family">The family.</param>
        /// <param name="elemFamily">The elem family.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="attendanceData">The attendance data.</param>
        private void AddFamilyAttendance( Group family, XElement elemFamily, RockContext rockContext, Dictionary<Guid, List<Attendance>> attendanceData )
        {
            // return from here if there's not startingAttendance date
            if ( elemFamily.Attribute( "startingAttendance" ) == null )
            {
                return;
            }

            // get some variables we'll need to create the attendance records
            DateTime startingDate = DateTime.Parse( elemFamily.Attribute( "startingAttendance" ).Value.Trim(), new CultureInfo( "en-US" ) );
            DateTime endDate = RockDateTime.Now;

            // If the XML specifies an endingAttendance date use it, otherwise use endingAttendanceWeeksAgo
            // to calculate the end date otherwise we'll just use the current date as the end date.
            if ( elemFamily.Attribute( "endingAttendance" ) != null )
            {
                DateTime.TryParse( elemFamily.Attribute( "endingAttendance" ).Value.Trim(), out endDate );
            }
            else if ( elemFamily.Attribute( "endingAttendanceWeeksAgo" ) != null )
            {
                int endingWeeksAgo = 0;
                int.TryParse( elemFamily.Attribute( "endingAttendanceWeeksAgo" ).Value.Trim(), out endingWeeksAgo );
                endDate = RockDateTime.Now.AddDays( -7 * endingWeeksAgo );
            }

            int pctAttendance = 100;
            if ( elemFamily.Attribute( "percentAttendance" ) != null )
            {
                int.TryParse( elemFamily.Attribute( "percentAttendance" ).Value.Trim(), out pctAttendance );
            }

            int pctAttendedRegularService = 100;
            if ( elemFamily.Attribute( "percentAttendedRegularService" ) != null )
            {
                int.TryParse( elemFamily.Attribute( "percentAttendedRegularService" ).Value.Trim(), out pctAttendedRegularService );
            }

            int scheduleId = 13;
            if ( elemFamily.Attribute( "attendingScheduleId" ) != null )
            {
                int.TryParse( elemFamily.Attribute( "attendingScheduleId" ).Value.Trim(), out scheduleId );
                if ( !_scheduleTimes.ContainsKey( scheduleId ) )
                {
                    Schedule schedule = new ScheduleService( rockContext ).Get( scheduleId );
                    if ( schedule == null )
                    {
                        // We're not going to continue if they are missing this schedule
                        return;
                    }

                    _scheduleTimes.Add( scheduleId, schedule.GetICalEvent().DtStart.Value );
                }
            }

            int altScheduleId = 4;
            if ( elemFamily.Attribute( "attendingAltScheduleId" ) != null )
            {
                int.TryParse( elemFamily.Attribute( "attendingAltScheduleId" ).Value.Trim(), out altScheduleId );
                if ( !_scheduleTimes.ContainsKey( altScheduleId ) )
                {
                    Schedule schedule = new ScheduleService( rockContext ).Get( altScheduleId );
                    if ( schedule == null )
                    {
                        // We're not going to continue if they are missing this schedule
                        return;
                    }

                    _scheduleTimes.Add( altScheduleId, schedule.GetICalEvent().DtStart.Value );
                }
            }

            CreateAttendance( family.Members, startingDate, endDate, pctAttendance, pctAttendedRegularService, scheduleId, altScheduleId, attendanceData, rockContext );
        }

        /// <summary>
        /// Adds attendance data for each child for each weekend since the starting date up to
        /// the weekend ending X weeks ago (endingWeeksAgo).  It will randomly skip a weekend
        /// based on the percentage (pctAttendance) and it will vary which service they attend
        /// between the scheduleId and altScheduleId based on the percentage (pctAttendedRegularService)
        /// given.
        /// </summary>
        /// <param name="familyMembers">The family members.</param>
        /// <param name="startingDate">The first date of attendance</param>
        /// <param name="endDate">The end date of attendance</param>
        /// <param name="pctAttendance">The PCT attendance.</param>
        /// <param name="pctAttendedRegularService">The PCT attended regular service.</param>
        /// <param name="scheduleId">The schedule identifier.</param>
        /// <param name="altScheduleId">The alt schedule identifier.</param>
        /// <param name="attendanceData">The attendance data.</param>
        /// <param name="rockContext">The rock context.</param>
        private void CreateAttendance( ICollection<GroupMember> familyMembers, DateTime startingDate, DateTime endDate, int pctAttendance, int pctAttendedRegularService, int scheduleId, int altScheduleId, Dictionary<Guid, List<Attendance>> attendanceData, RockContext rockContext )
        {
            // for each weekend between the starting and ending date...
            for ( DateTime date = startingDate; date <= endDate; date = date.AddDays( 7 ) )
            {
                // set an additional factor 
                int summerFactor = ( date.Month >= 7 && date.Month <= 9 ) ? summerPercentFactor : 0;
                if ( _random.Next( 0, 100 ) > pctAttendance - summerFactor )
                {
                    continue; // skip this week
                }

                // which service did they attend
                int serviceSchedId = ( _random.Next( 0, 100 ) > pctAttendedRegularService ) ? scheduleId : altScheduleId;

                // randomize check-in time slightly by +- 0-15 minutes (and 1 out of 4 times being late)
                int minutes = _random.Next( 0, 15 );
                int plusMinus = ( _random.Next( 0, 4 ) == 0 ) ? 1 : -1;
                int randomSeconds = _random.Next( 0, 60 );

                var time = _scheduleTimes[serviceSchedId];

                DateTime dtTime = new DateTime( date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second );
                DateTime checkinDateTime = dtTime.AddMinutes( Convert.ToDouble( plusMinus * minutes ) ).AddSeconds( randomSeconds );

                var attendanceService = new AttendanceService( rockContext );

                // for each child in the family
                foreach ( var member in familyMembers.Where( m => m.GroupRoleId == _childRoleId ) )
                {
                    // Find a class room (group location)
                    // TODO -- someday perhaps we will change this to actually find a real GroupLocationSchedule record
                    var item = ( from classroom in _classes
                                 where member.Person.AgePrecise >= classroom.MinAge
                                 && member.Person.AgePrecise <= classroom.MaxAge
                                 orderby classroom.MinAge, classroom.MaxAge
                                 select classroom ).FirstOrDefault();

                    // If no suitable classroom was found, skip
                    if ( item == null )
                    {
                        continue;
                    }

                    // Only create one attendance record per day for each person/schedule/group/location
                    AttendanceCode attendanceCode = new AttendanceCode()
                    {
                        Code = GenerateRandomCode( _securityCodeLength ),
                        IssueDateTime = _args.AttendanceCodeIssuedDateTime ?? RockDateTime.Now,
                    };

                    var attendance = attendanceService.AddOrUpdate( member.Person.PrimaryAliasId, checkinDateTime, item.GroupId, item.LocationId, scheduleId, 1, _kioskDeviceId, null, null, null, null );
                    attendance.AttendanceCode = attendanceCode;

                    if ( !attendanceData.Keys.Contains( member.Person.Guid ) )
                    {
                        attendanceData.Add( member.Person.Guid, new List<Attendance>() );
                    }

                    attendanceData[member.Person.Guid].Add( attendance );
                }
            }
        }

        /// <summary>
        /// A little method to generate a random sequence of characters of a certain length.
        /// </summary>
        /// <param name="len">length of code to generate</param>
        /// <returns>a random sequence of alpha numeric characters</returns>
        private static string GenerateRandomCode( int len )
        {
            string chars = "BCDFGHJKMNPQRTVWXYZ0123456789";
            var code = Enumerable.Range( 0, len ).Select( x => chars[_random.Next( 0, chars.Length )] );
            return new string( code.ToArray() );
        }

        /// <summary>
        /// Takes the given XML element and creates a family member collection.
        /// If the person already exists, their record will be loaded otherwise
        /// a new person will be created using all the attributes for the given
        /// 'person' tag.
        /// </summary>
        /// <param name="elemMembers"></param>
        /// <param name="rockContext">The rock context.</param>
        /// <returns>a list of family members.</returns>
        private List<GroupMember> BuildFamilyMembersFromXml( XElement elemMembers, RockContext rockContext )
        {
            var familyMembers = new List<GroupMember>();

            // First add each person to the familyMembers collection
            foreach ( var personElem in elemMembers.Elements( "person" ) )
            {
                var groupMember = new GroupMember();
                groupMember.GroupTypeId = GroupTypeCache.GetFamilyGroupType().Id;

                Guid guid = Guid.Parse( personElem.Attribute( "guid" ).Value.Trim() );

                // Attempt to find an existing person...
                Person person = null;
                if ( _personCache.ContainsKey( guid ) )
                {
                    person = _personCache[guid];
                }

                if ( person == null )
                {
                    person = new Person();
                    person.CreatedByPersonAliasId = _args.CreatorPersonAliasId;
                    person.CreatedDateTime = RockDateTime.Now;

                    person.Guid = guid;
                    person.FirstName = personElem.Attribute( "firstName" ).Value.Trim();
                    if ( personElem.Attribute( "suffix" ) != null )
                    {
                        person.SuffixValueId = GetOrAddDefinedValueId( personElem.Attribute( "suffix" ).Value.Trim(), _suffixDefinedType );
                    }

                    if ( personElem.Attribute( "nickName" ) != null )
                    {
                        person.NickName = personElem.Attribute( "nickName" ).Value.Trim();
                    }
                    else
                    {
                        person.NickName = personElem.Attribute( "firstName" ).Value.Trim();
                    }

                    if ( personElem.Attribute( "lastName" ) != null )
                    {
                        person.LastName = personElem.Attribute( "lastName" ).Value.Trim();
                    }

                    if ( personElem.Attribute( "birthDate" ) != null )
                    {
                        person.SetBirthDate( DateTime.Parse( personElem.Attribute( "birthDate" ).Value.Trim(), new CultureInfo( "en-US" ) ) );
                    }

                    if ( personElem.Attribute( "grade" ) != null )
                    {
                        int? grade = personElem.Attribute( "grade" ).Value.AsIntegerOrNull();
                        if ( grade.HasValue )
                        {
                            // convert the grade (0-12 where 12 = Senior), to a GradeOffset (12-0 where 12 = K and 0 = Senior)
                            int gradeOffset = 12 - grade.Value;
                            person.GradeOffset = gradeOffset >= 0 ? gradeOffset : ( int? ) null;
                        }
                    }
                    else if ( personElem.Attribute( "graduationDate" ) != null )
                    {
                        person.GraduationYear = DateTime.Parse( personElem.Attribute( "graduationDate" ).Value.Trim(), new CultureInfo( "en-US" ) ).Year;
                    }

                    // Now, if their age was given we'll change the given birth year to make them
                    // be this age as of Today.
                    if ( personElem.Attribute( "age" ) != null )
                    {
                        int age = int.Parse( personElem.Attribute( "age" ).Value.Trim() );
                        int ageDiff = Person.GetAge( person.BirthDate, null ) - age ?? 0;
                        person.SetBirthDate( person.BirthDate.Value.AddYears( ageDiff ) );
                    }

                    person.EmailPreference = EmailPreference.EmailAllowed;

                    if ( personElem.Attribute( "email" ) != null )
                    {
                        var emailAddress = personElem.Attribute( "email" ).Value.Trim();
                        if ( EmailAddressFieldValidator.IsValid( emailAddress ) )
                        {
                            person.Email = emailAddress;
                            person.IsEmailActive = personElem.Attribute( "emailIsActive" ) != null && personElem.Attribute( "emailIsActive" ).Value.AsBoolean();
                            if ( personElem.Attribute( "emailDoNotEmail" ) != null && personElem.Attribute( "emailDoNotEmail" ).Value.AsBoolean() )
                            {
                                person.EmailPreference = EmailPreference.DoNotEmail;
                            }
                        }
                    }

                    if ( personElem.Attribute( "photoUrl" ) != null )
                    {
                        person.Photo = SaveImage( personElem.Attribute( "photoUrl" ).Value.Trim(), _personImageBinaryFileType, _personImageBinaryFileTypeSettings, rockContext );
                    }

                    if ( personElem.Attribute( "recordType" ) == null || ( personElem.Attribute( "recordType" ) != null && personElem.Attribute( "recordType" ).Value.Trim() == "person" ) )
                    {
                        person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                    }
                    else
                    {
                        person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_BUSINESS.AsGuid() ).Id;
                    }

                    if ( personElem.Attribute( "maritalStatus" ) != null )
                    {
                        person.MaritalStatusValueId = GetOrAddDefinedValueId( personElem.Attribute( "maritalStatus" ).Value, _maritalStatusDefinedType );
                    }

                    if ( personElem.Attribute( "anniversaryDate" ) != null )
                    {
                        person.AnniversaryDate = DateTime.Parse( personElem.Attribute( "anniversaryDate" ).Value.Trim(), new CultureInfo( "en-US" ) );
                    }

                    switch ( personElem.Attribute( "recordStatus" ).Value.Trim() )
                    {
                        case "active":
                            person.RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id;
                            break;
                        case "inactive":
                            person.RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE.AsGuid() ).Id;
                            if ( personElem.Attribute( "recordStatusReason" ) != null )
                            {
                                person.RecordStatusReasonValueId = GetOrAddDefinedValueId( personElem.Attribute( "recordStatusReason" ).Value.Trim(), _recordStatusReasonDefinedType );
                                if ( person.RecordStatusReasonValueId == DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_REASON_DECEASED.AsGuid() ).Id )
                                {
                                    person.IsDeceased = true;
                                }
                            }

                            break;
                        default:
                            person.RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ).Id;
                            break;
                    }

                    if ( personElem.Attribute( "gender" ) != null )
                    {
                        switch ( personElem.Attribute( "gender" ).Value.Trim().ToLower() )
                        {
                            case "m":
                            case "male":
                                person.Gender = Gender.Male;
                                break;
                            case "f":
                            case "female":
                                person.Gender = Gender.Female;
                                break;
                            default:
                                person.Gender = Gender.Unknown;
                                break;
                        }
                    }
                    else
                    {
                        person.Gender = Gender.Unknown;
                    }

                    if ( personElem.Attribute( "connectionStatus" ) != null )
                    {
                        switch ( personElem.Attribute( "connectionStatus" ).Value.Trim().ToLower() )
                        {
                            case "member":
                                person.ConnectionStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_MEMBER.AsGuid() ).Id;
                                break;
                            case "attendee":
                                person.ConnectionStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_ATTENDEE.AsGuid() ).Id;
                                break;
                            case "web prospect":
                            case "prospect":
                                person.ConnectionStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_PROSPECT.AsGuid() ).Id;
                                break;
                            case "visitor":
                            default:
                                person.ConnectionStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_VISITOR.AsGuid() ).Id;
                                break;
                        }
                    }

                    if ( personElem.Attribute( "homePhone" ) != null && !string.IsNullOrEmpty( personElem.Attribute( "homePhone" ).Value.Trim() ) )
                    {
                        var phoneNumber = new PhoneNumber
                        {
                            NumberTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() ).Id,
                            Number = PhoneNumber.CleanNumber( personElem.Attribute( "homePhone" ).Value.Trim() ),
                            CountryCode = "1"
                        };

                        // Format number since default SaveChanges() is not being used.
                        phoneNumber.NumberFormatted = PhoneNumber.FormattedNumber( phoneNumber.CountryCode, phoneNumber.Number );

                        person.PhoneNumbers.Add( phoneNumber );
                    }

                    if ( personElem.Attribute( "mobilePhone" ) != null && !string.IsNullOrEmpty( personElem.Attribute( "mobilePhone" ).Value.Trim() ) )
                    {
                        var phoneNumber = new PhoneNumber
                        {
                            NumberTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Id,
                            Number = PhoneNumber.CleanNumber( personElem.Attribute( "mobilePhone" ).Value.Trim() )
                        };

                        // Format number since default SaveChanges() is not being used.
                        phoneNumber.NumberFormatted = PhoneNumber.FormattedNumber( phoneNumber.CountryCode, phoneNumber.Number );

                        person.PhoneNumbers.Add( phoneNumber );
                    }

                    if ( personElem.Attribute( "workPhone" ) != null && !string.IsNullOrEmpty( personElem.Attribute( "workPhone" ).Value.Trim() ) )
                    {
                        var phoneNumber = new PhoneNumber
                        {
                            NumberTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid() ).Id,
                            Number = PhoneNumber.CleanNumber( personElem.Attribute( "workPhone" ).Value.Trim() )
                        };

                        // Format number since default SaveChanges() is not being used.
                        phoneNumber.NumberFormatted = PhoneNumber.FormattedNumber( phoneNumber.CountryCode, phoneNumber.Number );

                        person.PhoneNumbers.Add( phoneNumber );
                    }

                    _personCache[guid] = person;
                }

                groupMember.Person = person;

                new Rock.Transactions.SaveMetaphoneTransaction( person ).Enqueue();

                if ( personElem.Attribute( "familyRole" ) != null && personElem.Attribute( "familyRole" ).Value.Trim().ToLower() == "adult" )
                {
                    groupMember.GroupRoleId = _adultRoleId;
                }
                else
                {
                    groupMember.GroupRoleId = _childRoleId;
                }

                // person attributes
                if ( personElem.Elements( "attributes" ).Any() )
                {
                    AddPersonAttributes( groupMember, personElem.Elements( "attributes" ), rockContext );
                }

                // person logins
                if ( personElem.Elements( "logins" ).Any() )
                {
                    // in here we are just going to store them in a dictionary for later
                    // saving because Rock requires that each person have a ID before
                    // we can call the UserLoginService.Create()
                    var logins = new List<string>();
                    foreach ( var login in personElem.Elements( "logins" ).Elements( "login" ) )
                    {
                        logins.Add( login.Attribute( "userName" ).Value );
                    }

                    _peopleLoginsDictionary.Add( groupMember.Person, logins );
                }

                familyMembers.Add( groupMember );
            }

            return familyMembers;
        }

        /// <summary>
        /// Gets or adds a new DefinedValue to the given DefinedTypeCache and returns the Id of the value.
        /// </summary>
        /// <param name="theValue">The value.</param>
        /// <param name="aDefinedType">a definedTypeCache.</param>
        /// <returns>
        /// the id of the defined value
        /// </returns>
        private int GetOrAddDefinedValueId( string theValue, DefinedTypeCache aDefinedType )
        {
            DefinedValueCache theDefinedValue = aDefinedType.DefinedValues.FirstOrDefault( a => string.Equals( a.Value, theValue, StringComparison.CurrentCultureIgnoreCase ) );

            // add it as new if we didn't find it.
            if ( theDefinedValue == null )
            {
                theDefinedValue = AddDefinedTypeValue( theValue, aDefinedType );
            }

            return theDefinedValue.Id;
        }

        /// <summary>
        /// Add a note on the given person's record.
        /// </summary>
        /// <param name="personId"></param>
        /// <param name="noteTypeName"></param>
        /// <param name="noteText"></param>
        /// <param name="noteDate">(optional) The date the note was created</param>
        /// <param name="byPersonGuid">(optional) The guid of the person who created the note</param>
        /// <param name="isPrivate"></param>
        /// <param name="isAlert"></param>
        /// <param name="rockContext"></param>
        private void AddNote( int personId, string noteTypeName, string noteText, string noteDate, string byPersonGuid, string isPrivate, string isAlert, RockContext rockContext )
        {
            var service = new NoteTypeService( rockContext );
            var noteType = service.Get( _personEntityTypeId, noteTypeName );

            if ( noteType != null )
            {
                // Find the person's alias
                int? createdByPersonAliasId = null;
                if ( byPersonGuid != null )
                {
                    createdByPersonAliasId = _personCache[byPersonGuid.AsGuid()].PrimaryAliasId;
                }

                var noteService = new NoteService( rockContext );
                var note = new Note()
                {
                    IsSystem = false,
                    NoteTypeId = noteType.Id,
                    EntityId = personId,
                    Caption = string.Empty,
                    CreatedByPersonAliasId = createdByPersonAliasId,
                    Text = noteText,
                    IsAlert = isAlert.AsBoolean(),
                    IsPrivateNote = isPrivate.AsBoolean(),
                    CreatedDateTime = string.IsNullOrWhiteSpace( noteDate ) ? RockDateTime.Now : DateTime.Parse( noteDate, new CultureInfo( "en-US" ) )
                };

                noteService.Add( note );
            }
        }

        /// <summary>
        /// Adds any logins stored in the _peopleLoginsDictionary.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        private void AddPersonLogins( RockContext rockContext )
        {
            var password = _args.Password.Trim();
            var userLoginService = new UserLoginService( rockContext );

            foreach ( var set in _peopleLoginsDictionary )
            {
                foreach ( var userName in set.Value )
                {
                    var userLogin = userLoginService.GetByUserName( userName );

                    // only create the login if the user name is not already taken
                    if ( userLogin == null )
                    {
                        UserLoginService.Create(
                                            rockContext,
                                            set.Key,
                                            Rock.Model.AuthenticationServiceType.Internal,
                                            _authenticationDatabaseEntityTypeId,
                                            userName,
                                            password,
                                            isConfirmed: true );
                    }
                }
            }
        }

        /// <summary>
        /// Adds the person attributes and values found in the XML to the person's attribute-value collection.
        /// </summary>
        /// <param name="groupMember"></param>
        /// <param name="attributes"></param>
        /// <param name="rockContext">The rock context.</param>
        private void AddPersonAttributes( GroupMember groupMember, IEnumerable<XElement> attributes, RockContext rockContext )
        {
            // In order to add attributes to the person, you have to first load them all
            groupMember.Person.LoadAttributes( rockContext );

            foreach ( var personAttribute in attributes.Elements( "attribute" ) )
            {
                // Add them to the list of people who need to have their attribute values saved.
                // This will be done after all the family groups have been saved.
                _personWithAttributes[groupMember.Person.Guid] = true;
                foreach ( var pa in personAttribute.Attributes() )
                {
                    groupMember.Person.SetAttributeValue( pa.Name.LocalName, pa.Value );
                }
            }
        }

        /// <summary>
        /// Fetches the given remote photoUrl and stores it locally in the binary file table
        /// then returns Id of the binary file.
        /// </summary>
        /// <param name="imageUrl"></param>
        /// <param name="binaryFileType"></param>
        /// <param name="binaryFileTypeSettings"></param>
        /// <param name="context"></param>
        /// <returns>Id of the binaryFile</returns>
        private BinaryFile SaveImage( string imageUrl, BinaryFileType binaryFileType, string binaryFileTypeSettings, RockContext context )
        {
            // always create a new BinaryFile record of IsTemporary when a file is uploaded
            BinaryFile binaryFile = new BinaryFile();
            binaryFile.IsTemporary = true;
            binaryFile.BinaryFileTypeId = binaryFileType.Id;
            binaryFile.FileName = Path.GetFileName( imageUrl );
            Stopwatch stopwatch = Stopwatch.StartNew();
            var webClient = new WebClient();
            webClient.Proxy = null;
            try
            {
                byte[] imageData = webClient.DownloadData( imageUrl );
                stopwatch.Stop();
                binaryFile.FileSize = imageData.Length;
                binaryFile.ContentStream = new MemoryStream( imageData );

                if ( webClient.ResponseHeaders != null )
                {
                    binaryFile.MimeType = webClient.ResponseHeaders["content-type"];
                }
                else
                {
                    switch ( Path.GetExtension( imageUrl ) )
                    {
                        case ".jpg":
                        case ".jpeg":
                            binaryFile.MimeType = "image/jpg";
                            break;
                        case ".png":
                            binaryFile.MimeType = "image/png";
                            break;
                        case ".gif":
                            binaryFile.MimeType = "image/gif";
                            break;
                        case ".bmp":
                            binaryFile.MimeType = "image/bmp";
                            break;
                        case ".tiff":
                            binaryFile.MimeType = "image/tiff";
                            break;
                        case ".svg":
                        case ".svgz":
                            binaryFile.MimeType = "image/svg+xml";
                            break;
                        default:
                            throw new NotSupportedException( string.Format( "unknown MimeType for {0}", imageUrl ) );
                    }
                }

                // Because pre-post processing is disabled for this rock context, need to
                // manually have the storage provider save the contents of the binary file
                binaryFile.SetStorageEntityTypeId( binaryFileType.StorageEntityTypeId );
                binaryFile.StorageEntitySettings = binaryFileTypeSettings;
                if ( binaryFile.StorageProvider != null )
                {
                    binaryFile.StorageProvider.SaveContent( binaryFile );
                    binaryFile.Path = binaryFile.StorageProvider.GetPath( binaryFile );
                }

                var binaryFileService = new BinaryFileService( context );
                binaryFileService.Add( binaryFile );
                return binaryFile;
            }
            catch ( WebException )
            {
                return null;
            }
        }

        /// <summary>
        /// Adds the given addresses in the xml snippet to the given family.
        /// </summary>
        /// <param name="groupService">The group service.</param>
        /// <param name="family">The family.</param>
        /// <param name="addresses">The addresses.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        private void AddFamilyAddresses( GroupService groupService, Group family, XElement addresses, RockContext rockContext )
        {
            if ( addresses == null || addresses.Elements( "address" ) == null )
            {
                return;
            }

            // First add each person to the familyMembers collection
            foreach ( var addressElem in addresses.Elements( "address" ) )
            {
                var addressType = ( addressElem.Attribute( "type" ) != null ) ? addressElem.Attribute( "type" ).Value.Trim() : string.Empty;
                var street1 = ( addressElem.Attribute( "street1" ) != null ) ? addressElem.Attribute( "street1" ).Value.Trim() : string.Empty;
                var street2 = ( addressElem.Attribute( "street2" ) != null ) ? addressElem.Attribute( "street2" ).Value.Trim() : string.Empty;
                var city = ( addressElem.Attribute( "city" ) != null ) ? addressElem.Attribute( "city" ).Value.Trim() : string.Empty;
                var state = ( addressElem.Attribute( "state" ) != null ) ? addressElem.Attribute( "state" ).Value.Trim() : string.Empty;
                var postalCode = ( addressElem.Attribute( "postalCode" ) != null ) ? addressElem.Attribute( "postalCode" ).Value.Trim() : string.Empty;
                var country = ( addressElem.Attribute( "country" ) != null ) ? addressElem.Attribute( "country" ).Value.Trim() : "US";
                var lat = ( addressElem.Attribute( "lat" ) != null ) ? addressElem.Attribute( "lat" ).Value.Trim() : string.Empty;
                var lng = ( addressElem.Attribute( "long" ) != null ) ? addressElem.Attribute( "long" ).Value.Trim() : string.Empty;

                string locationTypeGuid;

                switch ( addressType )
                {
                    case "home":
                        locationTypeGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME;
                        break;
                    case "work":
                        locationTypeGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK;
                        break;
                    case "previous":
                        locationTypeGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS;
                        break;
                    default:
                        throw new NotSupportedException( string.Format( "unknown addressType: {0}", addressType ) );
                }

                // Call replica of the groupService's AddNewFamilyAddress in an attempt to speed it up
                AddNewFamilyAddress( family, locationTypeGuid, street1, street2, city, state, postalCode, country, rockContext );

                var location = family.GroupLocations.Where( gl => gl.Location.Street1 == street1 ).Select( gl => gl.Location ).FirstOrDefault();

                // Set the address with the given latitude and longitude
                double latitude;
                double longitude;
                if ( !string.IsNullOrEmpty( lat ) && !string.IsNullOrEmpty( lng )
                    && double.TryParse( lat, out latitude ) && double.TryParse( lng, out longitude )
                    && location != null )
                {
                    location.SetLocationPointFromLatLong( latitude, longitude );
                }

                // Put the location id into the dictionary for later use.
                if ( location != null && !_familyLocationDictionary.ContainsKey( family.Guid ) )
                {
                    _familyLocationDictionary.Add( family.Guid, location.Id );
                }
            }
        }

        /// <summary>
        /// Creates a new family using the given set of people.
        /// </summary>
        /// <param name="familyMembers">The family members.</param>
        /// <param name="campusId">The campus identifier.</param>
        /// <returns></returns>
        public Group CreateNewFamily( List<GroupMember> familyMembers, int? campusId )
        {
            var familyGroupType = GroupTypeCache.GetFamilyGroupType();

            if ( familyGroupType != null )
            {
                var familyGroup = new Group();
                familyGroup.GroupTypeId = familyGroupType.Id;
                familyGroup.Name = familyMembers.FirstOrDefault().Person.LastName + " Family";
                familyGroup.CampusId = campusId;

                foreach ( var familyMember in familyMembers )
                {
                    var person = familyMember.Person;
                    if ( person != null )
                    {
                        familyGroup.Members.Add( familyMember );
                    }
                }

                foreach ( var groupMember in familyMembers )
                {
                    var person = groupMember.Person;

                    if ( groupMember.GroupRoleId != _childRoleId )
                    {
                        person.GivingGroup = familyGroup;
                    }
                }

                return familyGroup;
            }

            return null;
        }

        /// <summary>
        /// Adds the new family address.
        /// </summary>
        /// <param name="family">The family.</param>
        /// <param name="locationTypeGuid">The location type unique identifier.</param>
        /// <param name="street1">The street1.</param>
        /// <param name="street2">The street2.</param>
        /// <param name="city">The city.</param>
        /// <param name="state">The state.</param>
        /// <param name="postalCode">The zip.</param>
        /// <param name="country">The country.</param>
        /// <param name="rockContext">The rock context.</param>
        public void AddNewFamilyAddress( Group family, string locationTypeGuid, string street1, string street2, string city, string state, string postalCode, string country, RockContext rockContext )
        {
            if ( !string.IsNullOrWhiteSpace( street1 ) ||
                 !string.IsNullOrWhiteSpace( street2 ) ||
                 !string.IsNullOrWhiteSpace( city ) ||
                 !string.IsNullOrWhiteSpace( postalCode ) ||
                 !string.IsNullOrWhiteSpace( country ) )
            {
                var groupLocation = new GroupLocation();

                // Get new or existing location and associate it with group
                var location = new LocationService( rockContext ).Get( street1, street2, city, state, postalCode, country );
                groupLocation.Location = location;
                groupLocation.IsMailingLocation = true;
                groupLocation.IsMappedLocation = true;

                Guid guid = Guid.Empty;
                if ( Guid.TryParse( locationTypeGuid, out guid ) )
                {
                    var locationType = DefinedValueCache.Get( guid );
                    if ( locationType != null )
                    {
                        groupLocation.GroupLocationTypeValueId = locationType.Id;
                    }
                }

                family.GroupLocations.Add( groupLocation );
            }
        }
        private RockContext GetConfiguredDataContext()
        {
            var rockContext = new RockContext();

            // Set the timeout to 30 mins, to allow processing of very large datasets.
            rockContext.Database.CommandTimeout = 1800;

            return rockContext;
        }

        private void AddFinancialGateways( XElement financialGateways, RockContext rockContext )
        {
            if ( financialGateways == null )
            {
                return;
            }

            var financialGatewayService = new FinancialGatewayService( rockContext );
            foreach ( var element in financialGateways.Elements( "financialGateway" ) )
            {
                var guidAttribute = element.Attribute( "guid" );
                if ( guidAttribute == null )
                {
                    continue;
                }

                var guid = guidAttribute.Value.Trim().AsGuid();

                if ( financialGatewayService.Queryable().Any( f => f.Guid == guid ) )
                {
                    continue;
                }

                var entityTypeName = element.Attribute( "entityTypeName" ).Value.Trim();
                var gatewayComponent = Rock.Financial.GatewayContainer.GetComponent( entityTypeName );

                var financialGateway = new FinancialGateway
                {
                    Name = element.Attribute( "name" ).Value.Trim(),
                    Guid = guid,
                    IsActive = GetBooleanValueSafe( element, "isActive" ),
                    EntityTypeId = gatewayComponent.EntityType.Id
                };

                financialGatewayService.Add( financialGateway );
                rockContext.SaveChanges();
            }
        }

        #endregion Methods

        #region Helper Classes


        /// <summary>
        /// A set of configuration parameters for the import action.
        /// </summary>
        public class SampleDataImportActionArgs
        {
            /// <summary>
            /// The password for new logins.
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// A flag specifying if attendance data will be added using settings in the import document.
            /// </summary>
            public bool FabricateAttendance { get; set; } = false;

            /// <summary>
            /// A flag specifying if everything in the import file should be ignored other than giving data.
            /// </summary>
            public bool ProcessOnlyGivingData { get; set; } = false;

            /// <summary>
            /// A flag specifying if giving data should be imported; if set to false, giving data will be ignored.",
            /// </summary>
            public bool EnableGiving { get; set; } = false;

            /// <summary>
            /// A flag specifying if existing data should be removed before the new data is imported.
            /// </summary>
            public bool DeleteExistingData { get; set; } = false;

            /// <summary>
            /// A flag specifying if logging will be added to time each of the major operations.
            /// </summary>
            public bool EnableStopwatch { get; set; } = false;

            /// <summary>
            /// Sets the seed value to use for randomizing some sample data.
            /// If set to a non-zero value, the randomizer used during the creation of attendance and financial transactions will produce a repeatable sequence.
            /// </summary>
            public int? RandomizerSeed = null;

            /// <summary>
            /// The default Lava template for registration confirmation emails.
            /// </summary>
            public string RegistrationConfirmationEmailTemplate { get; set; } = null;

            /// <summary>
            /// The default Lava template for registration reminder emails.
            /// </summary>
            public string RegistrationReminderEmailTemplate { get; set; } = null;

            /// <summary>
            /// The default Lava template for the registration success message.
            /// </summary>
            public string RegistrationSuccessText { get; set; } = null;

            /// <summary>
            /// The default Lava template for the payment reminder message.
            /// </summary>
            public string RegistrationPaymentReminderTemplate { get; set; } = null;

            /// <summary>
            /// The alias identifier of the person who is deemed the creator of the sample data.
            /// </summary>
            public int? CreatorPersonAliasId { get; set; }

            /// <summary>
            /// The date and time to use for generated attendance codes.
            /// </summary>
            public DateTime? AttendanceCodeIssuedDateTime { get; set; }
        }

        /// <summary>
        /// Stores data for internal processing
        /// </summary>
        private class ClassGroupLocation
        {
            public string Name { get; set; }

            public int GroupId { get; set; }

            public int LocationId { get; set; }

            public double MinAge { get; set; }

            public double MaxAge { get; set; }
        }

        /// <summary>
        /// Indicates Giving frequency.
        /// </summary>
        private enum Frequency
        {
            onetime = 0,
            weekly = 1,
            monthly = 2
        }

        #endregion Helper Classes
    }
}