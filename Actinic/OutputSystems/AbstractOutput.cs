//
//  AbstractOutput.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2015 - 2016
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;

// Output systems (transitioning legacy to modern)
using Actinic.Output;

// Rendering
using Actinic.Rendering;

namespace Actinic.Outputs
{
	public abstract class AbstractOutput : IComparable <AbstractOutput>
	{

		/// <summary>
		/// Gets a value indicating whether the output system is connected
		/// and set up for use.
		/// </summary>
		/// <value><c>true</c> if output system is set up; otherwise, <c>false</c>.</value>
		public abstract bool Initialized {
			get;
		}

		/// <summary>
		/// Gets the identifier for which output system is connected.
		/// </summary>
		/// <value>Output system connection identifier.</value>
		public abstract string Identifier {
			get;
		}

		/// <summary>
		/// Gets the version identifier for chosen output system.
		/// </summary>
		/// <value>Output system version identifier.</value>
		public abstract string VersionIdentifier {
			get;
		}

		/// <summary>
		/// Gets the priority of this output system, with lower numbers being higher priority.
		/// </summary>
		/// <value>The priority.</value>
		public abstract int Priority {
			get;
		}

		/// <summary>
		/// Gets the device's configuration details, e.g. number of lights and
		/// length of string.
		/// </summary>
		/// <value>The device configuration configuration.</value>
		public abstract ReadOnlyDeviceConfiguration Configuration {
			get;
		}

		public delegate void SystemDataReceivedHandler (object sender,EventArgs e);

		/// <summary>
		/// Occurs when the output system provides data.
		/// </summary>
		public event SystemDataReceivedHandler SystemDataReceived;

		/// <summary>
		/// When true, system is presently resetting
		/// </summary>
		protected bool ResettingSystem;

		public AbstractOutput ()
		{
		}

		/// <summary>
		/// Initializes the output system.
		/// </summary>
		/// <returns><c>true</c>, if system was successfully initialized, <c>false</c> otherwise.</returns>
		public abstract bool InitializeSystem ();

		/// <summary>
		/// Shutdowns the output system.
		/// </summary>
		/// <returns><c>true</c>, if system was successfully shutdown, <c>false</c> otherwise.</returns>
		public abstract bool ShutdownSystem ();

		/// <summary>
		/// Shutdown then re-initialize the output system.
		/// </summary>
		/// <returns><c>true</c>, if system was successfully reset, <c>false</c> otherwise.</returns>
		public bool ResetSystem ()
		{
			bool result;
			ResettingSystem = true;
			result = (ShutdownSystem () && InitializeSystem ());
			ResettingSystem = false;
			return result;
		}

		/// <summary>
		/// Verifies the given LED light set provides the right number of lights, etc
		/// <exception cref="ArgumentException">Thrown when provided with an invalid light set</exception>
		/// </summary>
		/// <param name="Actinic_Light_Set">List of LEDs to verify</param>
		protected void ValidateLightSet (Layer Actinic_Light_Set)
		{
			if (Actinic_Light_Set.PixelCount != Configuration.LightCount)
				throw new ArgumentException (
					"Actinic_Light_Set",
					string.Format (
						"Given Actinic_Light_Set with {0} lights, needed {1}",
						Actinic_Light_Set.PixelCount, Configuration.LightCount
					)
				);
		}

		/// <summary>
		/// Updates the brightness of the lights.
		/// </summary>
		/// <returns><c>true</c>, if brightness was updated, <c>false</c> otherwise.</returns>
		/// <param name="Actinic_Light_Set">LED list representing desired state of lights, ignoring the color component.</param>
		/// <param name="ProcessingOverhead">Any time spent processing before calling this function, used to provide consistent timing.</param>
		public abstract bool UpdateLightsBrightness (
			Layer Actinic_Light_Set, double ProcessingOverhead = 0);

		/// <summary>
		/// Updates the color of the lights.
		/// </summary>
		/// <returns><c>true</c>, if color was updated, <c>false</c> otherwise.</returns>
		/// <param name="Actinic_Light_Set">LED list representing desired state of lights, ignoring the brightness components.</param>
		/// <param name="ProcessingOverhead">Any time spent processing before calling this function, used to provide consistent timing.</param>
		public abstract bool UpdateLightsColor (
			Layer Actinic_Light_Set, double ProcessingOverhead = 0);

		/// <summary>
		/// Updates both color and brightness of the lights.
		/// </summary>
		/// <returns><c>true</c>, if color and brightness was updated, <c>false</c> otherwise.</returns>
		/// <param name="Actinic_Light_Set">LED list representing desired state of lights.</param>
		/// <param name="ProcessingOverhead">Any time spent processing before calling this function, used to provide consistent timing.</param>
		public abstract bool UpdateLightsAll (
			Layer Actinic_Light_Set, double ProcessingOverhead = 0);

		/// <summary>
		/// Raises the system data received event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Event arguments.</param>
		protected void OnSystemDataReceived (object sender, EventArgs e)
		{
			SystemDataReceivedHandler handler = SystemDataReceived;
			if (handler != null)
				handler (this, e);
		}

		/// <summary>
		/// Returns the sort order of this output system compared with another.
		/// </summary>
		/// <returns>The sort order, positive preceding, negative following, zero equals.</returns>
		/// <param name="obj">The AbstractOutput to compare to this instance.</param>
		public int CompareTo (AbstractOutput otherOutput)
		{
			if (otherOutput == null)
				return 1;

			return this.Priority.CompareTo (otherOutput.Priority);
		}
	}
}

