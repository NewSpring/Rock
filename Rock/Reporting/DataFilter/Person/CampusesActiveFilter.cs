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
using System.ComponentModel.Composition;

using Rock.Data;
using Rock.Net;

using Rock.ViewModels.Utility;

using Rock.Web.Cache;

namespace Rock.Reporting.DataFilter.Person
{
    /// <summary>
    /// 
    /// </summary>
    [Description( "Filter people that are associated with any of the selected active campuses." )]
    [Export( typeof( DataFilterComponent ) )]
    [ExportMetadata( "ComponentName", "Person Active Campuses Filter" )]
    [Rock.SystemGuid.EntityTypeGuid( "8734B837-D689-4EE9-97FB-91701061897C" )]
    public class CampusesActiveFilter : CampusesFilter
    {
        #region Properties

        /// <summary>
        /// Gets the control class name.
        /// </summary>
        /// <value>
        /// The name of the control class.
        /// </value>
        internal override string ControlClassName
        {
            get { return "js-campuses-active-picker"; }
        }

        /// <summary>
        /// Gets a value indicating whether to include inactive campuses.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [include inactive]; otherwise, <c>false</c>.
        /// </value>
        internal override bool IncludeInactive
        {
            get { return false; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <value>
        /// The title.
        /// </value>
        public override string GetTitle( Type entityType )
        {
            return "Campuses (Active Only)";
        }

        #endregion

        /// <inheritdoc/>
        public override Dictionary<string, string> GetObsidianComponentData( Type entityType, string selection, RockContext rockContext, RockRequestContext requestContext )
        {
            var result = new Dictionary<string, string>();

            result.AddOrReplace( "multiple", "True" );
            result.AddOrReplace( "label", CampusPickerLabel );
            result.AddOrReplace( "includeInactive", IncludeInactive.ToTrueFalse() );

            if ( selection.IsNotNullOrWhiteSpace() )
            {
                var selectionValues = selection.Split( '|' );
                var campuses = new List<ListItemBag>();
                if ( selectionValues.Length >= 1 )
                {
                    var campusGuidList = selectionValues[0].Split( ',' ).AsGuidList();
                    foreach ( var campusGuid in campusGuidList )
                    {
                        var campus = CampusCache.Get( campusGuid );
                        if ( campus != null )
                        {
                            campuses.Add( new ListItemBag { Text = campus.Name, Value = campus.Guid.ToString() } );
                        }
                    }
                }

                result.AddOrReplace( "campus", campuses.ToCamelCaseJson( false, true ) );
            }

            return result;
        }
    }
}