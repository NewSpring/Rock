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

using Rock.CheckIn.v2;
using Rock.Data;
using Rock.Web.Cache;

namespace Rock.Model
{
    public partial class Device
    {
        internal class SaveHook : EntitySaveHook<Device>
        {
            private static readonly Lazy<int> KioskDeviceTypeValueId = new Lazy<int>( () =>
            {
                return DefinedValueCache.Get( SystemGuid.DefinedValue.DEVICE_TYPE_CHECKIN_KIOSK )?.Id ?? 0;
            } );

            /// <inheritdoc/>
            protected override void PostSave()
            {
                if ( PreSaveState == EntityContextState.Modified || PreSaveState == EntityContextState.Deleted )
                {
                    if ( Entity.DeviceTypeValueId == KioskDeviceTypeValueId.Value )
                    {
                        CheckInDirector.SendRefreshKioskConfiguration();
                    }
                }

                base.PostSave();
            }
        }
    }
}
